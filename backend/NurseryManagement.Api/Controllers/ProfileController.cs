using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NurseryManagement.Api.Contracts.Users;
using NurseryManagement.Api.Services.SecurityAdmin;

namespace NurseryManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfileController(IProfileService profileService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var profile = await profileService.GetAsync(userId.Value, cancellationToken);
        return profile is null ? NotFound() : Ok(profile);
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        try
        {
            await profileService.ChangePasswordAsync(
                userId.Value,
                request,
                User.Identity?.Name,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private Guid? CurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : null;
    }
}
