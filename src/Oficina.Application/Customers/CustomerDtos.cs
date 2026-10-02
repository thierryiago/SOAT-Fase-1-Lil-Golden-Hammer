using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Oficina.Application.Customers;

public sealed record CreateCustomerRequest(string Name, string Email, string TelephoneNumber)
{
    private string _document = string.Empty;

    [DocumentValidator]
    public string Document
    {
        get => _document;
        set => _document = NormalizeDocument(value ?? string.Empty);
    }

    private string NormalizeDocument(string document)
    {
        return Regex.Replace(document, @"[^\d]", "");
    }
};

public sealed record UpdateCustomerRequest(string Name, string Email, string TelephoneNumber, [DocumentValidator] string Document);

public sealed record CustomerResponse(
    Guid Id,
    string Name,
    string Email,
    string TelephoneNumber,
    string Document
    );


public class DocumentValidatorAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string document)
        {
            return new ValidationResult("Document must be a string.");
        }
        var normalizedDocument = NormalizeDocument(document);
        if (!IsValidDocument(normalizedDocument))
        {
            return new ValidationResult("Error validating the provided document. Verify that the CPF or CNPJ is valid.");
        }

        return ValidationResult.Success;
    }
    public static string NormalizeDocument(string document)
    {
        return Regex.Replace(document, @"[^\d]", "");
    }
    private static bool IsValidDocument(string document)
    {
        return document.Length == 11 || document.Length == 14;
    }
}