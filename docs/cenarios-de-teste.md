# Cenários de Teste — Oficina

> Catálogo gerado em 2026-08-26 a partir da suíte de testes automatizados existente no repositório (`tests/Oficina.Tests` e `tests/Oficina.Api.ContractTests`), organizado por camada e por classe. Cada item abaixo corresponde a um `[Fact]`/`[Theory]` real já implementado — este documento é uma leitura estruturada do que está validado hoje, não uma proposta de cenários novos. Serve como referência rápida do que a suíte cobre e como checklist ao planejar testes adicionais.

Convenções: **OK** = fluxo válido; **Erro** = valida rejeição/exceção; **Borda** = caso limite/edge case.

---

## 1. Domain (`Oficina.Domain`)

### `Customer` — `tests/Oficina.Tests/Domain/CustomerTests.cs`
- **OK** — `Create` normaliza e-mail (lowercase/trim) e documento (remove máscara de CPF).
- **OK** — `Create` aceita CNPJ válido (14 dígitos) e normaliza.
- **OK** — `Update` altera nome, e-mail, telefone e documento com sucesso.
- **Erro** — `Create` rejeita nome vazio.
- **Erro** — `Create` rejeita CPF com todos os dígitos repetidos (`000.000.000-00`).
- **Erro** — `Create` rejeita CNPJ com todos os dígitos repetidos.
- **Erro** — `Create` rejeita documento sem nenhum dígito (ex.: `"---"`).
- **Erro** (2026-08-27, item 6) — `Create` rejeita CPF/CNPJ com quantidade correta de dígitos, não repetidos, mas dígito verificador matematicamente incorreto (`111.444.777-36` e `11.222.333/0001-82`, cada um alterando só o último dígito verificador de um documento válido conhecido) — espelho unitário do que já era testado só via HTTP em `Domains/Customer.cs`.

### `Vehicle` — `tests/Oficina.Tests/Domain/VehicleTests.cs` (+ casos em `CustomerTests.cs`)
- **OK** — `Create` vincula veículo ao cliente e preenche marca/modelo/ano.
- **OK** — `Create` normaliza placa antiga com/sem hífen (`ABC1234` → `ABC-1234`).
- **OK** — `Create` normaliza placa Mercosul (`abc1d23` → `ABC1D23`).
- **OK** — `Create` aceita ano até um ano à frente do atual.
- **OK** — `Update` altera placa, marca, modelo, ano e categoria.
- **OK/Erro** — `IsValidPlate` (teoria): valida formato antigo, Mercosul e com hífen; rejeita formato inválido, vazio e espaço em branco.
- **Erro** — `Create` rejeita `customerId` vazio (`Guid.Empty`).
- **Erro** — `Create` rejeita marca, modelo vazios.
- **Erro** — `Create` rejeita ano antes de 1900 ou mais de um ano à frente.
- **Erro** — `Create`/`Update` rejeitam formato de placa inválido.
- **OK** — `Deactivate` marca veículo como inativo.
- **Erro** (2026-08-27, item 8) — `IsValidPlate` (Theory ampliada com 2 casos de fronteira adicionais, não os 8 originalmente escritos — cortado por excesso, ver nota abaixo): `ABC12345` (formato antigo com 5 dígitos) e `ABC1DD34` (duas letras onde o Mercosul espera uma), ambas corretamente rejeitadas.
- **Nota** (item 21) — a fronteira exata `AnoAtual+1` aceito / `AnoAtual+2` rejeitado já era coberta com precisão por `Create_should_accept_year_one_year_ahead`/`Create_should_reject_year_too_far_in_the_future`; nenhum teste novo foi necessário, só confirmado e documentado.

### `Part` — `tests/Oficina.Tests/Domain/PartTests.cs`
- **OK** — `Create` normaliza código (uppercase/trim) e nome (trim); tipo padrão é `Part` quando omitido.
- **OK** — `Update` altera nome, código, preço e tipo.
- **Erro** — `Create`/`Update` rejeitam nome ou código vazios.
- **Erro** — `Create`/`Update` rejeitam preço unitário negativo.
- **OK** — `Deactivate` marca peça como inativa.

### `StockPart` — `tests/Oficina.Tests/Domain/StockPartTests.cs`
- **OK** — `Create` aceita quantidade zero.
- **OK** — `AddQuantity` incrementa; `RemoveQuantity` decrementa; `AdjustQuantity` aplica delta positivo e negativo; `SetQuantity` substitui o valor absoluto.
- **Erro** — `Create` rejeita `partId` vazio ou quantidade negativa.
- **Erro** — `AddQuantity`/`AdjustQuantity` rejeitam movimento zero.
- **Erro** — `RemoveQuantity`/`AdjustQuantity` rejeitam resultado negativo.
- **Erro** — `SetQuantity` rejeita valor negativo.

### `Mechanic` — `tests/Oficina.Tests/Domain/MechanicTests.cs`
- **OK** — `Create`/`Update` fazem trim do nome.
- **Erro** — `Create`/`Update` rejeitam nome vazio.
- **OK** — `Deactivate` marca mecânico como inativo.

### `WorkshopService` — `tests/Oficina.Tests/Domain/WorkshopServiceTests.cs`
- **OK** — `Create` faz trim de nome/descrição.
- **OK** — `Update` altera todos os campos.
- **Erro** — `Create`/`Update` rejeitam nome ou descrição vazios, preço negativo, duração não positiva.
- **OK** — `Deactivate` marca serviço como inativo.

