namespace Oficina.Domain.Customers;

public sealed class Customer
{
    private Customer(Guid id, string name, string email, string document)
    {
        Id = id;
        Name = name;
        Email = email;
        Document = document;
        IsActive = true;
    }

    public Guid Id { get; }
    public string Name { get; private set; }
    public string Email { get; private set; }
    public string Document { get; private set; }
    public bool IsActive { get; private set; }

    public static Customer Create(string name, string email, string document)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Customer name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Customer email is required.", nameof(email));
        }

        if (string.IsNullOrWhiteSpace(document))
        {
            throw new ArgumentException("Customer document is required.", nameof(document));
        }

        return new Customer(
            Guid.NewGuid(),
            name.Trim(),
            email.Trim().ToLowerInvariant(),
            document.Trim());
    }

    public void Update(string name, string email, string document)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Customer name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Customer email is required.", nameof(email));
        }

        if (string.IsNullOrWhiteSpace(document))
        {
            throw new ArgumentException("Customer document is required.", nameof(document));
        }

        Name = name.Trim();
        Email = email.Trim().ToLowerInvariant();
        Document = document.Trim();
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
