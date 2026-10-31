using Microsoft.EntityFrameworkCore;
using NurseryManagement.Api.Contracts.Operations;
using NurseryManagement.Api.Data;
using NurseryManagement.Api.Domain.Entities;

namespace NurseryManagement.Api.Services.Finance;

public class PricePlansService(AppDbContext db, IAuditService auditService) : IPricePlansService
{
    public async Task<IReadOnlyList<object>> GetAllAsync(bool activeOnly, CancellationToken cancellationToken = default)
    {
        var query = db.PricePlans.AsNoTracking().AsQueryable();
        if (activeOnly)
        {
            query = query.Where(p => p.IsActive);
        }

        var items = await query
            .OrderBy(p => p.Name)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Description,
                p.Amount,
                billingCycle = p.BillingCycle.ToString(),
                p.DaysCount,
                p.IsActive,
                childrenCount = p.Children.Count(c => c.IsActive)
            })
            .ToListAsync(cancellationToken);

        return items.Cast<object>().ToList();
    }

    public async Task<object> CreateAsync(PricePlanRequest request, Guid? userId, string? userName, string? ip, CancellationToken cancellationToken = default)
    {
        var name = request.Name.Trim();
        if (await db.PricePlans.AnyAsync(p => p.Name == name, cancellationToken))
        {
            throw new ConflictException("Plan name already exists");
        }

        var entity = new PricePlan
        {
            Name = name,
            Description = request.Description?.Trim(),
            Amount = request.Amount,
            BillingCycle = request.BillingCycle,
            DaysCount = NormalizeDaysCount(request.BillingCycle, request.DaysCount),
            IsActive = request.IsActive
        };

        db.PricePlans.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        await auditService.LogAsync("Created", "PricePlan", entity.Id.ToString(), null,
            new { entity.Name, entity.Amount, entity.BillingCycle, entity.DaysCount }, userId, userName, ip, cancellationToken);
        return new { entity.Id };
    }

    public async Task UpdateAsync(Guid id, PricePlanRequest request, Guid? userId, string? userName, string? ip, CancellationToken cancellationToken = default)
    {
        var entity = await db.PricePlans.FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Price plan not found");

        var name = request.Name.Trim();
        if (await db.PricePlans.AnyAsync(p => p.Id != id && p.Name == name, cancellationToken))
        {
            throw new ConflictException("Plan name already exists");
        }

        var old = new { entity.Name, entity.Amount, entity.BillingCycle, entity.DaysCount, entity.IsActive };
        entity.Name = name;
        entity.Description = request.Description?.Trim();
        entity.Amount = request.Amount;
        entity.BillingCycle = request.BillingCycle;
        entity.DaysCount = NormalizeDaysCount(request.BillingCycle, request.DaysCount);
        entity.IsActive = request.IsActive;
        await db.SaveChangesAsync(cancellationToken);
        await auditService.LogAsync("Updated", "PricePlan", entity.Id.ToString(), old,
            new { entity.Name, entity.Amount, entity.BillingCycle, entity.DaysCount, entity.IsActive },
            userId, userName, ip, cancellationToken);
    }

    private static int NormalizeDaysCount(BillingCycle cycle, int daysCount)
    {
        if (daysCount > 0)
        {
            return Math.Clamp(daysCount, 1, 3660);
        }

        return cycle switch
        {
            BillingCycle.Daily => 1,
            BillingCycle.Term => 90,
            BillingCycle.Yearly => 365,
            _ => 30
        };
    }

    public async Task<bool> DeleteAsync(Guid id, Guid? userId, string? userName, string? ip, CancellationToken cancellationToken = default)
    {
        var entity = await db.PricePlans
            .Include(p => p.Children)
            .Include(p => p.Payments)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Price plan not found");

        if (entity.Children.Count > 0 || entity.Payments.Count > 0)
        {
            entity.IsActive = false;
            await db.SaveChangesAsync(cancellationToken);
            await auditService.LogAsync("Deactivated", "PricePlan", id.ToString(), null,
                new { entity.Name }, userId, userName, ip, cancellationToken);
            return true;
        }

        db.PricePlans.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
        await auditService.LogAsync("Deleted", "PricePlan", id.ToString(), new { entity.Name }, null,
            userId, userName, ip, cancellationToken);
        return false;
    }
}
