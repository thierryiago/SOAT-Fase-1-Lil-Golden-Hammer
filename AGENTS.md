# AGENTS.md

Este arquivo orienta pessoas e agentes automatizados que alterem este repositório. As regras valem para toda a árvore do projeto, salvo quando um `AGENTS.md` mais específico existir em um subdiretório.

## Visão geral do sistema

A **Oficina API** é uma API REST para a operação de uma oficina mecânica. O sistema gerencia clientes, veículos, peças, estoque, mecânicos, catálogo de serviços, ordens de serviço, orçamentos, histórico, métricas e notificações por e-mail.

Stack principal:

- .NET 10, C# e ASP.NET Core;
- Entity Framework Core com PostgreSQL/Npgsql;
- autenticação JWT Bearer;
- Swagger/OpenAPI;
- xUnit e `WebApplicationFactory`;
- Docker Compose com API, PostgreSQL e Mailpit.

## Estrutura do repositório

```text
.
├── src/
│   ├── Oficina.Domain/          # Entidades, enums, invariantes e regras de negócio
│   ├── Oficina.Application/     # Casos de uso, DTOs e contratos (ports)
│   ├── Oficina.Infrastructure/  # EF Core, repositórios, PostgreSQL e SMTP
│   └── Oficina.Api/             # Controllers, JWT, OpenAPI e composition root
├── tests/
│   ├── Oficina.Tests/           # Testes de domínio, aplicação, infraestrutura e arquitetura
│   └── Oficina.Api.ContractTests/ # Testes HTTP/contrato com WebApplicationFactory
├── docs/                        # Contexto, diagramas e fluxos do sistema
├── draft/                       # Rascunhos; não é fonte normativa
├── Oficina.sln
├── docker-compose.yml
├── Dockerfile
├── oficina.http                 # Exemplos de chamadas e fluxo manual da API
└── seed-data.sql
```

Documentos de referência: `README.md`, `docs/SYSTEM_CONTEXT.md`, `docs/fluxo-ordem-servico.md`, `docs/contexto-aplicacao.md` e `docs/diagrama-classes-banco.md`. Em caso de divergência, o comportamento coberto por código e testes prevalece; atualize a documentação junto com a mudança.

## Arquitetura e direção das dependências

O projeto está em processo de migração de uma arquitetura tradicional em camadas para **Clean Architecture**. A estrutura atual já aplica parte desse modelo, mas ainda pode conter código legado que não representa a arquitetura de destino. Toda implementação nova deve seguir a Clean Architecture e toda alteração em código existente deve, quando viável e dentro do escopo da tarefa, aproximá-lo gradualmente dessa arquitetura.

A direção de dependências desejada é:

```text
Api ────────> Application ────────> Domain
 │                                      ▲
 └────────> Infrastructure ─────────────┘
                    │
                    └──────> Application
```

Respeite estas regras:

- `Domain` não depende de `Application`, `Infrastructure` ou `Api`.
- `Application` depende apenas de `Domain` e de abstrações da plataforma; não referencia `Infrastructure`.
- Interfaces de repositório e integrações externas pertencem a `Application`; implementações pertencem a `Infrastructure`.
- Controllers dependem de serviços e DTOs de `Application`, nunca de repositórios, `DbContext` ou tipos concretos de `Infrastructure`.
- `Program.cs` é o composition root e pode conectar `Api`, `Application` e `Infrastructure`.
- Registre serviços nos arquivos `DependencyInjection.cs` das respectivas camadas.
- Os testes em `tests/Oficina.Tests/Architeture` protegem parte dessas regras e devem continuar passando.
- Não use inconsistências legadas como modelo para código novo. Consulte as regras deste arquivo e os módulos já aderentes à arquitetura de destino.
- Faça a migração de maneira incremental: não promova reestruturações amplas e sem relação com a tarefa, mas também não introduza novas violações arquiteturais.
- Quando uma mudança revelar uma violação que não possa ser corrigida com segurança no mesmo escopo, preserve o comportamento, evite aumentar o acoplamento e registre claramente a pendência.

