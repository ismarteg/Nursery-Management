using NurseryManagement.Api.Contracts.Users;

namespace NurseryManagement.Api.Services.SecurityAdmin;

public interface IProfileService
{
    Task<object?> GetAsync(Guid userId, CancellationToken cancellationToken = default);
    Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, string? userName, string? ip, CancellationToken cancellationToken = default);
}
