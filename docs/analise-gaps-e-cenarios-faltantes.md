# Análise de Gaps e Cenários de Teste Faltantes

> Gerado em 2026-08-27 a partir da leitura do enunciado do Tech Challenge (Fase 1) e da suíte de testes existente (`tests/Oficina.Tests`, `tests/Oficina.Api.ContractTests`), comparado com `docs/cenarios-de-teste.md` (catálogo do que já está coberto). Este documento tem duas partes: **(1)** o que falta em relação ao enunciado, e **(2)** cenários de teste concretos ainda não escritos, prontos para virar `[Fact]`/`[Theory]`.

---

## Parte 1 — O que falta em relação ao enunciado

### 1.1 Funcional

- **"Permitir consulta por parte do cliente via API para acompanhar o progresso"** — hoje **não existe** nenhum endpoint de consulta de OS que não exija o JWT administrativo (`[Authorize]` em `ServiceOrdersController`). O enunciado distingue claramente "gestão interna" de "cliente acompanha via app/API", sugerindo um canal de consulta mais simples para o cliente (ex.: por CPF/CNPJ + placa, ou por um identificador da OS), sem o mesmo nível de credencial administrativa. Isso é uma lacuna funcional, não só de teste — vale decidir com o time se entra no escopo do MVP ou se documentar como decisão consciente de escopo.
  - ✅ **(2026-08-27) Desenho confirmado e testes escritos (não implementados de propósito)**: `GET /api/v1/service-orders/{id}/track?document={cpfOuCnpj}`, anônimo, identifica pelo nº da OS + CPF/CNPJ. 4 testes em `tests/Oficina.Api.ContractTests/Domains/ServiceOrderTracking.cs` (`ServiceOrderTrackingTests`). 🔴 **1 vermelho de propósito** (`Track_should_return_order_status_when_document_matches_without_a_token`, espera 200 e hoje recebe 404 porque a rota não existe); os outros 3 passam hoje só porque esperam 404/não-401 e a rota inexistente já responde assim para qualquer chamada — deixam de ser "verdes por coincidência" e passam a provar a regra de negócio de verdade assim que o endpoint for implementado.
- **`BudgetsController` e `NotificationsController` sem `[Authorize]`** — **confirmado como decisão intencional (2026-08-27)**: o cliente precisa consultar seu orçamento e receber notificações sem precisar de credencial administrativa. Não é mais tratado como gap; falta apenas registrar essa decisão com um teste de contrato explícito (ver 2.1, itens 1–2) para que não seja "corrigido" por engano no futuro.
- **Validação de documento no DTO é mais fraca que no domínio** (`DocumentValidatorAttribute` só checa tamanho, `Customer.IsValidDocument` roda mod-11 completo) — **avaliado e aceito como está (2026-08-27)**: a validação forte acontece de qualquer forma na entidade antes de persistir, então não é tratado como gap a corrigir.

### 1.2 Qualidade / cobertura

- **Não há gate de cobertura automatizado.** O enunciado pede "cobertura mínima de 80% nos domínios críticos", mas não existe `.github/workflows`, `coverlet.runsettings` com threshold, nem qualquer script de CI no repositório que falhe o build abaixo de 80%. Hoje é só uma meta documental (`docs/Tech_Challenge_Fase1_DDD_C4_Ajustes.md`). Isso é um risco de entrega — o número reportado no relatório final pode não corresponder ao que é reforçado automaticamente.
- **Nenhum relatório de scan de vulnerabilidades encontrado no repositório** (`docs/`, raiz). O enunciado exige explicitamente "análise do scan realizado no código" como parte do relatório de entrega. Vale rodar `dotnet list package --vulnerable` (dependências) e um SAST (ex.: `/security-review` deste próprio Claude Code, ou CodeQL/Snyk) e anexar o resultado a `docs/`.

### 1.3 Documentação (parte não-código do enunciado)

- A pasta `docs/` já tem bastante material (`C4.md`, `linguagem-ubiqua-*.md`, `bounded-contexts-monolito-modular.md`, `diagrama-classes-banco.md`) — parece cobrir boa parte de "Documentação DDD" e "Linguagem Ubíqua". Não foi possível confirmar deste ambiente se o **Event Storming** e os diagramas estão de fato publicados num board Miro (ou equivalente) com link acessível, nem se o **vídeo de até 15 min** e o **documento de entrega em PDF** (nome do grupo, participantes, links, relatório de vulnerabilidades) já existem — esses itens são fora do repositório de código e precisam ser confirmados manualmente pelo time.
- Confirmar que o repositório privado tem acesso concedido ao usuário `soat-architecture`, conforme exigido.