### `ServiceOrder` (máquina de estados) — `tests/Oficina.Tests/Domain/ServiceOrderTests.cs`
- **OK** — `Open` inicia com `Status = null`.
- **OK** — `UpdateStatus` avança exatamente um passo por chamada: `null → Received` (checklist definido) → `Received → InDiagnosis` (mecânico atribuído) → `InDiagnosis → AwaitingApproval` (≥1 serviço de oficina) → `AwaitingApproval → InExecution` (cliente aprova) → `InExecution → Finalized` (flag `finalized`) → `Finalized → Delivered` (flag `delivered`).
- **OK** — checklist e mecânico chegando juntos ainda avançam só um passo por chamada.
- **OK** — adicionar peça isolada não avança para `AwaitingApproval`; só o serviço de oficina avança; peça pode acompanhar o serviço na mesma transição.
- **Erro/Borda** — `UpdateStatus` não faz nada sem checklist, sem mecânico, ou sem decisão do cliente em `AwaitingApproval`.
- **OK** — aprovação do cliente move para `InExecution`; rejeição move para `Rejected` (terminal).
- **Erro** — `UpdateStatus` lança `InvalidOperationException` uma vez `Delivered` ou `Rejected` (não pode mudar mais).
- **Borda** — flags `finalized`/`delivered` fora de ordem são ignoradas (não avançam fora de `InExecution`/`Finalized` respectivamente).
- **OK** — `Update` preserva texto opcional e mecânico já atribuído quando não informados.
- **Erro** — `ValidateUpdate` lança para ordens `Finalized`/`Delivered`/`Rejected`.
- **Erro** — `ValidateUpdate` bloqueia troca de mecânico após diagnóstico iniciado.
- **Erro** — `ValidateUpdate` bloqueia itens novos antes de mecânico atribuído e durante `InExecution`.
- **OK** — `ValidateUpdate` permite omitir mecânico já atribuído durante diagnóstico.
- **OK** — teste de ciclo de vida completo (`Full_lifecycle...`) percorre todas as transições em sequência e confirma que não é possível voltar/pular estados.

### `ServiceOrderPart` — `tests/Oficina.Tests/Domain/ServiceOrderPartTests.cs`
- **OK** — `Create` define ids e quantidade; `UpdateQuantity` altera a quantidade usada.
- **Erro** — `Create`/`UpdateQuantity` rejeitam quantidade não positiva.

### `ServiceOrderHistory` — `tests/Oficina.Tests/Domain/ServiceOrderHistoryTests.cs`
- **OK** — `Create` faz trim do nome do status.
- **Borda** — `Create` define `StatusName = "Unknown"` quando nulo ou em branco.
- **Erro** — `Create` rejeita `orderServiceId` vazio.

### `Budget` / `BudgetParts` / `BudgetWorkshopServices` — `tests/Oficina.Tests/Domain/BudgetTests.cs`
- **OK** — `Open` calcula `TotalValue` a partir de peças + serviços de oficina.
- **Borda** — `Open` calcula total só com serviços quando não há peças (`parts = null`).
- **Erro** — `Open` rejeita id, `customerId` ou `serviceOrderId` vazios.
- **Erro** — `Open` rejeita lista de serviços de oficina nula ou vazia (orçamento exige ao menos 1 serviço).
- **OK** — `BudgetParts.Create`/`BudgetWorkshopServices.Create` definem ids corretamente.
- **Erro** — `BudgetParts.Create` rejeita quantidade não positiva.
- **Borda** (2026-08-27, item 22) — `Open` calcula o mesmo `TotalValue` (e mantém `Parts` vazio) tanto para `parts = null` quanto para `parts = new List<BudgetParts>()` (lista vazia não-nula) — sem bug de referência nula vs. coleção vazia.

---

## 2. Application (`Oficina.Application`)

### `CustomerService` — `CustomerServiceTests.cs`
- **OK** — `ListAsync` retorna só clientes ativos e filtra por termo de busca (nome/e-mail/documento).
- **OK** — `CreateAsync` cria novo cliente; reativa cliente inativo que reusa o mesmo documento.
- **OK** — `UpdateAsync` altera dados do cliente.
- **Erro** — `CreateAsync`/`UpdateAsync` lançam `ConflictException` quando o documento já pertence a outro cliente ativo.
- **Erro** — `UpdateAsync` lança `KeyNotFoundException` para cliente inexistente.
- **Borda** — `GetByIdAsync` retorna `null` para cliente inativo.
- **OK/Borda** — `DeleteAsync` desativa cliente existente; retorna `false` se já não existe.

### `VehicleService` — `VehicleServiceTests.cs`
- **Erro** — `CreateAsync` lança `KeyNotFoundException` quando cliente não existe ou está inativo.
- **Erro** — `CreateAsync`/`UpdateAsync` lançam `ConflictException` quando a placa já pertence a outro veículo.
- **OK** — `CreateAsync` registra veículo para cliente existente.
- **OK/Erro** — `IdentifyCustomerAndRegisterVehicleAsync` registra veículo ao achar cliente pelo documento; lança `KeyNotFoundException` se documento não encontrado.
- **OK** — `ListAsync` filtra por `customerId`.
- **Borda** — `GetByIdAsync` retorna `null` para veículo inativo; retorna dados para veículo ativo.
- **OK** — `UpdateAsync` altera dados do veículo.
- **Erro** — `UpdateAsync` lança `KeyNotFoundException` para veículo inexistente.
- **OK** — `DeleteAsync` desativa veículo existente.

### `PartService` — `PartServiceTests.cs`
- **OK** — `CreateAsync` cria peça **e** o registro de estoque zerado correspondente.
- **Erro** — `CreateAsync`/`UpdateAsync` lançam `ConflictException` quando o código já existe em outra peça.
- **Erro** — `UpdateAsync` lança `KeyNotFoundException` para peça inexistente.
- **OK** — `ListAsync` retorna só peças ativas, filtradas por busca.
- **Erro** — `AdjustStockAsync` rejeita motivo (`Reason`) vazio.
- **OK** — `AdjustStockAsync` cria estoque quando não existe e atualiza quando já existe.
- **Erro** — `AdjustStockAsync` lança `KeyNotFoundException` para peça inexistente.
- **OK/Borda** — `DeleteAsync` desativa peça existente; retorna `false` se não existe.

