using NurseryManagement.Api.Contracts.Operations;

namespace NurseryManagement.Api.Services.Finance;

public interface IPaymentsService
{
    Task<IReadOnlyList<object>> GetAllAsync(Guid? childId, string? status, DateTime? from, DateTime? to, CancellationToken cancellationToken = default);
    Task<object> GetReceivablesAsync(CancellationToken cancellationToken = default);
    Task<object> CreateAsync(PaymentRequest request, Guid? userId, string? userName, string? ip, CancellationToken cancellationToken = default);
    Task<object> RecordPaymentAsync(Guid id, RecordPaymentRequest request, Guid? userId, string? userName, string? ip, CancellationToken cancellationToken = default);
    Task CancelAsync(Guid id, Guid? userId, string? userName, string? ip, CancellationToken cancellationToken = default);
}
