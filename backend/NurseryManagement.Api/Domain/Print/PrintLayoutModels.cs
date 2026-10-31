using System.Text.Json.Serialization;

namespace NurseryManagement.Api.Domain.Print;

public class PrintLayoutsDocument
{
    public PrintCardLayout NormalCard { get; set; } = PrintLayoutDefaults.NormalCard();
    public PrintCardLayout PickupPass { get; set; } = PrintLayoutDefaults.PickupPass();
}

public class PrintCardLayout
{
    public string Type { get; set; } = "NormalCard";
    public double WidthMm { get; set; } = 70;
    public double HeightMm { get; set; } = 40;
    public string BackgroundColor { get; set; } = "#ffffff";
    public List<PrintFieldLayout> Fields { get; set; } = [];
}

public class PrintFieldLayout
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];

    /// <summary>text | static | qr | barcode | image</summary>
    public string FieldType { get; set; } = "text";

    /// <summary>DB/source field key, e.g. fullName, code, className, systemCode, photoPath</summary>
    public string DbField { get; set; } = "fullName";

    public string? StaticText { get; set; }
    public double FontSize { get; set; } = 10;
    public string FontWeight { get; set; } = "normal";
    public string Color { get; set; } = "#111827";
    public string Align { get; set; } = "center";
    public double XMm { get; set; }
    public double YMm { get; set; }
    public double WidthMm { get; set; } = 30;
    public double HeightMm { get; set; } = 6;

    /// <summary>For qr/barcode: size hint in mm (uses width/height primarily).</summary>
    public string? CodeFormat { get; set; }
}

public static class PrintLayoutDefaults
{
    public static PrintLayoutsDocument Create() => new()
    {
        NormalCard = NormalCard(),
        PickupPass = PickupPass()
    };

    public static PrintCardLayout NormalCard() => new()
    {
        Type = "NormalCard",
        WidthMm = 70,
        HeightMm = 40,
        Fields =
        [
            Field("static", "static", "Nursery", 9, 2, 2, 66, 5, "center", "#4a3480"),
            Field("text", "fullName", null, 12, 2, 8, 66, 7, "center", "#111827", "bold"),
            Field("text", "code", null, 9, 2, 15, 40, 5, "start", "#334155"),
            Field("text", "className", null, 9, 42, 15, 26, 5, "end", "#334155"),
            Field("qr", "systemCode", null, 10, 8, 21, 18, 18, "center", "#111827"),
            Field("barcode", "systemCode", null, 8, 30, 22, 36, 14, "center", "#111827", codeFormat: "CODE128")
        ]
    };

    public static PrintCardLayout PickupPass() => new()
    {
        Type = "PickupPass",
        WidthMm = 50,
        HeightMm = 30,
        Fields =
        [
            Field("static", "static", "Pickup", 8, 2, 1.5, 46, 4, "center", "#4a3480"),
            Field("text", "fullName", null, 10, 2, 6, 46, 5, "center", "#111827", "bold"),
            Field("text", "code", null, 8, 2, 11, 46, 4, "center", "#334155"),
            Field("qr", "entryCode", null, 8, 16, 15, 14, 14, "center", "#111827")
        ]
    };

    private static PrintFieldLayout Field(
        string fieldType,
        string dbField,
        string? staticText,
        double fontSize,
        double x,
        double y,
        double w,
        double h,
        string align,
        string color,
        string fontWeight = "normal",
        string? codeFormat = null) => new()
    {
        FieldType = fieldType,
        DbField = dbField,
        StaticText = staticText,
        FontSize = fontSize,
        XMm = x,
        YMm = y,
        WidthMm = w,
        HeightMm = h,
        Align = align,
        Color = color,
        FontWeight = fontWeight,
        CodeFormat = codeFormat
    };
}
