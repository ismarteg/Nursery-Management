namespace NurseryManagement.Api.Domain.Entities;

public class Expense
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CategoryId { get; set; }
    public ExpenseCategory Category { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime ExpenseDate { get; set; } = DateTime.UtcNow.Date;
    public string? Notes { get; set; }
    public string? PaidTo { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
