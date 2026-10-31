namespace NurseryManagement.Api.Services;

public interface IFileStorageService
{
    Task<string> SaveAsync(IFormFile file, string folder, CancellationToken cancellationToken = default);
    Task DeleteAsync(string? relativePath, CancellationToken cancellationToken = default);
    string? GetAbsolutePath(string? relativePath);
    bool Exists(string? relativePath);
}
