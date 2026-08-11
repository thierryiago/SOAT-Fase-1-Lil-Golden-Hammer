using Oficina.Domain.ServiceOrders;
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

        if (digits.Length == 11)
            return $"{digits.Substring(0, 3)}.{digits.Substring(3, 3)}.{digits.Substring(6, 3)}-{digits.Substring(9, 2)}";

        if (digits.Length == 14)
            return $"{digits.Substring(0, 2)}.{digits.Substring(2, 3)}.{digits.Substring(5, 3)}/{digits.Substring(8, 4)}-{digits.Substring(12, 2)}";

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

    private static bool IsCpf(string cpf)
    {
        int[] multiplicador1 = new int[9] { 10, 9, 8, 7, 6, 5, 4, 3, 2 };
        int[] multiplicador2 = new int[10] { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2 };

        cpf = cpf.Trim().Replace(".", "").Replace("-", "");
        if (cpf.Length != 11)
            return false;

        if (HasRepeatedDigits(cpf))
            return false;

        string tempCpf = cpf.Substring(0, 9);
        int soma = 0;

        for (int i = 0; i < 9; i++)
            soma += int.Parse(tempCpf[i].ToString()) * multiplicador1[i];

        int resto = soma % 11;
        if (resto < 2)
            resto = 0;
        else
            resto = 11 - resto;

        string digito = resto.ToString();
        tempCpf = tempCpf + digito;
        soma = 0;
        for (int i = 0; i < 10; i++)
            soma += int.Parse(tempCpf[i].ToString()) * multiplicador2[i];

        resto = soma % 11;
        if (resto < 2)
            resto = 0;
        else
            resto = 11 - resto;

        digito = digito + resto.ToString();

        return cpf.EndsWith(digito);
    }

    private static bool IsCnpj(string cnpj)
    {
        int[] multiplicador1 = new int[12] { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        int[] multiplicador2 = new int[13] { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

        cnpj = cnpj.Trim().Replace(".", "").Replace("-", "").Replace("/", "");
        if (cnpj.Length != 14)
            return false;

        if (HasRepeatedDigits(cnpj))
            return false;

        string tempCnpj = cnpj.Substring(0, 12);
        int soma = 0;

        for (int i = 0; i < 12; i++)
            soma += int.Parse(tempCnpj[i].ToString()) * multiplicador1[i];

        int resto = (soma % 11);
        if (resto < 2)
            resto = 0;
        else
            resto = 11 - resto;

        string digito = resto.ToString();
        tempCnpj = tempCnpj + digito;
        soma = 0;
        for (int i = 0; i < 13; i++)
            soma += int.Parse(tempCnpj[i].ToString()) * multiplicador2[i];

        resto = (soma % 11);
        if (resto < 2)
            resto = 0;
        else
            resto = 11 - resto;

        digito = digito + resto.ToString();

        return cnpj.EndsWith(digito);
    }

}
