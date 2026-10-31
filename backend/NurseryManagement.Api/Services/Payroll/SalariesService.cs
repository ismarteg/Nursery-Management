using Microsoft.EntityFrameworkCore;
using NurseryManagement.Api.Contracts.Operations;
using NurseryManagement.Api.Data;
using NurseryManagement.Api.Domain.Entities;

namespace NurseryManagement.Api.Services.Payroll;

public class SalariesService(AppDbContext db, IAuditService auditService) : ISalariesService
{
    public async Task<IReadOnlyList<object>> GetAllAsync(int? year, int? month, CancellationToken cancellationToken = default)
    {
        var query = db.SalaryPayments.AsNoTracking().AsQueryable();
        if (year.HasValue)
        {
            query = query.Where(s => s.Year == year);
        }

        if (month.HasValue)
        {
            query = query.Where(s => s.Month == month);
        }

        var items = await query
            .OrderByDescending(s => s.Year)
            .ThenByDescending(s => s.Month)
            .ThenBy(s => s.Staff.FullName)
            .Select(s => new
            {
                s.Id,
                s.StaffId,
                staffCode = s.Staff.Code,
                staffName = s.Staff.FullName,
                s.Year,
                s.Month,
                s.WorkedDays,
                s.WorkedHours,
                s.Amount,
                salaryType = s.SalaryTypeSnapshot.ToString(),
                status = s.Status.ToString(),
                s.CalculatedAt,
                s.PaidAt,
                s.Notes
            })
            .ToListAsync(cancellationToken);

        return items.Cast<object>().ToList();
    }

    public async Task<object> CalculateAsync(CalculateSalariesRequest request, Guid? userId, string? userName, string? ip, CancellationToken cancellationToken = default)
    {
        if (request.Year < 2000 || request.Month is < 1 or > 12)
        {
            throw new InvalidOperationException("Invalid year/month");
        }

        var from = new DateTime(request.Year, request.Month, 1);
        var to = from.AddMonths(1);

        var attendanceRaw = await db.AttendanceRecords
            .AsNoTracking()
            .Where(a => a.WorkDate >= from && a.WorkDate < to)
            .Select(a => new { a.StaffId, a.WorkDate, a.ClockIn, a.ClockOut })
            .ToListAsync(cancellationToken);

        var attendance = attendanceRaw
            .Select(a => new
            {
                a.StaffId,
                a.WorkDate,
                Hours = Math.Max(0, (a.ClockOut - a.ClockIn).TotalHours)
            })
            .ToList();

        if (attendance.Count == 0)
        {
            throw new InvalidOperationException("No attendance records for this period");
        }

        var staffIds = attendance.Select(a => a.StaffId).Distinct().ToList();
        var staffMembers = await db.StaffMembers
            .Where(s => staffIds.Contains(s.Id) && s.IsActive)
            .ToListAsync(cancellationToken);

        var created = 0;
        var updated = 0;

        foreach (var staff in staffMembers)
        {
            var rows = attendance.Where(a => a.StaffId == staff.Id).ToList();
            var workedDays = rows.Select(r => r.WorkDate.Date).Distinct().Count();
            var workedHours = (decimal)Math.Round(rows.Sum(r => r.Hours), 2);
            var amount = CalculateAmount(staff, workedDays, workedHours);

            var existing = await db.SalaryPayments
                .FirstOrDefaultAsync(s =>
                    s.StaffId == staff.Id &&
                    s.Year == request.Year &&
                    s.Month == request.Month, cancellationToken);

            if (existing is null)
            {
                db.SalaryPayments.Add(new SalaryPayment
                {
                    StaffId = staff.Id,
                    Year = request.Year,
                    Month = request.Month,
                    WorkedDays = workedDays,
                    WorkedHours = workedHours,
                    Amount = amount,
                    SalaryTypeSnapshot = staff.SalaryType,
                    Status = SalaryPaymentStatus.Calculated,
                    CalculatedAt = DateTime.UtcNow
                });
                created++;
            }
            else if (existing.Status != SalaryPaymentStatus.Paid)
            {
                existing.WorkedDays = workedDays;
                existing.WorkedHours = workedHours;
                existing.Amount = amount;
                existing.SalaryTypeSnapshot = staff.SalaryType;
                existing.Status = SalaryPaymentStatus.Calculated;
                existing.CalculatedAt = DateTime.UtcNow;
                existing.PaidAt = null;
                updated++;
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(
            "Calculate",
            "SalaryPayment",
            $"{request.Year}-{request.Month:D2}",
            null,
            new { request.Year, request.Month, created, updated },
            userId,
            userName,
            ip,
            cancellationToken);

        return new { request.Year, request.Month, created, updated };
    }

    public async Task<object> MarkPaidAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await db.SalaryPayments.FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Salary payment not found");

        if (entity.Status == SalaryPaymentStatus.Cancelled)
        {
            throw new InvalidOperationException("Cancelled salary cannot be paid");
        }

        entity.Status = SalaryPaymentStatus.Paid;
        entity.PaidAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return new { entity.Id, status = entity.Status.ToString(), entity.PaidAt };
    }

    private static decimal CalculateAmount(StaffMember staff, int workedDays, decimal workedHours)
    {
        return staff.SalaryType switch
        {
            SalaryType.Hourly => Math.Round(workedHours * (staff.HourlyRate ?? staff.BaseSalary), 2),
            SalaryType.Daily => Math.Round(workedDays * (staff.DailyRate ?? staff.BaseSalary), 2),
            _ => staff.BaseSalary
        };
    }
}
