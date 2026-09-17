using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Oficina.Tests.Architeture;

/// <summary>
/// Testes de arquitetura baseados em reflection sobre os assemblies COMPILADOS
/// (não em análise de código-fonte). Para isso funcionar, os projetos de teste
/// precisam referenciar Oficina.Domain, Oficina.Application, Oficina.Infrastructure
/// e Oficina.Api (o projeto Web/Api), para que os assemblies estejam carregados
/// no AppDomain e possam ser localizados por nome.
///
/// Ajuste os nomes dos assemblies abaixo se forem diferentes dos nomes dos projetos.
/// </summary>
public class ArchitectureTests
{
    private const string DomainAssemblyName = "Oficina.Domain";
    private const string ApplicationAssemblyName = "Oficina.Application";
    private const string InfrastructureAssemblyName = "Oficina.Infrastructure";
    private const string ApiAssemblyName = "Oficina.Api";
    private const string ControllersNamespace = "Oficina.Api.Controllers";

    // --------------------------------------------------------------
    // Helpers
    // --------------------------------------------------------------

    /// <summary>
    /// Carrega um assembly pelo nome simples, procurando primeiro nos assemblies
    /// já carregados no AppDomain atual (é o caso normal quando o projeto de teste
    /// referencia os demais projetos). Faz fallback para Assembly.Load caso o
    /// assembly ainda não tenha sido carregado (ex.: referenciado apenas
    /// transitivamente e ainda não tocado pelo JIT).
    /// </summary>
    private static Assembly GetAssembly(string assemblyName)
    {
        var loaded = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == assemblyName);

        if (loaded != null)
            return loaded;

