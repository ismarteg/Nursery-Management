namespace NurseryManagement.Api.Domain.Entities;

public enum SalaryPaymentStatus
{
    Calculated = 0,
    Paid = 1,
    Cancelled = 2
}

public class SalaryPayment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StaffId { get; set; }
    public StaffMember Staff { get; set; } = null!;
    public int Year { get; set; }
    public int Month { get; set; }
    public int WorkedDays { get; set; }
    public decimal WorkedHours { get; set; }
    public decimal Amount { get; set; }
    public SalaryType SalaryTypeSnapshot { get; set; }
    public SalaryPaymentStatus Status { get; set; } = SalaryPaymentStatus.Calculated;
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; set; }
    public string? Notes { get; set; }
}
