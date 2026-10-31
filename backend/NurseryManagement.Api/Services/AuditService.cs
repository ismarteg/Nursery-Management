using System.Text.Json;
using NurseryManagement.Api.Data;
using NurseryManagement.Api.Domain.Entities;

namespace NurseryManagement.Api.Services;

public class AuditService(AppDbContext db) : IAuditService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task LogAsync(
        string action,
        string entityName,
        string? entityId = null,
        object? oldValues = null,
        object? newValues = null,
        Guid? userId = null,
        string? userName = null,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        db.AuditLogs.Add(new AuditLog
        {
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            OldValues = oldValues is null ? null : JsonSerializer.Serialize(oldValues, JsonOptions),
            NewValues = newValues is null ? null : JsonSerializer.Serialize(newValues, JsonOptions),
            UserId = userId,
            UserName = userName,
            IpAddress = ipAddress,
            OccurredAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);
    }
}
