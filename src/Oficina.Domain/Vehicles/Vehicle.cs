namespace Oficina.Domain.Customers;

using Oficina.Domain.Vehicles;
using System.Text.RegularExpressions;

public sealed class Vehicle
{
    private Vehicle(Guid id, Guid customerId, string plate, string brand, string model, int year, EnumVehicleCategory category)
    {
        Id = id;
        CustomerId = customerId;
        Plate = plate;
        Brand = brand;
        Model = model;
        Year = year;
        Category = category;
        IsActive = true;
    }

    public Guid Id { get; }
    public Guid CustomerId { get; }
    public Customer Customer { get; private set; } = null!;
    public string Plate { get; private set; }
    public string Brand { get; private set; }
    public string Model { get; private set; }
    public int Year { get; private set; }

    public EnumVehicleCategory Category { get; private set; }
    public bool IsActive { get; private set; }

    public static Vehicle Create(Guid customerId, string plate, string brand, string model, int year, EnumVehicleCategory category)
    {
        if (customerId == Guid.Empty)
        {
            throw new ArgumentException("Customer is required.", nameof(customerId));
        }

        Validate(plate, brand, model, year, category);

        if (!IsValidPlate(plate))
        {
            throw new ArgumentException("Error validating the provided plate. Verify that the plate is valid.");
        }

        return new Vehicle(
            Guid.NewGuid(),
            customerId,
            NormalizePlate(plate),
            brand.Trim(),
            model.Trim(),
            year,
            category);
    }

    public void Update(string plate, string brand, string model, int year, EnumVehicleCategory category)
    {
        Validate(plate, brand, model, year, category);

        if (!IsValidPlate(plate))
        {
            throw new ArgumentException("Error validating the provided plate. Verify that the plate is valid.");
        }

        Plate = NormalizePlate(plate);
        Brand = brand.Trim();
        Model = model.Trim();
        Year = year;
        Category = category;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    private static void Validate(string plate, string brand, string model, int year, EnumVehicleCategory category)
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

    public static string NormalizePlate(string plate)
    {
        if (string.IsNullOrWhiteSpace(plate))
            return string.Empty;

        var trimmed = plate.Trim();
        if (!Regex.IsMatch(trimmed, @"^[A-Za-z0-9-]+$"))
        {
            return trimmed;
        }

        var normalized = trimmed.Replace("-", string.Empty).ToUpperInvariant();

        if (Regex.IsMatch(normalized, @"^[A-Z]{3}[0-9]{4}$"))
            return $"{normalized.Substring(0, 3)}-{normalized.Substring(3, 4)}";

        if (Regex.IsMatch(normalized, @"^[A-Z]{3}[0-9][A-Z][0-9]{2}$"))
            return normalized;

        return normalized;
    }

    public static bool IsValidPlate(string plate)
    {
        if (string.IsNullOrWhiteSpace(plate))
            return false;

        plate = NormalizePlate(plate);

        return Regex.IsMatch(plate, @"^(?:[A-Z]{3}[0-9]{4}|[A-Z]{3}[0-9][A-Z][0-9]{2}|[A-Z]{3}-[0-9]{4})$");
    }
}