### `StockService` — `StockServiceTests.cs`
- **OK** — `ListAsync` retorna estoques ordenados por peça; `GetByIdAsync` retorna estoque existente ou `null`.
- **Erro** — `CreateAsync` lança `ConflictException` se já existe estoque para a peça.
- **OK** — `EntryAsync` cria estoque quando não existe; `ConsumeAsync` reduz quantidade existente; `AdjustAsync` substitui a quantidade.
- **Erro** — `EntryAsync`/`ConsumeAsync`/`AdjustAsync` rejeitam quantidade negativa.

### `MechanicService` — `MechanicServiceTests.cs`
- **OK** — `ListAsync` retorna só mecânicos ativos; `GetByIdAsync` retorna `null` para inativo.
- **OK** — `CreateAsync`/`UpdateAsync` funcionam para caso feliz.
- **Erro** — `UpdateAsync` lança `KeyNotFoundException` para mecânico inexistente.
- **OK/Borda** — `DeleteAsync` desativa existente; retorna `false` se não existe.

### `ServiceCatalogService` (Workshop Services) — `ServiceCatalogServiceTests.cs`
- **OK** — `ListAsync` retorna só serviços ativos.
- **Erro** — `CreateAsync`/`UpdateAsync` lançam `ConflictException` quando o nome já existe em outro serviço.
- **Erro** — `UpdateAsync` lança `KeyNotFoundException` para serviço inexistente.
- **OK** — `UpdateAsync` altera dados do serviço com sucesso.
- **OK/Borda** — `DeleteAsync` desativa existente; retorna `false` se não existe.

### `ServiceOrderService` — `ServiceOrderContractTests.cs` (o arquivo mais extenso da suíte)
- **OK** — `OpenAsync` retorna DTO de detalhe; `ListAsync` retorna resumo das ordens.
- **Erro** — `OpenAsync` lança `InvalidOperationException` quando cliente ou veículo não existem.
- **Borda** — `GetByIdAsync` retorna `null` para ordem inexistente; retorna detalhe para ordem existente.
- **OK** — `UpdateAsync` consome estoque ao adicionar peça nova; devolve estoque ao reduzir a quantidade de uma peça já usada.
- **Erro** — `UpdateAsync` lança quando estoque é insuficiente, peça não existe, serviço de oficina não existe, ou ao tentar trocar o mecânico após o diagnóstico ter começado.
- **OK** — `ApproveAsync` avança para `InExecution`; `FinalizeAsync` avança para `Finalized`; `DeliverAsync` avança para `Delivered`.
- **Erro** — `ApproveAsync`/`CancelAsync`/`FinalizeAsync`/`DeliverAsync` lançam quando a ordem está no status errado **ou** quando a ordem não existe.
- **OK** — `CancelAsync` rejeita a ordem e devolve ao estoque as peças já consumidas.
- **Borda** — `CancelAsync` não falha (apenas ignora) quando uma peça da ordem não tem mais registro de estoque.
- **OK** — histórico grava exatamente uma entrada por transição real de status ao longo do ciclo de vida completo (`Full_lifecycle_should_record_one_history_entry_per_real_transition`).
- **OK/Borda** — `ListSchedulesAsync` retorna as ordens agendadas ou lista vazia quando não há nenhuma; `ListSchedulesByDateAsync` filtra corretamente por data.

### `ServiceOrderHistoryService` — `ServiceOrderHistoryServiceTests.cs`
- **OK** — `FindAllAsync` retorna todo o histórico; `FindByServiceOrderAsync` filtra por ordem de serviço.
- **Erro** — `FindByServiceOrderAsync`/`CreateAsync` rejeitam id de ordem de serviço vazio.
- **OK** — `CreateAsync` persiste uma entrada de histórico.

### `BudgetService` — `BudgetServiceTests.cs`
- **OK** — `ListAsync` retorna orçamentos registrados; `GetByIdAsync` retorna `null` quando não existe.
- **Erro** — `OpenFromServiceOrderAsync` lança quando a ordem de serviço não existe, quando não tem nenhum serviço de oficina, ou quando uma peça referenciada não existe mais no catálogo.
- **OK** — `OpenFromServiceOrderAsync` abre orçamento e calcula `TotalValue` corretamente (regressão do bug de `PartId`/`WorkshopServiceId` trocados por `Id` da linha de junção — corrigido em 2026-08-25).

### `MetricsService` — `MetricsServiceTests.cs`
- **OK** — `GetWorkshopServiceExecutionTimesAsync` consolida serviços repetidos numa mesma ordem, distribui a duração da ordem proporcionalmente ao tempo estimado de cada serviço, calcula a média de execuções alocadas para o mesmo serviço em ordens diferentes, e mantém no resultado serviços sem nenhuma execução válida (com tempo médio nulo).

### `NotificationService` — `NotificationServiceTests.cs`
- **OK** — `SendEmailAsync` envia notificação simples ao destinatário.
- **Erro** — `SendEmailAsync` rejeita e-mail inválido; propaga falha do `INotificationEmailSender` (não engole a exceção).

### `Pagination` — `PaginationTests.cs`
- **Erro** — `Create` rejeita `Page < 1` e `PageSize` fora do intervalo 1–100.
- **Borda** — `Create` retorna `TotalPages = 0` para fonte vazia.
- **OK** — `Create` pagina corretamente uma fonte com múltiplas páginas.

### `DependencyInjection` (Application) — `DependencyInjectionTests.cs`
- **OK** — `AddApplication` registra os 10 serviços da camada de aplicação como `Scoped`.

---

## 3. Infrastructure (`Oficina.Infrastructure`)

> Testados com EF Core **InMemory** (`Microsoft.EntityFrameworkCore.InMemory`), banco isolado por teste — mesmo padrão já usado em `OficinaApiFactory` para os testes de contrato. Sem dependência de Postgres real.

