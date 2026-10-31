using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NurseryManagement.Api.Contracts.Operations;
using NurseryManagement.Api.Security;
using NurseryManagement.Api.Services.Payroll;

namespace NurseryManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SalariesController(ISalariesService salariesService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionPolicies.SalariesRead)]
    public async Task<IActionResult> GetAll([FromQuery] int? year, [FromQuery] int? month, CancellationToken cancellationToken = default)
        => Ok(await salariesService.GetAllAsync(year, month, cancellationToken));

    [HttpPost("calculate")]
    [Authorize(Policy = PermissionPolicies.SalariesCalculate)]
    public async Task<IActionResult> Calculate([FromBody] CalculateSalariesRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await salariesService.CalculateAsync(request, UserId(), UserName(), Ip(), cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/mark-paid")]
    [Authorize(Policy = PermissionPolicies.SalariesCalculate)]
    public async Task<IActionResult> MarkPaid(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await salariesService.MarkPaidAsync(id, cancellationToken));
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

    private Guid? UserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : null;
    }

    private string? UserName() => User.Identity?.Name;
    private string? Ip() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
