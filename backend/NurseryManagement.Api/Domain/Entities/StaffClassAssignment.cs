namespace NurseryManagement.Api.Domain.Entities;

public class StaffClassAssignment
{
    public Guid StaffId { get; set; }
    public StaffMember Staff { get; set; } = null!;
    public Guid ClassId { get; set; }
    public NurseryClass Class { get; set; } = null!;
}
