namespace Oficina.Domain.Parts;

public sealed class Part
{
    private Part(
        Guid id,
        string name,
        string code,
        decimal unitPrice,
        EnumPartKind kind)
    {
        Id = id;
        Name = name;
        Code = code;
        UnitPrice = unitPrice;
        Kind = kind;
        CreateDate = DateTime.UtcNow;
        IsActive = true;
    }

    public Guid Id { get; }
    public string Name { get; private set; }
    public string Code { get; private set; }
    public decimal UnitPrice { get; private set; }
    public EnumPartKind Kind { get; private set; }
    public DateTime CreateDate { get; private set; }
    public DateTime UpdateDate { get; private set; }
    public bool IsActive { get; private set; }

    public static Part Create(
        string name,
        string code,
        decimal unitPrice,
        EnumPartKind kind = EnumPartKind.Part)
    {
        Validate(name, code, unitPrice);

        return new Part(
            Guid.NewGuid(),
            name.Trim(),
            code.Trim().ToUpperInvariant(),
            unitPrice,
            kind);
    }

    public void Update(string name, string code, decimal unitPrice, EnumPartKind kind)
    {
        Validate(name, code, unitPrice);

        Name = name.Trim();
        Code = code.Trim().ToUpperInvariant();
        UnitPrice = unitPrice;
        Kind = kind;
        UpdateDate = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    private static void Validate(string name, string code, decimal unitPrice)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Part name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Part code is required.", nameof(code));
        }

        if (unitPrice < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(unitPrice), "Unit price cannot be negative.");
        }
    }
}
