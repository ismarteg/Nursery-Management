using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NurseryManagement.Api.Contracts.Operations;
using NurseryManagement.Api.Security;
using NurseryManagement.Api.Services;
using NurseryManagement.Api.Services.Classes;

namespace NurseryManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClassesController(IClassesService classesService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionPolicies.ClassesRead)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        => Ok(await classesService.GetAllAsync(cancellationToken));

    [HttpPost]
    [Authorize(Policy = PermissionPolicies.ClassesWrite)]
    public async Task<IActionResult> Create([FromBody] ClassRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await classesService.CreateAsync(request, UserId(), UserName(), Ip(), cancellationToken));
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
    [Authorize(Policy = PermissionPolicies.ClassesWrite)]
    public async Task<IActionResult> Update(Guid id, [FromBody] ClassRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await classesService.UpdateAsync(id, request, UserId(), UserName(), Ip(), cancellationToken);
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
    [Authorize(Policy = PermissionPolicies.ClassesWrite)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await classesService.DeleteAsync(id, UserId(), UserName(), Ip(), cancellationToken);
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
