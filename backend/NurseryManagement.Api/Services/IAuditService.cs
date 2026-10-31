namespace NurseryManagement.Api.Services;

public interface IAuditService
{
    Task LogAsync(
        string action,
        string entityName,
        string? entityId = null,
        object? oldValues = null,
        object? newValues = null,
        Guid? userId = null,
        string? userName = null,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);
}
