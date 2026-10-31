using Microsoft.EntityFrameworkCore;
using NurseryManagement.Api.Contracts.Operations;
using NurseryManagement.Api.Data;
using NurseryManagement.Api.Domain.Entities;

namespace NurseryManagement.Api.Services.Children;

public class ChildrenService(AppDbContext db, IAuditService auditService, IFileStorageService files) : IChildrenService
{
    public async Task<IReadOnlyList<object>> GetAllAsync(Guid? classId, CancellationToken cancellationToken = default)
    {
        var query = db.Children.AsNoTracking().AsQueryable();
        if (classId.HasValue)
        {
            query = query.Where(c => c.ClassId == classId);
        }

        var items = await query
            .OrderBy(c => c.FullName)
            .Select(c => new
            {
                c.Id,
                c.Code,
                c.FullName,
                c.BirthDate,
                c.Gender,
                c.PhotoPath,
                c.GuardianName,
                c.GuardianPhone,
                c.GuardianEmail,
                c.GuardianRelation,
                c.ClassId,
                className = c.Class != null ? c.Class.Name : null,
                c.PricePlanId,
                pricePlanName = c.PricePlan != null ? c.PricePlan.Name : null,
                c.QrCode,
                c.Barcode,
                c.EntryQrCode,
                c.EntryBarcode,
                c.Notes,
                c.IsActive,
                c.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return items.Cast<object>().ToList();
    }

    public async Task<object?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var child = await db.Children
            .AsNoTracking()
            .Include(c => c.Father)
            .Include(c => c.Mother)
            .Include(c => c.Class)
            .Include(c => c.PricePlan)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        return child is null ? null : MapDetail(child);
    }

    public async Task<object> CreateAsync(ChildRequest request, Guid? userId, string? userName, string? ip, CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);
        await EnsureRefsAsync(request, cancellationToken);

        var father = ToParent(request.Father);
        var mother = ToParent(request.Mother);
        db.ParentPersons.AddRange(father, mother);

        var code = await CodeGenerator.NextChildCodeAsync(db, cancellationToken);
        var entity = new Child
        {
            Code = code,
            FullName = request.FullName.Trim(),
            BirthDate = request.BirthDate,
            Gender = request.Gender?.Trim(),
            Father = father,
            Mother = mother,
            GuardianName = NormalizeOptional(request.Guardian?.Name) ?? string.Empty,
            GuardianPhone = NormalizeOptional(request.Guardian?.Phone),
            GuardianEmail = NormalizeOptional(request.Guardian?.Email),
            GuardianRelation = NormalizeOptional(request.Guardian?.Relation),
            ClassId = request.ClassId,
            PricePlanId = request.PricePlanId,
            Notes = request.Notes?.Trim(),
            IsActive = request.IsActive,
            QrCode = code,
            Barcode = code
        };

        db.Children.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        await auditService.LogAsync("Created", "Child", entity.Id.ToString(), null,
            new { entity.Code, entity.FullName }, userId, userName, ip, cancellationToken);

        return new { entity.Id, entity.Code, entity.QrCode, entity.Barcode };
    }

    public async Task UpdateAsync(Guid id, ChildRequest request, Guid? userId, string? userName, string? ip, CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);
        await EnsureRefsAsync(request, cancellationToken);

        var entity = await db.Children
            .Include(c => c.Father)
            .Include(c => c.Mother)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Child not found");

        var old = new { entity.FullName, entity.ClassId, entity.IsActive };
        entity.FullName = request.FullName.Trim();
        entity.BirthDate = request.BirthDate;
        entity.Gender = request.Gender?.Trim();
        entity.GuardianName = NormalizeOptional(request.Guardian?.Name) ?? string.Empty;
        entity.GuardianPhone = NormalizeOptional(request.Guardian?.Phone);
        entity.GuardianEmail = NormalizeOptional(request.Guardian?.Email);
        entity.GuardianRelation = NormalizeOptional(request.Guardian?.Relation);
        entity.ClassId = request.ClassId;
        entity.PricePlanId = request.PricePlanId;
        entity.Notes = request.Notes?.Trim();
        entity.IsActive = request.IsActive;

