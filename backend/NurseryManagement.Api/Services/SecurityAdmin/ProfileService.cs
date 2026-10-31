using Microsoft.EntityFrameworkCore;
using NurseryManagement.Api.Contracts.Users;
using NurseryManagement.Api.Data;

namespace NurseryManagement.Api.Services.SecurityAdmin;

public class ProfileService(AppDbContext db, IAuditService auditService) : IProfileService
{
    public async Task<object?> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new
            {
                u.Id,
                u.UserName,
                u.FullName,
                u.Email,
                u.IsActive,
                u.CreatedAt,
                u.LastLoginAt,
                roles = u.UserRoles.Select(ur => ur.Role.Name).OrderBy(n => n).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task ChangePasswordAsync(
        Guid userId,
        ChangePasswordRequest request,
        string? userName,
        string? ip,
        CancellationToken cancellationToken = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new KeyNotFoundException("User not found");

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
        {
            throw new InvalidOperationException("Current password is incorrect");
        }

        if (string.Equals(request.CurrentPassword, request.NewPassword, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("New password must be different from current password");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await db.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync("PasswordChanged", "User", user.Id.ToString(), null,
            new { user.UserName }, userId, userName, ip, cancellationToken);
    }
}
