using Microsoft.EntityFrameworkCore;
using NurseryManagement.Api.Data;
using NurseryManagement.Api.Domain.Entities;

namespace NurseryManagement.Api.Services.Dashboard;

public class DashboardService(AppDbContext db) : IDashboardService
{
    public async Task<object> GetSummaryAsync(bool isAdmin, Func<string, bool> hasPermission, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1);
        var monthEnd = monthStart.AddMonths(1);

        object? children = null;
        object? present = null;
        object? receivables = null;
        object? expenses = null;
        object? payroll = null;
        object? staff = null;
        object? classes = null;

        if (isAdmin || hasPermission("children.read"))
        {
            var activeChildren = await db.Children.AsNoTracking().CountAsync(c => c.IsActive, cancellationToken);
            children = new { activeCount = activeChildren };
        }

        if (isAdmin || hasPermission("entry.scan"))
        {
            var presentCount = await CountPresentAsync(cancellationToken);
            present = new { count = presentCount };
        }

        if (isAdmin || hasPermission("payments.read"))
        {
            await RefreshOverdueAsync(cancellationToken);
            var open = await db.Payments
                .AsNoTracking()
                .Where(p => p.Status == PaymentStatus.Pending ||
                            p.Status == PaymentStatus.Partial ||
                            p.Status == PaymentStatus.Overdue)
                .Select(p => new { Balance = p.AmountDue - p.AmountPaid, p.Status })
                .ToListAsync(cancellationToken);

            receivables = new
            {
                openCount = open.Count,
                totalBalance = open.Sum(x => x.Balance),
                overdueCount = open.Count(x => x.Status == PaymentStatus.Overdue),
                overdueBalance = open.Where(x => x.Status == PaymentStatus.Overdue).Sum(x => x.Balance)
            };
        }

        if (isAdmin || hasPermission("expenses.read"))
        {
            var monthExpenses = await db.Expenses
                .AsNoTracking()
                .Where(e => e.ExpenseDate >= monthStart && e.ExpenseDate < monthEnd)
                .Select(e => e.Amount)
                .ToListAsync(cancellationToken);

            expenses = new
            {
                year = now.Year,
                month = now.Month,
                count = monthExpenses.Count,
                totalAmount = monthExpenses.Sum()
            };
        }

        if (isAdmin || hasPermission("salaries.read"))
        {
            var salaries = await db.SalaryPayments
                .AsNoTracking()
                .Where(s => s.Year == now.Year && s.Month == now.Month && s.Status != SalaryPaymentStatus.Cancelled)
                .Select(s => new { s.Amount, s.Status })
                .ToListAsync(cancellationToken);

            payroll = new
            {
                year = now.Year,
                month = now.Month,
                count = salaries.Count,
                totalAmount = salaries.Sum(s => s.Amount),
                paidAmount = salaries.Where(s => s.Status == SalaryPaymentStatus.Paid).Sum(s => s.Amount),
                unpaidAmount = salaries.Where(s => s.Status == SalaryPaymentStatus.Calculated).Sum(s => s.Amount)
            };
        }

        if (isAdmin || hasPermission("staff.read"))
        {
            staff = new
            {
                activeCount = await db.StaffMembers.AsNoTracking().CountAsync(s => s.IsActive, cancellationToken)
            };
        }

        if (isAdmin || hasPermission("classes.read"))
        {
            classes = new
            {
                activeCount = await db.Classes.AsNoTracking().CountAsync(c => c.IsActive, cancellationToken)
            };
        }

        return new
        {
            generatedAt = now,
            year = now.Year,
            month = now.Month,
            children,
            present,
            receivables,
            expenses,
            payroll,
            staff,
            classes
        };
    }

    private async Task<int> CountPresentAsync(CancellationToken cancellationToken)
    {
        var latestTimes = await db.EntryRecords
            .AsNoTracking()
            .GroupBy(e => e.ChildId)
            .Select(g => new { ChildId = g.Key, OccurredAt = g.Max(x => x.OccurredAt) })
            .ToListAsync(cancellationToken);

        if (latestTimes.Count == 0)
        {
            return 0;
        }

        var childIds = latestTimes.Select(x => x.ChildId).ToList();
        var latestLookup = latestTimes.ToDictionary(x => x.ChildId, x => x.OccurredAt);

        var candidates = await db.EntryRecords
            .AsNoTracking()
            .Include(e => e.Child)
            .Where(e => childIds.Contains(e.ChildId))
            .ToListAsync(cancellationToken);

        return candidates
            .Where(e => latestLookup.TryGetValue(e.ChildId, out var at) && e.OccurredAt == at)
            .Count(e => e.Type == EntryType.In && e.Child.IsActive);
    }

    private async Task RefreshOverdueAsync(CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var overdue = await db.Payments
            .Where(p => (p.Status == PaymentStatus.Pending || p.Status == PaymentStatus.Partial) &&
                        p.DueDate < today &&
                        p.AmountPaid < p.AmountDue)
            .ToListAsync(cancellationToken);

        if (overdue.Count == 0)
        {
            return;
        }

        foreach (var payment in overdue)
        {
            payment.Status = PaymentStatus.Overdue;
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
