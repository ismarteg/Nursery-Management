using NurseryManagement.Api.Contracts.Operations;

namespace NurseryManagement.Api.Services.Classes;

public interface IClassesService
{
    Task<IReadOnlyList<object>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<object> CreateAsync(ClassRequest request, Guid? userId, string? userName, string? ip, CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid id, ClassRequest request, Guid? userId, string? userName, string? ip, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, Guid? userId, string? userName, string? ip, CancellationToken cancellationToken = default);
}
