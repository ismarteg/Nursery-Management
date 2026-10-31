using Microsoft.EntityFrameworkCore;
using NurseryManagement.Api.Contracts.Operations;
using NurseryManagement.Api.Data;
using NurseryManagement.Api.Domain.Entities;

namespace NurseryManagement.Api.Services.Classes;

public class ClassesService(AppDbContext db, IAuditService auditService) : IClassesService
{
    public async Task<IReadOnlyList<object>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var items = await db.Classes
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Capacity,
                c.MinAgeMonths,
                c.MaxAgeMonths,
                c.Notes,
                c.IsActive,
                childrenCount = c.Children.Count(ch => ch.IsActive),
                staffIds = c.StaffAssignments.Select(s => s.StaffId).ToList(),
                staffNames = c.StaffAssignments.Select(s => s.Staff.FullName).OrderBy(n => n).ToList()
            })
            .ToListAsync(cancellationToken);

        return items.Cast<object>().ToList();
    }

    public async Task<object> CreateAsync(ClassRequest request, Guid? userId, string? userName, string? ip, CancellationToken cancellationToken = default)
    {
        var name = request.Name.Trim();
        if (await db.Classes.AnyAsync(c => c.Name == name, cancellationToken))
        {
            throw new ConflictException("Class name already exists");
        }

        var staffIds = await ValidateStaffIdsAsync(request.StaffIds, cancellationToken)
            ?? throw new InvalidOperationException("One or more staff members are invalid");

        var entity = new NurseryClass
        {
            Name = name,
            Capacity = request.Capacity,
            MinAgeMonths = request.MinAgeMonths,
            MaxAgeMonths = request.MaxAgeMonths,
            Notes = request.Notes?.Trim(),
            IsActive = request.IsActive
        };

        db.Classes.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        await ReplaceStaffAsync(entity.Id, staffIds, cancellationToken);
        await auditService.LogAsync("Created", "Class", entity.Id.ToString(), null, entity, userId, userName, ip, cancellationToken);
        return new { entity.Id };
    }

    public async Task UpdateAsync(Guid id, ClassRequest request, Guid? userId, string? userName, string? ip, CancellationToken cancellationToken = default)
    {
        var entity = await db.Classes.Include(c => c.StaffAssignments).FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Class not found");

        var name = request.Name.Trim();
        if (await db.Classes.AnyAsync(c => c.Id != id && c.Name == name, cancellationToken))
        {
            throw new ConflictException("Class name already exists");
        }

        var staffIds = await ValidateStaffIdsAsync(request.StaffIds, cancellationToken)
            ?? throw new InvalidOperationException("One or more staff members are invalid");

        var old = new { entity.Name, entity.Capacity, entity.IsActive };
        entity.Name = name;
        entity.Capacity = request.Capacity;
        entity.MinAgeMonths = request.MinAgeMonths;
        entity.MaxAgeMonths = request.MaxAgeMonths;
        entity.Notes = request.Notes?.Trim();
        entity.IsActive = request.IsActive;

        await ReplaceStaffAsync(entity.Id, staffIds, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await auditService.LogAsync("Updated", "Class", entity.Id.ToString(), old,
            new { entity.Name, entity.Capacity, entity.IsActive }, userId, userName, ip, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, Guid? userId, string? userName, string? ip, CancellationToken cancellationToken = default)
    {
        var entity = await db.Classes
            .Include(c => c.StaffAssignments)
            .Include(c => c.Children)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Class not found");

        if (entity.Children.Count > 0)
        {
            throw new InvalidOperationException("Class has children. Move them first.");
        }

        db.StaffClassAssignments.RemoveRange(entity.StaffAssignments);
        db.Classes.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
        await auditService.LogAsync("Deleted", "Class", id.ToString(), new { entity.Name }, null, userId, userName, ip, cancellationToken);
    }

    private async Task ReplaceStaffAsync(Guid classId, List<Guid> staffIds, CancellationToken cancellationToken)
    {
        var existing = await db.StaffClassAssignments.Where(x => x.ClassId == classId).ToListAsync(cancellationToken);
        db.StaffClassAssignments.RemoveRange(existing);
        db.StaffClassAssignments.AddRange(staffIds.Select(staffId => new StaffClassAssignment
        {
            ClassId = classId,
            StaffId = staffId
        }));
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<List<Guid>?> ValidateStaffIdsAsync(List<Guid> staffIds, CancellationToken cancellationToken)
    {
        var distinct = staffIds.Distinct().ToList();
        if (distinct.Count == 0)
        {
            return distinct;
        }

        var count = await db.StaffMembers.CountAsync(s => distinct.Contains(s.Id), cancellationToken);
        return count == distinct.Count ? distinct : null;
    }
}
