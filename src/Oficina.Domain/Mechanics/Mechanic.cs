namespace Oficina.Domain.Mechanics;

public sealed class Mechanic
{
    private Mechanic(Guid id, string name)
    {
        Id = id;
        Name = name;
        IsActive = true;
    }

    public Guid Id { get; }
    public string Name { get; private set; }
    public bool IsActive { get; private set; }

    public static Mechanic Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Mechanic name is required.", nameof(name));
        }

        return new Mechanic(Guid.NewGuid(), name.Trim());
    }

    public void Update(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Mechanic name is required.", nameof(name));
        }

        Name = name.Trim();
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
