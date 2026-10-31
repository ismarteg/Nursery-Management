using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using NurseryManagement.Api.Domain.Entities;
using NurseryManagement.Api.Domain.Print;

namespace NurseryManagement.Api.Contracts.Operations;

public class UpdateSettingsRequest
{
    [Required, MaxLength(20)]
    public string IdentityLabelSize { get; set; } = "70x40";

    [Required, MaxLength(20)]
    public string PickupLabelSize { get; set; } = "50x30";

    public PrintCodeMode PrintCodeMode { get; set; } = PrintCodeMode.Both;

    [Range(60, 320)]
    public int QrSizePx { get; set; } = 140;

    [Range(20, 120)]
    public int BarcodeHeight { get; set; } = 40;

    public BarcodeFormatType BarcodeFormat { get; set; } = BarcodeFormatType.CODE128;

    public CodeDataType CodeDataType { get; set; } = CodeDataType.SystemCode;

    /// <summary>Full print layouts JSON (NormalCard + PickupPass).</summary>
    public JsonElement? PrintLayouts { get; set; }
}

public class UpdatePrintLayoutsRequest
{
    [Required]
    public PrintLayoutsDocument PrintLayouts { get; set; } = PrintLayoutDefaults.Create();
}
