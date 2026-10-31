using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NurseryManagement.Api.Contracts.Operations;
using NurseryManagement.Api.Security;
using NurseryManagement.Api.Services.Finance;

namespace NurseryManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExpensesController(IExpensesService expensesService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionPolicies.ExpensesRead)]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? categoryId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken = default)
        => Ok(await expensesService.GetAllAsync(categoryId, from, to, cancellationToken));

    [HttpGet("summary")]
    [Authorize(Policy = PermissionPolicies.ExpensesRead)]
    public async Task<IActionResult> GetMonthlySummary([FromQuery] int? year, [FromQuery] int? month, CancellationToken cancellationToken = default)
        => Ok(await expensesService.GetMonthlySummaryAsync(year, month, cancellationToken));

    [HttpPost]
    [Authorize(Policy = PermissionPolicies.ExpensesWrite)]
    public async Task<IActionResult> Create([FromBody] ExpenseRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await expensesService.CreateAsync(request, UserId(), UserName(), Ip(), cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = PermissionPolicies.ExpensesWrite)]
    public async Task<IActionResult> Update(Guid id, [FromBody] ExpenseRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await expensesService.UpdateAsync(id, request, UserId(), UserName(), Ip(), cancellationToken);
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
    [Authorize(Policy = PermissionPolicies.ExpensesWrite)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await expensesService.DeleteAsync(id, UserId(), UserName(), Ip(), cancellationToken);
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
