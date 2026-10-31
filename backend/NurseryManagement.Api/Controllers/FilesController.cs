using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NurseryManagement.Api.Services;

namespace NurseryManagement.Api.Controllers;

[ApiController]
[Route("api/files")]
[Authorize]
public class FilesController(IFileStorageService files) : ControllerBase
{
    [HttpGet("{*path}")]
    public IActionResult Get(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !files.Exists(path))
        {
            return NotFound();
        }

        var absolute = files.GetAbsolutePath(path)!;
        var contentType = Path.GetExtension(absolute).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "image/jpeg"
        };

        return PhysicalFile(absolute, contentType);
    }
}
