using NurseryManagement.Api.Contracts.Operations;

namespace NurseryManagement.Api.Services.Finance;

public interface IExpensesService
{
    // Expenses
    Task<object> GetAllAsync(Guid? categoryId, DateTime? from, DateTime? to, CancellationToken cancellationToken = default);
    Task<object> GetMonthlySummaryAsync(int? year, int? month, CancellationToken cancellationToken = default);
    Task<object> CreateAsync(ExpenseRequest request, Guid? userId, string? userName, string? ip, CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid id, ExpenseRequest request, Guid? userId, string? userName, string? ip, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, Guid? userId, string? userName, string? ip, CancellationToken cancellationToken = default);

    // Expense categories
    Task<IReadOnlyList<object>> GetCategoriesAsync(bool activeOnly, CancellationToken cancellationToken = default);
    Task<object> CreateCategoryAsync(ExpenseCategoryRequest request, Guid? userId, string? userName, string? ip, CancellationToken cancellationToken = default);
    Task UpdateCategoryAsync(Guid id, ExpenseCategoryRequest request, Guid? userId, string? userName, string? ip, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the category, or deactivates it when expenses reference it.
    /// Returns true when the category was deactivated instead of removed.
    /// </summary>
    Task<bool> DeleteCategoryAsync(Guid id, Guid? userId, string? userName, string? ip, CancellationToken cancellationToken = default);
}
