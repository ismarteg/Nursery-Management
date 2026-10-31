using NurseryManagement.Api.Contracts.Operations;

namespace NurseryManagement.Api.Services.Children;

public interface IChildrenService
{
    Task<IReadOnlyList<object>> GetAllAsync(Guid? classId, CancellationToken cancellationToken = default);
    Task<object?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<object> CreateAsync(ChildRequest request, Guid? userId, string? userName, string? ip, CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid id, ChildRequest request, Guid? userId, string? userName, string? ip, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, Guid? userId, string? userName, string? ip, CancellationToken cancellationToken = default);
    Task<object> UploadChildPhotoAsync(Guid id, IFormFile file, Guid? userId, string? userName, string? ip, CancellationToken cancellationToken = default);
    Task<object> UploadParentPhotoAsync(Guid id, string parentRole, IFormFile file, Guid? userId, string? userName, string? ip, CancellationToken cancellationToken = default);
    Task<object> GenerateEntryCodeAsync(Guid id, Guid? userId, string? userName, string? ip, CancellationToken cancellationToken = default);
    Task<object> SetEntryCodeAsync(Guid id, string code, Guid? userId, string? userName, string? ip, CancellationToken cancellationToken = default);
    Task ClearEntryCodeAsync(Guid id, Guid? userId, string? userName, string? ip, CancellationToken cancellationToken = default);
}
