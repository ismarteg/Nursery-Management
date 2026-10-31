using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NurseryManagement.Api.Contracts.Operations;
using NurseryManagement.Api.Security;
using NurseryManagement.Api.Services;
using NurseryManagement.Api.Services.Finance;

namespace NurseryManagement.Api.Controllers;

[ApiController]
[Route("api/price-plans")]
public class PricePlansController(IPricePlansService pricePlansService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionPolicies.PricePlansList)]
    public async Task<IActionResult> GetAll([FromQuery] bool activeOnly = false, CancellationToken cancellationToken = default)
        => Ok(await pricePlansService.GetAllAsync(activeOnly, cancellationToken));

    [HttpPost]
    [Authorize(Policy = PermissionPolicies.PricePlansWrite)]
    public async Task<IActionResult> Create([FromBody] PricePlanRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await pricePlansService.CreateAsync(request, UserId(), UserName(), Ip(), cancellationToken));
        }
        catch (ConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = PermissionPolicies.PricePlansWrite)]
    public async Task<IActionResult> Update(Guid id, [FromBody] PricePlanRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await pricePlansService.UpdateAsync(id, request, UserId(), UserName(), Ip(), cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (ConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = PermissionPolicies.PricePlansWrite)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var deactivated = await pricePlansService.DeleteAsync(id, UserId(), UserName(), Ip(), cancellationToken);
            return deactivated ? Ok(new { deactivated = true }) : NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
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
