using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Api.Authentication;

namespace Oficina.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(IAccessTokenGenerator tokens) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("token", Name = "IssueTechnicalToken")]
    [ProducesResponseType(typeof(AccessTokenResponse), StatusCodes.Status200OK)]
    public ActionResult<AccessTokenResponse> Issue() => Ok(tokens.Generate());
}
