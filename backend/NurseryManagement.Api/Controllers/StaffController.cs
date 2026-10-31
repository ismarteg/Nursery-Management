using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NurseryManagement.Api.Contracts.Operations;
using NurseryManagement.Api.Security;
using NurseryManagement.Api.Services.Staff;

namespace NurseryManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StaffController(IStaffService staffService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionPolicies.StaffRead)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        => Ok(await staffService.GetAllAsync(cancellationToken));

    [HttpPost]
    [Authorize(Policy = PermissionPolicies.StaffWrite)]
    public async Task<IActionResult> Create([FromBody] StaffRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await staffService.CreateAsync(request, UserId(), UserName(), Ip(), cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = PermissionPolicies.StaffWrite)]
    public async Task<IActionResult> Update(Guid id, [FromBody] StaffRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await staffService.UpdateAsync(id, request, UserId(), UserName(), Ip(), cancellationToken);
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

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = PermissionPolicies.StaffWrite)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await staffService.DeleteAsync(id, UserId(), UserName(), Ip(), cancellationToken);
            return NoContent();
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
