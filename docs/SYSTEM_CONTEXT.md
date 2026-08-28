# Oficina — Contexto Completo do Sistema

> Gerado em 2026-08-25 a partir do repositório `SOAT-Fase-1-Lil-Golden-Hammer` (branch `feature/issue-13`).
> Este documento é uma **fotografia** do estado atual do código-fonte, para servir de contexto/base a mudanças futuras. Não é uma fonte viva — releia os arquivos citados antes de confiar em detalhes finos.

## 1. Visão geral

API REST (.NET 10 / ASP.NET Core, C#) para gestão de uma oficina mecânica: clientes, veículos, peças, estoque, mecânicos, catálogo de serviços, ordens de serviço (com fluxo de status), orçamentos, histórico de OS, métricas e notificações por e-mail. Autenticação via JWT Bearer (token técnico único, sem usuários/senhas). Persistência em PostgreSQL via EF Core (Npgsql).

Solução (`Oficina.sln`) em Clean Architecture, 4 camadas + 2 projetos de teste:

```
src/
  Oficina.Api             -> Web API (controllers, auth, Program.cs, Swagger)
  Oficina.Application     -> Casos de uso (Services, DTOs, interfaces de repositório)
  Oficina.Domain          -> Entidades e regras de negócio puras (sem dependências externas)
  Oficina.Infrastructure  -> EF Core (DbContext, migrations, repositórios), SMTP, DI de infra
tests/
  Oficina.Tests               -> Testes unitários (domínio, aplicação, infra)
  Oficina.Api.ContractTests   -> Testes de contrato (rotas, OpenAPI, JWT) via WebApplicationFactory
```

Regra de dependência: `Api -> Application -> Domain` e `Infrastructure -> Application + Domain`. `Domain` não depende de nada.

## 2. Stack e pacotes

- **TargetFramework**: `net10.0` em todos os projetos.
- **Oficina.Api.csproj**: `Microsoft.AspNetCore.Authentication.JwtBearer 10.0.10`, `Microsoft.EntityFrameworkCore.Design 10.0.10`, `Swashbuckle.AspNetCore 6.9.0` (Swagger/OpenAPI). Gera XML de documentação (`GenerateDocumentationFile`) incluído no Swagger.
- **Oficina.Infrastructure.csproj**: `Microsoft.EntityFrameworkCore.Design/Relational 10.0.10`, `Npgsql.EntityFrameworkCore.PostgreSQL 10.0.0`, `Microsoft.Extensions.Options.ConfigurationExtensions`.
- Banco: **PostgreSQL** (connection string em `appsettings.json` → `ConnectionStrings:Database`).
- E-mail: SMTP simples (`SmtpOptions`, `SmtpNotificationEmailSender`).

## 3. Ponto de entrada — `Program.cs` (`src/Oficina.Api/Program.cs`)

Ordem de bootstrap:
1. `DotEnvLoader.LoadFromProjectRoot()` — carrega `.env` da raiz do projeto (ver `src/Oficina.Api/Configuration/DotEnvLoader.cs`).
2. `AddInfrastructure(connectionString, configuration)` — registra `AppDbContext` (Npgsql) e todos os repositórios/serviços de infra.
3. `AddControllers()`, `AddApplication()` — registra os *Services* da camada de aplicação.
4. JWT: `JwtOptions` vem de `appsettings.json:Jwt`, validado no start via `JwtOptionsValidator` (`IValidateOptions<JwtOptions>`) e `.ValidateOnStart()`. Autenticação configurada com `AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer()`, parâmetros de validação (issuer/audience/assinatura simétrica HMAC) montados a partir de `JwtOptions`. `ClockSkew` de 30s.
5. `AddAuthorization()`, `AddHealthChecks()` (`/health`), Swagger com `BearerSecurityOperationFilter` (adiciona cadeado nos endpoints protegidos) e comentários XML.
6. Middleware de exceção global (`UseExceptionHandler`) mapeia:
   - `KeyNotFoundException` → 404
   - `ConflictException` (custom, `Oficina.Application.Common`) → 409
   - `ArgumentException` / `InvalidOperationException` → 400
   - qualquer outra → 500 (mensagem oculta, só loga)
   Resposta sempre em `application/problem+json` com `title`, `status`, `detail`, `traceId`.
7. `UseSwagger/UseSwaggerUI`, `UseHttpsRedirection`, `UseAuthentication`, `UseAuthorization`, `MapHealthChecks`, `MapControllers`.
8. Fora do ambiente `Testing`, roda `context.Database.Migrate()` automaticamente no startup (auto-apply de migrations), logando erro sem derrubar a app se falhar.
9. `public partial class Program;` no fim — necessário para o `WebApplicationFactory<Program>` dos testes de contrato.

## 4. Autenticação (`src/Oficina.Api/Authentication/`)

Modelo simples de **token técnico único** (não há cadastro de usuário/senha):
- `POST /api/v1/auth/token` (`AuthController`, `[AllowAnonymous]`) → gera e devolve um `AccessTokenResponse` via `IAccessTokenGenerator` (`JwtAccessTokenGenerator`).
- Todos os demais endpoints (exceto `NotificationsController`, `BudgetsController` e a rota de token) exigem `[Authorize]` com Bearer JWT.
- `JwtOptions`: `Issuer`, `Audience`, `SigningKey` (HMAC simétrica), `ExpirationMinutes` (default 120). Validado por `JwtOptionsValidator`.
- **Atenção**: `BudgetsController` e `NotificationsController` atualmente **não têm `[Authorize]`** — endpoints públicos. Vale confirmar se isso é intencional antes de mudanças de segurança.

## 5. Camada Domain (`src/Oficina.Domain/`)

Entidades ricas (construtor privado + factory `Create`/`Open` + métodos de mutação com validação). Sem anotações de EF — mapeamento é 100% via Fluent API no `AppDbContext`.

| Entidade | Namespace | Campos principais | Regras notáveis |
|---|---|---|---|
| `Customer` | `Oficina.Domain.Customers` | Id, Name, Email, TelephoneNumber, Document (CPF/CNPJ), CreateDate, IsActive, `Vehicles`, `ServiceOrders` | Valida e normaliza documento (remove não-dígitos se 11/14 chars), rejeita dígitos repetidos. `Activate()/Deactivate()`. |
| `Vehicle` | `Oficina.Domain.Customers` (arquivo em `Vehicles/Vehicle.cs` mas namespace `Customers`) | Id, CustomerId, Plate, Brand, Model, Year, `EnumVehicleCategory` Category, IsActive | Valida e normaliza placa (formato antigo `ABC-1234` ou Mercosul `ABC1D23`), ano entre 1900 e ano atual+1. |
| `EnumVehicleCategory` | `Oficina.Domain.Vehicles` | Car=1, Motorcycle=2, Truck=3, Bus=4 | |
| `Part` | `Oficina.Domain.Parts` | Id, Name, Code, UnitPrice, `EnumPartKind` Kind, CreateDate, UpdateDate, IsActive | Código normalizado uppercase. Métodos de estoque (`AdjustStock`/`WithdrawStock`) estão **comentados/mortos no código** — estoque de fato vive em `StockPart`. |
| `EnumPartKind` | `Oficina.Domain.Parts` | Part=1, Consumable=2 | |
| `StockPart` | `Oficina.Domain.Stock` | Id, PartId, Quantity, CreatedDate, `Part?` | `AddQuantity`, `RemoveQuantity` (erro se negativo), `AdjustQuantity` (delta, pode ficar negativo? não — valida), `SetQuantity` (absoluto). Toda quantidade não pode ficar < 0. |
| `Mechanic` | `Oficina.Domain.Mechanics` | Id, Name, IsActive | CRUD simples. |
| `WorkshopService` | `Oficina.Domain.WorkshopServices` | Id, Name, Description, UnitPrice, EstimatedDurationMinutes, IsActive | Catálogo de serviços oferecidos pela oficina. |
| `ServiceOrder` | `Oficina.Domain.ServiceOrders` | Id, CustomerId, MechanicId?, VehicleId?, Description, CheckList?, `ServiceOrderStatus?` Status, CreatedAt, ScheduledAt, TotalParts, `Customer`, `Mechanic?`, `Vehicle?`, `Parts` (`ServiceOrderPart`), `WorkshopServices` (`ServiceOrderWorkshop`) | Máquina de estados em `UpdateStatus()` (ver §6). `TotalParts` recalculado ao setar peças. `ValidateUpdate` bloqueia troca de mecânico e adição de itens fora de certos estágios. |
| `ServiceOrderStatus` (enum) | `Oficina.Domain.ServiceOrders` | Received=1, InDiagnosis=2, AwaitingApproval=3, InExecution=4, Finalized=5, Delivered=6, Rejected=7 | Persistido como `string` no banco (`HasConversion<string>`). |
| `ServiceOrderPart` | `Oficina.Domain.OrderService` | Id, PartId, OrderServiceId, QuantityUsed, `OrderService?`, `Part?` | Item de peça consumida numa OS. |
| `ServiceOrderWorkshop` | `Oficina.Domain.OrderService` | Id, ServiceOrderId, WorkshopServiceId, navegações | Serviço de oficina vinculado a uma OS. |
| `ServiceOrderHistory` | `Oficina.Domain.OrderServiceHistory` | record: Id, OrderServiceId, StatusName, CreatedDate, `OrderService?` | Snapshot do status a cada mudança (auditoria). |
| `Budget` (orçamento) | `Oficina.Domain.Budget` | Id, CustomerId, ServiceOrderId, CreatedAt, IsApproved (bool?), TotalValue, `Parts` (`BudgetParts`), `WorkshopServices` (`BudgetWorkshopServices`) | `Open(...)` exige ao menos 1 serviço de oficina; `TotalValue` calculado a partir de peças + serviços. |
| `BudgetParts` / `BudgetWorkshopServices` | `Oficina.Domain.Budget` | Ligações N:N Budget↔Part e Budget↔WorkshopService com Quantity (parts) | |

Exceção de domínio compartilhada: `ConflictException` (`Oficina.Application.Common`) — mapeada para HTTP 409 no `Program.cs`.

## 6. Máquina de estados da Ordem de Serviço

Fluxo linear controlado em `ServiceOrder.UpdateStatus()` (`src/Oficina.Domain/OrderService/ServiceOrder.cs`), avaliado em cascata (primeira transição aplicável vence):

```
(null) --[tem CheckList]--> Received
Received --[tem MechanicId]--> InDiagnosis
InDiagnosis --[tem >=1 WorkshopService]--> AwaitingApproval
AwaitingApproval --[clientApproved=true]--> InExecution
AwaitingApproval --[clientApproved=false]--> Rejected  (terminal)
InExecution --[finalized=true]--> Finalized
Finalized --[delivered=true]--> Delivered  (terminal)
```

- Estados terminais (`Delivered`, `Rejected`) lançam `InvalidOperationException` em qualquer nova chamada de `UpdateStatus`.
- `ValidateUpdate` (chamado no `Update` do controller): em `InDiagnosis/AwaitingApproval/InExecution` não permite trocar o mecânico; em `null/Received/InExecution` não permite adicionar novas peças/serviços.
- Endpoints que disparam transições: `PUT /service-orders` (Update — pode setar CheckList, MechanicId, Parts, WorkshopServices, dispara `UpdateStatus()` sem parâmetros = tenta avançar via CheckList/Mecânico/Serviços), `POST /{id}/approve` (clientApproved=true), `POST /{id}/cancel` (clientApproved=false, **devolve peças ao estoque**), `POST /{id}/finalize`, `POST /{id}/deliver`.
- Toda transição de status é auditada em `ServiceOrderHistory` (via `RecordHistoryAsync`, só grava se o status realmente mudou).
- Ao cancelar (`CancelAsync`), peças já consumidas voltam ao estoque via `StockPart.AddQuantity`.
- Ao atualizar peças (`ResolvePartsAsync`), a diferença de quantidade é debitada/creditada no estoque automaticamente (delta positivo consome, negativo devolve). Lança erro se estoque insuficiente ou parte sem registro de estoque.

## 7. Camada Application (`src/Oficina.Application/`)

Um *Service* por bounded context, registrado em `DependencyInjection.AddApplication()`. Cada um: recebe repositórios (interfaces) via DI, contém a lógica de orquestração/casos de uso, mapeia entidades para DTOs de resposta (`*Dtos.cs`).

| Service | Pasta | Repositórios usados | Responsabilidade |
|---|---|---|---|
| `CustomerService` | `Clientes/` | `ICustomerRepository` | CRUD paginado de clientes |
| `VehicleService` | `Clientes/` | `IVehicleRepository`, (+customer) | CRUD de veículos + fluxo `IdentifyCustomerAndRegisterVehicleAsync` (busca/cria cliente e cadastra veículo em uma chamada) |
| `PartService` | `Parts/` | `IPartRepository` | CRUD paginado de peças |
| `StockService` | `Stocks/` | `IStockRepository`, `IPartRepository` | Consulta de estoque + movimentações (`Entry`/`Consume`/`Adjust`), cria estoque zerado sob demanda (`GetOrCreateStockAsync`) |
| `MechanicService` | `Mechanics/` | `IMechanicRepository` | CRUD de mecânicos |
| `ServiceCatalogService` | `WorkshopServices/` | `IWorkshopServiceRepository` | CRUD do catálogo de serviços |
| `ServiceOrderService` | `OrdensServico/` | `IServiceOrderRepository`, `ICustomerRepository`, `IVehicleRepository`, `IPartRepository`, `IWorkshopServiceRepository`, `IStockRepository`, `IServiceOrderHistoryRepository` | Orquestra abertura/atualização/transições de OS, integra com estoque e histórico; agenda (`ListSchedulesAsync`/`ListSchedulesByDateAsync`, converte UTC→horário de Brasília `E. South America Standard Time`) |
| `ServiceOrderHistoryService` | (registrado em `Infrastructure.DependencyInjection`, não em `Application`) | `IServiceOrderHistoryRepository` | Consulta de histórico (`FindAllAsync`, `FindByServiceOrderAsync`) |
| `BudgetService` | `Budgets/` | `IBudgetRepository` | Consulta paginada de orçamentos (sem endpoint de criação exposto ainda — só `List`/`GetById`) |
| `MetricsService` | `Metrics/` | `IMetricExecutionTimeRepository` | Tempo de execução por serviço de oficina |
| `NotificationService` | `Notifications/` | `INotificationEmailSender` | Envio de e-mail avulso |

`Common/`:
- `Pagination.cs` — helper `Pagination.Create(query, PageRequest)` que produz `PagedResponse<T>` (paginação padrão usada por quase todos os `List` endpoints).
- `ConflictException.cs` — exceção de negócio → HTTP 409.

## 8. Camada Infrastructure (`src/Oficina.Infrastructure/`)

- **`Persistence/AppDbContext.cs`** — único `DbContext`. `DbSet`s: `Customers`, `Vehicles`, `Parts`, `ServiceOrders`, `Mechanics`, `WorkshopServices`, `ServiceOrderWorkshops`, `StockParts`, `ServiceOrderHistories`, `Budgets`. Todo o mapeamento (chaves, FKs, `OnDelete`, tamanhos de coluna, conversões enum→string, `decimal(18,2)` para valores monetários) é Fluent API em `OnModelCreating`. Cascades: a maioria é `Cascade`; `ServiceOrder.Vehicle`/`Mechanic` são `SetNull`.
- **`Persistence/*Repository.cs`** — um repositório por agregado (`CustomerRepository`, `VehicleRepository`, `PartRepository`, `StockPartRepository` implementa `IStockRepository`, `WorkshopServiceRepository`, `ServiceOrderRepository`, `MechanicRepository`, `ServiceOrderHistoryRepository`, `MetricExecutionTimeRepository`, `BudgetRepository`).
- **`Migrations/`** — histórico EF Core (Npgsql), do mais antigo ao mais novo:
  1. `20260807040822_addCustomerAndVehicle`
  2. `20260811015036_AddNewTablesAccordingToSchema`
  3. `20260812023255_UpdateServiceOrder`
  4. `20260812024045_MakeVehicleAndMechanicOptional`
  5. `20260819023548_AddScheduledDateToOrderService`
  6. `20260819034039_AddBudget`
  7. `20260819035814_AddBudgetServiceOrderLink`
  `AppDbContextModelSnapshot.cs` reflete o estado atual do modelo — **sempre gerar migration (`dotnet ef migrations add`) ao alterar entidades/mapeamento**, nunca editar o snapshot manualmente.
- **`Notifications/`** — `SmtpNotificationEmailSender` (implementa `INotificationEmailSender`), `SmtpOptions` (Host/Port/From/EnableSsl, seção `Smtp` do `appsettings.json`).
- **`DependencyInjection.cs`** — `AddInfrastructure(connectionString, configuration)`: registra `AppDbContext` com Npgsql, todos os repositórios acima, `ServiceOrderHistoryService`, `SmtpOptions` e `INotificationEmailSender`.

## 9. Camada Api — Controllers e rotas (`src/Oficina.Api/Controllers/`)

Todos com `[ApiController]`, prefixo `api/v1/...`, a maioria `[Authorize]` (JWT Bearer). Padrão de resposta: `PagedResponse<T>` para listas paginadas, `ProducesResponseType` documentando 200/201/400/404/409.

| Controller | Rota base | Auth | Endpoints |
|---|---|---|---|
| `AuthController` | `/api/v1/auth` | anônimo | `POST /token` → gera token técnico JWT |
| `CustomersController` | `/api/v1/customers` | JWT | `GET` (paginado), `GET /{id}`, `POST`, `PUT /{id}`, `DELETE /{id}` |
| `VehiclesController` | `/api/v1/vehicles` | JWT | `GET` (paginado, filtro `customerId`), `GET /{id}`, `POST`, `POST /identify-customer-and-register`, `PUT /{id}`, `DELETE /{id}` |
| `PartsController` | `/api/v1/parts` | JWT | `GET` (paginado), `GET /{id}`, `POST`, `PUT /{id}`, `DELETE /{id}` |
| `StocksController` | `/api/v1/stocks` | JWT | `GET` (paginado), `GET /{id}`, `PUT /stocks-part/{partId}/entries`, `PUT /stocks-part/{partId}/consumptions`, `PUT /stocks-part/{partId}/adjustments` — **sem `POST` de criação exposto** (criação implícita na primeira movimentação via `GetOrCreateStockAsync`; `StockService.CreateAsync` existe mas não tem endpoint) |
| `MechanicsController` | `/api/v1/mechanics` | JWT | CRUD completo paginado |
| `WorkshopServicesController` | `/api/v1/workshop-services` | JWT | CRUD completo paginado |
| `ServiceOrdersController` | `/api/v1/service-orders` | JWT | `GET` (lista simples, não paginada), `GET /{id}`, `POST` (Open), `PUT` (Update, id no body), `POST /{id}/approve`, `POST /{id}/cancel`, `POST /{id}/finalize`, `POST /{id}/deliver` |
| `ServiceOrderHistoryController` | `/api/v1/service-order-history` | JWT | `GET` (tudo), `GET /service-order/{serviceOrderId}` |
| `ScheduleController` | `/api/v1/schedules` | JWT | `GET ?date=` → agenda de OS (por data ou todas); 404 se vazio |
| `BudgetsController` | `/api/v1/budgets` | **sem `[Authorize]`** | `GET` (paginado), `GET /{id}` — **sem criação/edição exposta** |
| `MetricsController` | `/api/v1/metrics` | JWT | `GET /workshop-service/execution-time` |
| `NotificationsController` | `/api/v1/notifications` | **sem `[Authorize]`** | `POST /email` → dispara e-mail avulso |

Infra transversal: `/health` (health checks), `/swagger` (UI + JSON), `BearerSecurityOperationFilter` (`src/Oficina.Api/OpenApi/`) injeta o requisito de segurança Bearer nos endpoints do Swagger.

## 10. Configuração (`src/Oficina.Api/appsettings.json` + `.env`)

```jsonc
{
  "ConnectionStrings": { "Database": "Host=localhost;Port=5432;Database=oficina;Username=oficina;Password=***" },
  "Smtp": { "Host": "smtp.example.com", "Port": 587, "From": "noreply@example.com", "EnableSsl": true },
  "Jwt": { "Issuer": "Oficina.Api", "Audience": "Oficina.Administration", "SigningKey": "***", "ExpirationMinutes": 120 }
}
```
- `DotEnvLoader` (`src/Oficina.Api/Configuration/DotEnvLoader.cs`) carrega variáveis de um `.env` na raiz antes do `WebApplication.CreateBuilder` — usado para sobrescrever segredos (signing key, connection string, SMTP) fora do `appsettings.json` versionado. Os valores acima em `appsettings.json` são placeholders de desenvolvimento, **não segredos reais de produção**.

## 11. Testes

- **`tests/Oficina.Tests/`** (unitário): `Domain/` (`CustomerTests`, `VehicleTests`, `PartTests`, `StockPartTests`, `MechanicTests`, `WorkshopServiceTests`, `BudgetTests`, `ServiceOrderHistoryTests`, `ServiceOrderTests`), `Application/` (`CustomerServiceTests`, `VehicleServiceTests`, `PartServiceTests`, `MechanicServiceTests`, `ServiceCatalogServiceTests`, `ServiceOrderHistoryServiceTests`, `BudgetServiceTests`, `MetricsServiceTests`, `NotificationServiceTests`, `ServiceOrderContractTests` — cobre `ServiceOrderService`, incluindo consumo/devolução de estoque e transições de status via o service —, `StockServiceTests`), `Api/` (geração/validação de JWT, rotas administrativas), `Infrastructure/` (`AppDbContextModelTests` — provavelmente valida o mapeamento do modelo). Todos os módulos de Domain, Application e Infrastructure têm cobertura unitária (2026-08-25, ≥90% de linhas por pacote via `dotnet test --settings CodeCoverage.runsettings --collect:"XPlat Code Coverage"`): os 10 repositórios EF Core (`tests/Oficina.Tests/Infrastructure/*RepositoryTests.cs`) são testados com o provider `Microsoft.EntityFrameworkCore.InMemory` (mesmo padrão já usado em `OficinaApiFactory`), não Postgres real. `CodeCoverage.runsettings` (raiz do repo) exclui `Migrations/*`/`*.Designer.cs`/`AppDbContextModelSnapshot.cs` da métrica — código gerado pelo EF, não testável de forma significativa. `Oficina.Api.Controllers` (2026-08-25): todos os 13 controllers foram marcados com `[ExcludeFromCodeCoverage]` (mesmo padrão das Migrations) a pedido do usuário — não recebem testes unitários dedicados; a lógica de negócio deles já é validada via os `*Service` da Application, e o roteamento/contrato HTTP via `Oficina.Api.ContractTests`. O atributo só afeta a métrica de cobertura, não impede nem interfere em testes de integração futuros contra esses endpoints.
- **`tests/Oficina.Api.ContractTests/`** (contrato, via `WebApplicationFactory<Program>` — por isso o `public partial class Program;` em `Program.cs`): `AdministrativeRoutesTests`, `JwtAuthenticationTests`, `JwtOpenApiContractTests`, `MetricsContractTests`, `NotificationsControllerTests`, `OpenApiContractTests`, `TokenEndpointTests`. Infra de apoio em `Infrastructure/OficinaApiFactory.cs`.

## 12. Pontos de atenção para próximas mudanças

- `BudgetsController` e `NotificationsController` não exigem `[Authorize]` — confirmar se é proposital antes de "proteger todas as rotas administrativas" (há uma issue/PR anterior — #18 — sobre proteger APIs administrativas com JWT, mas parece não ter coberto esses dois).
- `StockService.CreateAsync` existe na camada de aplicação mas não há `POST /api/v1/stocks` no controller — criação de estoque só acontece implicitamente na 1ª movimentação.
- `BudgetService` só expõe leitura (`List`/`GetById`) no controller; `OpenFromServiceOrderAsync` existe na camada de aplicação mas não tem endpoint. **Corrigido em 2026-08-25**: esse método usava `ServiceOrderPart.Id`/`ServiceOrderWorkshop.Id` (chave da própria linha de junção) em vez de `PartId`/`WorkshopServiceId` (FK para a Part/WorkshopService real) ao consultar `GetAllById`, o que fazia o método sempre lançar `InvalidOperationException` mesmo em ordens de serviço válidas — corrigido para usar os campos FK corretos, com teste de regressão em `BudgetServiceTests`.
- Código morto comentado em `Part.cs` (`AdjustStock`/`WithdrawStock`) — o controle real de estoque está em `StockPart`, não em `Part`. Não reativar esse código sem revisar duplicidade de responsabilidade.
- **Removido em 2026-08-25**: `ServiceOrderItem` (`Oficina.Domain.ServiceOrders`) não tinha nenhuma referência em `src/` nem mapeamento no `AppDbContext` — era código morto, foi excluído.
- Qualquer alteração em entidades/mapeamento requer nova migration EF Core (`dotnet ef migrations add <Nome> --project src/Oficina.Infrastructure --startup-project src/Oficina.Api`) — não editar `AppDbContextModelSnapshot.cs` manualmente.
- Migrations aplicam automaticamente no startup (`context.Database.Migrate()`) fora do ambiente `Testing` — cuidado ao rodar localmente contra um banco compartilhado.
- **Funcionalidade faltando, NÃO implementada de propósito (2026-08-26)**: a regra de negócio "quando a OS atinge `AwaitingApproval`, abrir um orçamento automaticamente com o total dos serviços de oficina anexados" não existe em lugar nenhum do código — confirmado por busca: `ServiceOrderService` não referencia `BudgetService`/`IBudgetRepository`, e `BudgetService.OpenFromServiceOrderAsync` só aparece na própria definição (nunca é chamado automaticamente nem exposto por rota HTTP). Teste de regressão em `tests/Oficina.Api.ContractTests/Domains/Budget.cs` (`Service_order_should_open_a_budget_when_it_reaches_awaiting_approval`) documenta o comportamento correto esperado e fica **vermelho deliberadamente** como lembrete.
- **Corrigido em 2026-08-26**: `VehicleService.CreateAsync`/`UpdateAsync` (`src/Oficina.Application/Clientes/VehicleService.cs`) checavam duplicidade de placa comparando a placa **crua** da requisição contra o valor já **normalizado** no banco (`_vehicles.GetByPlateAsync(request.Plate, ...)`), em vez de normalizar antes de comparar — a mesma placa física digitada com casing diferente não era pega como duplicata. Corrigido chamando `_vehicles.GetByPlateAsync(Vehicle.NormalizePlate(request.Plate), ...)` nos dois métodos. Aproveitado para fechar também uma segunda lacuna encontrada na mesma revisão: `Vehicle.NormalizePlate` (`src/Oficina.Domain/Vehicles/Vehicle.cs`) fazia `Regex.Replace(plate, "[^A-Za-z0-9]", "")`, removendo silenciosamente qualquer símbolo (`#`, `$`, espaço, ponto etc.) antes da checagem de formato — placas como `"A#B$C%9^9&9*9"` ou `"AB C.999.9"` eram aceitas (201) em vez de rejeitadas. Corrigido: `NormalizePlate` agora primeiro valida o conjunto de caracteres (`^[A-Za-z0-9-]+$`, hífen opcional) e, se houver qualquer outro símbolo, devolve a string intocada para que a checagem de formato subsequente rejeite (400) — só depois desse gate é que o hífen é removido e a placa é normalizada. `NormalizePlate` passou de `private` para `public static` (usado agora também por `VehicleService`). Testes de regressão em `tests/Oficina.Api.ContractTests/Domains/Vehicle.cs` (`Create_should_reject_same_plate_typed_with_different_casing_as_duplicate`, `Create_should_reject_plate_with_special_characters`) confirmam o fix — os 13 testes do arquivo passam (verificado via `dotnet test`).
- **Corrigido em 2026-08-26**: `Customer.IsValidDocument` (`src/Oficina.Domain/Customers/Customer.cs`) não calculava o dígito verificador real (mod-11) de CPF/CNPJ — só rejeitava documentos com todos os dígitos repetidos (ex. `000.000.000-00`), então um documento com 11/14 dígitos não repetidos mas com dígito verificador matematicamente incorreto (ex. `123.456.789-01`, `11.222.333/0001-00`) era aceito. Corrigido implementando o algoritmo mod-11 real: `IsValidCpf`/`IsValidCnpj`/`CalculateCheckDigit` (novos métodos privados em `Customer.cs`), aplicados por comprimento de dígitos (11 → CPF, 14 → CNPJ, qualquer outro comprimento é rejeitado). A validação de tamanho (`DocumentValidatorAttribute` em `src/Oficina.Application/Clientes/CustomerDtos.cs`) continua checando só o comprimento — o request passa por ela primeiro e a checagem de dígito verificador real acontece no domínio (`Customer.Create`/`Update`), que já lança `ArgumentException` (400) hoje. **Efeito colateral relevante**: como a suíte inteira usava CPFs fictícios sem dígito verificador válido (ex. `12345678901`, `10987654321`, contadores tipo `sequence.ToString().PadLeft(11,'0')`), essa correção quebrou ~115 testes em cascata até os fixtures serem trocados por documentos realmente válidos — ver `tests/Oficina.Api.ContractTests/Infrastructure/TestDocuments.cs` (gerador `ValidCpf(int sequence)`, mesmo algoritmo mod-11, usado pelos testes de contrato que precisam de um CPF único por execução) e os literais trocados por `11144477735`/`52998224725` (CPFs de teste reais, verificados manualmente) nos testes unitários de `Oficina.Tests`. Testes de regressão em `tests/Oficina.Api.ContractTests/Domains/Customer.cs` confirmam o fix (4/4 verdes).
- **Corrigido em 2026-08-26**: `OficinaApiFactory.cs` (`tests/Oficina.Api.ContractTests/Infrastructure/`) calculava o nome do banco InMemory (`Guid.NewGuid()`) dentro do lambda de `AddDbContext` — como essa configuração é `Scoped` por padrão, cada request HTTP recriava um banco novo e vazio, quebrando qualquer teste de contrato que precisasse criar um recurso numa chamada e lê-lo em outra. Corrigido calculando o nome uma única vez, fora do lambda. Ver `tests/Oficina.Api.ContractTests/Contracts/ServiceOrderLifecycleContractTests.cs` para um teste que exercita esse cenário (cliente → veículo → OS percorrendo os 6 status via HTTP).
