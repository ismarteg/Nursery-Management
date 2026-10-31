using NurseryManagement.Api.Contracts.Operations;

namespace NurseryManagement.Api.Services.Staff;

public interface IStaffService
{
    Task<IReadOnlyList<object>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<object> CreateAsync(StaffRequest request, Guid? userId, string? userName, string? ip, CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid id, StaffRequest request, Guid? userId, string? userName, string? ip, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, Guid? userId, string? userName, string? ip, CancellationToken cancellationToken = default);
}
