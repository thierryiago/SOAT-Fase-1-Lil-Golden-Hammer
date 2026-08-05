namespace Oficina.Domain.Customers;
using System.Text.RegularExpressions;

public sealed class Vehicle
{
    private Vehicle(Guid id, Guid customerId, string plate, string brand, string model, int year)
    {
        Id = id;
        CustomerId = customerId;
        Plate = plate;
        Brand = brand;
        Model = model;
        Year = year;
        IsActive = true;
    }

    public Guid Id { get; }
    public Guid CustomerId { get; }
    public string Plate { get; private set; }
    public string Brand { get; private set; }
    public string Model { get; private set; }
    public int Year { get; private set; }
    public bool IsActive { get; private set; }

    public static Vehicle Create(Guid customerId, string plate, string brand, string model, int year)
    {
        if (customerId == Guid.Empty)
        {
            throw new ArgumentException("Customer is required.", nameof(customerId));
        }

        Validate(plate, brand, model, year);

        return new Vehicle(
            Guid.NewGuid(),
            customerId,
            NormalizePlate(plate),
            brand.Trim(),
            model.Trim(),
            year);
    }

    public void Update(string plate, string brand, string model, int year)
    {
        Validate(plate, brand, model, year);

        Plate = NormalizePlate(plate);
        Brand = brand.Trim();
        Model = model.Trim();
        Year = year;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    private static void Validate(string plate, string brand, string model, int year)
    {
        if (string.IsNullOrWhiteSpace(plate))
        {
            throw new ArgumentException("Vehicle plate is required.", nameof(plate));
        }

        if (string.IsNullOrWhiteSpace(brand))
        {
            throw new ArgumentException("Vehicle brand is required.", nameof(brand));
        }

        if (string.IsNullOrWhiteSpace(model))
        {
            throw new ArgumentException("Vehicle model is required.", nameof(model));
        }

        if (year < 1900 || year > DateTime.UtcNow.Year + 1)
        {
            throw new ArgumentException("Vehicle year is invalid.", nameof(year));
        }
    }

    private static string NormalizePlate(string plate) =>
        plate.Trim().Replace("-", string.Empty).ToUpperInvariant();

    public static bool IsValidPlate(string placa)
    {
        if (string.IsNullOrWhiteSpace(placa))
            return false;

        placa = NormalizePlate(placa);

        return Regex.IsMatch(placa, @"^(?:[A-Z]{3}[0-9]{4}|[A-Z]{3}[0-9][A-Z][0-9]{2})$");
    }
}
