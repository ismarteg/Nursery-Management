using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NurseryManagement.Api.Data;
using NurseryManagement.Api.Security;

namespace NurseryManagement.Api.Controllers;

[ApiController]
[Route("api/audit-logs")]
[Authorize(Policy = PermissionPolicies.AuditRead)]
public class AuditLogsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] string? userName,
        [FromQuery] string? entityName,
        [FromQuery] string? action,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var query = db.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(userName))
        {
            query = query.Where(x => x.UserName != null && x.UserName.Contains(userName));
        }

        if (!string.IsNullOrWhiteSpace(entityName))
        {
            query = query.Where(x => x.EntityName == entityName);
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            query = query.Where(x => x.Action == action);
        }

        if (from.HasValue)
        {
            query = query.Where(x => x.OccurredAt >= from.Value.ToUniversalTime());
        }

        if (to.HasValue)
        {
            query = query.Where(x => x.OccurredAt <= to.Value.ToUniversalTime());
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.OccurredAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.UserId,
                x.UserName,
                x.Action,
                x.EntityName,
                x.EntityId,
                x.OldValues,
                x.NewValues,
                x.IpAddress,
                x.OccurredAt
            })
            .ToListAsync(cancellationToken);

        return Ok(new { total, page, pageSize, items });
    }
}
