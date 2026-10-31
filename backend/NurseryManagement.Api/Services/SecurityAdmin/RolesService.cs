using Microsoft.EntityFrameworkCore;
using NurseryManagement.Api.Contracts.Roles;
using NurseryManagement.Api.Data;
using NurseryManagement.Api.Domain.Entities;
using NurseryManagement.Api.Security;

namespace NurseryManagement.Api.Services.SecurityAdmin;

public class RolesService(AppDbContext db, IAuditService auditService) : IRolesService
{
    public async Task<IReadOnlyList<object>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var roles = await db.Roles
            .AsNoTracking()
            .Select(r => new
            {
                r.Id,
                r.Name,
                r.Description,
                permissions = r.RolePermissions.Select(rp => rp.Permission.Code).OrderBy(c => c).ToList(),
                permissionIds = r.RolePermissions.Select(rp => rp.PermissionId).ToList(),
                usersCount = r.UserRoles.Count,
                isSystem = r.Name == "Admin"
            })
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);

        return roles.Cast<object>().ToList();
    }

    public async Task<IReadOnlyList<object>> GetPermissionsAsync(CancellationToken cancellationToken = default)
    {
        var permissions = await db.Permissions
            .AsNoTracking()
            .OrderBy(p => p.Module)
            .ThenBy(p => p.Code)
            .Select(p => new { p.Id, p.Code, p.Name, p.Module })
            .ToListAsync(cancellationToken);

        return permissions.Cast<object>().ToList();
    }

    public async Task<object?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var role = await db.Roles
            .AsNoTracking()
            .Where(r => r.Id == id)
            .Select(r => new
            {
                r.Id,
                r.Name,
                r.Description,
                permissions = r.RolePermissions.Select(rp => rp.Permission.Code).OrderBy(c => c).ToList(),
                permissionIds = r.RolePermissions.Select(rp => rp.PermissionId).ToList(),
                isSystem = r.Name == "Admin"
            })
            .FirstOrDefaultAsync(cancellationToken);

        return role;
    }

    public async Task<Guid> CreateAsync(CreateRoleRequest request, Guid? userId, string? userName, string? ip, CancellationToken cancellationToken = default)
    {
        var name = request.Name.Trim();
        if (await db.Roles.AnyAsync(r => r.Name == name, cancellationToken))
        {
            throw new ConflictException("Role name already exists");
        }

        var permissionIds = await ValidatePermissionIdsAsync(request.PermissionIds, cancellationToken)
            ?? throw new InvalidOperationException("One or more permissions are invalid");

        var role = new Role
        {
            Name = name,
            Description = request.Description?.Trim()
        };

        db.Roles.Add(role);
        await db.SaveChangesAsync(cancellationToken);

        db.RolePermissions.AddRange(permissionIds.Select(permissionId => new RolePermission
        {
            RoleId = role.Id,
            PermissionId = permissionId
        }));
        await db.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync("Created", "Role", role.Id.ToString(), null, new
        {
            role.Name,
            role.Description,
            PermissionIds = permissionIds
        }, userId, userName, ip, cancellationToken);

        return role.Id;
    }

    public async Task UpdateAsync(Guid id, UpdateRoleRequest request, Guid? userId, string? userName, string? ip, CancellationToken cancellationToken = default)
    {
        var role = await db.Roles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Role not found");

        var name = request.Name.Trim();
        if (role.Name == "Admin" && !string.Equals(name, "Admin", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Admin role name cannot be changed");
        }

        if (await db.Roles.AnyAsync(r => r.Id != id && r.Name == name, cancellationToken))
        {
            throw new ConflictException("Role name already exists");
        }

        var permissionIds = await ValidatePermissionIdsAsync(request.PermissionIds, cancellationToken)
            ?? throw new InvalidOperationException("One or more permissions are invalid");

        if (role.Name == "Admin")
        {
            var manageRolesPermission = await db.Permissions
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Code == PermissionPolicies.RolesManage, cancellationToken);

            if (manageRolesPermission is not null && !permissionIds.Contains(manageRolesPermission.Id))
            {
                throw new InvalidOperationException("Admin must keep roles.manage permission");
            }
        }

        var oldValues = new
        {
            role.Name,
            role.Description,
            PermissionIds = role.RolePermissions.Select(rp => rp.PermissionId).ToList()
        };

        role.Name = name;
        role.Description = request.Description?.Trim();

        db.RolePermissions.RemoveRange(role.RolePermissions);
        db.RolePermissions.AddRange(permissionIds.Select(permissionId => new RolePermission
        {
            RoleId = role.Id,
            PermissionId = permissionId
        }));

        await db.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync("Updated", "Role", role.Id.ToString(), oldValues, new
        {
            role.Name,
            role.Description,
            PermissionIds = permissionIds
        }, userId, userName, ip, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, Guid? userId, string? userName, string? ip, CancellationToken cancellationToken = default)
    {
        var role = await db.Roles
            .Include(r => r.RolePermissions)
            .Include(r => r.UserRoles)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Role not found");

        if (role.Name == "Admin")
        {
            throw new InvalidOperationException("Admin role cannot be deleted");
        }

        if (role.UserRoles.Count > 0)
        {
            throw new InvalidOperationException("Role is assigned to users. Reassign users first.");
        }

        var snapshot = new
        {
            role.Name,
            role.Description,
            PermissionIds = role.RolePermissions.Select(rp => rp.PermissionId).ToList()
        };

        db.RolePermissions.RemoveRange(role.RolePermissions);
        db.Roles.Remove(role);
        await db.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync("Deleted", "Role", id.ToString(), snapshot, null, userId, userName, ip, cancellationToken);
    }

    private async Task<List<Guid>?> ValidatePermissionIdsAsync(List<Guid> permissionIds, CancellationToken cancellationToken)
    {
        var distinct = permissionIds.Distinct().ToList();
        if (distinct.Count == 0)
        {
            return distinct;
        }

        var count = await db.Permissions.CountAsync(p => distinct.Contains(p.Id), cancellationToken);
        return count == distinct.Count ? distinct : null;
    }
}
