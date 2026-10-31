using Microsoft.EntityFrameworkCore;
using NurseryManagement.Api.Contracts.Operations;
using NurseryManagement.Api.Data;
using NurseryManagement.Api.Domain.Entities;

namespace NurseryManagement.Api.Services.Staff;

public class StaffService(AppDbContext db, IAuditService auditService) : IStaffService
{
    public async Task<IReadOnlyList<object>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var items = await db.StaffMembers
            .AsNoTracking()
            .OrderBy(s => s.FullName)
            .Select(s => new
            {
                s.Id,
                s.Code,
                s.FullName,
                s.Phone,
                s.Email,
                s.HireDate,
                salaryType = s.SalaryType.ToString(),
                s.BaseSalary,
                s.HourlyRate,
                s.DailyRate,
                s.IsActive,
                classIds = s.ClassAssignments.Select(c => c.ClassId).ToList(),
                classNames = s.ClassAssignments.Select(c => c.Class.Name).OrderBy(n => n).ToList()
            })
            .ToListAsync(cancellationToken);

        return items.Cast<object>().ToList();
    }

    public async Task<object> CreateAsync(StaffRequest request, Guid? userId, string? userName, string? ip, CancellationToken cancellationToken = default)
    {
        var classIds = await ValidateClassIdsAsync(request.ClassIds, cancellationToken)
            ?? throw new InvalidOperationException("One or more classes are invalid");

        var code = await CodeGenerator.NextStaffCodeAsync(db, cancellationToken);
        var entity = new StaffMember
        {
            Code = code,
            FullName = request.FullName.Trim(),
            Phone = request.Phone?.Trim(),
            Email = request.Email?.Trim(),
            HireDate = request.HireDate,
            SalaryType = request.SalaryType,
            BaseSalary = request.BaseSalary,
            HourlyRate = request.HourlyRate,
            DailyRate = request.DailyRate,
            IsActive = request.IsActive
        };

        db.StaffMembers.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        await ReplaceClassesAsync(entity.Id, classIds, cancellationToken);
        await auditService.LogAsync("Created", "Staff", entity.Id.ToString(), null,
            new { entity.Code, entity.FullName }, userId, userName, ip, cancellationToken);
        return new { entity.Id, entity.Code };
    }

    public async Task UpdateAsync(Guid id, StaffRequest request, Guid? userId, string? userName, string? ip, CancellationToken cancellationToken = default)
    {
        var entity = await db.StaffMembers.Include(s => s.ClassAssignments).FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Staff member not found");

        var classIds = await ValidateClassIdsAsync(request.ClassIds, cancellationToken)
            ?? throw new InvalidOperationException("One or more classes are invalid");

        var old = new { entity.FullName, entity.IsActive, entity.BaseSalary };
        entity.FullName = request.FullName.Trim();
        entity.Phone = request.Phone?.Trim();
        entity.Email = request.Email?.Trim();
        entity.HireDate = request.HireDate;
        entity.SalaryType = request.SalaryType;
        entity.BaseSalary = request.BaseSalary;
        entity.HourlyRate = request.HourlyRate;
        entity.DailyRate = request.DailyRate;
        entity.IsActive = request.IsActive;

        await ReplaceClassesAsync(entity.Id, classIds, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await auditService.LogAsync("Updated", "Staff", entity.Id.ToString(), old,
            new { entity.FullName, entity.IsActive, entity.BaseSalary }, userId, userName, ip, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, Guid? userId, string? userName, string? ip, CancellationToken cancellationToken = default)
    {
        var entity = await db.StaffMembers.Include(s => s.ClassAssignments).FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Staff member not found");

        db.StaffClassAssignments.RemoveRange(entity.ClassAssignments);
        db.StaffMembers.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
        await auditService.LogAsync("Deleted", "Staff", id.ToString(), new { entity.Code, entity.FullName }, null,
            userId, userName, ip, cancellationToken);
    }

    private async Task ReplaceClassesAsync(Guid staffId, List<Guid> classIds, CancellationToken cancellationToken)
    {
        var existing = await db.StaffClassAssignments.Where(x => x.StaffId == staffId).ToListAsync(cancellationToken);
        db.StaffClassAssignments.RemoveRange(existing);
        db.StaffClassAssignments.AddRange(classIds.Select(classId => new StaffClassAssignment
        {
            StaffId = staffId,
            ClassId = classId
        }));
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<List<Guid>?> ValidateClassIdsAsync(List<Guid> classIds, CancellationToken cancellationToken)
    {
        var distinct = classIds.Distinct().ToList();
        if (distinct.Count == 0)
        {
            return distinct;
        }

        var count = await db.Classes.CountAsync(c => distinct.Contains(c.Id), cancellationToken);
        return count == distinct.Count ? distinct : null;
    }
}