        ApplyParent(entity.Father ??= new ParentPerson(), request.Father);
        if (entity.FatherId is null)
        {
            db.ParentPersons.Add(entity.Father);
            entity.FatherId = entity.Father.Id;
        }

        ApplyParent(entity.Mother ??= new ParentPerson(), request.Mother);
        if (entity.MotherId is null)
        {
            db.ParentPersons.Add(entity.Mother);
            entity.MotherId = entity.Mother.Id;
        }

        await db.SaveChangesAsync(cancellationToken);
        await auditService.LogAsync("Updated", "Child", entity.Id.ToString(), old,
            new { entity.FullName, entity.ClassId, entity.IsActive }, userId, userName, ip, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, Guid? userId, string? userName, string? ip, CancellationToken cancellationToken = default)
    {
        var entity = await db.Children
            .Include(c => c.Father)
            .Include(c => c.Mother)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Child not found");

        await files.DeleteAsync(entity.PhotoPath, cancellationToken);
        if (entity.Father is not null)
        {
            await files.DeleteAsync(entity.Father.PhotoPath, cancellationToken);
            db.ParentPersons.Remove(entity.Father);
        }

        if (entity.Mother is not null)
        {
            await files.DeleteAsync(entity.Mother.PhotoPath, cancellationToken);
            db.ParentPersons.Remove(entity.Mother);
        }

        db.Children.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
        await auditService.LogAsync("Deleted", "Child", id.ToString(), new { entity.Code, entity.FullName }, null,
            userId, userName, ip, cancellationToken);
    }

