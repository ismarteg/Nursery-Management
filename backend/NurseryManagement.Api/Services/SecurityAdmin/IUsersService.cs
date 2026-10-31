using NurseryManagement.Api.Contracts.Users;

namespace NurseryManagement.Api.Services.SecurityAdmin;

public interface IUsersService
{
    Task<IReadOnlyList<object>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<object?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Creates a user and returns the new user id.</summary>
    Task<Guid> CreateAsync(CreateUserRequest request, Guid? userId, string? userName, string? ip, CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid id, UpdateUserRequest request, Guid? userId, string? userName, string? ip, CancellationToken cancellationToken = default);
    Task ResetPasswordAsync(Guid id, ResetPasswordRequest request, Guid? userId, string? userName, string? ip, CancellationToken cancellationToken = default);
    Task<object> ToggleActiveAsync(Guid id, Guid? userId, string? userName, string? ip, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, Guid? userId, string? userName, string? ip, CancellationToken cancellationToken = default);
}