### Repositórios — `tests/Oficina.Tests/Infrastructure/*RepositoryTests.cs`
Padrão comum a todos os 10 repositórios (`Customer`, `Vehicle`, `Part`, `StockPart`, `WorkshopService`, `Mechanic`, `ServiceOrderHistory`, `Budget`, e casos adicionais em `ServiceOrder`/`MetricExecutionTime`):
- **OK** — `AddAsync` + `ListAsync` persistem e retornam a entidade criada.
- **Borda** — `GetByIdAsync` retorna `null` quando o registro não existe.
- **OK** — métodos de busca específicos funcionam: `GetByDocumentAsync` (Customer), `GetByPlateAsync` (Vehicle), `GetByCodeAsync`/`GetAllById` (Part), `GetByPartIdAsync` (StockPart), `GetByNameAsync`/`GetAllById` (WorkshopService, ignorando maiúsculas/minúsculas).
- **OK** — `UpdateAsync` persiste alterações feitas na entidade.

Casos específicos:
- **`ServiceOrderRepositoryTests`**: `ListAsync` inclui peças e serviços de oficina; `ListSchedulesAsync` retorna ordens agendadas para os próximos 30 dias; `ListSchedulesByDateAsync` filtra por data exata; `UpdateAsync` persiste peças/serviços novos adicionados à ordem.
- **`MetricExecutionTimeRepositoryTests`**: `GetAsync` agrega corretamente serviços de oficina + histórico de ordens **finalizadas**; ordens que não chegaram a `Finalized` são ignoradas no resultado.
- **`BudgetRepositoryTests`**: `ListAsync`/`GetByIdAsync` trazem peças e serviços de oficina com os dados relacionados (`Include`/`ThenInclude`) carregados.

### `SmtpNotificationEmailSender` — `SmtpNotificationEmailSenderTests.cs`
- **Erro** — `SendAsync` lança `InvalidOperationException` quando `Host` ou `From` estão vazios (antes de qualquer I/O de rede).
- **OK** — `SendAsync` monta `MailMessage`/`SmtpClient` e tenta o envio real (sem/com credenciais configuradas) — testado apontando para uma porta loopback livre, sem precisar de um servidor SMTP fake.

### `DependencyInjection` (Infrastructure) — `DependencyInjectionTests.cs`
- **OK** — `AddInfrastructure` registra o `AppDbContext` e todos os repositórios/serviços de infraestrutura como `Scoped`.

> `Oficina.Infrastructure.Migrations` está marcado com `[ExcludeFromCodeCoverage]` em cada classe (código gerado por `dotnet ef migrations add`) — deliberadamente sem testes.

---

## 4. Api (`Oficina.Api`) — testes unitários diretos

### `JwtAccessTokenGenerator` — `tests/Oficina.Tests/Api/JwtAccessTokenGeneratorTests.cs`
- **OK** — `Generate` cria um token JWT assinado, de curta duração, sem roles/claims de autorização.
- **OK** — `Generate` gera um `jti` (identificador único) diferente a cada chamada.

### `JwtOptionsValidator` — `tests/Oficina.Tests/Api/JwtOptionsValidatorTests.cs`
- **OK** — `Validate` aceita configuração válida.
- **Erro** — `Validate` rejeita configuração pública inválida (issuer/audience) e chave de assinatura inválida (curta demais).

### `DotEnvLoader` — `tests/Oficina.Tests/Api/DotEnvLoaderTests.cs`
- **OK** — `LoadFromProjectRoot` interpreta corretamente `.env`: valores simples, prefixo `export`, aspas simples/duplas, comentários, e **não sobrescreve** variável de ambiente já definida.
- **Borda** — `LoadFromProjectRoot` não faz nada (não lança) quando nenhum `.env` é encontrado subindo a árvore de diretórios.

### Rotas administrativas (unitário) — `tests/Oficina.Tests/Api/AdministrativeRouteTests.cs`
- **OK** — controllers administrativos exigem `[Authorize]` e usam o prefixo `v1`.

> `Oficina.Api.Controllers` está marcado com `[ExcludeFromCodeCoverage]` em cada classe — a lógica de negócio já é validada nos `*Service` (Application) listados acima; o papel do controller (roteamento/binding/status HTTP) é validado pelos testes de contrato abaixo.

---

## 5. Testes de contrato / integração (`Oficina.Api.ContractTests`)

Sobem a aplicação inteira via `WebApplicationFactory<Program>` (`OficinaApiFactory`), com banco EF Core InMemory e JWT de teste — validam o comportamento real de HTTP, roteamento, autenticação e OpenAPI.

- **`TokenEndpointTests`**: `POST /api/v1/auth/token` emite um JWT básico sem exigir credenciais (issuer/audience/subject corretos, sem claims de role).
- **`JwtAuthenticationTests`**: rota administrativa sem token retorna 401; com token válido é alcançada; token inválido/malformado retorna 401; rotas de infraestrutura e de token seguem anônimas.
- **`AdministrativeRoutesTests`**: recursos administrativos (`customers`, `vehicles`, `mechanics`, `workshop-services`, `parts`, `stocks`, `service-orders`, `service-order-history`) só existem sob `/api/v1/`; `/health` e `/swagger` continuam sem versionamento.
- **`JwtOpenApiContractTests`** / **`OpenApiContractTests`**: o documento OpenAPI descreve segurança Bearer apenas nas operações protegidas, e publica só os paths/DTOs administrativos versionados.
- **`MetricsContractTests`**: rota de tempo de execução por serviço de oficina está disponível sob `/api/v1/metrics`.
  - **OK** (2026-08-27, item 16) — `Workshop_service_execution_time_should_reflect_a_real_finalized_order`: percorre uma OS real via HTTP até `Finalized` com um `WorkshopService` de duração estimada conhecida (30 min) e confirma que a métrica retorna `EstimatedTimeMinutes = 30` e `AverageTimeMinutes` não-nulo calculado de verdade (não fixa um valor exato de minutos porque o teste roda em tempo real/rápido, mas confirma que o número vem de um cálculo real, não só que a rota responde).
