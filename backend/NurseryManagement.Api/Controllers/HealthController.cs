using Microsoft.AspNetCore.Mvc;

namespace NurseryManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new
    {
        status = "ok",
        app = "Nursery Management API",
        utc = DateTime.UtcNow
    });
}
