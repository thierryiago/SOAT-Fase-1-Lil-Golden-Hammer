namespace Oficina.Domain.Customers;

public sealed class Vehicle
{
    public Vehicle(string plate, string make, string model, int year)
    {
        if (string.IsNullOrWhiteSpace(plate))
        {
            throw new ArgumentException("Vehicle plate is required.", nameof(plate));
        }

        if (string.IsNullOrWhiteSpace(make))
        {
            throw new ArgumentException("Vehicle make is required.", nameof(make));
        }

        if (string.IsNullOrWhiteSpace(model))
        {
            throw new ArgumentException("Vehicle model is required.", nameof(model));
        }

        if (year < 1900 || year > DateTime.UtcNow.Year + 1)
        {
            throw new ArgumentException("Vehicle year is invalid.", nameof(year));
        }

        Plate = plate.Trim().ToUpperInvariant();
        Make = make.Trim();
        Model = model.Trim();
        Year = year;
    }

    public string Plate { get; }
    public string Make { get; }
    public string Model { get; }
    public int Year { get; }
}