- **`NotificationsControllerTests`**: `POST /api/v1/notifications/email` retorna 204 em caso de sucesso e 400 para e-mail inválido. Nenhuma das chamadas envia token — já era, implicitamente, evidência de que a rota é anônima; `AnonymousEndpointsTests` (abaixo) torna essa decisão explícita.
- **`AnonymousEndpointsTests`** (2026-08-27, novo arquivo — docs/analise-gaps-e-cenarios-faltantes.md seção 2.1, itens 1–5): trava o limite de segurança/autorização atual da API.
  - **OK/Decisão** — `GET /api/v1/budgets` e `GET /api/v1/budgets/{id}` funcionam sem token (decisão intencional, já documentada em `Domains/Budget.cs`; aqui ganham um teste dedicado e comentado).
  - **OK/Decisão** — `POST /api/v1/notifications/email` funciona sem token (idem).
  - **Erro** — `GET /api/v1/metrics/workshop-service/execution-time` sem token retorna 401 (rota administrativa).
  - **Erro** — `GET /api/v1/schedules` sem token retorna 401 (rota administrativa).
  - **Borda/Documentação** — não há RBAC: o mesmo token emitido por `POST /api/v1/auth/token` (sem credenciais, sem claims de role) cria **e** deleta um mecânico — qualquer token válido acessa qualquer rota administrativa. Guarda de regressão para o dia em que roles forem adicionadas.
- **`Domains/ServiceOrder.cs` (`ServiceOrderTests`, 25 testes)**: cobre a máquina de estados da OS de ponta a ponta via HTTP (`Received=1, InDiagnosis=2, AwaitingApproval=3, InExecution=4, Finalized=5, Delivered=6, Rejected=7`, além do estado inicial `null`).
  - **Teste de ciclo completo**: cria cliente, veículo, mecânico e serviço de oficina, abre uma OS e percorre as 6 transições em sequência, conferindo o status code e o campo `status` de cada resposta — cobre "quero testar todos os status da OS numa passada só". Usa `ITestOutputHelper` para narrar cada etapa.
  - **Grupo A — um teste por status (8 testes, todos OK)**: confirma que cada status (`null`, `Received`, `InDiagnosis`, `AwaitingApproval`, `InExecution`, `Finalized`, `Delivered`, `Rejected`) é alcançado pela ação correta, isoladamente.
  - **Grupo B — tentativas de pular status (8 testes, todos corretamente barrados com 400)**: aprovar uma OS recém-aberta, em `Received` ou em `InDiagnosis` (pulando os estágios intermediários); finalizar uma OS recém-aberta ou ainda em `AwaitingApproval` (pulando `InExecution`); entregar uma OS ainda em `InExecution` (pulando `Finalized`); cancelar uma OS em `InDiagnosis` ou já em `InExecution` (só é permitido cancelar em `AwaitingApproval`).
  - **Grupo C — estados terminais (2 testes, todos corretamente barrados com 400)**: uma vez `Delivered` ou `Rejected`, **nenhuma** ação (`approve`/`cancel`/`finalize`/`deliver`) funciona mais.
  - **Grupo D — tentativas de "quebrar" via `Update` (5 testes, todos corretamente barrados com 400)**: trocar o mecânico durante `InDiagnosis`/`AwaitingApproval`/`InExecution`; anexar serviço de oficina antes do mecânico ser atribuído (ainda em `Received`); anexar peça nova depois que a OS já está em `InExecution`.
  - **Nenhum bug encontrado neste domínio** — a máquina de estados barra corretamente todas as 15 tentativas de pular, voltar ou quebrar o fluxo testadas (grupos B, C e D), sempre com 400 e sem alterar o status da OS.
  - **Grupo E — cenários adicionais de negócio (2026-08-27, docs/analise-gaps-e-cenarios-faltantes.md seção 2.3, itens 12/13/23/24)**:
    - **Erro/Borda** (item 12) — `Cancel_twice_in_a_row_should_fail_the_second_time`: primeiro `POST /cancel` em `AwaitingApproval` funciona (200, `Rejected`); o segundo, na mesma OS já `Rejected`, falha com 400 — idempotência do cancelamento (não há devolução dupla de estoque porque a segunda chamada nem chega a mexer no estoque).
    - **OK** (item 13) — `Full_lifecycle_without_any_part_should_reach_delivered_and_budget_should_total_services_only`: ciclo completo de uma OS **sem nenhuma peça**, só com um serviço de oficina de R$250 — confirma `TotalParts = 0`, orçamento aberto com `TotalValue = 250` e `Parts` vazio, e a OS chega normalmente a `Delivered`.
    - **Borda/Caracterização** (item 23) — `Order_should_keep_working_after_its_assigned_mechanic_is_deactivated`: soft-deleta (`DELETE /api/v1/mechanics/{id}`) o mecânico já atribuído a uma OS em `InDiagnosis` e confirma que a OS continua avançando normalmente (chega a `AwaitingApproval`, mantém o mesmo `mechanicId`). Documenta o comportamento atual — nada em `ServiceOrderService`/`ValidateUpdate` verifica `Mechanic.IsActive` — sem prescrever se deveria ser diferente.
    - **Borda/Caracterização** (item 24) — `Order_and_budget_should_stay_consistent_after_an_attached_workshop_service_is_deactivated` e `Order_and_budget_should_stay_consistent_after_an_attached_part_is_deactivated`: soft-deleta um serviço de oficina/peça já anexado a uma OS aberta — o item some do catálogo ativo (`GET` por id retorna 404), mas a OS (`GET /api/v1/service-orders/{id}`) e o orçamento automaticamente aberto continuam referenciando o item e com o total consistente.
