# Diagrama de classes do banco de dados

O modelo abaixo representa a estrutura relacional recomendada para o PostgreSQL.
Apesar do formato de diagrama de classes, cada classe representa uma tabela.

```mermaid
classDiagram
    direction LR

    class Customer {
        +Guid Id PK
        +string Name
        +string Email
        +string Document UK
        +DateTimeOffset CreatedAt
    }

    class Vehicle {
        +Guid Id PK
        +Guid CustomerId FK
        +string Plate UK
        +string Make
        +string Model
        +int Year
        +DateTimeOffset CreatedAt
    }

    class ServiceOrder {
        +Guid Id PK
        +Guid CustomerId FK
        +Guid VehicleId FK
        +string Description
        +ServiceOrderStatus Status
        +DateTimeOffset CreatedAt
        +DateTimeOffset FinalizedAt
        +DateTimeOffset DeliveredAt
    }

    class ServiceOrderItem {
        +Guid Id PK
        +Guid ServiceOrderId FK
        +Guid PartId FK
        +string PartNameSnapshot
        +decimal UnitPriceSnapshot
        +int Quantity
        +decimal Total
    }

    class Part {
        +Guid Id PK
        +string Name
        +string Code UK
        +decimal UnitPrice
        +int StockQuantity
        +DateTimeOffset CreatedAt
    }

    class InventoryMovement {
        +Guid Id PK
        +Guid PartId FK
        +Guid ServiceOrderId FK
        +InventoryMovementType Type
        +int Quantity
        +decimal UnitPrice
        +DateTimeOffset CreatedAt
    }

    class ServiceOrderStatus {
        <<enumeration>>
        Received
        InDiagnosis
        AwaitingApproval
        InExecution
        Finalized
        Delivered
    }

    class InventoryMovementType {
        <<enumeration>>
        Inbound
        Outbound
        Adjustment
    }

    Customer "1" --> "0..*" Vehicle : owns
    Customer "1" --> "0..*" ServiceOrder : requests
    Vehicle "1" --> "0..*" ServiceOrder : receives service
    ServiceOrder "1" *-- "0..*" ServiceOrderItem : contains
    Part "1" --> "0..*" ServiceOrderItem : used in
    Part "1" --> "0..*" InventoryMovement : has movements
    ServiceOrder "1" --> "0..*" InventoryMovement : generates
    ServiceOrder --> ServiceOrderStatus : has status
    InventoryMovement --> InventoryMovementType : has type
```

## Regras de integridade

- `Customer.Id`, `Vehicle.Id`, `ServiceOrder.Id`, `ServiceOrderItem.Id` e `Part.Id` são chaves primárias.
- `Vehicle.CustomerId` referencia `Customer.Id`.
- `ServiceOrder.VehicleId` referencia `Vehicle.Id`.
- `ServiceOrder.CustomerId` referencia `Customer.Id`.
- O par `(CustomerId, VehicleId)` da ordem deve ser validado para garantir que o veículo pertence ao cliente.
- `ServiceOrderItem.ServiceOrderId` referencia `ServiceOrder.Id` com exclusão em cascata.
- `ServiceOrderItem.PartId` referencia `Part.Id`; a peça não deve ser excluída se já estiver no histórico.
- `Document`, `Plate` e `Code` devem possuir índice único após normalização.
- `UnitPrice`, `UnitPriceSnapshot` e `Total` não podem ser negativos.
- `Quantity` e `StockQuantity` não podem ser negativos; a quantidade de um item deve ser maior que zero.
- A baixa de estoque e a inclusão do item da ordem devem ocorrer na mesma transação.

## Observações de modelagem

`Vehicle` deve ter um identificador próprio. Assim, uma ordem de serviço fica associada ao veículo específico, e não apenas ao proprietário.

`ServiceOrderItem` mantém o nome e o preço da peça no momento da utilização. Isso preserva o histórico mesmo que o cadastro da peça seja alterado posteriormente.

`InventoryMovement` é recomendado para auditoria do estoque. `Part.StockQuantity` pode ser mantido como saldo atual, enquanto os movimentos registram entradas, saídas e ajustes.
