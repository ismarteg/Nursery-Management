namespace NurseryManagement.Api.Domain.Entities;

public class AttendanceUploadBatch
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FileName { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public Guid? UploadedByUserId { get; set; }
    public int TotalRows { get; set; }
    public int SuccessRows { get; set; }
    public int FailedRows { get; set; }
    public string? ErrorReport { get; set; }

    public ICollection<AttendanceRecord> Records { get; set; } = new List<AttendanceRecord>();
}

public class AttendanceRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StaffId { get; set; }
    public StaffMember Staff { get; set; } = null!;
    public DateTime WorkDate { get; set; }
    public TimeSpan ClockIn { get; set; }
    public TimeSpan ClockOut { get; set; }
    public Guid? UploadBatchId { get; set; }
    public AttendanceUploadBatch? UploadBatch { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public double WorkedHours => Math.Max(0, (ClockOut - ClockIn).TotalHours);
}
