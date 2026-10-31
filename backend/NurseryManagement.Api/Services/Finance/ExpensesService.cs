using Microsoft.EntityFrameworkCore;
using NurseryManagement.Api.Contracts.Operations;
using NurseryManagement.Api.Data;
using NurseryManagement.Api.Domain.Entities;

namespace NurseryManagement.Api.Services.Finance;

public class ExpensesService(AppDbContext db, IAuditService auditService) : IExpensesService
{
    public async Task<object> GetAllAsync(Guid? categoryId, DateTime? from, DateTime? to, CancellationToken cancellationToken = default)
    {
        var query = db.Expenses.AsNoTracking().AsQueryable();

        if (categoryId.HasValue)
        {
            query = query.Where(e => e.CategoryId == categoryId);
        }

        if (from.HasValue)
        {
            query = query.Where(e => e.ExpenseDate >= from.Value.Date);
        }

        if (to.HasValue)
        {
            query = query.Where(e => e.ExpenseDate <= to.Value.Date);
        }

        var items = await query
            .OrderByDescending(e => e.ExpenseDate)
            .ThenByDescending(e => e.CreatedAt)
            .Select(e => new
            {
                e.Id,
                e.CategoryId,
                categoryName = e.Category.Name,
                e.Title,
                e.Amount,
                e.ExpenseDate,
                e.PaidTo,
                e.Notes
            })
            .ToListAsync(cancellationToken);

        return new
        {
            totalAmount = items.Sum(x => x.Amount),
            count = items.Count,
            items
        };
    }

    public async Task<object> GetMonthlySummaryAsync(int? year, int? month, CancellationToken cancellationToken = default)
    {
        var y = year ?? DateTime.UtcNow.Year;
        var m = month ?? DateTime.UtcNow.Month;
        var from = new DateTime(y, m, 1);
        var to = from.AddMonths(1).AddDays(-1);

        var items = await db.Expenses
            .AsNoTracking()
            .Where(e => e.ExpenseDate >= from && e.ExpenseDate <= to)
            .GroupBy(e => new { e.CategoryId, e.Category.Name })
            .Select(g => new
            {
                categoryId = g.Key.CategoryId,
                categoryName = g.Key.Name,
                total = g.Sum(x => x.Amount),
                count = g.Count()
            })
            .OrderByDescending(x => x.total)
            .ToListAsync(cancellationToken);

        return new
        {
            year = y,
            month = m,
            totalAmount = items.Sum(x => x.total),
            items
        };
    }

    public async Task<object> CreateAsync(ExpenseRequest request, Guid? userId, string? userName, string? ip, CancellationToken cancellationToken = default)
    {
        await EnsureCategoryAsync(request.CategoryId, cancellationToken);

        var entity = new Expense
        {
            CategoryId = request.CategoryId,
            Title = request.Title.Trim(),
            Amount = request.Amount,
            ExpenseDate = request.ExpenseDate.Date,
            PaidTo = request.PaidTo?.Trim(),
            Notes = request.Notes?.Trim()
        };

        db.Expenses.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        await auditService.LogAsync("Created", "Expense", entity.Id.ToString(), null,
            new { entity.Title, entity.Amount, entity.CategoryId }, userId, userName, ip, cancellationToken);
        return new { entity.Id };
    }

