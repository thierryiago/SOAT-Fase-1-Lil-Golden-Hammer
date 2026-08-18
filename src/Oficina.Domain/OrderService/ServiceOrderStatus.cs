namespace Oficina.Domain.ServiceOrders;

public enum ServiceOrderStatus
{
    Received = 1,
    InDiagnosis = 2,
    AwaitingApproval = 3,
    InExecution = 4,
    Finalized = 5,
    Delivered = 6,
    Rejected = 7
}
