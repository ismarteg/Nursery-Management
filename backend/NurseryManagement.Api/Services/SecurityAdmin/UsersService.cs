using Microsoft.EntityFrameworkCore;
using NurseryManagement.Api.Contracts.Users;
using NurseryManagement.Api.Data;
using NurseryManagement.Api.Domain.Entities;

namespace NurseryManagement.Api.Services.SecurityAdmin;

public class UsersService(AppDbContext db, IAuditService auditService) : IUsersService
{
    public async Task<IReadOnlyList<object>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var users = await db.Users
            .AsNoTracking()
            .Select(u => new
            {
                u.Id,
                u.UserName,
                u.FullName,
                u.Email,
                u.IsActive,
                u.CreatedAt,
                roles = u.UserRoles.Select(ur => ur.Role.Name).OrderBy(n => n).ToList(),
                roleIds = u.UserRoles.Select(ur => ur.RoleId).ToList()
            })
            .OrderBy(u => u.UserName)
            .ToListAsync(cancellationToken);

        return users.Cast<object>().ToList();
    }

    public async Task<object?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await db.Users
            .AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new
            {
                u.Id,
                u.UserName,
                u.FullName,
                u.Email,
                u.IsActive,
                u.CreatedAt,
                roles = u.UserRoles.Select(ur => ur.Role.Name).OrderBy(n => n).ToList(),
                roleIds = u.UserRoles.Select(ur => ur.RoleId).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        return user;
    }

    public async Task<Guid> CreateAsync(CreateUserRequest request, Guid? userId, string? userName, string? ip, CancellationToken cancellationToken = default)
    {
        var newUserName = request.UserName.Trim();
        var email = request.Email.Trim();

        if (await db.Users.AnyAsync(u => u.UserName == newUserName || u.Email == email, cancellationToken))
        {
            throw new ConflictException("Username or email already exists");
        }

        var roleIds = await ValidateRoleIdsAsync(request.RoleIds, cancellationToken)
            ?? throw new InvalidOperationException("One or more roles are invalid");

        var user = new User
        {
            UserName = newUserName,
            FullName = request.FullName.Trim(),
            Email = email,
            IsActive = request.IsActive,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        db.UserRoles.AddRange(roleIds.Select(roleId => new UserRole
        {
            UserId = user.Id,
            RoleId = roleId
        }));
        await db.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync("Created", "User", user.Id.ToString(), null, new
        {
            user.UserName,
            user.FullName,
            user.Email,
            user.IsActive,
            RoleIds = roleIds
        }, userId, userName, ip, cancellationToken);

        return user.Id;
    }

    public async Task UpdateAsync(Guid id, UpdateUserRequest request, Guid? userId, string? userName, string? ip, CancellationToken cancellationToken = default)
    {
        var user = await db.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("User not found");

        var email = request.Email.Trim();
        if (await db.Users.AnyAsync(u => u.Id != id && u.Email == email, cancellationToken))
        {
            throw new ConflictException("Email already exists");
        }

        var roleIds = await ValidateRoleIdsAsync(request.RoleIds, cancellationToken)
            ?? throw new InvalidOperationException("One or more roles are invalid");

        var oldValues = new
        {
            user.FullName,
            user.Email,
            user.IsActive,
            RoleIds = user.UserRoles.Select(ur => ur.RoleId).ToList()
        };

        user.FullName = request.FullName.Trim();
        user.Email = email;
        user.IsActive = request.IsActive;

        db.UserRoles.RemoveRange(user.UserRoles);
        db.UserRoles.AddRange(roleIds.Select(roleId => new UserRole
        {
            UserId = user.Id,
            RoleId = roleId
        }));

        await db.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync("Updated", "User", user.Id.ToString(), oldValues, new
        {
            user.FullName,
            user.Email,
            user.IsActive,
            RoleIds = roleIds
        }, userId, userName, ip, cancellationToken);
    }

    public async Task ResetPasswordAsync(Guid id, ResetPasswordRequest request, Guid? userId, string? userName, string? ip, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("User not found");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await db.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync("PasswordReset", "User", user.Id.ToString(), null,
            new { user.UserName }, userId, userName, ip, cancellationToken);
    }

    public async Task<object> ToggleActiveAsync(Guid id, Guid? userId, string? userName, string? ip, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("User not found");

        if (userId.HasValue && userId.Value == user.Id && user.IsActive)
        {
            throw new InvalidOperationException("You cannot deactivate your own account");
        }

        var old = user.IsActive;
        user.IsActive = !user.IsActive;
        await db.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync("StatusChanged", "User", user.Id.ToString(),
            new { IsActive = old }, new { user.IsActive }, userId, userName, ip, cancellationToken);
        return new { user.Id, user.IsActive };
    }

    public async Task DeleteAsync(Guid id, Guid? userId, string? userName, string? ip, CancellationToken cancellationToken = default)
    {
        var user = await db.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("User not found");

        if (userId.HasValue && userId.Value == user.Id)
        {
            throw new InvalidOperationException("You cannot delete your own account");
        }

        var snapshot = new
        {
            user.UserName,
            user.FullName,
            user.Email,
            user.IsActive,
            RoleIds = user.UserRoles.Select(ur => ur.RoleId).ToList()
        };

        db.UserRoles.RemoveRange(user.UserRoles);
        db.Users.Remove(user);
        await db.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync("Deleted", "User", id.ToString(), snapshot, null, userId, userName, ip, cancellationToken);
    }

    private async Task<List<Guid>?> ValidateRoleIdsAsync(List<Guid> roleIds, CancellationToken cancellationToken)
    {
        var distinct = roleIds.Distinct().ToList();
        if (distinct.Count == 0)
        {
            return distinct;
        }

        var count = await db.Roles.CountAsync(r => distinct.Contains(r.Id), cancellationToken);
        return count == distinct.Count ? distinct : null;
    }
}
