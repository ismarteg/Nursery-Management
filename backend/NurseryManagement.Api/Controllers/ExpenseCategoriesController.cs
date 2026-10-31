using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NurseryManagement.Api.Contracts.Operations;
using NurseryManagement.Api.Security;
using NurseryManagement.Api.Services;
using NurseryManagement.Api.Services.Finance;

namespace NurseryManagement.Api.Controllers;

[ApiController]
[Route("api/expense-categories")]
public class ExpenseCategoriesController(IExpensesService expensesService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionPolicies.ExpensesRead)]
    public async Task<IActionResult> GetAll([FromQuery] bool activeOnly = false, CancellationToken cancellationToken = default)
        => Ok(await expensesService.GetCategoriesAsync(activeOnly, cancellationToken));

    [HttpPost]
    [Authorize(Policy = PermissionPolicies.ExpensesWrite)]
    public async Task<IActionResult> Create([FromBody] ExpenseCategoryRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await expensesService.CreateCategoryAsync(request, UserId(), UserName(), Ip(), cancellationToken));
        }
        catch (ConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = PermissionPolicies.ExpensesWrite)]
    public async Task<IActionResult> Update(Guid id, [FromBody] ExpenseCategoryRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await expensesService.UpdateCategoryAsync(id, request, UserId(), UserName(), Ip(), cancellationToken);
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
    [Authorize(Policy = PermissionPolicies.ExpensesWrite)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var deactivated = await expensesService.DeleteCategoryAsync(id, UserId(), UserName(), Ip(), cancellationToken);
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