        try
        {
            return Assembly.Load(new AssemblyName(assemblyName));
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Não foi possível carregar o assembly '{assemblyName}'. " +
                "Verifique se o projeto de testes referencia (direta ou " +
                "transitivamente) esse assembly.", ex);
        }
    }

    /// <summary>
    /// Retorna os nomes simples de todos os assemblies referenciados diretamente
    /// pelo assembly informado (metadata de referência, é exatamente o que sai do
    /// compilador — não inclui referências apenas usadas via reflection dinâmica).
    /// </summary>
    private static IReadOnlyCollection<string> GetReferencedAssemblyNames(Assembly assembly)
    {
        return assembly.GetReferencedAssemblies()
            .Select(an => an.Name!)
            .ToList();
    }

    // --------------------------------------------------------------
    // Regra 1: Domain não pode depender de Application nem de Infrastructure
    // --------------------------------------------------------------

    [Fact]
    public void Domain_NaoDeveReferenciar_Application()
    {
        var domain = GetAssembly(DomainAssemblyName);
        var referenced = GetReferencedAssemblyNames(domain);

        Assert.False(
            referenced.Contains(ApplicationAssemblyName),
            $"'{DomainAssemblyName}' não pode referenciar '{ApplicationAssemblyName}'. " +
            $"Referências encontradas: {string.Join(", ", referenced)}");
    }

    [Fact]
    public void Domain_NaoDeveReferenciar_Infrastructure()
    {
        var domain = GetAssembly(DomainAssemblyName);
        var referenced = GetReferencedAssemblyNames(domain);

        Assert.False(
            referenced.Contains(InfrastructureAssemblyName),
            $"'{DomainAssemblyName}' não pode referenciar '{InfrastructureAssemblyName}'. " +
            $"Referências encontradas: {string.Join(", ", referenced)}");
    }

    // --------------------------------------------------------------
    // Regra 2: Application não pode depender de Infrastructure
    // --------------------------------------------------------------

    [Fact]
    public void Application_NaoDeveReferenciar_Infrastructure()
    {
        var application = GetAssembly(ApplicationAssemblyName);
        var referenced = GetReferencedAssemblyNames(application);

        Assert.False(
            referenced.Contains(InfrastructureAssemblyName),
            $"'{ApplicationAssemblyName}' não pode referenciar '{InfrastructureAssemblyName}'. " +
            $"Referências encontradas: {string.Join(", ", referenced)}");
    }

    // --------------------------------------------------------------
    // Regra 3: Tipos em Oficina.Api.Controllers não podem referenciar
    // namespaces de Oficina.Infrastructure, exceto Program.cs (composition root)
    // Program (top-level statements ou classe explícita) é o composition root e fica fora da regra.
    // --------------------------------------------------------------

    [Fact]
    public void Controllers_NaoDevemReferenciar_Infrastructure()
    {
        var apiAssembly = GetAssembly(ApiAssemblyName);

        var controllerTypes = apiAssembly.GetTypes()
            .Where(t => t.Namespace != null
                        && (t.Namespace == ControllersNamespace
                            || t.Namespace!.StartsWith(ControllersNamespace + ".")))
            .Where(t => t.Name != "Program")
            .ToList();

        var violacoes = new List<string>();

        foreach (var type in controllerTypes)
        {
            foreach (var usedType in GetTiposUsadosDiretamente(type))
            {
                if (EstaEmNamespaceInfrastructure(usedType))
                {
                    violacoes.Add(
                        $"{type.FullName} referencia {usedType.FullName} " +
                        $"(namespace '{usedType.Namespace}')");
                }
            }
        }

        Assert.True(
            violacoes.Count == 0,
            "Controllers não podem referenciar tipos de Oficina.Infrastructure:\n" +
            string.Join("\n", violacoes.Distinct()));
    }

    private static bool EstaEmNamespaceInfrastructure(Type type)
    {
        return type.Namespace != null
               && (type.Namespace == InfrastructureAssemblyName
                   || type.Namespace!.StartsWith(InfrastructureAssemblyName + "."));
    }

    /// <summary>
    /// Coleta, via reflection, os tipos referenciados "estaticamente" (nível de
    /// metadata) por um tipo: classe base, interfaces implementadas, tipos de
    /// campos, tipos de propriedades, tipos de parâmetros e retorno de métodos e
    /// construtores (incluindo argumentos de tipos genéricos).
    ///
    /// LIMITAÇÃO IMPORTANTE: reflection sobre metadata de membros NÃO enxerga
    /// tipos usados apenas dentro do CORPO dos métodos (variáveis locais, `new`,
    /// casts, chamadas a métodos estáticos de outro tipo etc. sem que esse tipo
    /// apareça em um campo/propriedade/assinatura). Para cobrir 100% dos casos
    /// (incluindo corpo de método) seria necessário inspecionar o IL, o que exige
    /// uma biblioteca de leitura de IL/metadata (ex.: Mono.Cecil) — reflection
    /// pura do BCL não expõe isso. Se precisar dessa cobertura completa, posso
    /// adaptar esse teste para usar Mono.Cecil.
    /// </summary>
    private static IEnumerable<Type> GetTiposUsadosDiretamente(Type type)
    {
        var tipos = new List<Type>();

        if (type.BaseType != null)
            tipos.Add(type.BaseType);

        tipos.AddRange(type.GetInterfaces());

        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic
                                    | BindingFlags.Instance | BindingFlags.Static
                                    | BindingFlags.DeclaredOnly;

        foreach (var field in type.GetFields(flags))
            tipos.Add(field.FieldType);

        foreach (var prop in type.GetProperties(flags))
            tipos.Add(prop.PropertyType);

        foreach (var ctor in type.GetConstructors(flags))
            foreach (var p in ctor.GetParameters())
                tipos.Add(p.ParameterType);

        foreach (var method in type.GetMethods(flags))
        {
            tipos.Add(method.ReturnType);
            foreach (var p in method.GetParameters())
                tipos.Add(p.ParameterType);
        }

        // Expande tipos genéricos (ex.: IEnumerable<AlgumTipoDeInfra>)
        var expandido = new List<Type>();
        foreach (var t in tipos)
        {
            expandido.Add(t);
            if (t.IsGenericType)
                expandido.AddRange(t.GetGenericArguments());
        }

        return expandido.Distinct();
    }
}