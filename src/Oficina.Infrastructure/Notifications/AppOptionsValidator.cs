using Microsoft.Extensions.Options;

namespace Oficina.Infrastructure.Notifications;

public sealed class AppOptionsValidator : IValidateOptions<AppOptions>
{
    public ValidateOptionsResult Validate(string? name, AppOptions options)
    {
        var value = options.PublicBaseUrl;
        if (string.IsNullOrWhiteSpace(value) ||
            !string.Equals(value, value.Trim(), StringComparison.Ordinal) ||
            !Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
            string.IsNullOrWhiteSpace(uri.Host) ||
            uri.UserInfo.Length != 0 ||
            uri.AbsolutePath != "/" ||
            uri.Query.Length != 0 ||
            uri.Fragment.Length != 0 ||
            HasPathBeyondOptionalTrailingSlash(value))
        {
            return ValidateOptionsResult.Fail(
                "App:PublicBaseUrl must be an HTTP(S) origin without a path, query, or fragment.");
        }

        return ValidateOptionsResult.Success;
    }

    private static bool HasPathBeyondOptionalTrailingSlash(string value)
    {
        var authorityStart = value.IndexOf("://", StringComparison.Ordinal) + 3;
        var suffixStart = value.IndexOfAny(['/', '?', '#', '\\'], authorityStart);
        return suffixStart >= 0 && value[suffixStart..] != "/";
    }
}