### 1.4 Nota sobre documentação desatualizada — RESOLVIDO (2026-08-27)

- `docs/cenarios-de-teste.md` documentava um teste como "deliberadamente vermelho" (`Service_order_should_open_a_budget_when_it_reaches_awaiting_approval`), afirmando que a abertura automática de orçamento ao atingir `AwaitingApproval` não estava implementada. Reverificado rodando `dotnet test --filter Service_order_should_open_a_budget_when_it_reaches_awaiting_approval`: **o teste passa**. `docs/cenarios-de-teste.md` foi atualizado para refletir isso.

---

## Parte 2 — Cenários de teste ainda não escritos

Convenção: mesma de `docs/cenarios-de-teste.md` — **OK** (fluxo válido), **Erro** (rejeição/exceção), **Borda** (caso limite).

> **Status (2026-08-27): todos os 25 itens desta Parte 2 foram implementados como testes reais.** Cada item abaixo traz o nome do `[Fact]`/`[Theory]`, o arquivo, e se o resultado é 🟢 verde ou 🔴 vermelho de propósito (documentando uma funcionalidade ainda ausente, sem implementá-la — conforme instrução explícita do time).
>
> **Correção 2026-08-28**: a primeira implementação do item 20 e parte do item 8 saiu exaustiva demais (Theory repetida em 5 entidades para o mesmo guard clause de string em branco; 8 casos extras de fronteira de placa) — cortado a pedido do time, sem valor de regra de negócio novo além do que já existia. Item 20 removido por completo; item 8 reduzido a 2 casos. Suíte final: 375 → **426** testes (era 453 antes do corte), 422 verdes, os mesmos 4 vermelhos de propósito. Ver `docs/cenarios-de-teste.md` para o resumo por camada.

### 2.1 Segurança / Autorização

1. ✅ **Erro/Decisão** — `GET /api/v1/budgets` e `GET /api/v1/budgets/{id}` sem token. Implementado como `Budgets_list_should_work_without_a_token_by_design` e `Budget_by_id_should_return_not_found_not_unauthorized_without_a_token`, em `tests/Oficina.Api.ContractTests/Contracts/AnonymousEndpointsTests.cs`. 🟢 Verde — confirma a decisão intencional (sem `[Authorize]`).
2. ✅ **Erro/Decisão** — `POST /api/v1/notifications/email` sem token. Implementado como `Send_email_notification_should_work_without_a_token_by_design`, no mesmo arquivo. 🟢 Verde.
3. ✅ **Erro** — `GET /api/v1/metrics/.../execution-time` sem token → 401. Implementado como `Metrics_route_without_token_should_return_unauthorized`, no mesmo arquivo. 🟢 Verde.
4. ✅ **Erro** — `GET /api/v1/schedules` sem token → 401. Implementado como `Schedules_route_without_token_should_return_unauthorized`, no mesmo arquivo. 🟢 Verde.
5. ✅ **Borda/Documentação** — sem RBAC, qualquer token válido acessa qualquer rota administrativa. Implementado como `Any_valid_token_should_be_able_to_perform_any_administrative_operation` (cria e deleta um mecânico com um token genérico, sem claims de role), no mesmo arquivo. 🟢 Verde.

### 2.2 Validação de dados sensíveis

6. ✅ **Erro** — CPF/CNPJ com dígito verificador matematicamente incorreto, no domínio. Implementado como `Create_should_reject_cpf_with_correct_digit_count_but_wrong_check_digit` e `Create_should_reject_cnpj_with_correct_digit_count_but_wrong_check_digit`, em `tests/Oficina.Tests/Domain/CustomerTests.cs`. 🟢 Verde.
7. ✅ **Erro** — documento com letras → 400 de validação de modelo. Implementado como `Create_should_reject_document_containing_letters`, em `tests/Oficina.Api.ContractTests/Domains/Customer.cs`. 🟢 Verde.
8. ✅ **Erro** — mais combinações de fronteira de placa inválida. Ampliada a `Theory` `IsValidPlate_should_validate_old_and_mercosul_formats` em `tests/Oficina.Tests/Domain/VehicleTests.cs` — **reduzido de 8 para 2 casos em 2026-08-28** (excesso cortado a pedido do time): `ABC12345`, `ABC1DD34`. 🟢 Verde.
9. ✅ **Erro/Borda** — CPF duplicado com formatação diferente → 409. Implementado como `Create_should_reject_same_cpf_typed_with_different_formatting_as_duplicate`, em `tests/Oficina.Api.ContractTests/Domains/Customer.cs`. 🟢 Verde — **não havia bug**: `CreateCustomerRequest.Document` já normaliza (remove máscara) no próprio setter do DTO, antes de `CustomerService.CreateAsync` comparar contra o repositório; diferente do bug já corrigido em `VehicleService` (placa).

