using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NurseryManagement.Api.Contracts.Operations;
using NurseryManagement.Api.Security;
using NurseryManagement.Api.Services.Finance;

namespace NurseryManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController(IPaymentsService paymentsService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionPolicies.PaymentsRead)]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? childId,
        [FromQuery] string? status,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken = default)
        => Ok(await paymentsService.GetAllAsync(childId, status, from, to, cancellationToken));

    [HttpGet("receivables")]
    [Authorize(Policy = PermissionPolicies.PaymentsRead)]
    public async Task<IActionResult> GetReceivables(CancellationToken cancellationToken)
        => Ok(await paymentsService.GetReceivablesAsync(cancellationToken));

    [HttpPost]
    [Authorize(Policy = PermissionPolicies.PaymentsWrite)]
    public async Task<IActionResult> Create([FromBody] PaymentRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await paymentsService.CreateAsync(request, UserId(), UserName(), Ip(), cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/pay")]
    [Authorize(Policy = PermissionPolicies.PaymentsWrite)]
    public async Task<IActionResult> RecordPayment(Guid id, [FromBody] RecordPaymentRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await paymentsService.RecordPaymentAsync(id, request, UserId(), UserName(), Ip(), cancellationToken));
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
    [Authorize(Policy = PermissionPolicies.PaymentsWrite)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await paymentsService.CancelAsync(id, UserId(), UserName(), Ip(), cancellationToken);
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
