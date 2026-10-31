namespace NurseryManagement.Api.Domain.Entities;

public enum BillingCycle
{
    Monthly = 0,
    Term = 1,
    Yearly = 2,
    Daily = 3
}

public class PricePlan
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public BillingCycle BillingCycle { get; set; } = BillingCycle.Monthly;

    /// <summary>Number of days covered by this plan's billing period.</summary>
    public int DaysCount { get; set; } = 30;

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Child> Children { get; set; } = new List<Child>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
