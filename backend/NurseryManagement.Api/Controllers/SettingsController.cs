using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NurseryManagement.Api.Contracts.Operations;
using NurseryManagement.Api.Security;
using NurseryManagement.Api.Services.Settings;

namespace NurseryManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SettingsController(ISettingsService settingsService) : ControllerBase
{
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
        => Ok(await settingsService.GetAsync(cancellationToken));

    [HttpPut]
    [Authorize(Policy = PermissionPolicies.SettingsWrite)]
    public async Task<IActionResult> Update([FromBody] UpdateSettingsRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await settingsService.UpdateAsync(
                request,
                UserId(),
                User.Identity?.Name,
                Ip(),
                cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("print-layouts")]
    [Authorize(Policy = PermissionPolicies.SettingsWrite)]
    public async Task<IActionResult> UpdatePrintLayouts(
        [FromBody] UpdatePrintLayoutsRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await settingsService.UpdatePrintLayoutsAsync(
                request,
                UserId(),
                User.Identity?.Name,
                Ip(),
                cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private Guid? UserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : null;
    }

    private string? Ip() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