## Responsabilidades por camada

### Domain

- Mantenha invariantes dentro das entidades e métodos de domínio.
- Prefira métodos que expressem ações do negócio a setters públicos.
- Não introduza DTOs HTTP, EF Core, acesso a banco, SMTP ou detalhes de autenticação.
- Preserve a linguagem do negócio já usada pelo projeto, mesmo quando namespaces legados misturam português e inglês.

### Application

- Orquestre casos de uso, transações lógicas e chamadas aos contratos externos.
- Use DTOs de entrada e saída; não exponha entidades diretamente pela API.
- Propague `CancellationToken` em todo fluxo assíncrono.
- Use `ConflictException` para conflitos, `KeyNotFoundException` para recurso ausente e exceções de argumento/operação para entradas ou transições inválidas, mantendo o mapeamento HTTP definido em `Program.cs`.
- Evite lógica de negócio nos mapeamentos de DTOs.

### Infrastructure

- Centralize mapeamentos EF Core em `AppDbContext` e persistência nos repositórios.
- Use consultas assíncronas e encaminhe o `CancellationToken`.
- Não vaze tipos do EF Core para `Application` ou `Domain`.
- Mudanças persistentes de modelo exigem migration gerada pelo EF Core.
- Nunca edite manualmente arquivos `*.Designer.cs` nem `AppDbContextModelSnapshot.cs`.

### Api

- Controllers devem ser finos: validar o contrato HTTP, chamar o caso de uso e formar a resposta.
- Mantenha o prefixo versionado `/api/v1` e documente respostas relevantes com `ProducesResponseType`.
- Preserve a política de autenticação existente; qualquer endpoint anônimo novo deve ser decisão explícita e coberta por teste de contrato.
- Não retorne detalhes internos ou stack traces. Erros inesperados devem continuar sem `detail` público.
- Ao alterar rotas ou modelos públicos, atualize Swagger, testes de contrato, `README.md` e `oficina.http` quando aplicável.

## Regras de negócio sensíveis

- O fluxo principal da ordem de serviço é `Created -> Received -> InDiagnosis -> AwaitingApproval -> InExecution -> Finalized -> Delivered`; rejeição/cancelamento e transições alternativas devem obedecer às regras da entidade e do serviço de aplicação.
- Toda mudança de status relevante deve manter o histórico da ordem.
- A entrada em `AwaitingApproval` cria uma nova versão de orçamento. Versões anteriores são registros históricos e não devem ser sobrescritas.
- Alterações de peças e serviços durante a execução podem exigir retorno para aprovação. Preserve a consistência entre ordem, orçamento, histórico, estoque e notificação.
- Movimentações de estoque nunca podem resultar em quantidade negativa. Consumo e devolução devem considerar deltas e ser idempotentes quando a mesma composição for reenviada.
- Exclusões de cadastros são, em geral, lógicas por `IsActive`; não substitua por exclusão física sem requisito explícito e análise das referências.
- Documentos e placas possuem normalização e validação no domínio. Verifique duplicidade usando a forma normalizada.
- Valores monetários usam `decimal`; não use `float` ou `double` para preços e totais.
- Prefira `TimeProvider` ou o padrão de tempo já injetado a chamadas diretas espalhadas a `DateTime.Now`/`UtcNow`.

## Boas práticas de implementação

