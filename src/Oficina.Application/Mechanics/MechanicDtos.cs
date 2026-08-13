namespace Oficina.Application.Mechanics;

public sealed record CreateMechanicRequest(string Name);

public sealed record UpdateMechanicRequest(string Name);

public sealed record MechanicResponse(Guid Id, string Name);