- **`Domains/Customer.cs` (`CustomerTests`)**: valida CPF/CNPJ com dígito verificador real (algoritmo mod-11), não só o formato/tamanho. 4 testes, todos verdes (verificado 2026-08-26).
  - **OK** — CPF válido (`086.043.100-29`) e CNPJ válido (`11.222.333/0001-81`, dígitos verificadores conferidos manualmente) são aceitos (201).
  - **Corrigido em 2026-08-26** — CPF (`123.456.789-01`) e CNPJ (`11.222.333/0001-00`) com dígito verificador matematicamente errado agora são corretamente rejeitados (400). **Causa raiz**: `Customer.IsValidDocument` (`src/Oficina.Domain/Customers/Customer.cs`) só rejeitava documentos com todos os dígitos repetidos — nunca calculava o dígito verificador real. **Fix**: implementado o algoritmo mod-11 completo (`IsValidCpf`/`IsValidCnpj`/`CalculateCheckDigit`). **Efeito colateral**: essa correção quebrou ~115 testes em toda a solução que usavam CPFs fictícios sem dígito verificador válido (ex. `12345678901`); fixtures foram substituídos por CPFs realmente válidos (`11144477735`, `52998224725`) e por um gerador dedicado (`tests/Oficina.Api.ContractTests/Infrastructure/TestDocuments.cs`, `ValidCpf(int sequence)`) para os testes de contrato que precisam de um documento único por execução.
  - **Erro** (2026-08-27, item 7) — `Create_should_reject_document_containing_letters`: documento com letras (`"123.ABC.789-01"`) é rejeitado com 400 antes mesmo de chegar à camada de domínio — `DocumentValidatorAttribute`/o setter de `CreateCustomerRequest.Document` normalizam removendo tudo que não é dígito, então o valor vira `"12378901"` (8 dígitos) e falha na checagem de tamanho (11/14).
  - **Erro/Borda** (2026-08-27, item 9) — `Create_should_reject_same_cpf_typed_with_different_formatting_as_duplicate`: cadastra um CPF sem máscara e depois o mesmo CPF formatado (`111.444.777-35`) — corretamente rejeitado como duplicata (409). Diferente do bug já corrigido em `VehicleService` (placa), aqui não havia bug: o setter de `CreateCustomerRequest.Document` já normaliza (remove máscara) antes de `CustomerService.CreateAsync` comparar contra o repositório, então os dois cadastros chegam com o mesmo valor de documento.
  - **Erro/Borda** (2026-08-27, item 19) — `List_should_reject_invalid_pagination_parameters` (`Theory`: `page=0`, `pageSize=0`, `pageSize=101`): 400 via HTTP real, confirmando que `Pagination.Create` (que já lança `ArgumentOutOfRangeException`, testado isoladamente em `PaginationTests`) está de fato conectado ao pipeline de exceções do `Program.cs` (`ArgumentOutOfRangeException` → 400).

- **`Domains/Vehicle.cs` (`VehicleTests`)**: valida o formato real da placa via HTTP (`POST /api/v1/vehicles`), cobrindo os dois formatos aceitos. 13 testes, todos verdes (verificado 2026-08-26).
  - **OK** — formato antigo sem hífen (`ABC1234`), formato antigo com hífen (`XYZ-9876`) e formato Mercosul (`ABC1D23`) são aceitos (201). Validação de **formato** correta, sem bug encontrado aqui.
  - **Erro** (todas corretamente rejeitadas com 400) — letras insuficientes (`AB1234`), letras em excesso (`ABCD1234`), dígitos insuficientes no formato antigo (`ABC123`), começar com números (`1234ABC`), duas letras nas posições de dígito do Mercosul (`ABC1DD3`), placa vazia.
  - **OK** — placa em minúsculo (`low1234`) é aceita e normalizada.
  - **Corrigido em 2026-08-26** — **`Create_should_reject_same_plate_typed_with_different_casing_as_duplicate`**: cadastra a placa `DUP5678` para um cliente, depois tenta cadastrar `dup5678` (a mesma placa física, em minúsculo) para outro cliente — agora corretamente rejeitado como duplicata (409). **Causa raiz**: `VehicleService.CreateAsync`/`UpdateAsync` (`src/Oficina.Application/Clientes/VehicleService.cs`) comparavam a placa **crua** da requisição contra o valor já **normalizado** no banco (`_vehicles.GetByPlateAsync(request.Plate, ...)`). **Fix**: normalizar antes de comparar (`_vehicles.GetByPlateAsync(Vehicle.NormalizePlate(request.Plate), ...)`).
  - **Corrigido em 2026-08-26** — **`Create_should_reject_plate_with_special_characters`** (`"A#B$C%9^9&9*9"`, `"AB C.999.9"`): agora corretamente rejeitadas com 400 (antes eram aceitas com 201). **Causa raiz**: `Vehicle.NormalizePlate` (`src/Oficina.Domain/Vehicles/Vehicle.cs`) removia silenciosamente qualquer caractere não alfanumérico via `Regex.Replace(plate, "[^A-Za-z0-9]", "")` antes da checagem de formato, então símbolos e espaços eram descartados em vez de causar rejeição. **Fix**: `NormalizePlate` agora valida primeiro o conjunto de caracteres (`^[A-Za-z0-9-]+$`, hífen opcional); qualquer outro símbolo faz a placa ser devolvida intocada, e o regex de formato subsequente a rejeita.
  - **OK** (2026-08-27, item 14) — `IdentifyCustomerAndRegister_should_register_vehicle_for_existing_customer`: `POST /api/v1/vehicles/identify-customer-and-register` identifica o cliente pelo documento e cadastra o veículo novo (200).
  - **Erro** (2026-08-27, item 14) — `IdentifyCustomerAndRegister_should_fail_when_document_is_not_found`: documento nunca cadastrado retorna 404 (`VehicleService` lança `KeyNotFoundException`, mapeada pelo `Program.cs`).
  - **Erro** (2026-08-27, item 14) — `IdentifyCustomerAndRegister_should_fail_when_customer_is_inactive`: cliente soft-deletado (`DELETE /api/v1/customers/{id}`) tem o mesmo comportamento de "documento não encontrado" (404).
  - 🔴 **Vermelho de propósito** (2026-08-27, item 25) — `Create_should_reject_category_outside_the_enum_range` (`Theory`: `category=0`, `5`, `-1`): documenta uma lacuna real — `EnumVehicleCategory` não tem nenhuma validação (`[EnumDataType]`/conversor de enum) em `CreateVehicleRequest` nem em `Vehicle.Validate`, então `System.Text.Json` desserializa qualquer inteiro e a API aceita com 201 em vez de rejeitar com 400. **Não implementado de propósito** — só o teste, documentando o gap para uma futura correção (adicionar `[EnumDataType(typeof(EnumVehicleCategory))]` ou validar no domínio).

