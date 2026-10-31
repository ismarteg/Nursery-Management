using NurseryManagement.Api.Contracts.Operations;

namespace NurseryManagement.Api.Services.Settings;

public interface ISettingsService
{
    Task<object> GetAsync(CancellationToken cancellationToken = default);
    Task<object> UpdateAsync(UpdateSettingsRequest request, Guid? userId, string? userName, string? ip, CancellationToken cancellationToken = default);
    Task<object> UpdatePrintLayoutsAsync(UpdatePrintLayoutsRequest request, Guid? userId, string? userName, string? ip, CancellationToken cancellationToken = default);
}
