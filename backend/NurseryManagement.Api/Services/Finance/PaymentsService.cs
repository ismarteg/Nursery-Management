using Microsoft.EntityFrameworkCore;
using NurseryManagement.Api.Contracts.Operations;
using NurseryManagement.Api.Data;
using NurseryManagement.Api.Domain.Entities;

namespace NurseryManagement.Api.Services.Finance;

public class PaymentsService(AppDbContext db, IAuditService auditService) : IPaymentsService
{
    public async Task<IReadOnlyList<object>> GetAllAsync(
        Guid? childId,
        string? status,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken = default)
    {
        await RefreshOverdueAsync(cancellationToken);

        var query = db.Payments.AsNoTracking().AsQueryable();
        if (childId.HasValue)
        {
            query = query.Where(p => p.ChildId == childId);
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<PaymentStatus>(status, true, out var parsed))
        {
            query = query.Where(p => p.Status == parsed);
        }

        if (from.HasValue)
        {
            query = query.Where(p => p.DueDate >= from.Value.Date);
        }

        if (to.HasValue)
        {
            query = query.Where(p => p.DueDate <= to.Value.Date);
        }

        var items = await query
            .OrderByDescending(p => p.DueDate)
            .Select(p => new
            {
                p.Id,
                p.ChildId,
                childCode = p.Child.Code,
                childName = p.Child.FullName,
                p.PricePlanId,
                planName = p.PlanNameSnapshot ?? (p.PricePlan != null ? p.PricePlan.Name : null),
                p.AmountDue,
                p.AmountPaid,
                balance = p.AmountDue - p.AmountPaid,
                p.DueDate,
                p.PaidAt,
                status = p.Status.ToString(),
                method = p.Method.HasValue ? p.Method.Value.ToString() : null,
                p.Notes
            })
            .ToListAsync(cancellationToken);

        return items.Cast<object>().ToList();
    }

    public async Task<object> GetReceivablesAsync(CancellationToken cancellationToken = default)
    {
        await RefreshOverdueAsync(cancellationToken);

        var items = await db.Payments
            .AsNoTracking()
            .Where(p => p.Status == PaymentStatus.Pending ||
                        p.Status == PaymentStatus.Partial ||
                        p.Status == PaymentStatus.Overdue)
            .OrderBy(p => p.DueDate)
            .Select(p => new
            {
                p.Id,
                childCode = p.Child.Code,
                childName = p.Child.FullName,
                planName = p.PlanNameSnapshot,
                p.AmountDue,
                p.AmountPaid,
                balance = p.AmountDue - p.AmountPaid,
                p.DueDate,
                status = p.Status.ToString()
            })
            .ToListAsync(cancellationToken);

        return new
        {
            count = items.Count,
            totalBalance = items.Sum(x => x.balance),
            items
        };
    }

    public async Task<object> CreateAsync(PaymentRequest request, Guid? userId, string? userName, string? ip, CancellationToken cancellationToken = default)
    {
        var child = await db.Children.FirstOrDefaultAsync(c => c.Id == request.ChildId, cancellationToken)
            ?? throw new InvalidOperationException("Child is invalid");

        PricePlan? plan = null;
        var planId = request.PricePlanId ?? child.PricePlanId;
        if (planId.HasValue)
        {
            plan = await db.PricePlans.FirstOrDefaultAsync(p => p.Id == planId, cancellationToken)
                ?? throw new InvalidOperationException("Price plan is invalid");
        }

        var amountDue = request.AmountDue > 0 ? request.AmountDue : plan?.Amount ?? 0;
        if (amountDue <= 0)
        {
            throw new InvalidOperationException("Amount due is required");
        }

        var amountPaid = Math.Clamp(request.AmountPaid, 0, amountDue);
        var status = request.Status ?? ResolveStatus(amountDue, amountPaid, request.DueDate);

        var entity = new Payment
        {
            ChildId = child.Id,
            PricePlanId = plan?.Id,
            PlanNameSnapshot = plan?.Name,
            AmountDue = amountDue,
            AmountPaid = amountPaid,
            DueDate = request.DueDate.Date,
            PaidAt = amountPaid > 0 ? (request.PaidAt ?? DateTime.UtcNow) : null,
            Status = status,
            Method = request.Method,
            Notes = request.Notes?.Trim()
        };

        db.Payments.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        await auditService.LogAsync("Created", "Payment", entity.Id.ToString(), null,
            new { entity.ChildId, entity.AmountDue, entity.Status }, userId, userName, ip, cancellationToken);
        return new { entity.Id };
    }

    public async Task<object> RecordPaymentAsync(Guid id, RecordPaymentRequest request, Guid? userId, string? userName, string? ip, CancellationToken cancellationToken = default)
    {
        var entity = await db.Payments.FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Payment not found");

        if (entity.Status == PaymentStatus.Cancelled || entity.Status == PaymentStatus.Paid)
        {
            throw new InvalidOperationException("Payment cannot be updated");
        }

        var old = new { entity.AmountPaid, entity.Status };
        entity.AmountPaid = Math.Min(entity.AmountDue, entity.AmountPaid + request.Amount);
        entity.Method = request.Method;
        entity.PaidAt = request.PaidAt ?? DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            entity.Notes = string.IsNullOrWhiteSpace(entity.Notes)
                ? request.Notes.Trim()
                : $"{entity.Notes} | {request.Notes.Trim()}";
        }

        entity.Status = ResolveStatus(entity.AmountDue, entity.AmountPaid, entity.DueDate);
        await db.SaveChangesAsync(cancellationToken);
        await auditService.LogAsync("PaymentRecorded", "Payment", entity.Id.ToString(), old,
            new { entity.AmountPaid, entity.Status }, userId, userName, ip, cancellationToken);
        return new
        {
            entity.Id,
            entity.AmountPaid,
            balance = entity.AmountDue - entity.AmountPaid,
            status = entity.Status.ToString()
        };
    }

    public async Task CancelAsync(Guid id, Guid? userId, string? userName, string? ip, CancellationToken cancellationToken = default)
    {
        var entity = await db.Payments.FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Payment not found");

        entity.Status = PaymentStatus.Cancelled;
        await db.SaveChangesAsync(cancellationToken);
        await auditService.LogAsync("Cancelled", "Payment", entity.Id.ToString(), null,
            new { entity.ChildId }, userId, userName, ip, cancellationToken);
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

    private static PaymentStatus ResolveStatus(decimal amountDue, decimal amountPaid, DateTime dueDate)
    {
        if (amountPaid <= 0)
        {
            return dueDate.Date < DateTime.UtcNow.Date ? PaymentStatus.Overdue : PaymentStatus.Pending;
        }

        if (amountPaid >= amountDue)
        {
            return PaymentStatus.Paid;
        }

        return dueDate.Date < DateTime.UtcNow.Date ? PaymentStatus.Overdue : PaymentStatus.Partial;
    }
}
