using Microsoft.AspNetCore.Authorization;

namespace NurseryManagement.Api.Security;

public class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User.HasClaim("permission", requirement.Permission) ||
            context.User.IsInRole("Admin"))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

public static class PermissionPolicies
{
    public const string RolesManage = "roles.manage";
    public const string AuditRead = "audit.read";
    public const string ChildrenRead = "children.read";
    public const string ChildrenWrite = "children.write";
    public const string EntryScan = "entry.scan";
    public const string ClassesRead = "classes.read";
    public const string ClassesWrite = "classes.write";
    public const string StaffRead = "staff.read";
    public const string StaffWrite = "staff.write";
    public const string PaymentsRead = "payments.read";
    public const string PaymentsWrite = "payments.write";
    public const string PricePlansRead = "priceplans.read";
    public const string PricePlansWrite = "priceplans.write";
    /// <summary>Internal list access for dropdowns (not shown as a role checkbox).</summary>
    public const string PricePlansList = "priceplans.list";
    public const string ExpensesRead = "expenses.read";
    public const string ExpensesWrite = "expenses.write";
    public const string SalariesRead = "salaries.read";
    public const string SalariesCalculate = "salaries.calculate";
    public const string SettingsRead = "settings.read";
    public const string SettingsWrite = "settings.write";
    public const string LogsRead = "logs.read";

    public static readonly string[] All =
    [
        RolesManage,
        AuditRead,
        ChildrenRead,
        ChildrenWrite,
        EntryScan,
        ClassesRead,
        ClassesWrite,
        StaffRead,
        StaffWrite,
        PaymentsRead,
        PaymentsWrite,
        PricePlansRead,
        PricePlansWrite,
        ExpensesRead,
        ExpensesWrite,
        SalariesRead,
        SalariesCalculate,
        SettingsRead,
        SettingsWrite,
        LogsRead
    ];
}
