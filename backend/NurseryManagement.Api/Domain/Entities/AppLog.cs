namespace NurseryManagement.Api.Domain.Entities;

public enum AppLogType
{
    Info = 0,
    Success = 1,
    Warning = 2,
    Error = 3
}

public class AppLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public AppLogType LogType { get; set; } = AppLogType.Info;
    public string Module { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }
    public bool Succeeded { get; set; } = true;
    public int? StatusCode { get; set; }
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
    public string? IpAddress { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
