using Oficina.Domain.Parts;

namespace Oficina.Application.Parts;

internal static class PartResponseMapper
{
    public static PartResponse Map(Part part) =>
        new(
            part.Id,
            part.Name,
            part.Code,
            part.UnitPrice,
            part.Kind);
}
