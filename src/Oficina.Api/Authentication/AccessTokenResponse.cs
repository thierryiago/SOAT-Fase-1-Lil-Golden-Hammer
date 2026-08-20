namespace Oficina.Api.Authentication;

public sealed record AccessTokenResponse(
    string AccessToken,
    string TokenType,
    DateTimeOffset ExpiresAt);
