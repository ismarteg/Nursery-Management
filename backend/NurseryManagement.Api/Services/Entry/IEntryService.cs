using NurseryManagement.Api.Contracts.Operations;

namespace NurseryManagement.Api.Services.Entry;

public interface IEntryService
{
    /// <summary>
    /// Records an entry/exit scan. Matches EntryQrCode/EntryBarcode first, then identity Code/QrCode/Barcode.
    /// The response includes matchedCodeKind ("Entry" or "Identity").
    /// Throws InvalidOperationException when the code is missing and KeyNotFoundException when no child matches.
    /// </summary>
    Task<object> ScanAsync(ScanEntryRequest request, Guid? userId, string? userName, string? ip, CancellationToken cancellationToken = default);

    Task<object> GetHistoryAsync(
        Guid? childId,
        Guid? classId,
        DateTime? from,
        DateTime? to,
        string? type,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<object> GetPresentAsync(CancellationToken cancellationToken = default);
}
