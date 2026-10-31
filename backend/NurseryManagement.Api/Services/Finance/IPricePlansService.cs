using NurseryManagement.Api.Contracts.Operations;

namespace NurseryManagement.Api.Services.Finance;

public interface IPricePlansService
{
    Task<IReadOnlyList<object>> GetAllAsync(bool activeOnly, CancellationToken cancellationToken = default);
    Task<object> CreateAsync(PricePlanRequest request, Guid? userId, string? userName, string? ip, CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid id, PricePlanRequest request, Guid? userId, string? userName, string? ip, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the plan, or deactivates it when it is referenced by children/payments.
    /// Returns true when the plan was deactivated instead of removed.
    /// </summary>
    Task<bool> DeleteAsync(Guid id, Guid? userId, string? userName, string? ip, CancellationToken cancellationToken = default);
}