### 2.3 Fluxo de negócio via HTTP (contrato) ainda não exercitado

10. ✅ **Erro** — aumentar quantidade de peça já anexada além do estoque disponível → 400 via HTTP. Implementado como `Service_order_should_reject_increasing_attached_part_quantity_beyond_available_stock`, em `tests/Oficina.Api.ContractTests/Domains/Stock.cs`. 🟢 Verde.
11. ✅ **Borda** — duas OS disputando a última unidade de uma peça. Implementado como `Two_orders_disputing_the_same_part_should_reject_the_second_once_stock_is_exhausted`, no mesmo arquivo. 🟢 Verde.
12. ✅ **Erro/Borda** — cancelar a mesma OS duas vezes (idempotência). Implementado como `Cancel_twice_in_a_row_should_fail_the_second_time`, em `tests/Oficina.Api.ContractTests/Domains/ServiceOrder.cs`. 🟢 Verde.
13. ✅ **OK** — ciclo completo de OS sem nenhuma peça, só serviço. Implementado como `Full_lifecycle_without_any_part_should_reach_delivered_and_budget_should_total_services_only`, no mesmo arquivo. 🟢 Verde.
14. ✅ **OK** — `POST /vehicles/identify-customer-and-register` (sucesso, documento não encontrado, cliente inativo). Implementado como `IdentifyCustomerAndRegister_should_register_vehicle_for_existing_customer`, `IdentifyCustomerAndRegister_should_fail_when_document_is_not_found` e `IdentifyCustomerAndRegister_should_fail_when_customer_is_inactive`, em `tests/Oficina.Api.ContractTests/Domains/Vehicle.cs`. 🟢 Verde (os dois casos de erro retornam 404, não um "erro esperado" genérico — `VehicleService` lança `KeyNotFoundException`, mapeada pelo `Program.cs`).
15. ✅ **OK** — `GET /schedules?date=` via HTTP. Implementado como `List_without_date_should_return_orders_scheduled_within_the_next_30_days`, `List_with_todays_date_should_include_the_order_just_opened` e `List_with_a_date_far_in_the_past_should_return_not_found`, em `tests/Oficina.Api.ContractTests/Domains/Schedule.cs` (novo arquivo). 🟢 Verde.
16. ✅ **OK** — `GET /metrics/.../execution-time` fim-a-fim com valores calculados reais. Implementado como `Workshop_service_execution_time_should_reflect_a_real_finalized_order`, em `tests/Oficina.Api.ContractTests/Contracts/MetricsContractTests.cs`. 🟢 Verde (confirma `EstimatedTimeMinutes` e um `AverageTimeMinutes` calculado e não-nulo; não fixa um valor exato de minutos porque o teste roda em tempo real).
17. ✅ **CRUD via HTTP para `MechanicsController` e `WorkshopServicesController`** (create/conflito/404/update/delete). Implementado em dois arquivos novos: `tests/Oficina.Api.ContractTests/Domains/Mechanic.cs` (6 testes — sem cenário de conflito, pois `Mechanic` não tem checagem de nome duplicado) e `tests/Oficina.Api.ContractTests/Domains/WorkshopService.cs` (7 testes, incluindo conflito de nome duplicado → 409). 🟢 Verde.
18. ✅ **OK** — envio real do e-mail de orçamento verificado via contrato. Implementado como `Service_order_should_actually_dispatch_the_budget_email_when_it_reaches_awaiting_approval`, em `tests/Oficina.Api.ContractTests/Domains/Budget.cs`; `OficinaApiFactory.FakeNotificationEmailSender` (`tests/Oficina.Api.ContractTests/Infrastructure/OficinaApiFactory.cs`) foi tornado público e passou a capturar os e-mails "enviados" numa lista estática consultável pelos testes. 🟢 Verde.
19. ✅ **Erro/Borda** — paginação inválida via HTTP. Implementado como `List_should_reject_invalid_pagination_parameters` (`Theory`: `page=0`, `pageSize=0`, `pageSize=101`), em `tests/Oficina.Api.ContractTests/Domains/Customer.cs`. 🟢 Verde.

