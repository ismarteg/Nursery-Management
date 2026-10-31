using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NurseryManagement.Api.Security;
using NurseryManagement.Api.Services.Payroll;

namespace NurseryManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AttendanceController(IAttendanceService attendanceService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionPolicies.SalariesRead)]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? staffId,
        [FromQuery] int? year,
        [FromQuery] int? month,
        CancellationToken cancellationToken = default)
        => Ok(await attendanceService.GetAllAsync(staffId, year, month, cancellationToken));

    [HttpPost("upload")]
    [Authorize(Policy = PermissionPolicies.SalariesCalculate)]
    [RequestSizeLimit(5_000_000)]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = "CSV file is required" });
        }

        return Ok(await attendanceService.UploadAsync(file, UserId(), UserName(), Ip(), cancellationToken));
    }

    private Guid? UserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : null;
    }

    private string? UserName() => User.Identity?.Name;
    private string? Ip() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