- **`Domains/Part.cs` (`PartTests`)**: cadastro e atualização de peça via HTTP.
  - **OK** — `POST /api/v1/parts` cria a peça **e** confirma (via `GET /api/v1/stocks`) que um registro de estoque zerado foi criado automaticamente para ela.
  - **Erro** — criar peça com código já usado por outra retorna 409 (`Conflict`).
  - **OK** — `PUT /api/v1/parts/{id}` altera nome/preço com sucesso.

- **`Domains/Mechanic.cs` (`MechanicTests`, 2026-08-27, item 17)**: CRUD via HTTP para `MechanicsController` — antes só existiam testes de Application com fakes.
  - **OK** — `POST /api/v1/mechanics` cria mecânico; `GET /{id}` retorna 404 para inexistente; `PUT /{id}` altera o nome (200) ou retorna 404 para inexistente; `DELETE /{id}` soft-deleta (204) e o mecânico deixa de aparecer em `GET /{id}` (404); `DELETE` de id inexistente retorna 404. Não há checagem de nome duplicado no domínio/serviço, então não há cenário de 409 aqui (diferente de `Part`/`WorkshopService`/`Customer`/`Vehicle`).

- **`Domains/WorkshopService.cs` (`WorkshopServiceTests`, 2026-08-27, item 17)**: CRUD via HTTP para `WorkshopServicesController` — antes só existiam testes de Application com fakes.
  - **OK** — `POST /api/v1/workshop-services` cria serviço; **Erro** — nome duplicado retorna 409; `GET /{id}` retorna 404 para inexistente; `PUT /{id}` altera preço/duração (200) ou retorna 404 para inexistente; `DELETE /{id}` soft-deleta (204) e o serviço deixa de aparecer em `GET /{id}` (404); `DELETE` de id inexistente retorna 404.

- **`Domains/Schedule.cs` (`ScheduleTests`, 2026-08-27, item 15)**: `GET /api/v1/schedules` via HTTP real (a lógica do repositório já era testada em `ServiceOrderRepositoryTests`).
  - **OK** — sem filtro de data, retorna as ordens agendadas nos próximos 30 dias, incluindo uma ordem recém-aberta.
  - **OK** — `?date=YYYY-MM-DD` com a data de hoje inclui a ordem recém-aberta.
  - **Borda** — `?date=2000-01-01` (data sem nenhuma ordem agendada) retorna 404 (`ScheduleController` responde `NotFound("No schedules found.")` quando a lista vem vazia).

- **`Domains/ServiceOrderTracking.cs` (`ServiceOrderTrackingTests`, 2026-08-27, item A / Parte 1.1)**: cobre o desenho confirmado para o futuro endpoint de consulta anônima do cliente — `GET /api/v1/service-orders/{id}/track?document={cpfOuCnpj}`. **O endpoint ainda não existe** (não foi implementado de propósito, só o teste).
  - 🔴 **Vermelho de propósito** — `Track_should_return_order_status_when_document_matches_without_a_token`: espera 200 com o status da OS; hoje recebe 404 porque a rota não existe.
  - **Verde "por coincidência"** — `Track_should_return_not_found_when_document_does_not_match_the_order_customer` e `Track_should_return_not_found_for_unknown_order_id`: ambos esperam 404, que é exatamente o que a rota inexistente retorna hoje para qualquer chamada — passam agora, mas não provam a regra de negócio (não vazar existência da OS); voltam a ser a prova real assim que o endpoint existir.
  - **Verde "por coincidência"** — `Track_should_not_require_an_authorization_header`: confirma que a chamada não é 401; como a rota não existe, isso é trivialmente verdade hoje, mas vira a garantia real de "endpoint anônimo" quando implementado.

- **`Domains/Stock.cs` (`StockTests`)**: movimentação de estoque isolada + o fluxo real de consumo/devolução disparado pela Ordem de Serviço.
  - **OK** — `PUT .../entries` soma quantidade a um estoque zerado; `PUT .../consumptions` reduz quantidade existente; `PUT .../adjustments` substitui o valor absoluto (independente do anterior).
  - **Erro** — `PUT .../consumptions` pedindo mais do que o disponível retorna 400.
  - **OK** — **`Service_order_should_deduct_stock_when_part_is_attached`**: cria peça com 10 em estoque, abre uma OS, anexa 3 unidades da peça (ainda em `InDiagnosis`) e confirma que o estoque cai para 7 **naquele momento** — aprovar, finalizar e entregar a OS depois **não** alteram o estoque de novo (a subtração acontece só quando a peça é anexada via `Update`, não em `approve`/`finalize`/`deliver`).
  - **OK** — **`Service_order_should_return_stock_when_cancelled`**: mesmo cenário, mas cancelando a OS em vez de aprovar — o estoque volta de 7 para 10, confirmando que `CancelAsync` devolve ao estoque as peças já anexadas.
  - **Erro** (2026-08-27, item 10) — **`Service_order_should_reject_increasing_attached_part_quantity_beyond_available_stock`**: anexa 3 unidades (sobram 2 em estoque), depois tenta aumentar a quantidade já anexada para 10 (precisaria de +7, só há 2) — 400.
  - **Borda** (2026-08-27, item 11) — **`Two_orders_disputing_the_same_part_should_reject_the_second_once_stock_is_exhausted`**: OS A anexa as 5 unidades restantes de uma peça; OS B tenta anexar 1 unidade da mesma peça em seguida — 400 (esgotamento sequencial disputado por duas OS).

