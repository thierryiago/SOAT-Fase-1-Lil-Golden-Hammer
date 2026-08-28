using Microsoft.Extensions.Options;
using System.Text;

namespace Oficina.Api.Authentication;

public sealed class JwtOptionsValidator : IValidateOptions<JwtOptions>
{
    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.SigningKey))
        {
            failures.Add("A JWT signing key is required.");
        }
        else if (Encoding.UTF8.GetByteCount(options.SigningKey) < 32)
        {
            failures.Add("The JWT signing key must contain at least 32 UTF-8 bytes.");
        }

        if (string.IsNullOrWhiteSpace(options.Issuer))
        {
            failures.Add("A JWT issuer is required.");
        }

        if (string.IsNullOrWhiteSpace(options.Audience))
        {
            failures.Add("A JWT audience is required.");
        }

        if (options.ExpirationMinutes < 1)
        {
            failures.Add("JWT expiration must be at least one minute.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
