namespace NurseryManagement.Api.Services;

public class FileStorageService(IWebHostEnvironment env, IConfiguration configuration) : IFileStorageService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp"
    };

    private const long MaxBytes = 2 * 1024 * 1024;

    private string RootPath =>
        Path.GetFullPath(Path.Combine(
            env.ContentRootPath,
            configuration["Storage:Root"] ?? "App_Data/uploads"));

    public async Task<string> SaveAsync(IFormFile file, string folder, CancellationToken cancellationToken = default)
    {
        if (file.Length <= 0)
        {
            throw new InvalidOperationException("Empty file");
        }

        if (file.Length > MaxBytes)
        {
            throw new InvalidOperationException("File exceeds 2MB limit");
        }

        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(ext) || !AllowedExtensions.Contains(ext))
        {
            throw new InvalidOperationException("Only jpg, png, webp are allowed");
        }

        var safeFolder = string.Join("_", folder.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        var dir = Path.Combine(RootPath, safeFolder);
        Directory.CreateDirectory(dir);

        var fileName = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
        var absolute = Path.Combine(dir, fileName);
        await using var stream = File.Create(absolute);
        await file.CopyToAsync(stream, cancellationToken);

        return Path.Combine(safeFolder, fileName).Replace('\\', '/');
    }

    public Task DeleteAsync(string? relativePath, CancellationToken cancellationToken = default)
    {
        var absolute = GetAbsolutePath(relativePath);
        if (absolute is not null && File.Exists(absolute))
        {
            File.Delete(absolute);
        }

        return Task.CompletedTask;
    }

    public string? GetAbsolutePath(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return null;
        }

        var normalized = relativePath.Replace('\\', '/').TrimStart('/');
        if (normalized.Contains("..", StringComparison.Ordinal))
        {
            return null;
        }

        var absolute = Path.GetFullPath(Path.Combine(RootPath, normalized));
        if (!absolute.StartsWith(RootPath, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return absolute;
    }

    public bool Exists(string? relativePath)
    {
        var absolute = GetAbsolutePath(relativePath);
        return absolute is not null && File.Exists(absolute);
    }
}
