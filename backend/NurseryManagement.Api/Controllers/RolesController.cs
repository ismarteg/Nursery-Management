using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NurseryManagement.Api.Contracts.Roles;
using NurseryManagement.Api.Security;
using NurseryManagement.Api.Services;
using NurseryManagement.Api.Services.SecurityAdmin;

namespace NurseryManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = PermissionPolicies.RolesManage)]
public class RolesController(IRolesService rolesService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        => Ok(await rolesService.GetAllAsync(cancellationToken));

    [HttpGet("permissions")]
    public async Task<IActionResult> GetPermissions(CancellationToken cancellationToken)
        => Ok(await rolesService.GetPermissionsAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var role = await rolesService.GetByIdAsync(id, cancellationToken);
        return role is null ? NotFound() : Ok(role);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRoleRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var id = await rolesService.CreateAsync(request, UserId(), UserName(), Ip(), cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id }, new { Id = id });
        }
        catch (ConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRoleRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await rolesService.UpdateAsync(id, request, UserId(), UserName(), Ip(), cancellationToken);
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
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await rolesService.DeleteAsync(id, UserId(), UserName(), Ip(), cancellationToken);
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

    private Guid? UserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : null;
    }

    private string? UserName() => User.Identity?.Name;
    private string? Ip() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
