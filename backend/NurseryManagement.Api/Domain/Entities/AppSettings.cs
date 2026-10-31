namespace NurseryManagement.Api.Domain.Entities;

public enum PrintCodeMode
{
    BarcodeOnly = 0,
    QrOnly = 1,
    Both = 2
}

/// <summary>What value is encoded into printed QR/barcode (legacy global default).</summary>
public enum CodeDataType
{
    SystemCode = 0,
    ChildCode = 1,
    ChildCodeAndName = 2
}

public enum BarcodeFormatType
{
    CODE128 = 0,
    CODE39 = 1
}

/// <summary>Singleton app settings row (one record).</summary>
public class AppSettings
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string IdentityLabelSize { get; set; } = "70x40";
    public string PickupLabelSize { get; set; } = "50x30";
    public PrintCodeMode PrintCodeMode { get; set; } = PrintCodeMode.Both;
    public int QrSizePx { get; set; } = 140;
    public int BarcodeHeight { get; set; } = 40;
    public BarcodeFormatType BarcodeFormat { get; set; } = BarcodeFormatType.CODE128;
    public CodeDataType CodeDataType { get; set; } = CodeDataType.SystemCode;

    /// <summary>JSON document for NormalCard + PickupPass visual layouts.</summary>
    public string PrintLayoutsJson { get; set; } = "{}";

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
