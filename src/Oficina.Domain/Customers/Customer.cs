using Oficina.Domain.ServiceOrders;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Oficina.Domain.Customers;

public sealed class Customer
{
    private Customer(Guid id, string name, string email, string telephoneNumber, string document)
    {
        Id = id;
        Name = name;
        Email = email;
        TelephoneNumber = telephoneNumber;
        CreateDate = DateTime.UtcNow;
        Document = document;
        IsActive = true;
    }

    public Guid Id { get; }

    public List<Vehicle> Vehicles { get; private set; } = new List<Vehicle>();
    public string Name { get; private set; }
    public string Email { get; private set; }
    public string TelephoneNumber { get; private set; }
    public DateTime CreateDate { get; private set; }
    public string Document { get; private set; }
    public bool IsActive { get; private set; }
    public List<ServiceOrder> ServiceOrders { get; private set; } = new List<ServiceOrder>();

    public static Customer Create(string name, string email, string telephoneNumber, string document)
    {
        Validate(name, email, telephoneNumber, document);

        var normalizedDocument = NormalizeDocument(document);

        if (!IsValidDocument(normalizedDocument))
        {
            throw new ArgumentException("Error validating the provided document. Verify that the CPF or CNPJ is valid.");
        }

        return new Customer(
            Guid.NewGuid(),
            name.Trim(),
            email.Trim().ToLowerInvariant(),
            telephoneNumber.Trim(),
            normalizedDocument);
    }

    public void Update(string name, string email, string telephoneNumber, string document)
    {
        Validate(name, email, telephoneNumber, document);

        var normalizedDocument = NormalizeDocument(document);

        if (!IsValidDocument(normalizedDocument))
        {
            throw new ArgumentException("Error validating the provided document. Verify that the CPF or CNPJ is valid.");
        }

        Name = name.Trim();
        Email = email.Trim().ToLowerInvariant();
        TelephoneNumber = telephoneNumber.Trim();
        Document = normalizedDocument;
    }

    public static void Validate(string name, string email, string telephoneNumber, string document)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Customer name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Customer email is required.", nameof(email));
        }

        if (string.IsNullOrWhiteSpace(telephoneNumber))
        {
            throw new ArgumentException("Customer telephone number is required.", nameof(telephoneNumber));
        }

        if (string.IsNullOrWhiteSpace(document))
        {
            throw new ArgumentException("Customer document is required.", nameof(document));
        }
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public static string NormalizeDocument(string document)
    {
        if (string.IsNullOrWhiteSpace(document))
            return string.Empty;

        var digits = Regex.Replace(document.Trim(), "\\D", string.Empty);

        if(digits.Length == 11 || digits.Length == 14)
            return digits;

        return document.Trim();
    }

    public static bool IsValidDocument(string cpfCnpj)
    {
        if (string.IsNullOrWhiteSpace(cpfCnpj))
        {
            return false;
        }

        var digits = Regex.Replace(cpfCnpj.Trim(), "\\D", string.Empty);
        if (digits.Length == 0)
        {
            return false;
        }

        return !HasRepeatedDigits(digits);
    }

    private static bool HasRepeatedDigits(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return true;

        var digits = value.Trim()
            .Replace(".", string.Empty)
            .Replace("-", string.Empty)
            .Replace("/", string.Empty);

        if (digits.Length == 0)
            return true;

        return digits.All(ch => ch == digits[0]);
    }
}
