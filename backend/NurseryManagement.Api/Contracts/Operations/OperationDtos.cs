using System.ComponentModel.DataAnnotations;
using NurseryManagement.Api.Domain.Entities;

namespace NurseryManagement.Api.Contracts.Operations;

public class ClassRequest
{
    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Range(1, 500)]
    public int Capacity { get; set; } = 20;

    public int? MinAgeMonths { get; set; }
    public int? MaxAgeMonths { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;

    public List<Guid> StaffIds { get; set; } = [];
}

public class StaffRequest
{
    [Required, MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Phone { get; set; }

    [EmailAddress, MaxLength(256)]
    public string? Email { get; set; }

    public DateTime? HireDate { get; set; }
    public SalaryType SalaryType { get; set; } = SalaryType.Monthly;

    [Range(0, 9999999)]
    public decimal BaseSalary { get; set; }

    public decimal? HourlyRate { get; set; }
    public decimal? DailyRate { get; set; }
    public bool IsActive { get; set; } = true;
    public List<Guid> ClassIds { get; set; } = [];
}

public class ParentPersonRequest
{
    [Required, MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string Phone { get; set; } = string.Empty;

    [EmailAddress, MaxLength(256)]
    public string? Email { get; set; }

    [RegularExpression(@"^(\d{14})?$", ErrorMessage = "National ID must be exactly 14 digits")]
    public string? NationalId { get; set; }
}

public class GuardianRequest
{
    [MaxLength(200)]
    public string? Name { get; set; }

    [MaxLength(50)]
    public string? Phone { get; set; }

    [MaxLength(50)]
    public string? Relation { get; set; }

    [EmailAddress, MaxLength(256)]
    public string? Email { get; set; }
}

public class ChildRequest
{
    [Required, MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    public DateTime? BirthDate { get; set; }

    [MaxLength(20)]
    public string? Gender { get; set; }

    [Required]
    public ParentPersonRequest Father { get; set; } = new();

    [Required]
    public ParentPersonRequest Mother { get; set; } = new();

    /// <summary>Optional pickup contact. Omit or leave fields empty when not used.</summary>
    public GuardianRequest? Guardian { get; set; }

    public Guid? ClassId { get; set; }
    public Guid? PricePlanId { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;
}

public class SetEntryCodeRequest
{
    [Required, MaxLength(50)]
    public string Code { get; set; } = string.Empty;
}

public class PricePlanRequest
{
    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Range(0, 9999999)]
    public decimal Amount { get; set; }

    public BillingCycle BillingCycle { get; set; } = BillingCycle.Monthly;

    [Range(1, 3660)]
    public int DaysCount { get; set; } = 30;

    public bool IsActive { get; set; } = true;
}

public class PaymentRequest
{
    [Required]
    public Guid ChildId { get; set; }

    public Guid? PricePlanId { get; set; }

    [Range(0.01, 9999999)]
    public decimal AmountDue { get; set; }

    [Range(0, 9999999)]
    public decimal AmountPaid { get; set; }

    [Required]
    public DateTime DueDate { get; set; }

    public DateTime? PaidAt { get; set; }
    public PaymentStatus? Status { get; set; }
    public PaymentMethod? Method { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}

public class RecordPaymentRequest
{
    [Range(0.01, 9999999)]
    public decimal Amount { get; set; }

    public PaymentMethod Method { get; set; } = PaymentMethod.Cash;
    public DateTime? PaidAt { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}

public class ExpenseCategoryRequest
{
    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}

public class ExpenseRequest
{
    [Required]
    public Guid CategoryId { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Range(0.01, 9999999)]
    public decimal Amount { get; set; }

    [Required]
    public DateTime ExpenseDate { get; set; }

    [MaxLength(200)]
    public string? PaidTo { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }
}
