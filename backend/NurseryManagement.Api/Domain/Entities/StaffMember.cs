namespace NurseryManagement.Api.Domain.Entities;

public enum SalaryType
{
    Monthly = 0,
    Daily = 1,
    Hourly = 2
}

public class StaffMember
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public DateTime? HireDate { get; set; }
    public SalaryType SalaryType { get; set; } = SalaryType.Monthly;
    public decimal BaseSalary { get; set; }
    public decimal? HourlyRate { get; set; }
    public decimal? DailyRate { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<StaffClassAssignment> ClassAssignments { get; set; } = new List<StaffClassAssignment>();
}
