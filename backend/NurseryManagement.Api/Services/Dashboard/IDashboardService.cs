namespace NurseryManagement.Api.Services.Dashboard;

public interface IDashboardService
{
    /// <summary>
    /// Builds the dashboard summary. Sections are included only when the caller is an admin
    /// or holds the matching permission (e.g. "children.read").
    /// </summary>
    Task<object> GetSummaryAsync(bool isAdmin, Func<string, bool> hasPermission, CancellationToken cancellationToken = default);
}
