namespace Oficina.Api.Authentication;

public interface IAccessTokenGenerator
{
    AccessTokenResponse Generate();
}
