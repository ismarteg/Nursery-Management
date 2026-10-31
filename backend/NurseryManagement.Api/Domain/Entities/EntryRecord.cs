namespace NurseryManagement.Api.Domain.Entities;

public enum EntryType
{
    In = 0,
    Out = 1
}

public enum EntryMethod
{
    Qr = 0,
    Barcode = 1,
    Manual = 2
}

public class EntryRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ChildId { get; set; }
    public Child Child { get; set; } = null!;
    public EntryType Type { get; set; }
    public EntryMethod Method { get; set; } = EntryMethod.Manual;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
    public Guid? RecordedByUserId { get; set; }
}
