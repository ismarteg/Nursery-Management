using NurseryManagement.Api.Contracts.Operations;

namespace NurseryManagement.Api.Services.Payroll;

public interface ISalariesService
{
    Task<IReadOnlyList<object>> GetAllAsync(int? year, int? month, CancellationToken cancellationToken = default);
    Task<object> CalculateAsync(CalculateSalariesRequest request, Guid? userId, string? userName, string? ip, CancellationToken cancellationToken = default);
    Task<object> MarkPaidAsync(Guid id, CancellationToken cancellationToken = default);
}
