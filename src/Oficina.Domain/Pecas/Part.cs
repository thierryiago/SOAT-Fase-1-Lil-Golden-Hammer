namespace Oficina.Domain.Parts;

public sealed class Part
{
    private Part(
        Guid id,
        string name,
        string code,
        decimal unitPrice,
        int stockQuantity,
        PartKind kind)
    {
        Id = id;
        Name = name;
        Code = code;
        UnitPrice = unitPrice;
        StockQuantity = stockQuantity;
        Kind = kind;
        IsActive = true;
    }

    public Guid Id { get; }
    public string Name { get; private set; }
    public string Code { get; private set; }
    public decimal UnitPrice { get; private set; }
    public int StockQuantity { get; private set; }
    public PartKind Kind { get; private set; }
    public bool IsActive { get; private set; }

    public static Part Create(
        string name,
        string code,
        decimal unitPrice,
        int stockQuantity,
        PartKind kind = PartKind.Part)
    {
        Validate(name, code, unitPrice);

        if (stockQuantity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(stockQuantity), "Stock cannot be negative.");
        }

        return new Part(
            Guid.NewGuid(),
            name.Trim(),
            code.Trim().ToUpperInvariant(),
            unitPrice,
            stockQuantity,
            kind);
    }

    public void Update(string name, string code, decimal unitPrice, PartKind kind)
    {
        Validate(name, code, unitPrice);

        Name = name.Trim();
        Code = code.Trim().ToUpperInvariant();
        UnitPrice = unitPrice;
        Kind = kind;
    }

    public void AdjustStock(int quantity)
    {
        if (quantity == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Stock adjustment cannot be zero.");
        }

        if (StockQuantity + quantity < 0)
        {
            throw new InvalidOperationException("Stock adjustment cannot result in negative stock.");
        }

        StockQuantity += quantity;
    }

    public void WithdrawStock(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        }

        if (quantity > StockQuantity)
        {
            throw new InvalidOperationException("Insufficient stock for the requested part.");
        }

        StockQuantity -= quantity;
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
