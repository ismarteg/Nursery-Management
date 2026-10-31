using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using NurseryManagement.Api.Data;
using NurseryManagement.Api.Domain.Entities;

namespace NurseryManagement.Api.Services.Payroll;

public class AttendanceService(AppDbContext db, IAuditService auditService) : IAttendanceService
{
    public async Task<IReadOnlyList<object>> GetAllAsync(Guid? staffId, int? year, int? month, CancellationToken cancellationToken = default)
    {
        var query = db.AttendanceRecords.AsNoTracking().AsQueryable();
        if (staffId.HasValue)
        {
            query = query.Where(a => a.StaffId == staffId);
        }

        if (year.HasValue && month.HasValue)
        {
            var from = new DateTime(year.Value, month.Value, 1);
            var to = from.AddMonths(1);
            query = query.Where(a => a.WorkDate >= from && a.WorkDate < to);
        }

        var raw = await query
            .OrderByDescending(a => a.WorkDate)
            .Select(a => new
            {
                a.Id,
                a.StaffId,
                staffCode = a.Staff.Code,
                staffName = a.Staff.FullName,
                a.WorkDate,
                a.ClockIn,
                a.ClockOut
            })
            .ToListAsync(cancellationToken);

        return raw.Select(a => (object)new
        {
            a.Id,
            a.StaffId,
            a.staffCode,
            a.staffName,
            a.WorkDate,
            clockIn = a.ClockIn.ToString(@"hh\:mm"),
            clockOut = a.ClockOut.ToString(@"hh\:mm"),
            workedHours = Math.Round(Math.Max(0, (a.ClockOut - a.ClockIn).TotalHours), 2)
        }).ToList();
    }

    public async Task<object> UploadAsync(IFormFile file, Guid? userId, string? userName, string? ip, CancellationToken cancellationToken = default)
    {
        var batch = new AttendanceUploadBatch
        {
            FileName = file.FileName,
            UploadedByUserId = userId
        };
        db.AttendanceUploadBatches.Add(batch);
        await db.SaveChangesAsync(cancellationToken);

        var staffByCode = await db.StaffMembers
            .AsNoTracking()
            .Where(s => s.IsActive)
            .ToDictionaryAsync(s => s.Code, s => s.Id, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var errors = new List<string>();
        var success = 0;
        var total = 0;

        using var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8);
        var headerSkipped = false;

        while (true)
        {
            var line = (await reader.ReadLineAsync(cancellationToken))?.Trim();
            if (line is null)
            {
                break;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (!headerSkipped && line.Contains("StaffCode", StringComparison.OrdinalIgnoreCase))
            {
                headerSkipped = true;
                continue;
            }

            total++;
            var parts = line.Split(',', StringSplitOptions.TrimEntries);
            if (parts.Length < 4)
            {
                errors.Add($"Row {total}: expected StaffCode,Date,ClockIn,ClockOut");
                continue;
            }

            var staffCode = parts[0];
            if (!staffByCode.TryGetValue(staffCode, out var staffId))
            {
                errors.Add($"Row {total}: unknown staff code '{staffCode}'");
                continue;
            }

            if (!DateTime.TryParse(parts[1], CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var workDate))
            {
                errors.Add($"Row {total}: invalid date '{parts[1]}'");
                continue;
            }

            if (!TimeSpan.TryParse(parts[2], CultureInfo.InvariantCulture, out var clockIn) ||
                !TimeSpan.TryParse(parts[3], CultureInfo.InvariantCulture, out var clockOut))
            {
                errors.Add($"Row {total}: invalid clock times");
                continue;
            }

            if (clockOut <= clockIn)
            {
                errors.Add($"Row {total}: ClockOut must be after ClockIn");
                continue;
            }

            workDate = workDate.Date;
            var existing = await db.AttendanceRecords
                .FirstOrDefaultAsync(a => a.StaffId == staffId && a.WorkDate == workDate, cancellationToken);

            if (existing is null)
            {
                db.AttendanceRecords.Add(new AttendanceRecord
                {
                    StaffId = staffId,
                    WorkDate = workDate,
                    ClockIn = clockIn,
                    ClockOut = clockOut,
                    UploadBatchId = batch.Id
                });
            }
            else
            {
                existing.ClockIn = clockIn;
                existing.ClockOut = clockOut;
                existing.UploadBatchId = batch.Id;
            }

            success++;
        }

        batch.TotalRows = total;
        batch.SuccessRows = success;
        batch.FailedRows = total - success;
        batch.ErrorReport = errors.Count == 0 ? null : string.Join("\n", errors.Take(100));
        await db.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(
            "Upload",
            "Attendance",
            batch.Id.ToString(),
            null,
            new { batch.FileName, batch.TotalRows, batch.SuccessRows, batch.FailedRows },
            userId,
            userName,
            ip,
            cancellationToken);

        return new
        {
            batchId = batch.Id,
            batch.FileName,
            batch.TotalRows,
            batch.SuccessRows,
            batch.FailedRows,
            errors = errors.Take(50)
        };
    }
}
