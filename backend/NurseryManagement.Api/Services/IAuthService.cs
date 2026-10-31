using NurseryManagement.Api.Contracts.Auth;

namespace NurseryManagement.Api.Services;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken cancellationToken = default);
}
