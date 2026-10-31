using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NurseryManagement.Api.Contracts.Auth;
using NurseryManagement.Api.Data;

namespace NurseryManagement.Api.Services;

public class AuthService(
    AppDbContext db,
    IConfiguration configuration,
    IAuditService auditService) : IAuthService
{
    public async Task<LoginResponse?> LoginAsync(
        LoginRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var user = await db.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .ThenInclude(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.UserName == request.UserName, cancellationToken);

        if (user is null || !user.IsActive || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            await auditService.LogAsync(
                "LoginFailed",
                "User",
                request.UserName,
                userName: request.UserName,
                ipAddress: ipAddress,
                cancellationToken: cancellationToken);
            return null;
        }

        var roles = user.UserRoles.Select(ur => ur.Role.Name).Distinct().OrderBy(x => x).ToList();
        var permissions = user.UserRoles
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission.Code)
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        var expiresMinutes = configuration.GetValue("Jwt:ExpiresMinutes", 480);
        var expiresAt = DateTime.UtcNow.AddMinutes(expiresMinutes);
        var token = CreateToken(user.Id, user.UserName, roles, permissions, expiresAt);

        user.LastLoginAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(
            "Login",
            "User",
            user.Id.ToString(),
            newValues: new { user.UserName, user.LastLoginAt },
            userId: user.Id,
            userName: user.UserName,
            ipAddress: ipAddress,
            cancellationToken: cancellationToken);

        return new LoginResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            User = new UserInfoDto
            {
                Id = user.Id,
                UserName = user.UserName,
                FullName = user.FullName,
                Email = user.Email,
                Roles = roles,
                Permissions = permissions
            }
        };
    }

    private string CreateToken(
        Guid userId,
        string userName,
        IEnumerable<string> roles,
        IEnumerable<string> permissions,
        DateTime expiresAt)
    {
        var key = configuration["Jwt:Key"] ?? "NurseryManagement_Dev_Secret_Key_ChangeMe_32+";
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, userName),
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, userName)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        claims.AddRange(permissions.Select(permission => new Claim("permission", permission)));

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"] ?? "NurseryManagement",
            audience: configuration["Jwt:Audience"] ?? "NurseryManagement",
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
