namespace NurseryManagement.Api.Domain.Entities;

public class ExpenseCategory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
}
