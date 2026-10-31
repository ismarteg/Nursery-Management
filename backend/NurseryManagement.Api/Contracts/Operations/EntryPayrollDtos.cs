using NurseryManagement.Api.Domain.Entities;

namespace NurseryManagement.Api.Contracts.Operations;

public class ScanEntryRequest
{
    public string Code { get; set; } = string.Empty;
    public EntryType? ForceType { get; set; }
    public EntryMethod Method { get; set; } = EntryMethod.Barcode;
    public string? Notes { get; set; }
}

public class CalculateSalariesRequest
{
    public int Year { get; set; }
    public int Month { get; set; }
}
