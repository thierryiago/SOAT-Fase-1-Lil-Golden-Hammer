using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Api.Authentication;
using System.Diagnostics.CodeAnalysis;

namespace Oficina.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
[ExcludeFromCodeCoverage]
public sealed class AuthController(IAccessTokenGenerator tokens) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("token", Name = "IssueTechnicalToken")]
    [ProducesResponseType(typeof(AccessTokenResponse), StatusCodes.Status200OK)]
    public ActionResult<AccessTokenResponse> Issue() => Ok(tokens.Generate());
}
