namespace Oficina.Application.Services;

public sealed record CreateWorkshopServiceRequest(
    string Name,
    string Description,
    decimal UnitPrice,
    int EstimatedDurationMinutes);

public sealed record UpdateWorkshopServiceRequest(
    string Name,
    string Description,
    decimal UnitPrice,
    int EstimatedDurationMinutes);

public sealed record WorkshopServiceResponse(
    Guid Id,
    string Name,
    string Description,
    decimal UnitPrice,
    int EstimatedDurationMinutes);