- **`Domains/Budget.cs` (`BudgetTests`)**: `BudgetsController` só expõe `GET` (lista) e `GET /{id}` — não há rota HTTP para abrir orçamento manualmente (a abertura é sempre automática, disparada pelo `ServiceOrderService`). As chamadas também não enviam token — reflete o comportamento real de hoje, já que `BudgetsController` não tem `[Authorize]`: decisão intencional, pois o cliente precisa consultar seu orçamento sem credencial administrativa.
  - **OK** — `GET /api/v1/budgets` lista um orçamento recém-inserido, sem token.
  - **OK** — `GET /api/v1/budgets/{id}` retorna o orçamento com `TotalValue` calculado corretamente (2 unidades de peça a R$50 + 1 serviço de oficina a R$200 = R$300).
  - **Borda** — `GET /api/v1/budgets/{id}` com id inexistente retorna 404.
  - **OK (corrigido em 2026-08-27 — antes era teste vermelho de propósito)** — **`Service_order_should_open_a_budget_when_it_reaches_awaiting_approval`**: abre OS via HTTP, anexa 2 serviços de oficina de R$400 cada, leva a OS até `AwaitingApproval` (200 OK, status correto) e confirma em `GET /api/v1/budgets` um orçamento vinculado a essa OS com `TotalValue = 800`. **Passa hoje** (reverificado em 2026-08-27, `dotnet test --filter Service_order_should_open_a_budget_when_it_reaches_awaiting_approval`): `ServiceOrderService.UpdateAsync` (`src/Oficina.Application/OrdensServico/ServiceOrderService.cs`) agora chama `IBudgetService.OpenFromServiceOrderAsync` automaticamente na primeira vez que a OS atinge `AwaitingApproval`, e dispara o e-mail de notificação ao cliente (`NotificationService.SendBudgetAwaitingApprovalAsync`).
  - **OK** (2026-08-27, item 18) — **`Service_order_should_actually_dispatch_the_budget_email_when_it_reaches_awaiting_approval`**: mesmo fluxo, mas verificando o e-mail de verdade — `OficinaApiFactory.FakeNotificationEmailSender` (registrado no lugar do sender SMTP real) agora captura cada e-mail "enviado" em uma lista estática consultável pelos testes; o teste confirma que um e-mail com assunto contendo "Budget Awaiting to Approval" e corpo contendo o valor total ("500.00") foi realmente despachado para o e-mail do cliente — não só que `NotificationService` foi chamado corretamente com fakes (já testado na Application).

- **`Domains/ServiceOrderHistory.cs` (`ServiceOrderHistoryTests`)**: confirma que o histórico é gravado corretamente via HTTP ao longo do ciclo de vida completo.
  - **OK** — `GET /api/v1/service-order-history/service-order/{id}` retorna exatamente 6 entradas após o ciclo completo, na ordem cronológica certa: `Received -> InDiagnosis -> AwaitingApproval -> InExecution -> Finalized -> Delivered`. Sem bug encontrado aqui.
  - **Borda** — uma OS recém-aberta (sem nenhuma transição) retorna histórico vazio.
  - **OK** — `GET /api/v1/service-order-history` (sem filtro) também inclui as 6 entradas da OS testada.

> Nota sobre a pasta `Domains/` (`Oficina.Api.ContractTests`): novo padrão de organização por domínio (um arquivo por entidade, ex. `ServiceOrder.cs`, `Customer.cs`, `Vehicle.cs`, `Part.cs`, `Stock.cs`, `Budget.cs`, `ServiceOrderHistory.cs`, `Mechanic.cs`, `WorkshopService.cs`, `Schedule.cs`, `ServiceOrderTracking.cs`, com a classe correspondente sufixada em `Tests`), substituindo a antiga pasta `Contracts/` para testes de fluxo de negócio ponta a ponta. `Contracts/` continua para os testes de infraestrutura transversal (JWT, OpenAPI, rotas administrativas, `AnonymousEndpointsTests`).

> **Atualização 2026-08-27** — implementados os 25 cenários da Parte 2 de `docs/analise-gaps-e-cenarios-faltantes.md` (ver esse documento para o detalhamento item a item, arquivo por arquivo, verde/vermelho). 4 ficaram vermelhos de propósito, documentando gaps reais ainda não implementados (endpoint de consulta anônima da OS — item A/Parte 1.1 — e validação de `category` de veículo fora do enum — item 25).
>
> **Correção 2026-08-28** — o item 20 (rejeitar string só com espaço em branco) foi escrito como `Theory` repetida em 5 entidades (`Customer` nome/e-mail/telefone, `Vehicle` marca/modelo, `Part` nome/código, `WorkshopService` nome/descrição, `ServiceOrder` descrição), e o item 8 ganhou 8 casos extras de fronteira de placa — feedback do time: exaustivo demais, sem atacar regra de negócio nova além da guard clause já coberta. Revertido: item 20 removido por completo (as entidades já tinham teste equivalente usando `" "` como entrada); item 8 reduzido a 2 casos representativos. Suíte final: 375 → **426** testes executados (80→126 em `Oficina.Api.ContractTests`, 295→300 em `Oficina.Tests`), rodando em ~9s no total. 422 verdes, os mesmos 4 vermelhos de propósito de antes.

---

## Resumo de cobertura por camada (referência)

| Camada | Testada por | Observação |
|---|---|---|
| `Oficina.Domain` | `Oficina.Tests/Domain` | ≥90% linhas (2026-08-25) |
| `Oficina.Application` | `Oficina.Tests/Application` | ≥90% linhas (2026-08-25) |
| `Oficina.Infrastructure` (exceto Migrations) | `Oficina.Tests/Infrastructure` | ≥90% linhas (2026-08-25); Migrations excluídas via `[ExcludeFromCodeCoverage]` |
| `Oficina.Api.Controllers` | `Oficina.Api.ContractTests` (parcial, por design) | Excluído da métrica via `[ExcludeFromCodeCoverage]`; lógica de negócio coberta via Application |
| `Oficina.Api.Authentication`/`Configuration` | `Oficina.Tests/Api` | JWT e `DotEnvLoader` cobertos |
