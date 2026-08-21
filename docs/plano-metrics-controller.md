# Plano de desenvolvimento — MetricsController

## Objetivo

Criar a controller `MetricsController` com o endpoint `GET /api/v1/metrics/workshop-service/execution-time` para consultar o tempo médio de execução dos serviços cadastrados.

O endpoint deverá listar todos os `WorkshopServices`, inclusive os que ainda não foram executados em uma ordem de serviço.

## Contrato da resposta

Criar uma DTO com os seguintes atributos:

```csharp
public sealed record WorkshopServiceExecutionTimeResponse(
    Guid Id,
    string Name,
    int EstimatedTimeMinutes,
    decimal? AverageTimeMinutes);
```

Mapeamento dos atributos:

| Atributo da DTO | Origem |
| --- | --- |
| `Id` | `WorkshopService.Id` |
| `Name` | `WorkshopService.Name` |
| `EstimatedTimeMinutes` | `WorkshopService.EstimatedDurationMinutes` |
| `AverageTimeMinutes` | Média simples dos minutos efetivos de execução das ordens finalizadas associadas ao serviço |

Quando não existir uma execução finalizada válida para um serviço, `AverageTimeMinutes` deverá ser `null`.

## Etapas de implementação

1. Criar a camada de aplicação `Oficina.Application.Metrics`.

   - Adicionar a DTO `WorkshopServiceExecutionTimeResponse`.
   - Criar `MetricsService` para orquestrar a consulta e aplicar a regra de cálculo.
   - Criar uma interface de leitura específica, como `IWorkshopServiceExecutionTimeRepository`.

2. Implementar o repositório de leitura na infraestrutura.

   - Criar `WorkshopServiceExecutionTimeRepository` usando `AppDbContext`.
   - Consultar `WorkshopServices`, `ServiceOrderWorkshops` e `ServiceOrderHistories`.
   - Preservar serviços sem ordens associadas na listagem.
   - Corrigir `ServiceOrderHistoryRepository.FindByServiceOrderAsync`, cujo filtro deve usar `historic.OrderServiceId` em vez de `historic.Id`.

3. Calcular o tempo efetivo de cada ordem de serviço.

   - Considerar somente ordens que possuam um histórico `Finalized`.
   - Para cada ordem vinculada ao serviço, encontrar o último histórico `InExecution` anterior ao `Finalized`.
   - Calcular `Finalized.CreatedDate - InExecution.CreatedDate` e converter o resultado para minutos.
   - Ignorar ordens sem um par válido de históricos.
   - Para cada serviço, calcular a média aritmética simples das durações válidas.
   - Comparar os status por `nameof(ServiceOrderStatus.InExecution)` e `nameof(ServiceOrderStatus.Finalized)`.

4. Expor a API.

   - Criar `MetricsController` com a rota base `api/v1/metrics`.
   - Implementar a action `GET workshop-service/execution-time`.
   - Declarar o retorno `200 OK` com a DTO criada.
   - Registrar o serviço e o repositório nos módulos de injeção de dependência de Application e Infrastructure.

5. Testar e documentar.

   - Criar testes unitários para média de uma e múltiplas ordens, serviços sem execução finalizada e históricos inválidos.
   - Criar teste de contrato para `GET /api/v1/metrics/workshop-service/execution-time`.
   - Incluir `metrics` no teste de rotas administrativas, se aplicável.
   - Atualizar `oficina.http` e o README com o novo endpoint.
   - Executar build e todas as suítes de teste.

## Regra para múltiplos ciclos de execução

Caso uma ordem retorne para execução mais de uma vez, o cálculo deverá usar o último evento `InExecution` anterior ao evento `Finalized`. Dessa forma, o intervalo representa o ciclo final que levou à conclusão da ordem.
