using Microsoft.EntityFrameworkCore;
using NurseryManagement.Api.Data;

namespace NurseryManagement.Api.Services;

public static class CodeGenerator
{
    public static async Task<string> NextChildCodeAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var last = await db.Children
            .AsNoTracking()
            .OrderByDescending(c => c.Code)
            .Select(c => c.Code)
            .FirstOrDefaultAsync(cancellationToken);

        return NextCode("CHD", last);
    }

    public static async Task<string> NextStaffCodeAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var last = await db.StaffMembers
            .AsNoTracking()
            .OrderByDescending(s => s.Code)
            .Select(s => s.Code)
            .FirstOrDefaultAsync(cancellationToken);

        return NextCode("STF", last);
    }

    public static async Task<string> NextEntryCodeAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var lastQr = await db.Children
            .AsNoTracking()
            .Where(c => c.EntryQrCode != null && c.EntryQrCode.StartsWith("ENT-"))
            .OrderByDescending(c => c.EntryQrCode)
            .Select(c => c.EntryQrCode)
            .FirstOrDefaultAsync(cancellationToken);

        return NextCode("ENT", lastQr);
    }

    private static string NextCode(string prefix, string? lastCode)
    {
        var next = 1;
        if (!string.IsNullOrWhiteSpace(lastCode) && lastCode.StartsWith(prefix + "-", StringComparison.OrdinalIgnoreCase))
        {
            var numberPart = lastCode[(prefix.Length + 1)..];
            if (int.TryParse(numberPart, out var parsed))
            {
                next = parsed + 1;
            }
        }

        return $"{prefix}-{next:D6}";
    }
}