- Siga os princípios de **Clean Code** em toda implementação e refatoração.
- Use nomes claros, precisos e orientados ao domínio; evite abreviações obscuras e nomes genéricos como `data`, `item`, `manager` ou `helper` quando houver um conceito mais específico.
- Mantenha classes e métodos com responsabilidade única, baixo acoplamento e alta coesão.
- Prefira código simples e explícito. Aplique DRY, KISS, YAGNI e princípios SOLID com bom senso, sem criar abstrações prematuras.
- Evite parâmetros booleanos que alterem radicalmente o comportamento de um método; prefira operações com intenção explícita.
- Limite efeitos colaterais, mantenha dependências visíveis e injete colaborações externas em vez de instanciá-las dentro das regras de negócio.
- Comentários devem explicar decisões, restrições ou contexto não evidente; o código deve explicar o funcionamento por meio de nomes e estrutura.
- Mantenha `Nullable` habilitado e corrija avisos de nulabilidade; não os silencie sem justificativa.
- Siga as convenções C#: tipos e membros públicos em `PascalCase`, variáveis e parâmetros em `camelCase`, interfaces iniciadas por `I`.
- Use `async`/`await` até o acesso a I/O; não bloqueie tarefas com `.Result`, `.Wait()` ou `GetAwaiter().GetResult()`.
- Faça validação defensiva nas fronteiras e preserve invariantes no domínio.
- Prefira funções pequenas, nomes orientados ao negócio e retornos antecipados a aninhamentos profundos.
- Evite duplicação, abstrações especulativas, comentários que apenas repetem o código e código morto comentado.
- Não faça refatorações amplas ou renomeações fora do escopo da tarefa.
- Não inclua segredos, tokens, senhas reais ou dados pessoais. Use `.env.example`, variáveis de ambiente e valores fictícios.
- Registre logs estruturados sem dados sensíveis; não concatene segredos ou documentos pessoais em mensagens.
- Preserve compatibilidade de API, banco e dados existentes, salvo quando a mudança incompatível for explicitamente solicitada e documentada.

## Testes

Toda correção deve incluir teste de regressão; toda funcionalidade nova deve cobrir caminho feliz, validações e falhas relevantes.

- Regras puras e invariantes: `tests/Oficina.Tests/Domain`.
- Orquestração de casos de uso: `tests/Oficina.Tests/Application`.
- Persistência, mapeamentos e DI: `tests/Oficina.Tests/Infrastructure`.
- Autenticação, rotas e contratos HTTP: `tests/Oficina.Api.ContractTests`.
- Dependências entre camadas: `tests/Oficina.Tests/Architeture` (nome legado mantido).

Padrões para testes:

- Use Arrange, Act e Assert de forma legível, sem comentários redundantes.
- Nomeie o teste pelo comportamento observado e pela condição relevante.
- Testes devem ser determinísticos e independentes da ordem de execução.
- Não dependam de rede, relógio real, SMTP real ou banco compartilhado.
- Para CPFs/CNPJs, use documentos matematicamente válidos; nos testes de contrato, reutilize `TestDocuments`.
- Ao corrigir um bug, escreva um teste que falhe antes da correção e passe depois dela.

## Comandos de desenvolvimento

Na raiz do repositório:

```bash
dotnet restore Oficina.sln
dotnet build Oficina.sln --no-restore
dotnet test Oficina.sln --no-build
```

Para ciclos menores:

```bash
dotnet test tests/Oficina.Tests/Oficina.Tests.csproj
dotnet test tests/Oficina.Api.ContractTests/Oficina.Api.ContractTests.csproj
dotnet run --project src/Oficina.Api/Oficina.Api.csproj
docker compose up --build -d
```

Migration de banco:

```bash
dotnet ef migrations add NomeDescritivo \
  --project src/Oficina.Infrastructure \
  --startup-project src/Oficina.Api

dotnet ef database update \
  --project src/Oficina.Infrastructure \
  --startup-project src/Oficina.Api
```

A aplicação executa migrations automaticamente no startup fora do ambiente `Testing`. Não aponte uma execução local para banco compartilhado sem confirmar o impacto.

## Critérios de conclusão

Antes de considerar uma alteração pronta:

1. Confirme que a mudança está na camada correta e não inverte dependências.
2. Execute build e os testes afetados; para mudanças transversais, execute `dotnet test Oficina.sln`.
3. Verifique migrations e compatibilidade de dados quando o modelo persistido mudar.
4. Revise autenticação, autorização, exposição de dados e tratamento de erros.
5. Atualize documentação e exemplos públicos afetados.
6. Inspecione o diff para remover arquivos gerados, credenciais, logs temporários e alterações sem relação com a tarefa.
7. Relate claramente o que mudou, quais testes foram executados e qualquer risco ou pendência conhecida.
