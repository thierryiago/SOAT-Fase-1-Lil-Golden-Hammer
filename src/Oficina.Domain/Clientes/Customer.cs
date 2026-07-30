namespace Oficina.Domain.Customers;

public sealed class Customer
{
    private readonly List<Vehicle> _vehicles = new();

    private Customer(Guid id, string name, string email, string document)
    {
        Id = id;
        Name = name;
        Email = email;
        Document = document;
    }

    public Guid Id { get; }
    public string Name { get; private set; }
    public string Email { get; private set; }
    public string Document { get; private set; }
    public IReadOnlyCollection<Vehicle> Vehicles => _vehicles.AsReadOnly();

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

    public Vehicle RegisterVehicle(string plate, string make, string model, int year)
    {
        var vehicle = new Vehicle(plate, make, model, year);
        _vehicles.Add(vehicle);
        return vehicle;
    }
}
