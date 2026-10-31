namespace NurseryManagement.Api.Domain.Entities;

public class NurseryClass
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public int Capacity { get; set; } = 20;
    public int? MinAgeMonths { get; set; }
    public int? MaxAgeMonths { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Child> Children { get; set; } = new List<Child>();
    public ICollection<StaffClassAssignment> StaffAssignments { get; set; } = new List<StaffClassAssignment>();
}