    public async Task UpdateAsync(Guid id, ExpenseRequest request, Guid? userId, string? userName, string? ip, CancellationToken cancellationToken = default)
    {
        var entity = await db.Expenses.FirstOrDefaultAsync(e => e.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Expense not found");

        await EnsureCategoryAsync(request.CategoryId, cancellationToken);

        var old = new { entity.Title, entity.Amount, entity.CategoryId };
        entity.CategoryId = request.CategoryId;
        entity.Title = request.Title.Trim();
        entity.Amount = request.Amount;
        entity.ExpenseDate = request.ExpenseDate.Date;
        entity.PaidTo = request.PaidTo?.Trim();
        entity.Notes = request.Notes?.Trim();
        await db.SaveChangesAsync(cancellationToken);
        await auditService.LogAsync("Updated", "Expense", entity.Id.ToString(), old,
            new { entity.Title, entity.Amount, entity.CategoryId }, userId, userName, ip, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, Guid? userId, string? userName, string? ip, CancellationToken cancellationToken = default)
    {
        var entity = await db.Expenses.FirstOrDefaultAsync(e => e.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Expense not found");

        db.Expenses.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
        await auditService.LogAsync("Deleted", "Expense", id.ToString(), new { entity.Title, entity.Amount }, null,
            userId, userName, ip, cancellationToken);
    }

    public async Task<IReadOnlyList<object>> GetCategoriesAsync(bool activeOnly, CancellationToken cancellationToken = default)
    {
        var query = db.ExpenseCategories.AsNoTracking().AsQueryable();
        if (activeOnly)
        {
            query = query.Where(c => c.IsActive);
        }

        var items = await query
            .OrderBy(c => c.Name)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Description,
                c.IsActive,
                expensesCount = c.Expenses.Count
            })
            .ToListAsync(cancellationToken);

        return items.Cast<object>().ToList();
    }

    public async Task<object> CreateCategoryAsync(ExpenseCategoryRequest request, Guid? userId, string? userName, string? ip, CancellationToken cancellationToken = default)
    {
        var name = request.Name.Trim();
        if (await db.ExpenseCategories.AnyAsync(c => c.Name == name, cancellationToken))
        {
            throw new ConflictException("Category name already exists");
        }

        var entity = new ExpenseCategory
        {
            Name = name,
            Description = request.Description?.Trim(),
            IsActive = request.IsActive
        };

        db.ExpenseCategories.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        await auditService.LogAsync("Created", "ExpenseCategory", entity.Id.ToString(), null,
            new { entity.Name }, userId, userName, ip, cancellationToken);
        return new { entity.Id };
    }

    public async Task UpdateCategoryAsync(Guid id, ExpenseCategoryRequest request, Guid? userId, string? userName, string? ip, CancellationToken cancellationToken = default)
    {
        var entity = await db.ExpenseCategories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Expense category not found");

        var name = request.Name.Trim();
        if (await db.ExpenseCategories.AnyAsync(c => c.Id != id && c.Name == name, cancellationToken))
        {
            throw new ConflictException("Category name already exists");
        }

        var old = new { entity.Name, entity.IsActive };
        entity.Name = name;
        entity.Description = request.Description?.Trim();
        entity.IsActive = request.IsActive;
        await db.SaveChangesAsync(cancellationToken);
        await auditService.LogAsync("Updated", "ExpenseCategory", entity.Id.ToString(), old,
            new { entity.Name, entity.IsActive }, userId, userName, ip, cancellationToken);
    }

    public async Task<bool> DeleteCategoryAsync(Guid id, Guid? userId, string? userName, string? ip, CancellationToken cancellationToken = default)
    {
        var entity = await db.ExpenseCategories
            .Include(c => c.Expenses)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Expense category not found");

        if (entity.Expenses.Count > 0)
        {
            entity.IsActive = false;
            await db.SaveChangesAsync(cancellationToken);
            await auditService.LogAsync("Deactivated", "ExpenseCategory", id.ToString(), null,
                new { entity.Name }, userId, userName, ip, cancellationToken);
            return true;
        }

        db.ExpenseCategories.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
        await auditService.LogAsync("Deleted", "ExpenseCategory", id.ToString(), new { entity.Name }, null,
            userId, userName, ip, cancellationToken);
        return false;
    }

    private async Task EnsureCategoryAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        if (!await db.ExpenseCategories.AnyAsync(c => c.Id == categoryId && c.IsActive, cancellationToken))
        {
            throw new InvalidOperationException("Category is invalid");
        }
    }
}
