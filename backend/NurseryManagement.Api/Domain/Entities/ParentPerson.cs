namespace NurseryManagement.Api.Domain.Entities;

public class ParentPerson
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? NationalId { get; set; }
    public string? PhotoPath { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
