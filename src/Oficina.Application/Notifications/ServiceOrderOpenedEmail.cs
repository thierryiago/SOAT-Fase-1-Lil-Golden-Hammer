using Oficina.Application.Budgets;

namespace Oficina.Application.Notifications;

public sealed record ServiceOrderOpenedEmail(
    Guid ServiceOrderId,
    string Description,
    DateTimeOffset CreatedAt,
    string CustomerName,
    string CustomerDocument,
    string CustomerEmail,
    string CustomerTelephoneNumber,
    string VehiclePlate,
    string VehicleBrand,
    string VehicleModel,
    int VehicleYear,
    BudgetResponse Budget);
