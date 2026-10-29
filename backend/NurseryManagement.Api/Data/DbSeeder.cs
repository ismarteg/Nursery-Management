using NurseryManagement.Api.Domain.Entities;

namespace NurseryManagement.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await db.Database.EnsureCreatedAsync();

        if (!db.Permissions.Any())
        {
            var permissions = new[]
            {
                ("roles.manage", "Manage roles", "Security"),
                ("audit.read", "View audit logs", "Security"),
                ("children.read", "View children", "Children"),
                ("children.write", "Manage children", "Children"),
                ("entry.scan", "Scan entry", "Entry"),
                ("classes.read", "View classes", "Classes"),
                ("classes.write", "Manage classes", "Classes"),
                ("payments.read", "View payments", "Finance"),
                ("payments.write", "Manage payments", "Finance"),
                ("expenses.read", "View expenses", "Finance"),
                ("expenses.write", "Manage expenses", "Finance"),
                ("salaries.read", "View salaries", "HR"),
                ("salaries.calculate", "Calculate salaries", "HR"),
            }.Select(p => new Permission { Code = p.Item1, Name = p.Item2, Module = p.Item3 }).ToList();

            db.Permissions.AddRange(permissions);
            await db.SaveChangesAsync();
        }

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
                "payments.read", "payments.write", "expenses.read", "expenses.write",
                "salaries.read", "salaries.calculate", "audit.read"));

            db.RolePermissions.AddRange(Map(teacher, allPermissions,
                "children.read", "entry.scan", "classes.read"));

            db.RolePermissions.AddRange(Map(accountant, allPermissions,
                "payments.read", "payments.write", "expenses.read", "expenses.write",
                "salaries.read", "salaries.calculate"));

            await db.SaveChangesAsync();
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
}
