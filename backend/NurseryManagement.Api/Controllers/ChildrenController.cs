using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NurseryManagement.Api.Contracts.Operations;
using NurseryManagement.Api.Security;
using NurseryManagement.Api.Services.Children;

namespace NurseryManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChildrenController(IChildrenService childrenService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionPolicies.ChildrenRead)]
    public async Task<IActionResult> GetAll([FromQuery] Guid? classId, CancellationToken cancellationToken)
        => Ok(await childrenService.GetAllAsync(classId, cancellationToken));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = PermissionPolicies.ChildrenRead)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var child = await childrenService.GetByIdAsync(id, cancellationToken);
        return child is null ? NotFound() : Ok(child);
    }

    [HttpPost]
    [Authorize(Policy = PermissionPolicies.ChildrenWrite)]
    public async Task<IActionResult> Create([FromBody] ChildRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var created = await childrenService.CreateAsync(request, UserId(), UserName(), Ip(), cancellationToken);
            return Ok(created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = PermissionPolicies.ChildrenWrite)]
    public async Task<IActionResult> Update(Guid id, [FromBody] ChildRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await childrenService.UpdateAsync(id, request, UserId(), UserName(), Ip(), cancellationToken);
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
    [Authorize(Policy = PermissionPolicies.ChildrenWrite)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await childrenService.DeleteAsync(id, UserId(), UserName(), Ip(), cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("{id:guid}/photo")]
    [Authorize(Policy = PermissionPolicies.ChildrenWrite)]
    [RequestSizeLimit(2_500_000)]
    public async Task<IActionResult> UploadPhoto(Guid id, IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = "Photo file is required" });
        }

        try
        {
            return Ok(await childrenService.UploadChildPhotoAsync(id, file, UserId(), UserName(), Ip(), cancellationToken));
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

    [HttpPost("{id:guid}/parents/{parentRole}/photo")]
    [Authorize(Policy = PermissionPolicies.ChildrenWrite)]
    [RequestSizeLimit(2_500_000)]
    public async Task<IActionResult> UploadParentPhoto(Guid id, string parentRole, IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = "Photo file is required" });
        }

        try
        {
            return Ok(await childrenService.UploadParentPhotoAsync(id, parentRole, file, UserId(), UserName(), Ip(), cancellationToken));
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

    [HttpPost("{id:guid}/entry-code/generate")]
    [Authorize(Policy = PermissionPolicies.ChildrenWrite)]
    public async Task<IActionResult> GenerateEntryCode(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await childrenService.GenerateEntryCodeAsync(id, UserId(), UserName(), Ip(), cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPut("{id:guid}/entry-code")]
    [Authorize(Policy = PermissionPolicies.ChildrenWrite)]
    public async Task<IActionResult> SetEntryCode(Guid id, [FromBody] SetEntryCodeRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await childrenService.SetEntryCodeAsync(id, request.Code, UserId(), UserName(), Ip(), cancellationToken));
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

    [HttpDelete("{id:guid}/entry-code")]
    [Authorize(Policy = PermissionPolicies.ChildrenWrite)]
    public async Task<IActionResult> ClearEntryCode(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await childrenService.ClearEntryCodeAsync(id, UserId(), UserName(), Ip(), cancellationToken);
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
