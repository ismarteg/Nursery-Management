using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using NurseryManagement.Api.Contracts.Operations;
using NurseryManagement.Api.Data;
using NurseryManagement.Api.Domain.Entities;
using NurseryManagement.Api.Domain.Print;

namespace NurseryManagement.Api.Services.Settings;

public partial class SettingsService(AppDbContext db, IAuditService auditService) : ISettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        PropertyNameCaseInsensitive = true
    };

    private static readonly HashSet<string> AllowedSizes = new(StringComparer.OrdinalIgnoreCase)
    {
        "40x30", "50x30", "50x40", "60x40", "70x40", "80x50", "100x50", "100x70"
    };

    private static readonly int[] AllowedQrSizes = [80, 100, 120, 140, 160, 180, 200, 240];
    private static readonly int[] AllowedBarcodeHeights = [24, 32, 40, 48, 56, 64, 80];

    public async Task<object> GetAsync(CancellationToken cancellationToken = default)
    {
        var settings = await EnsureAsync(cancellationToken);
        return Map(settings);
    }

    public async Task<object> UpdateAsync(
        UpdateSettingsRequest request,
        Guid? userId,
        string? userName,
        string? ip,
        CancellationToken cancellationToken = default)
    {
        ValidateSize(request.IdentityLabelSize, "Identity label size");
        ValidateSize(request.PickupLabelSize, "Pickup label size");
        if (!Enum.IsDefined(request.PrintCodeMode))
        {
            throw new InvalidOperationException("Invalid print code mode");
        }

        if (!Enum.IsDefined(request.BarcodeFormat))
        {
            throw new InvalidOperationException("Invalid barcode format");
        }

        if (!Enum.IsDefined(request.CodeDataType))
        {
            throw new InvalidOperationException("Invalid code data type");
        }

        if (!AllowedQrSizes.Contains(request.QrSizePx))
        {
            throw new InvalidOperationException($"QR size is invalid. Allowed: {string.Join(", ", AllowedQrSizes)}");
        }

        if (!AllowedBarcodeHeights.Contains(request.BarcodeHeight))
        {
            throw new InvalidOperationException($"Barcode height is invalid. Allowed: {string.Join(", ", AllowedBarcodeHeights)}");
        }

        var settings = await EnsureAsync(cancellationToken);
        var old = Map(settings);

        settings.IdentityLabelSize = NormalizeSize(request.IdentityLabelSize);
        settings.PickupLabelSize = NormalizeSize(request.PickupLabelSize);
        settings.PrintCodeMode = request.PrintCodeMode;
        settings.QrSizePx = request.QrSizePx;
        settings.BarcodeHeight = request.BarcodeHeight;
        settings.BarcodeFormat = request.BarcodeFormat;
        settings.CodeDataType = request.CodeDataType;

        if (request.PrintLayouts.HasValue &&
            request.PrintLayouts.Value.ValueKind is JsonValueKind.Object)
        {
            settings.PrintLayoutsJson = NormalizeLayoutsJson(request.PrintLayouts.Value.GetRawText());
            SyncLegacySizesFromLayouts(settings);
        }

        settings.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        await auditService.LogAsync("Updated", "AppSettings", settings.Id.ToString(), old, Map(settings),
            userId, userName, ip, cancellationToken);

        return Map(settings);
    }

    public async Task<object> UpdatePrintLayoutsAsync(
        UpdatePrintLayoutsRequest request,
        Guid? userId,
        string? userName,
        string? ip,
        CancellationToken cancellationToken = default)
    {
        ValidateLayout(request.PrintLayouts.NormalCard, "NormalCard");
        ValidateLayout(request.PrintLayouts.PickupPass, "PickupPass");

        var settings = await EnsureAsync(cancellationToken);
        var old = Map(settings);

        request.PrintLayouts.NormalCard.Type = "NormalCard";
        request.PrintLayouts.PickupPass.Type = "PickupPass";
        settings.PrintLayoutsJson = JsonSerializer.Serialize(request.PrintLayouts, JsonOptions);
        SyncLegacySizesFromLayouts(settings);
        settings.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        await auditService.LogAsync("UpdatedPrintLayouts", "AppSettings", settings.Id.ToString(), old, Map(settings),
            userId, userName, ip, cancellationToken);

        return Map(settings);
    }

    private async Task<AppSettings> EnsureAsync(CancellationToken cancellationToken)
    {
        var settings = await db.AppSettings.OrderBy(s => s.Id).FirstOrDefaultAsync(cancellationToken);
        if (settings is null)
        {
            settings = new AppSettings
            {
                PrintLayoutsJson = JsonSerializer.Serialize(PrintLayoutDefaults.Create(), JsonOptions)
            };
            db.AppSettings.Add(settings);
            await db.SaveChangesAsync(cancellationToken);
            return settings;
        }

        if (string.IsNullOrWhiteSpace(settings.PrintLayoutsJson) ||
            settings.PrintLayoutsJson is "{}" or "null")
        {
            settings.PrintLayoutsJson = JsonSerializer.Serialize(PrintLayoutDefaults.Create(), JsonOptions);
            SyncLegacySizesFromLayouts(settings);
            await db.SaveChangesAsync(cancellationToken);
        }

        return settings;
    }

    private static void ValidateLayout(PrintCardLayout layout, string name)
    {
        if (layout.WidthMm is < 20 or > 200 || layout.HeightMm is < 15 or > 200)
        {
            throw new InvalidOperationException($"{name} card size is invalid");
        }

        foreach (var field in layout.Fields)
        {
            if (string.IsNullOrWhiteSpace(field.FieldType))
            {
                throw new InvalidOperationException($"{name}: field type is required");
            }

            if (field.WidthMm <= 0 || field.HeightMm <= 0)
            {
                throw new InvalidOperationException($"{name}: field size must be positive");
            }
        }
    }

    private static void SyncLegacySizesFromLayouts(AppSettings settings)
    {
        var layouts = ParseLayouts(settings.PrintLayoutsJson);
        settings.IdentityLabelSize = $"{(int)Math.Round(layouts.NormalCard.WidthMm)}x{(int)Math.Round(layouts.NormalCard.HeightMm)}";
        settings.PickupLabelSize = $"{(int)Math.Round(layouts.PickupPass.WidthMm)}x{(int)Math.Round(layouts.PickupPass.HeightMm)}";
    }

    private static string NormalizeLayoutsJson(string raw)
    {
        var doc = JsonSerializer.Deserialize<PrintLayoutsDocument>(raw, JsonOptions)
            ?? PrintLayoutDefaults.Create();
        doc.NormalCard.Type = "NormalCard";
        doc.PickupPass.Type = "PickupPass";
        ValidateLayout(doc.NormalCard, "NormalCard");
        ValidateLayout(doc.PickupPass, "PickupPass");
        return JsonSerializer.Serialize(doc, JsonOptions);
    }

    private static PrintLayoutsDocument ParseLayouts(string? json)
    {
        if (string.IsNullOrWhiteSpace(json) || json is "{}" or "null")
        {
            return PrintLayoutDefaults.Create();
        }

        try
        {
            return JsonSerializer.Deserialize<PrintLayoutsDocument>(json, JsonOptions)
                ?? PrintLayoutDefaults.Create();
        }
        catch
        {
            return PrintLayoutDefaults.Create();
        }
    }

    private static void ValidateSize(string size, string label)
    {
        var normalized = NormalizeSize(size);
        if (!SizePattern().IsMatch(normalized))
        {
            throw new InvalidOperationException($"{label} is invalid");
        }

        // Allow custom sizes coming from visual editor (not only preset list).
        var parts = normalized.Split('x');
        if (parts.Length != 2 ||
            !int.TryParse(parts[0], out var w) ||
            !int.TryParse(parts[1], out var h) ||
            w is < 20 or > 200 ||
            h is < 15 or > 200)
        {
            throw new InvalidOperationException($"{label} is out of range");
        }
    }

    private static string NormalizeSize(string size) =>
        size.Trim().ToLowerInvariant().Replace(" ", "");

    private static object Map(AppSettings s)
    {
        var layouts = ParseLayouts(s.PrintLayoutsJson);
        return new
        {
            s.Id,
            identityLabelSize = s.IdentityLabelSize,
            pickupLabelSize = s.PickupLabelSize,
            printCodeMode = s.PrintCodeMode.ToString(),
            qrSizePx = s.QrSizePx,
            barcodeHeight = s.BarcodeHeight,
            barcodeFormat = s.BarcodeFormat.ToString(),
            codeDataType = s.CodeDataType.ToString(),
            printLayouts = layouts,
            allowedLabelSizes = AllowedSizes.Order().ToArray(),
            allowedQrSizes = AllowedQrSizes,
            allowedBarcodeHeights = AllowedBarcodeHeights,
            allowedBarcodeFormats = Enum.GetNames<BarcodeFormatType>(),
            allowedCodeDataTypes = Enum.GetNames<CodeDataType>(),
            availableDbFields = new[]
            {
                "fullName", "code", "className", "guardianName", "guardianPhone",
                "pricePlanName", "systemCode", "entryCode", "photoPath", "static"
            },
            s.UpdatedAt
        };
    }

    [GeneratedRegex(@"^\d{2,3}x\d{2,3}$")]
    private static partial Regex SizePattern();
}
