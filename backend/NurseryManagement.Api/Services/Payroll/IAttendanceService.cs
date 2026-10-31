namespace NurseryManagement.Api.Services.Payroll;

public interface IAttendanceService
{
    Task<IReadOnlyList<object>> GetAllAsync(Guid? staffId, int? year, int? month, CancellationToken cancellationToken = default);
    Task<object> UploadAsync(IFormFile file, Guid? userId, string? userName, string? ip, CancellationToken cancellationToken = default);
}
