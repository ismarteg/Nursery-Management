using Microsoft.EntityFrameworkCore;
using NurseryManagement.Api.Contracts.Operations;
using NurseryManagement.Api.Data;
using NurseryManagement.Api.Domain.Entities;

namespace NurseryManagement.Api.Services.Entry;

public class EntryService(AppDbContext db, IAuditService auditService) : IEntryService
{
    private const string EntryCodeKind = "Entry";
    private const string IdentityCodeKind = "Identity";

    public async Task<object> ScanAsync(ScanEntryRequest request, Guid? userId, string? userName, string? ip, CancellationToken cancellationToken = default)
    {
        var raw = request.Code?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new InvalidOperationException("Code is required");
        }

        // Support payloads like "CHILDCODE|Full Name" from print settings.
        var code = raw.Contains('|', StringComparison.Ordinal)
            ? raw.Split('|', 2)[0].Trim()
            : raw;

        // 1) Dedicated entry codes take priority.
        var matchedCodeKind = EntryCodeKind;
        var child = await db.Children
            .Include(c => c.Class)
            .FirstOrDefaultAsync(c =>
                c.IsActive &&
                (c.EntryQrCode == code || c.EntryBarcode == code ||
                 c.EntryQrCode == raw || c.EntryBarcode == raw),
                cancellationToken);

        // 2) Fall back to identity codes / child code.
        if (child is null)
        {
            matchedCodeKind = IdentityCodeKind;
            child = await db.Children
                .Include(c => c.Class)
                .FirstOrDefaultAsync(c =>
                    c.IsActive &&
                    (c.Code == code || c.QrCode == code || c.Barcode == code ||
                     c.Code == raw || c.QrCode == raw || c.Barcode == raw),
                    cancellationToken);
        }

        if (child is null)
        {
            throw new KeyNotFoundException("Child not found for this code");
        }

        var last = await db.EntryRecords
            .AsNoTracking()
            .Where(e => e.ChildId == child.Id)
            .OrderByDescending(e => e.OccurredAt)
            .FirstOrDefaultAsync(cancellationToken);

        var nextType = request.ForceType
            ?? (last?.Type == EntryType.In ? EntryType.Out : EntryType.In);

        var record = new EntryRecord
        {
            ChildId = child.Id,
            Type = nextType,
            Method = request.Method,
            Notes = request.Notes?.Trim(),
            OccurredAt = DateTime.UtcNow,
            RecordedByUserId = userId
        };

        db.EntryRecords.Add(record);
        await db.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(
            "Scan",
            "EntryRecord",
            record.Id.ToString(),
            null,
            new
            {
                child.Code,
                child.FullName,
                Type = nextType.ToString(),
                Method = request.Method.ToString(),
                MatchedCodeKind = matchedCodeKind
            },
            userId,
            userName,
            ip,
            cancellationToken);

        return new
        {
            record.Id,
            type = record.Type.ToString(),
            method = record.Method.ToString(),
            occurredAt = record.OccurredAt,
            previousType = last?.Type.ToString(),
            matchedCodeKind,
            child = new
            {
                child.Id,
                child.Code,
                child.FullName,
                className = child.Class?.Name,
                child.GuardianName,
                child.GuardianPhone
            }
        };
    }

    public async Task<object> GetHistoryAsync(
        Guid? childId,
        Guid? classId,
        DateTime? from,
        DateTime? to,
        string? type,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var query = db.EntryRecords.AsNoTracking().AsQueryable();

        if (childId.HasValue)
        {
            query = query.Where(e => e.ChildId == childId);
        }

        if (classId.HasValue)
        {
            query = query.Where(e => e.Child.ClassId == classId);
        }

        if (from.HasValue)
        {
            query = query.Where(e => e.OccurredAt >= from.Value.ToUniversalTime());
        }

        if (to.HasValue)
        {
            query = query.Where(e => e.OccurredAt <= to.Value.ToUniversalTime());
        }

        if (!string.IsNullOrWhiteSpace(type) && Enum.TryParse<EntryType>(type, true, out var entryType))
        {
            query = query.Where(e => e.Type == entryType);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(e => e.OccurredAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new
            {
                e.Id,
                e.ChildId,
                childCode = e.Child.Code,
                childName = e.Child.FullName,
                className = e.Child.Class != null ? e.Child.Class.Name : null,
                type = e.Type.ToString(),
                method = e.Method.ToString(),
                e.OccurredAt,
                e.Notes
            })
            .ToListAsync(cancellationToken);

        return new { total, page, pageSize, items };
    }

    public async Task<object> GetPresentAsync(CancellationToken cancellationToken = default)
    {
        var latestTimes = await db.EntryRecords
            .AsNoTracking()
            .GroupBy(e => e.ChildId)
            .Select(g => new { ChildId = g.Key, OccurredAt = g.Max(x => x.OccurredAt) })
            .ToListAsync(cancellationToken);

        if (latestTimes.Count == 0)
        {
            return new { count = 0, items = Array.Empty<object>() };
        }

        var childIds = latestTimes.Select(x => x.ChildId).ToList();
        var latestLookup = latestTimes.ToDictionary(x => x.ChildId, x => x.OccurredAt);

        var candidates = await db.EntryRecords
            .AsNoTracking()
            .Include(e => e.Child)
            .ThenInclude(c => c.Class)
            .Where(e => childIds.Contains(e.ChildId))
            .ToListAsync(cancellationToken);

        var present = candidates
            .Where(e => latestLookup.TryGetValue(e.ChildId, out var at) && e.OccurredAt == at)
            .Where(e => e.Type == EntryType.In && e.Child.IsActive)
            .Select(e => new
            {
                e.Child.Id,
                e.Child.Code,
                e.Child.FullName,
                className = e.Child.Class?.Name,
                since = e.OccurredAt
            })
            .OrderBy(x => x.FullName)
            .ToList();

        return new { count = present.Count, items = present };
    }
}
