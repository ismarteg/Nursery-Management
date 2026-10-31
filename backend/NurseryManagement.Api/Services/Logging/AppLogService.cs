using Microsoft.EntityFrameworkCore;
using NurseryManagement.Api.Data;
using NurseryManagement.Api.Domain.Entities;

namespace NurseryManagement.Api.Services.Logging;

public class AppLogService(AppDbContext db) : IAppLogService
{
    public async Task WriteAsync(
        AppLogType logType,
        string module,
        string action,
        string message,
        bool succeeded = true,
        int? statusCode = null,
        string? details = null,
        Guid? userId = null,
        string? userName = null,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        db.AppLogs.Add(new AppLog
        {
            LogType = logType,
            Module = Truncate(module, 100),
            Action = Truncate(action, 50),
            Message = Truncate(message, 500),
            Details = details is null ? null : Truncate(details, 4000),
            Succeeded = succeeded,
            StatusCode = statusCode,
            UserId = userId,
            UserName = Truncate(userName, 100),
            IpAddress = Truncate(ipAddress, 64),
            OccurredAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<object> QueryAsync(
        AppLogType? logType,
        string? module,
        string? userName,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var query = db.AppLogs.AsNoTracking().AsQueryable();
        if (logType.HasValue)
        {
            query = query.Where(l => l.LogType == logType);
        }

        if (!string.IsNullOrWhiteSpace(module))
        {
            query = query.Where(l => l.Module.Contains(module));
        }

        if (!string.IsNullOrWhiteSpace(userName))
        {
            query = query.Where(l => l.UserName != null && l.UserName.Contains(userName));
        }

        if (from.HasValue)
        {
            query = query.Where(l => l.OccurredAt >= from.Value.ToUniversalTime());
        }

        if (to.HasValue)
        {
            query = query.Where(l => l.OccurredAt <= to.Value.ToUniversalTime());
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(l => l.OccurredAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new
            {
                l.Id,
                logType = l.LogType.ToString(),
                l.Module,
                l.Action,
                l.Message,
                l.Details,
                l.Succeeded,
                l.StatusCode,
                l.UserName,
                l.IpAddress,
                l.OccurredAt
            })
            .ToListAsync(cancellationToken);

        return new { total, page, pageSize, items };
    }

    private static string Truncate(string? value, int max)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value.Length <= max ? value : value[..max];
    }
}
