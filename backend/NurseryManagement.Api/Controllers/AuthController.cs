using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NurseryManagement.Api.Contracts.Auth;
using NurseryManagement.Api.Services;

namespace NurseryManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await authService.LoginAsync(request, ip, cancellationToken);
        if (result is null)
        {
            return Unauthorized(new { message = "Invalid username or password" });
        }

        return Ok(result);
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        return Ok(new
        {
            id = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
            userName = User.Identity?.Name,
            roles = User.FindAll(System.Security.Claims.ClaimTypes.Role).Select(c => c.Value).ToArray(),
            permissions = User.FindAll("permission").Select(c => c.Value).ToArray()
        });
    }
}
