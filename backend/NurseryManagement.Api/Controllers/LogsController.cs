using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NurseryManagement.Api.Domain.Entities;
using NurseryManagement.Api.Security;
using NurseryManagement.Api.Services.Logging;

namespace NurseryManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = PermissionPolicies.LogsRead)]
public class LogsController(IAppLogService appLogService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] string? logType,
        [FromQuery] string? module,
        [FromQuery] string? userName,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        AppLogType? parsedType = null;
        if (!string.IsNullOrWhiteSpace(logType) &&
            Enum.TryParse<AppLogType>(logType, true, out var type))
        {
            parsedType = type;
        }

        return Ok(await appLogService.QueryAsync(
            parsedType,
            module,
            userName,
            from,
            to,
            page,
            pageSize,
            cancellationToken));
    }
}
