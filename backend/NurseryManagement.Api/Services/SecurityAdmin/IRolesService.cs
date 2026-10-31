using NurseryManagement.Api.Contracts.Roles;

namespace NurseryManagement.Api.Services.SecurityAdmin;

public interface IRolesService
{
    Task<IReadOnlyList<object>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<object>> GetPermissionsAsync(CancellationToken cancellationToken = default);
    Task<object?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Creates a role and returns the new role id.</summary>
    Task<Guid> CreateAsync(CreateRoleRequest request, Guid? userId, string? userName, string? ip, CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid id, UpdateRoleRequest request, Guid? userId, string? userName, string? ip, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, Guid? userId, string? userName, string? ip, CancellationToken cancellationToken = default);
}
