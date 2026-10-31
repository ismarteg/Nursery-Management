using Microsoft.EntityFrameworkCore;
using NurseryManagement.Api.Domain.Entities;

namespace NurseryManagement.Api.Data;

public static class DbSeeder
{
    private static readonly (string Code, string Name, string Module)[] PermissionSeed =
    [
        ("roles.manage", "Manage roles", "Security"),
        ("audit.read", "View audit logs", "Security"),
        ("children.read", "View children", "Children"),
        ("children.write", "Manage children", "Children"),
        ("entry.scan", "Scan entry", "Entry"),
        ("classes.read", "View classes", "Classes"),
        ("classes.write", "Manage classes", "Classes"),
        ("staff.read", "View staff", "HR"),
        ("staff.write", "Manage staff", "HR"),
        ("payments.read", "View payments", "Finance"),
        ("payments.write", "Manage payments", "Finance"),
        ("priceplans.read", "View price plans", "Finance"),
        ("priceplans.write", "Manage price plans", "Finance"),
        ("expenses.read", "View expenses", "Finance"),
        ("expenses.write", "Manage expenses", "Finance"),
        ("salaries.read", "View salaries", "HR"),
        ("salaries.calculate", "Calculate salaries", "HR"),
        ("settings.read", "View settings", "System"),
        ("settings.write", "Manage settings", "System"),
        ("logs.read", "View application logs", "System"),
    ];

