using Oficina.Domain.Parts;

namespace Oficina.Application.Parts;

public sealed record CreatePartRequest(
    string Name,
    string Code,
    decimal UnitPrice,
    EnumPartKind Kind = EnumPartKind.Part);

public sealed record UpdatePartRequest(
    string Name,
    string Code,
    decimal UnitPrice,
    EnumPartKind Kind);

public sealed record AdjustStockRequest(int Quantity, string Reason);

public sealed record PartResponse(
    Guid Id,
    string Name,
    string Code,
    decimal UnitPrice,
    EnumPartKind Kind);