### 2.4 Regras de domínio adicionais

20. ❌ **Revertido em 2026-08-28** — string só com espaço em branco. Foi implementado como `Theory` repetida em 5 entidades (`CustomerTests.cs`, `VehicleTests.cs`, `PartTests.cs`, `WorkshopServiceTests.cs`, `ServiceOrderTests.cs`), mas cada uma testava a mesma guard clause genérica (`IsNullOrWhiteSpace`) sem revelar nenhuma regra nova — todas essas entidades já tinham teste equivalente usando `" "` como entrada. Removido a pedido do time por não agregar valor além do que já existia; sem substituto.
21. ✅ **Erro** — `Vehicle.Create` com `AnoAtual + 2`. **Já estava coberto com precisão** pelos testes existentes `Create_should_accept_year_one_year_ahead` (`+1`, aceito) e `Create_should_reject_year_too_far_in_the_future` (`+2`, rejeitado) em `tests/Oficina.Tests/Domain/VehicleTests.cs` — confirmado e documentado com um comentário no arquivo; nenhum teste novo foi necessário. 🟢 Verde.
22. ✅ **Borda** — `Budget.Open` com `parts` vazia não-nula vs. `null`. Implementado como `Open_should_calculate_the_same_total_value_for_null_and_empty_parts_list`, em `tests/Oficina.Tests/Domain/BudgetTests.cs`. 🟢 Verde — mesmo `TotalValue` e `Parts` vazio nos dois casos, sem bug.
23. ✅ **Borda** — mecânico desativado depois de atribuído a OS em andamento. Implementado como `Order_should_keep_working_after_its_assigned_mechanic_is_deactivated` (teste de caracterização), em `tests/Oficina.Api.ContractTests/Domains/ServiceOrder.cs`. 🟢 Verde — **comportamento documentado**: nada bloqueia; a OS continua funcionando normalmente com o mecânico desativado ainda atribuído (nenhuma checagem de `Mechanic.IsActive` em `ServiceOrderService`/`ValidateUpdate`).
24. ✅ **Borda** — `Part`/`WorkshopService` desativada depois de anexada a OS aberta. Implementado como `Order_and_budget_should_stay_consistent_after_an_attached_workshop_service_is_deactivated` e `Order_and_budget_should_stay_consistent_after_an_attached_part_is_deactivated` (testes de caracterização), no mesmo arquivo. 🟢 Verde — o item some do catálogo ativo (404 em `GET` por id), mas a OS e o orçamento continuam consistentes/referenciando o item.
25. ✅ **Erro** — `category` de veículo fora do enum via HTTP. Implementado como `Create_should_reject_category_outside_the_enum_range` (`Theory`: `0`, `5`, `-1`), em `tests/Oficina.Api.ContractTests/Domains/Vehicle.cs`. 🔴 **Vermelho de propósito** — documenta uma lacuna real: não há `[EnumDataType]`/validação de enum em `CreateVehicleRequest` nem em `Vehicle.Validate`, então a API aceita (201) valores fora do intervalo de `EnumVehicleCategory` em vez de rejeitar (400). Não implementado de propósito (só o teste, por instrução explícita de não corrigir gaps agora).

---

## Como priorizar

Para o prazo do Tech Challenge, a ordem de valor sugerida é:
1. **2.1 (itens 1–4)** — fecha a exigência explícita de "JWT para APIs administrativas" com evidência de teste, rápido de escrever.
2. **2.3 (itens 10, 13, 17)** — fecha o maior buraco real de cobertura (CRUDs administrativos sem teste HTTP) e valida a regra de negócio mais sensível (estoque).
3. **2.2 (item 6)** — um `[Fact]` de 5 minutos que fecha a exigência explícita "validação de CPF/CNPJ" no nível de domínio, espelhando o que já foi corrigido no nível de contrato.
4. Os demais (2.4 e o restante de 2.3) são bons candidatos a issues separadas — endurecem o domínio mas não bloqueiam a entrega da Fase 1.

Itens da **Parte 1** (cobertura de CI, relatório de scan, endpoint público de consulta do cliente) não são cenários de teste — são decisões/trabalho que precisam existir *antes* de esses testes fazerem sentido como gate de qualidade.
