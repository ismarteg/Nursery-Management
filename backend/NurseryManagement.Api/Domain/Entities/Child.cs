namespace NurseryManagement.Api.Domain.Entities;

public static class GuardianRelations
{
    public static readonly string[] All =
    [
        "Brother",
        "Sister",
        "Grandfather",
        "Grandmother",
        "Uncle",
        "Aunt",
        "Other"
    ];
}

public class Child
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public DateTime? BirthDate { get; set; }
    public string? Gender { get; set; }
    public string? PhotoPath { get; set; }

    public Guid? FatherId { get; set; }
    public ParentPerson? Father { get; set; }
    public Guid? MotherId { get; set; }
    public ParentPerson? Mother { get; set; }

    public string GuardianName { get; set; } = string.Empty;
    public string? GuardianPhone { get; set; }
    public string? GuardianEmail { get; set; }
    public string? GuardianRelation { get; set; }

    public Guid? ClassId { get; set; }
    public NurseryClass? Class { get; set; }
    public Guid? PricePlanId { get; set; }
    public PricePlan? PricePlan { get; set; }

    /// <summary>Identity card QR (auto-generated with Code).</summary>
    public string QrCode { get; set; } = string.Empty;
    /// <summary>Identity card barcode (auto-generated with Code).</summary>
    public string Barcode { get; set; } = string.Empty;

    /// <summary>Pickup/entry QR given to parents.</summary>
    public string? EntryQrCode { get; set; }
    /// <summary>Pickup/entry barcode given to parents.</summary>
    public string? EntryBarcode { get; set; }

    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
