using Project.Entities.Finance;

namespace Repositories.Interfaces;

/// <summary>Persistência de dados financeiros (implementação atual: memória no processo).</summary>
public interface IFinanceStore
{
    Task<IReadOnlyList<Category>> ListCategoriesAsync(string userId, CancellationToken ct = default);
    Task<IReadOnlyList<Category>> ListExpenseCategoriesAsync(string userId, CancellationToken ct = default);
    Task<Category?> GetCategoryAsync(string userId, Guid id, CancellationToken ct = default);
    Task InsertCategoryAsync(Category entity, CancellationToken ct = default);
    Task UpdateCategoryAsync(Category entity, CancellationToken ct = default);
    Task DeleteCategoryAsync(string userId, Guid id, CancellationToken ct = default);
    Task<bool> CategoryInUseAsync(string userId, Guid categoryId, CancellationToken ct = default);

    Task<IReadOnlyList<Expense>> ListExpensesAsync(string userId, DateTime? from = null, DateTime? toExclusive = null,
        CancellationToken ct = default);
    Task<IReadOnlyList<Expense>> ListExpensesForCardAsync(string userId, Guid cardId, DateTime from, DateTime toExclusive,
        CancellationToken ct = default);
    Task HydrateExpenseCategoriesAsync(IEnumerable<Expense> expenses, string userId, CancellationToken ct = default);
    Task<Expense?> GetExpenseAsync(string userId, Guid id, CancellationToken ct = default);
    Task InsertExpenseAsync(Expense entity, CancellationToken ct = default);
    Task UpdateExpenseAsync(Expense entity, CancellationToken ct = default);
    Task DeleteExpenseAsync(string userId, Guid id, CancellationToken ct = default);
    Task<bool> AnyExpenseForCreditCardAsync(Guid cardId, CancellationToken ct = default);

    Task<IReadOnlyList<Income>> ListIncomesAsync(string userId, CancellationToken ct = default);
    Task<IReadOnlyList<Income>> ListIncomesFromMonthAsync(string userId, DateTime fromMonthFirstDay, CancellationToken ct = default);
    Task<decimal> SumIncomeForMonthAsync(string userId, DateTime monthStart, DateTime monthEndExclusive, CancellationToken ct = default);
    Task<Income?> GetIncomeAsync(string userId, Guid id, CancellationToken ct = default);
    Task InsertIncomeAsync(Income entity, CancellationToken ct = default);
    Task UpdateIncomeAsync(Income entity, CancellationToken ct = default);
    Task DeleteIncomeAsync(string userId, Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<CreditCard>> ListCreditCardsAsync(string userId, CancellationToken ct = default);
    Task<CreditCard?> GetCreditCardAsync(string userId, Guid id, CancellationToken ct = default);
    Task InsertCreditCardAsync(CreditCard entity, CancellationToken ct = default);
    Task UpdateCreditCardAsync(CreditCard entity, CancellationToken ct = default);
    Task DeleteCreditCardAsync(string userId, Guid id, CancellationToken ct = default);
    Task<bool> CreditCardInUseAsync(Guid cardId, CancellationToken ct = default);

    Task<IReadOnlyList<InstallmentPlan>> ListInstallmentPlansAsync(string userId, CancellationToken ct = default);
    Task<InstallmentPlan?> GetInstallmentPlanAsync(string userId, Guid id, bool withInstallments, CancellationToken ct = default);
    Task InsertInstallmentPlanAsync(InstallmentPlan entity, CancellationToken ct = default);
    Task DeleteInstallmentPlanAsync(string userId, Guid id, CancellationToken ct = default);
    Task<bool> AnyInstallmentPlanForCategoryAsync(string userId, Guid categoryId, CancellationToken ct = default);

    Task InsertInstallmentAsync(Installment entity, CancellationToken ct = default);
    Task<IReadOnlyList<Installment>> ListInstallmentsForPlanAsync(Guid planId, CancellationToken ct = default);
    Task<Installment?> GetInstallmentWithPlanAsync(Guid installmentId, CancellationToken ct = default);
    Task UpdateInstallmentAsync(Installment entity, CancellationToken ct = default);
    Task DeleteInstallmentAsync(Guid installmentId, CancellationToken ct = default);
    Task<IReadOnlyList<Installment>> ListUnpaidInstallmentsForUserPlansAsync(string userId, CancellationToken ct = default);
    Task<IReadOnlyList<Installment>> ListUnpaidInstallmentsInRangeAsync(string userId, DateTime flowStart, DateTime flowEndExclusive,
        CancellationToken ct = default);
    Task<Installment?> FindInstallmentByExpenseIdAsync(Guid expenseId, CancellationToken ct = default);

    Task HydrateInstallmentPlansAsync(IEnumerable<Installment> installments, CancellationToken ct = default);
    Task HydratePlanGraphAsync(InstallmentPlan plan, string userId, CancellationToken ct = default);

    Task<IReadOnlyList<RecurringExpense>> ListRecurringAsync(string userId, CancellationToken ct = default);
    Task<RecurringExpense?> GetRecurringAsync(string userId, Guid id, CancellationToken ct = default);
    Task InsertRecurringAsync(RecurringExpense entity, CancellationToken ct = default);
    Task UpdateRecurringAsync(RecurringExpense entity, CancellationToken ct = default);
    Task DeleteRecurringAsync(string userId, Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<RecurringExpense>> ListActiveRecurringForDayOfMonthAsync(int dayOfMonth, CancellationToken ct = default);
    Task<decimal> SumRecurringAsync(string userId, bool activeOnly, RecurringExpenseType type, CancellationToken ct = default);
    Task HydrateRecurringCategoriesAsync(IEnumerable<RecurringExpense> items, string userId, CancellationToken ct = default);

    Task<IReadOnlyList<Debt>> ListDebtsAsync(string userId, CancellationToken ct = default);
    Task<Debt?> GetDebtAsync(string userId, Guid id, CancellationToken ct = default);
    Task InsertDebtAsync(Debt entity, CancellationToken ct = default);
    Task UpdateDebtAsync(Debt entity, CancellationToken ct = default);
    Task DeleteDebtAsync(string userId, Guid id, CancellationToken ct = default);
    Task<decimal> SumDebtBalanceAsync(string userId, CancellationToken ct = default);

    Task<IReadOnlyList<Account>> ListAccountsAsync(string userId, CancellationToken ct = default);
    Task<Account?> GetAccountAsync(string userId, Guid id, CancellationToken ct = default);
    Task InsertAccountAsync(Account entity, CancellationToken ct = default);
    Task UpdateAccountAsync(Account entity, CancellationToken ct = default);
    Task DeleteAccountAsync(string userId, Guid id, CancellationToken ct = default);
    Task<decimal> SumAccountBalancesAsync(string userId, CancellationToken ct = default);

    Task<PatrimonyMonthlySnapshot?> FindPatrimonySnapshotAsync(string userId, int year, int month, CancellationToken ct = default);
    Task InsertPatrimonySnapshotAsync(PatrimonyMonthlySnapshot entity, CancellationToken ct = default);
    Task UpdatePatrimonySnapshotAsync(PatrimonyMonthlySnapshot entity, CancellationToken ct = default);
    Task<IReadOnlyList<PatrimonyMonthlySnapshot>> ListPatrimonySnapshotsAsync(string userId, int take, CancellationToken ct = default);
}
