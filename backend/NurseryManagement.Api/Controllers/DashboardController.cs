using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NurseryManagement.Api.Services.Dashboard;

namespace NurseryManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController(IDashboardService dashboardService) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
        => Ok(await dashboardService.GetSummaryAsync(
            User.IsInRole("Admin"),
            permission => User.HasClaim("permission", permission),
            cancellationToken));
}
