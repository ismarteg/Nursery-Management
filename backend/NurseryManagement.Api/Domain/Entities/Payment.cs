namespace NurseryManagement.Api.Domain.Entities;

public enum PaymentStatus
{
    Pending = 0,
    Partial = 1,
    Paid = 2,
    Overdue = 3,
    Cancelled = 4
}

public enum PaymentMethod
{
    Cash = 0,
    Card = 1,
    Transfer = 2,
    Other = 3
}

public class Payment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ChildId { get; set; }
    public Child Child { get; set; } = null!;
    public Guid? PricePlanId { get; set; }
    public PricePlan? PricePlan { get; set; }
    public string? PlanNameSnapshot { get; set; }
    public decimal AmountDue { get; set; }
    public decimal AmountPaid { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? PaidAt { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public PaymentMethod? Method { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
