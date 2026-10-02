using Oficina.Domain.Mechanics;

namespace Oficina.Application.Mechanics;

internal static class MechanicResponseMapper
{
    public static MechanicResponse Map(Mechanic mechanic) =>
        new(
            mechanic.Id,
            mechanic.Name);
}
