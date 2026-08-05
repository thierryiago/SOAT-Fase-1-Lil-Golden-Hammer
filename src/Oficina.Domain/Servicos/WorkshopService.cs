namespace Oficina.Domain.Services;

public sealed class WorkshopService
{
    private WorkshopService(
        Guid id,
        string name,
        string description,
        decimal unitPrice,
        int estimatedDurationMinutes)
    {
        Id = id;
        Name = name;
        Description = description;
        UnitPrice = unitPrice;
        EstimatedDurationMinutes = estimatedDurationMinutes;
        IsActive = true;
    }

    public Guid Id { get; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public decimal UnitPrice { get; private set; }
    public int EstimatedDurationMinutes { get; private set; }
    public bool IsActive { get; private set; }

    public static WorkshopService Create(
        string name,
        string description,
        decimal unitPrice,
        int estimatedDurationMinutes)
    {
        Validate(name, description, unitPrice, estimatedDurationMinutes);

        return new WorkshopService(
            Guid.NewGuid(),
            name.Trim(),
            description.Trim(),
            unitPrice,
            estimatedDurationMinutes);
    }

    public void Update(
        string name,
        string description,
        decimal unitPrice,
        int estimatedDurationMinutes)
    {
        Validate(name, description, unitPrice, estimatedDurationMinutes);

        Name = name.Trim();
        Description = description.Trim();
        UnitPrice = unitPrice;
        EstimatedDurationMinutes = estimatedDurationMinutes;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    private static void Validate(
        string name,
        string description,
        decimal unitPrice,
        int estimatedDurationMinutes)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Service name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Service description is required.", nameof(description));
        }

        if (unitPrice < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(unitPrice), "Unit price cannot be negative.");
        }

        if (estimatedDurationMinutes <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(estimatedDurationMinutes),
                "Estimated duration must be greater than zero.");
        }
    }
}
