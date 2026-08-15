# Relatório de vulnerabilidades

- Ferramenta: `dotnet list package --vulnerable --include-transitive`
- Data: 2026-08-11
- Escopo: todos os projetos e dependências NuGet da solução.
- Resultado: preencher com a saída reproduzível do comando antes da entrega em ambiente com acesso ao advisory feed do NuGet.
- Tratamento: vulnerabilidades altas/críticas bloqueiam a entrega; atualizar dependência direta ou framework, repetir testes e o scan. Falso positivo ou risco aceito exige justificativa, responsável e prazo neste arquivo.

Nenhum segredo real é versionado. As credenciais do compose são exclusivamente locais e devem ser substituídas por secret manager no deploy.
