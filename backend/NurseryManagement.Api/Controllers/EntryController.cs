using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NurseryManagement.Api.Contracts.Operations;
using NurseryManagement.Api.Security;
using NurseryManagement.Api.Services.Entry;

namespace NurseryManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EntryController(IEntryService entryService) : ControllerBase
{
    [HttpPost("scan")]
    [Authorize(Policy = PermissionPolicies.EntryScan)]
    public async Task<IActionResult> Scan([FromBody] ScanEntryRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await entryService.ScanAsync(request, UserId(), UserName(), Ip(), cancellationToken));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet]
    [Authorize(Policy = PermissionPolicies.EntryScan)]
    public async Task<IActionResult> GetHistory(
        [FromQuery] Guid? childId,
        [FromQuery] Guid? classId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? type,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
        => Ok(await entryService.GetHistoryAsync(childId, classId, from, to, type, page, pageSize, cancellationToken));

    [HttpGet("present")]
    [Authorize(Policy = PermissionPolicies.EntryScan)]
    public async Task<IActionResult> GetPresent(CancellationToken cancellationToken)
        => Ok(await entryService.GetPresentAsync(cancellationToken));

    private Guid? UserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : null;
    }

    private string? UserName() => User.Identity?.Name;
    private string? Ip() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
