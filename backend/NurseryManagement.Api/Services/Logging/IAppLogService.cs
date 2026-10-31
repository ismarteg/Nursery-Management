using NurseryManagement.Api.Domain.Entities;

namespace NurseryManagement.Api.Services.Logging;

public interface IAppLogService
{
    Task WriteAsync(
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
        CancellationToken cancellationToken = default);

    Task<object> QueryAsync(
        AppLogType? logType,
        string? module,
        string? userName,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