    public static async Task SeedAsync(AppDbContext db)
    {
        await db.Database.EnsureCreatedAsync();
        await EnsureEntryRecordsTableAsync(db);
        await EnsureFinanceTablesAsync(db);
        await EnsureExpenseTablesAsync(db);
        await EnsurePayrollTablesAsync(db);
        await EnsureChildFamilyTablesAsync(db);
        await EnsureAppSettingsTableAsync(db);
        await EnsureAppLogsTableAsync(db);
        await EnsureUserLastLoginColumnAsync(db);
        await EnsurePermissionsAsync(db);

        if (!db.Roles.Any())
        {
            var allPermissions = db.Permissions.ToList();
            var admin = new Role { Name = "Admin", Description = "Full system access" };
            var manager = new Role { Name = "Manager", Description = "Operations and finance" };
            var teacher = new Role { Name = "Teacher", Description = "Children and entry desk" };
            var accountant = new Role { Name = "Accountant", Description = "Payments, expenses, salaries" };

            db.Roles.AddRange(admin, manager, teacher, accountant);
            await db.SaveChangesAsync();

            db.RolePermissions.AddRange(allPermissions.Select(p => new RolePermission
            {
                RoleId = admin.Id,
                PermissionId = p.Id
            }));

            static IEnumerable<RolePermission> Map(Role role, IEnumerable<Permission> perms, params string[] codes) =>
                perms.Where(p => codes.Contains(p.Code))
                    .Select(p => new RolePermission { RoleId = role.Id, PermissionId = p.Id });

            db.RolePermissions.AddRange(Map(manager, allPermissions,
                "children.read", "children.write", "entry.scan", "classes.read", "classes.write",
                "staff.read", "staff.write",
                "payments.read", "payments.write", "priceplans.read", "priceplans.write",
                "expenses.read", "expenses.write",
                "salaries.read", "salaries.calculate", "settings.read", "settings.write",
                "logs.read", "audit.read"));

            db.RolePermissions.AddRange(Map(teacher, allPermissions,
                "children.read", "entry.scan", "classes.read", "staff.read"));

            db.RolePermissions.AddRange(Map(accountant, allPermissions,
                "payments.read", "payments.write", "priceplans.read", "priceplans.write",
                "expenses.read", "expenses.write",
                "salaries.read", "salaries.calculate", "staff.read"));

            await db.SaveChangesAsync();
        }
        else
        {
            await EnsureAdminHasAllPermissionsAsync(db);
            await EnsureRolePermissionCodesAsync(db, "Manager",
                "children.read", "children.write", "entry.scan", "classes.read", "classes.write",
                "staff.read", "staff.write",
                "payments.read", "payments.write", "priceplans.read", "priceplans.write",
                "expenses.read", "expenses.write",
                "salaries.read", "salaries.calculate", "settings.read", "settings.write",
                "logs.read", "audit.read");
            await EnsureRolePermissionCodesAsync(db, "Accountant",
                "payments.read", "payments.write", "priceplans.read", "priceplans.write",
                "expenses.read", "expenses.write",
                "salaries.read", "salaries.calculate", "staff.read");
        }

        if (!db.Users.Any())
        {
            var adminRole = db.Roles.Single(r => r.Name == "Admin");
            var adminUser = new User
            {
                UserName = "admin",
                Email = "admin@nursery.local",
                FullName = "System Admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123")
            };
            db.Users.Add(adminUser);
            await db.SaveChangesAsync();
            db.UserRoles.Add(new UserRole { UserId = adminUser.Id, RoleId = adminRole.Id });
            await db.SaveChangesAsync();
        }
    }

    private static async Task EnsureEntryRecordsTableAsync(AppDbContext db)
    {
        // EnsureCreated does not alter existing DBs; add EntryRecords for upgrades.
        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS "EntryRecords" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_EntryRecords" PRIMARY KEY,
                "ChildId" TEXT NOT NULL,
                "Type" INTEGER NOT NULL,
                "Method" INTEGER NOT NULL,
                "OccurredAt" TEXT NOT NULL,
                "Notes" TEXT NULL,
                "RecordedByUserId" TEXT NULL,
                CONSTRAINT "FK_EntryRecords_Children_ChildId" FOREIGN KEY ("ChildId") REFERENCES "Children" ("Id") ON DELETE CASCADE
            );
            CREATE INDEX IF NOT EXISTS "IX_EntryRecords_OccurredAt" ON "EntryRecords" ("OccurredAt");
            CREATE INDEX IF NOT EXISTS "IX_EntryRecords_ChildId_OccurredAt" ON "EntryRecords" ("ChildId", "OccurredAt");
            """);
    }

    private static async Task EnsureFinanceTablesAsync(AppDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS "PricePlans" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_PricePlans" PRIMARY KEY,
                "Name" TEXT NOT NULL,
                "Description" TEXT NULL,
                "Amount" TEXT NOT NULL,
                "BillingCycle" INTEGER NOT NULL,
                "DaysCount" INTEGER NOT NULL DEFAULT 30,
                "IsActive" INTEGER NOT NULL,
                "CreatedAt" TEXT NOT NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_PricePlans_Name" ON "PricePlans" ("Name");

            CREATE TABLE IF NOT EXISTS "Payments" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_Payments" PRIMARY KEY,
                "ChildId" TEXT NOT NULL,
                "PricePlanId" TEXT NULL,
                "PlanNameSnapshot" TEXT NULL,
                "AmountDue" TEXT NOT NULL,
                "AmountPaid" TEXT NOT NULL,
                "DueDate" TEXT NOT NULL,
                "PaidAt" TEXT NULL,
                "Status" INTEGER NOT NULL,
                "Method" INTEGER NULL,
                "Notes" TEXT NULL,
                "CreatedAt" TEXT NOT NULL,
                CONSTRAINT "FK_Payments_Children_ChildId" FOREIGN KEY ("ChildId") REFERENCES "Children" ("Id") ON DELETE CASCADE,
                CONSTRAINT "FK_Payments_PricePlans_PricePlanId" FOREIGN KEY ("PricePlanId") REFERENCES "PricePlans" ("Id") ON DELETE SET NULL
            );
            CREATE INDEX IF NOT EXISTS "IX_Payments_DueDate" ON "Payments" ("DueDate");
            CREATE INDEX IF NOT EXISTS "IX_Payments_Status" ON "Payments" ("Status");
            """);

        try
        {
            await db.Database.ExecuteSqlRawAsync(
                """ALTER TABLE "Children" ADD COLUMN "PricePlanId" TEXT NULL;""");
        }
        catch
        {
            // Column already exists on upgraded databases.
        }

        try
        {
            await db.Database.ExecuteSqlRawAsync(
                """ALTER TABLE "PricePlans" ADD COLUMN "DaysCount" INTEGER NOT NULL DEFAULT 30;""");
        }
        catch
        {
            // Column already exists on upgraded databases.
        }
    }

    private static async Task EnsureExpenseTablesAsync(AppDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS "ExpenseCategories" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_ExpenseCategories" PRIMARY KEY,
                "Name" TEXT NOT NULL,
                "Description" TEXT NULL,
                "IsActive" INTEGER NOT NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_ExpenseCategories_Name" ON "ExpenseCategories" ("Name");

            CREATE TABLE IF NOT EXISTS "Expenses" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_Expenses" PRIMARY KEY,
                "CategoryId" TEXT NOT NULL,
                "Title" TEXT NOT NULL,
                "Amount" TEXT NOT NULL,
                "ExpenseDate" TEXT NOT NULL,
                "Notes" TEXT NULL,
                "PaidTo" TEXT NULL,
                "CreatedAt" TEXT NOT NULL,
                CONSTRAINT "FK_Expenses_ExpenseCategories_CategoryId" FOREIGN KEY ("CategoryId") REFERENCES "ExpenseCategories" ("Id") ON DELETE RESTRICT
            );
            CREATE INDEX IF NOT EXISTS "IX_Expenses_ExpenseDate" ON "Expenses" ("ExpenseDate");
            """);

        if (!await db.ExpenseCategories.AnyAsync())
        {
            db.ExpenseCategories.AddRange(
                new ExpenseCategory { Name = "Supplies", Description = "Classroom and office supplies" },
                new ExpenseCategory { Name = "Utilities", Description = "Electricity, water, internet" },
                new ExpenseCategory { Name = "Maintenance", Description = "Repairs and maintenance" },
                new ExpenseCategory { Name = "Food", Description = "Meals and snacks" }
            );
            await db.SaveChangesAsync();
        }
    }

    private static async Task EnsurePayrollTablesAsync(AppDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS "AttendanceUploadBatches" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_AttendanceUploadBatches" PRIMARY KEY,
                "FileName" TEXT NOT NULL,
                "UploadedAt" TEXT NOT NULL,
                "UploadedByUserId" TEXT NULL,
                "TotalRows" INTEGER NOT NULL,
                "SuccessRows" INTEGER NOT NULL,
                "FailedRows" INTEGER NOT NULL,
                "ErrorReport" TEXT NULL
            );

            CREATE TABLE IF NOT EXISTS "AttendanceRecords" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_AttendanceRecords" PRIMARY KEY,
                "StaffId" TEXT NOT NULL,
                "WorkDate" TEXT NOT NULL,
                "ClockIn" TEXT NOT NULL,
                "ClockOut" TEXT NOT NULL,
                "UploadBatchId" TEXT NULL,
                "CreatedAt" TEXT NOT NULL,
                CONSTRAINT "FK_AttendanceRecords_StaffMembers_StaffId" FOREIGN KEY ("StaffId") REFERENCES "StaffMembers" ("Id") ON DELETE CASCADE,
                CONSTRAINT "FK_AttendanceRecords_AttendanceUploadBatches_UploadBatchId" FOREIGN KEY ("UploadBatchId") REFERENCES "AttendanceUploadBatches" ("Id") ON DELETE SET NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_AttendanceRecords_StaffId_WorkDate" ON "AttendanceRecords" ("StaffId", "WorkDate");

            CREATE TABLE IF NOT EXISTS "SalaryPayments" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_SalaryPayments" PRIMARY KEY,
                "StaffId" TEXT NOT NULL,
                "Year" INTEGER NOT NULL,
                "Month" INTEGER NOT NULL,
                "WorkedDays" INTEGER NOT NULL,
                "WorkedHours" TEXT NOT NULL,
                "Amount" TEXT NOT NULL,
                "SalaryTypeSnapshot" INTEGER NOT NULL,
                "Status" INTEGER NOT NULL,
                "CalculatedAt" TEXT NOT NULL,
                "PaidAt" TEXT NULL,
                "Notes" TEXT NULL,
                CONSTRAINT "FK_SalaryPayments_StaffMembers_StaffId" FOREIGN KEY ("StaffId") REFERENCES "StaffMembers" ("Id") ON DELETE CASCADE
            );
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_SalaryPayments_StaffId_Year_Month" ON "SalaryPayments" ("StaffId", "Year", "Month");
            """);
    }

    private static async Task EnsureChildFamilyTablesAsync(AppDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS "ParentPersons" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_ParentPersons" PRIMARY KEY,
                "FullName" TEXT NOT NULL,
                "Phone" TEXT NOT NULL,
                "Email" TEXT NULL,
                "NationalId" TEXT NULL,
                "PhotoPath" TEXT NULL,
                "CreatedAt" TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS "IX_ParentPersons_NationalId" ON "ParentPersons" ("NationalId");
            """);

        await TryAlterAsync(db, """ALTER TABLE "Children" ADD COLUMN "PhotoPath" TEXT NULL;""");
        await TryAlterAsync(db, """ALTER TABLE "Children" ADD COLUMN "FatherId" TEXT NULL;""");
        await TryAlterAsync(db, """ALTER TABLE "Children" ADD COLUMN "MotherId" TEXT NULL;""");
        await TryAlterAsync(db, """ALTER TABLE "Children" ADD COLUMN "GuardianRelation" TEXT NULL;""");
        await TryAlterAsync(db, """ALTER TABLE "Children" ADD COLUMN "EntryQrCode" TEXT NULL;""");
        await TryAlterAsync(db, """ALTER TABLE "Children" ADD COLUMN "EntryBarcode" TEXT NULL;""");
        await TryAlterAsync(db, """CREATE UNIQUE INDEX IF NOT EXISTS "IX_Children_EntryQrCode" ON "Children" ("EntryQrCode");""");
        await TryAlterAsync(db, """CREATE UNIQUE INDEX IF NOT EXISTS "IX_Children_EntryBarcode" ON "Children" ("EntryBarcode");""");
    }

    private static async Task TryAlterAsync(AppDbContext db, string sql)
    {
        try
        {
            await db.Database.ExecuteSqlRawAsync(sql);
        }
        catch
        {
            // Column/index already exists on upgraded DBs.
        }
    }

    private static async Task EnsureAppSettingsTableAsync(AppDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS "AppSettings" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_AppSettings" PRIMARY KEY,
                "IdentityLabelSize" TEXT NOT NULL,
                "PickupLabelSize" TEXT NOT NULL,
                "PrintCodeMode" INTEGER NOT NULL,
                "QrSizePx" INTEGER NOT NULL DEFAULT 140,
                "BarcodeHeight" INTEGER NOT NULL DEFAULT 40,
                "BarcodeFormat" INTEGER NOT NULL DEFAULT 0,
                "CodeDataType" INTEGER NOT NULL DEFAULT 0,
                "PrintLayoutsJson" TEXT NOT NULL DEFAULT '{{}}',
                "UpdatedAt" TEXT NOT NULL
            );
            """);

        await TryAlterAsync(db, """ALTER TABLE "AppSettings" ADD COLUMN "QrSizePx" INTEGER NOT NULL DEFAULT 140;""");
        await TryAlterAsync(db, """ALTER TABLE "AppSettings" ADD COLUMN "BarcodeHeight" INTEGER NOT NULL DEFAULT 40;""");
        await TryAlterAsync(db, """ALTER TABLE "AppSettings" ADD COLUMN "BarcodeFormat" INTEGER NOT NULL DEFAULT 0;""");
        await TryAlterAsync(db, """ALTER TABLE "AppSettings" ADD COLUMN "CodeDataType" INTEGER NOT NULL DEFAULT 0;""");
        await TryAlterAsync(db, """ALTER TABLE "AppSettings" ADD COLUMN "PrintLayoutsJson" TEXT NOT NULL DEFAULT '{{}}';""");

        if (!await db.AppSettings.AnyAsync())
        {
            db.AppSettings.Add(new AppSettings());
            await db.SaveChangesAsync();
        }
    }

    private static async Task EnsureAppLogsTableAsync(AppDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS "AppLogs" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_AppLogs" PRIMARY KEY,
                "LogType" INTEGER NOT NULL,
                "Module" TEXT NOT NULL,
                "Action" TEXT NOT NULL,
                "Message" TEXT NOT NULL,
                "Details" TEXT NULL,
                "Succeeded" INTEGER NOT NULL,
                "StatusCode" INTEGER NULL,
                "UserId" TEXT NULL,
                "UserName" TEXT NULL,
                "IpAddress" TEXT NULL,
                "OccurredAt" TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS "IX_AppLogs_OccurredAt" ON "AppLogs" ("OccurredAt");
            CREATE INDEX IF NOT EXISTS "IX_AppLogs_LogType" ON "AppLogs" ("LogType");
            """);
    }

    private static async Task EnsureUserLastLoginColumnAsync(AppDbContext db)
    {
        try
        {
            await db.Database.ExecuteSqlRawAsync(
                """ALTER TABLE "Users" ADD COLUMN "LastLoginAt" TEXT NULL;""");
        }
        catch
        {
            // Column already exists on upgraded databases.
        }
    }

    private static async Task EnsurePermissionsAsync(AppDbContext db)
    {
        var existing = await db.Permissions.Select(p => p.Code).ToListAsync();
        var missing = PermissionSeed
            .Where(p => !existing.Contains(p.Code))
            .Select(p => new Permission { Code = p.Code, Name = p.Name, Module = p.Module })
            .ToList();

        if (missing.Count == 0)
        {
            return;
        }

        db.Permissions.AddRange(missing);
        await db.SaveChangesAsync();
    }

    private static async Task EnsureAdminHasAllPermissionsAsync(AppDbContext db)
    {
        var admin = await db.Roles.Include(r => r.RolePermissions).FirstOrDefaultAsync(r => r.Name == "Admin");
        if (admin is null)
        {
            return;
        }

        var allPermissionIds = await db.Permissions.Select(p => p.Id).ToListAsync();
        var current = admin.RolePermissions.Select(rp => rp.PermissionId).ToHashSet();
        var toAdd = allPermissionIds
            .Where(id => !current.Contains(id))
            .Select(id => new RolePermission { RoleId = admin.Id, PermissionId = id })
            .ToList();

        if (toAdd.Count == 0)
        {
            return;
        }

        db.RolePermissions.AddRange(toAdd);
        await db.SaveChangesAsync();
    }

    private static async Task EnsureRolePermissionCodesAsync(AppDbContext db, string roleName, params string[] codes)
    {
        var role = await db.Roles.Include(r => r.RolePermissions).FirstOrDefaultAsync(r => r.Name == roleName);
        if (role is null)
        {
            return;
        }

        var permissions = await db.Permissions.Where(p => codes.Contains(p.Code)).ToListAsync();
        var current = role.RolePermissions.Select(rp => rp.PermissionId).ToHashSet();
        var toAdd = permissions
            .Where(p => !current.Contains(p.Id))
            .Select(p => new RolePermission { RoleId = role.Id, PermissionId = p.Id })
            .ToList();

        if (toAdd.Count == 0)
        {
            return;
        }

        db.RolePermissions.AddRange(toAdd);
        await db.SaveChangesAsync();
    }
}