    public async Task<object> UploadChildPhotoAsync(Guid id, IFormFile file, Guid? userId, string? userName, string? ip, CancellationToken cancellationToken = default)
    {
        var entity = await db.Children.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Child not found");

        await files.DeleteAsync(entity.PhotoPath, cancellationToken);
        entity.PhotoPath = await files.SaveAsync(file, "children", cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await auditService.LogAsync("PhotoUpdated", "Child", id.ToString(), null, new { entity.PhotoPath },
            userId, userName, ip, cancellationToken);
        return new { entity.Id, entity.PhotoPath };
    }

    public async Task<object> UploadParentPhotoAsync(Guid id, string parentRole, IFormFile file, Guid? userId, string? userName, string? ip, CancellationToken cancellationToken = default)
    {
        var role = parentRole.Trim().ToLowerInvariant();
        if (role is not ("father" or "mother"))
        {
            throw new InvalidOperationException("parentRole must be father or mother");
        }

        var entity = await db.Children
            .Include(c => c.Father)
            .Include(c => c.Mother)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Child not found");

        var parent = role == "father" ? entity.Father : entity.Mother;
        if (parent is null)
        {
            throw new InvalidOperationException("Parent record is missing; save child parents first");
        }

        await files.DeleteAsync(parent.PhotoPath, cancellationToken);
        parent.PhotoPath = await files.SaveAsync(file, "parents", cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await auditService.LogAsync("ParentPhotoUpdated", "Child", id.ToString(), null,
            new { role, parent.PhotoPath }, userId, userName, ip, cancellationToken);
        return new { entity.Id, role, parent.PhotoPath };
    }

    public async Task<object> GenerateEntryCodeAsync(Guid id, Guid? userId, string? userName, string? ip, CancellationToken cancellationToken = default)
    {
        var entity = await db.Children.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Child not found");

        var code = await CodeGenerator.NextEntryCodeAsync(db, cancellationToken);
        entity.EntryQrCode = code;
        entity.EntryBarcode = code;
        await db.SaveChangesAsync(cancellationToken);
        await auditService.LogAsync("EntryCodeGenerated", "Child", id.ToString(), null,
            new { code }, userId, userName, ip, cancellationToken);
        return new { entity.Id, entryQrCode = entity.EntryQrCode, entryBarcode = entity.EntryBarcode };
    }

    public async Task<object> SetEntryCodeAsync(Guid id, string code, Guid? userId, string? userName, string? ip, CancellationToken cancellationToken = default)
    {
        var value = code.Trim();
        if (string.IsNullOrWhiteSpace(value) || value.Length > 50)
        {
            throw new InvalidOperationException("Entry code is required (max 50 chars)");
        }

        var entity = await db.Children.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Child not found");

        var clash = await db.Children.AsNoTracking().AnyAsync(c =>
            c.Id != id &&
            (c.EntryQrCode == value || c.EntryBarcode == value ||
             c.QrCode == value || c.Barcode == value || c.Code == value), cancellationToken);
        if (clash)
        {
            throw new InvalidOperationException("Entry code is already in use");
        }

        entity.EntryQrCode = value;
        entity.EntryBarcode = value;
        await db.SaveChangesAsync(cancellationToken);
        await auditService.LogAsync("EntryCodeSet", "Child", id.ToString(), null,
            new { value }, userId, userName, ip, cancellationToken);
        return new { entity.Id, entryQrCode = entity.EntryQrCode, entryBarcode = entity.EntryBarcode };
    }

    public async Task ClearEntryCodeAsync(Guid id, Guid? userId, string? userName, string? ip, CancellationToken cancellationToken = default)
    {
        var entity = await db.Children.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Child not found");

        entity.EntryQrCode = null;
        entity.EntryBarcode = null;
        await db.SaveChangesAsync(cancellationToken);
        await auditService.LogAsync("EntryCodeCleared", "Child", id.ToString(), null, null,
            userId, userName, ip, cancellationToken);
    }

    private static void ValidateRequest(ChildRequest request)
    {
        ValidateParent(request.Father, "Father");
        ValidateParent(request.Mother, "Mother");

        var relation = NormalizeOptional(request.Guardian?.Relation);
        if (relation is not null &&
            !GuardianRelations.All.Contains(relation, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Invalid guardian relation");
        }
    }

    private static string? NormalizeOptional(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private static void ValidateParent(ParentPersonRequest parent, string label)
    {
        if (string.IsNullOrWhiteSpace(parent.FullName) || string.IsNullOrWhiteSpace(parent.Phone))
        {
            throw new InvalidOperationException($"{label} name and phone are required");
        }

        if (!string.IsNullOrWhiteSpace(parent.NationalId) &&
            (parent.NationalId.Length != 14 || !parent.NationalId.All(char.IsDigit)))
        {
            throw new InvalidOperationException($"{label} national ID must be 14 digits");
        }
    }

    private async Task EnsureRefsAsync(ChildRequest request, CancellationToken cancellationToken)
    {
        if (request.ClassId.HasValue &&
            !await db.Classes.AnyAsync(c => c.Id == request.ClassId, cancellationToken))
        {
            throw new InvalidOperationException("Class is invalid");
        }

        if (request.PricePlanId.HasValue &&
            !await db.PricePlans.AnyAsync(p => p.Id == request.PricePlanId, cancellationToken))
        {
            throw new InvalidOperationException("Price plan is invalid");
        }
    }

    private static ParentPerson ToParent(ParentPersonRequest request)
    {
        var p = new ParentPerson();
        ApplyParent(p, request);
        return p;
    }

    private static void ApplyParent(ParentPerson entity, ParentPersonRequest request)
    {
        entity.FullName = request.FullName.Trim();
        entity.Phone = request.Phone.Trim();
        entity.Email = request.Email?.Trim();
        entity.NationalId = string.IsNullOrWhiteSpace(request.NationalId) ? null : request.NationalId.Trim();
    }

    private static object MapDetail(Child c) => new
    {
        c.Id,
        c.Code,
        c.FullName,
        c.BirthDate,
        c.Gender,
        c.PhotoPath,
        father = c.Father is null ? null : MapParent(c.Father),
        mother = c.Mother is null ? null : MapParent(c.Mother),
        guardian = new
        {
            name = c.GuardianName,
            phone = c.GuardianPhone,
            email = c.GuardianEmail,
            relation = c.GuardianRelation
        },
        c.ClassId,
        className = c.Class?.Name,
        c.PricePlanId,
        pricePlanName = c.PricePlan?.Name,
        c.QrCode,
        c.Barcode,
        c.EntryQrCode,
        c.EntryBarcode,
        c.Notes,
        c.IsActive,
        c.CreatedAt
    };

    private static object MapParent(ParentPerson p) => new
    {
        p.Id,
        p.FullName,
        p.Phone,
        p.Email,
        p.NationalId,
        p.PhotoPath
    };
}
