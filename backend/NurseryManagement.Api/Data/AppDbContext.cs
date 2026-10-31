using Microsoft.EntityFrameworkCore;
using NurseryManagement.Api.Domain.Entities;

namespace NurseryManagement.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<NurseryClass> Classes => Set<NurseryClass>();
    public DbSet<StaffMember> StaffMembers => Set<StaffMember>();
    public DbSet<StaffClassAssignment> StaffClassAssignments => Set<StaffClassAssignment>();
    public DbSet<Child> Children => Set<Child>();
    public DbSet<ParentPerson> ParentPersons => Set<ParentPerson>();
    public DbSet<EntryRecord> EntryRecords => Set<EntryRecord>();
    public DbSet<PricePlan> PricePlans => Set<PricePlan>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<ExpenseCategory> ExpenseCategories => Set<ExpenseCategory>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<AttendanceUploadBatch> AttendanceUploadBatches => Set<AttendanceUploadBatch>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<SalaryPayment> SalaryPayments => Set<SalaryPayment>();
    public DbSet<AppSettings> AppSettings => Set<AppSettings>();
    public DbSet<AppLog> AppLogs => Set<AppLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(x => x.UserName).IsUnique();
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.UserName).HasMaxLength(100);
            e.Property(x => x.Email).HasMaxLength(256);
            e.Property(x => x.FullName).HasMaxLength(200);
        });

        modelBuilder.Entity<Role>(e =>
        {
            e.HasIndex(x => x.Name).IsUnique();
            e.Property(x => x.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<Permission>(e =>
        {
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Code).HasMaxLength(100);
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.Module).HasMaxLength(100);
        });

        modelBuilder.Entity<UserRole>(e =>
        {
            e.HasKey(x => new { x.UserId, x.RoleId });
            e.HasOne(x => x.User).WithMany(x => x.UserRoles).HasForeignKey(x => x.UserId);
            e.HasOne(x => x.Role).WithMany(x => x.UserRoles).HasForeignKey(x => x.RoleId);
        });

        modelBuilder.Entity<RolePermission>(e =>
        {
            e.HasKey(x => new { x.RoleId, x.PermissionId });
            e.HasOne(x => x.Role).WithMany(x => x.RolePermissions).HasForeignKey(x => x.RoleId);
            e.HasOne(x => x.Permission).WithMany(x => x.RolePermissions).HasForeignKey(x => x.PermissionId);
        });

        modelBuilder.Entity<AuditLog>(e =>
        {
            e.Property(x => x.Action).HasMaxLength(50);
            e.Property(x => x.EntityName).HasMaxLength(100);
            e.Property(x => x.EntityId).HasMaxLength(100);
            e.Property(x => x.UserName).HasMaxLength(100);
            e.Property(x => x.IpAddress).HasMaxLength(64);
            e.HasIndex(x => x.OccurredAt);
        });

        modelBuilder.Entity<NurseryClass>(e =>
        {
            e.ToTable("Classes");
            e.HasIndex(x => x.Name).IsUnique();
            e.Property(x => x.Name).HasMaxLength(150);
            e.Property(x => x.Notes).HasMaxLength(1000);
        });

        modelBuilder.Entity<StaffMember>(e =>
        {
            e.ToTable("StaffMembers");
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Code).HasMaxLength(30);
            e.Property(x => x.FullName).HasMaxLength(200);
            e.Property(x => x.Phone).HasMaxLength(50);
            e.Property(x => x.Email).HasMaxLength(256);
            e.Property(x => x.BaseSalary).HasPrecision(18, 2);
            e.Property(x => x.HourlyRate).HasPrecision(18, 2);
            e.Property(x => x.DailyRate).HasPrecision(18, 2);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<StaffClassAssignment>(e =>
        {
            e.HasKey(x => new { x.StaffId, x.ClassId });
            e.HasOne(x => x.Staff).WithMany(x => x.ClassAssignments).HasForeignKey(x => x.StaffId);
            e.HasOne(x => x.Class).WithMany(x => x.StaffAssignments).HasForeignKey(x => x.ClassId);
        });

        modelBuilder.Entity<ParentPerson>(e =>
        {
            e.ToTable("ParentPersons");
            e.Property(x => x.FullName).HasMaxLength(200);
            e.Property(x => x.Phone).HasMaxLength(50);
            e.Property(x => x.Email).HasMaxLength(256);
            e.Property(x => x.NationalId).HasMaxLength(14);
            e.Property(x => x.PhotoPath).HasMaxLength(500);
            e.HasIndex(x => x.NationalId);
        });

        modelBuilder.Entity<Child>(e =>
        {
            e.ToTable("Children");
            e.HasIndex(x => x.Code).IsUnique();
            e.HasIndex(x => x.QrCode).IsUnique();
            e.HasIndex(x => x.Barcode).IsUnique();
            e.HasIndex(x => x.EntryQrCode).IsUnique();
            e.HasIndex(x => x.EntryBarcode).IsUnique();
            e.Property(x => x.Code).HasMaxLength(30);
            e.Property(x => x.FullName).HasMaxLength(200);
            e.Property(x => x.Gender).HasMaxLength(20);
            e.Property(x => x.PhotoPath).HasMaxLength(500);
            e.Property(x => x.GuardianName).HasMaxLength(200);
            e.Property(x => x.GuardianPhone).HasMaxLength(50);
            e.Property(x => x.GuardianEmail).HasMaxLength(256);
            e.Property(x => x.GuardianRelation).HasMaxLength(50);
            e.Property(x => x.QrCode).HasMaxLength(50);
            e.Property(x => x.Barcode).HasMaxLength(50);
            e.Property(x => x.EntryQrCode).HasMaxLength(50);
            e.Property(x => x.EntryBarcode).HasMaxLength(50);
            e.Property(x => x.Notes).HasMaxLength(1000);
            e.HasOne(x => x.Class).WithMany(x => x.Children).HasForeignKey(x => x.ClassId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.PricePlan).WithMany(x => x.Children).HasForeignKey(x => x.PricePlanId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Father).WithMany().HasForeignKey(x => x.FatherId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Mother).WithMany().HasForeignKey(x => x.MotherId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<EntryRecord>(e =>
        {
            e.ToTable("EntryRecords");
            e.Property(x => x.Notes).HasMaxLength(500);
            e.HasIndex(x => x.OccurredAt);
            e.HasIndex(x => new { x.ChildId, x.OccurredAt });
            e.HasOne(x => x.Child).WithMany().HasForeignKey(x => x.ChildId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PricePlan>(e =>
        {
            e.ToTable("PricePlans");
            e.HasIndex(x => x.Name).IsUnique();
            e.Property(x => x.Name).HasMaxLength(150);
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.Amount).HasPrecision(18, 2);
        });

        modelBuilder.Entity<Payment>(e =>
        {
            e.ToTable("Payments");
            e.Property(x => x.PlanNameSnapshot).HasMaxLength(150);
            e.Property(x => x.AmountDue).HasPrecision(18, 2);
            e.Property(x => x.AmountPaid).HasPrecision(18, 2);
            e.Property(x => x.Notes).HasMaxLength(500);
            e.HasIndex(x => x.DueDate);
            e.HasIndex(x => x.Status);
            e.HasOne(x => x.Child).WithMany().HasForeignKey(x => x.ChildId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.PricePlan).WithMany(x => x.Payments).HasForeignKey(x => x.PricePlanId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ExpenseCategory>(e =>
        {
            e.ToTable("ExpenseCategories");
            e.HasIndex(x => x.Name).IsUnique();
            e.Property(x => x.Name).HasMaxLength(150);
            e.Property(x => x.Description).HasMaxLength(500);
        });

        modelBuilder.Entity<Expense>(e =>
        {
            e.ToTable("Expenses");
            e.Property(x => x.Title).HasMaxLength(200);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.Notes).HasMaxLength(1000);
            e.Property(x => x.PaidTo).HasMaxLength(200);
            e.HasIndex(x => x.ExpenseDate);
            e.HasOne(x => x.Category).WithMany(x => x.Expenses).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AttendanceUploadBatch>(e =>
        {
            e.ToTable("AttendanceUploadBatches");
            e.Property(x => x.FileName).HasMaxLength(260);
        });

        modelBuilder.Entity<AttendanceRecord>(e =>
        {
            e.ToTable("AttendanceRecords");
            e.HasIndex(x => new { x.StaffId, x.WorkDate }).IsUnique();
            e.Ignore(x => x.WorkedHours);
            e.HasOne(x => x.Staff).WithMany().HasForeignKey(x => x.StaffId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.UploadBatch).WithMany(x => x.Records).HasForeignKey(x => x.UploadBatchId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<SalaryPayment>(e =>
        {
            e.ToTable("SalaryPayments");
            e.HasIndex(x => new { x.StaffId, x.Year, x.Month }).IsUnique();
            e.Property(x => x.WorkedHours).HasPrecision(18, 2);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.Notes).HasMaxLength(500);
            e.HasOne(x => x.Staff).WithMany().HasForeignKey(x => x.StaffId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AppSettings>(e =>
        {
            e.ToTable("AppSettings");
            e.Property(x => x.IdentityLabelSize).HasMaxLength(20);
            e.Property(x => x.PickupLabelSize).HasMaxLength(20);
            e.Property(x => x.PrintLayoutsJson).HasColumnType("TEXT");
        });

        modelBuilder.Entity<AppLog>(e =>
        {
            e.ToTable("AppLogs");
            e.Property(x => x.Module).HasMaxLength(100);
            e.Property(x => x.Action).HasMaxLength(50);
            e.Property(x => x.Message).HasMaxLength(500);
            e.Property(x => x.UserName).HasMaxLength(100);
            e.Property(x => x.IpAddress).HasMaxLength(64);
            e.HasIndex(x => x.OccurredAt);
            e.HasIndex(x => x.LogType);
        });
    }
}

