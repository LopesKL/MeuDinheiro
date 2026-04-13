using Microsoft.EntityFrameworkCore;
using Project.Entities.Finance;
using Repositories.Interfaces;
using SqlServer.Context;

namespace Repositories.Repositories;

/// <summary>Persistência financeira em PostgreSQL/SQLite via <see cref="ApiServerContext"/>.</summary>
public sealed class EfFinanceStore(ApiServerContext db) : IFinanceStore
{
    private readonly ApiServerContext _db = db;

    private static DateTime NormalizeMonthStartUtc(DateTime any)
    {
        var d = any.Kind == DateTimeKind.Utc ? any : any.ToUniversalTime();
        return new DateTime(d.Year, d.Month, 1, 0, 0, 0, DateTimeKind.Utc);
    }

    private static void StripExpense(Expense e)
    {
        e.Category = null!;
        e.CreditCard = null;
        e.InstallmentPlan = null;
    }

    private static void StripRecurring(RecurringExpense r)
    {
        r.Category = null!;
        r.CreditCard = null;
    }

    private static void StripInstallmentPlan(InstallmentPlan p)
    {
        p.Category = null!;
        p.CreditCard = null;
        p.Installments = new List<Installment>();
    }

    private static void StripInstallment(Installment i)
    {
        i.InstallmentPlan = null!;
        i.Expense = null;
    }

    public async Task<IReadOnlyList<Category>> ListCategoriesAsync(string userId, CancellationToken ct = default) =>
        await _db.FinanceCategories.AsNoTracking().Where(c => c.UserId == userId).OrderBy(c => c.Name).ToListAsync(ct);

    public async Task<IReadOnlyList<Category>> ListExpenseCategoriesAsync(string userId, CancellationToken ct = default) =>
        await _db.FinanceCategories.AsNoTracking().Where(c => c.UserId == userId && c.IsExpense).OrderBy(c => c.Name)
            .ToListAsync(ct);

    public async Task<Category?> GetCategoryAsync(string userId, Guid id, CancellationToken ct = default) =>
        await _db.FinanceCategories.AsNoTracking().FirstOrDefaultAsync(c => c.UserId == userId && c.Id == id, ct);

    public async Task InsertCategoryAsync(Category entity, CancellationToken ct = default)
    {
        await _db.FinanceCategories.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateCategoryAsync(Category entity, CancellationToken ct = default)
    {
        _db.FinanceCategories.Update(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteCategoryAsync(string userId, Guid id, CancellationToken ct = default)
    {
        var row = await _db.FinanceCategories.FirstOrDefaultAsync(c => c.UserId == userId && c.Id == id, ct);
        if (row != null)
        {
            _db.FinanceCategories.Remove(row);
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task<bool> CategoryInUseAsync(string userId, Guid categoryId, CancellationToken ct = default)
    {
        if (await _db.FinanceExpenses.AnyAsync(e => e.UserId == userId && e.CategoryId == categoryId, ct))
            return true;
        if (await _db.FinanceRecurringExpenses.AnyAsync(r => r.UserId == userId && r.CategoryId == categoryId, ct))
            return true;
        if (await _db.FinanceInstallmentPlans.AnyAsync(p => p.UserId == userId && p.CategoryId == categoryId, ct))
            return true;
        return false;
    }

    public async Task<IReadOnlyList<Expense>> ListExpensesAsync(string userId, DateTime? from = null, DateTime? toExclusive = null,
        CancellationToken ct = default)
    {
        var q = _db.FinanceExpenses.AsNoTracking().Where(e => e.UserId == userId);
        if (from.HasValue)
        {
            var f = from.Value.ToUniversalTime();
            q = q.Where(e => e.Date.ToUniversalTime() >= f);
        }

        if (toExclusive.HasValue)
        {
            var t = toExclusive.Value.ToUniversalTime();
            q = q.Where(e => e.Date.ToUniversalTime() < t);
        }

        return await q.OrderByDescending(e => e.Date).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Expense>> ListExpensesForCardAsync(string userId, Guid cardId, DateTime from, DateTime toExclusive,
        CancellationToken ct = default)
    {
        var f = from.ToUniversalTime();
        var t = toExclusive.ToUniversalTime();
        return await _db.FinanceExpenses.AsNoTracking()
            .Where(e => e.UserId == userId && e.CreditCardId == cardId
                && e.Date.ToUniversalTime() >= f && e.Date.ToUniversalTime() < t)
            .OrderByDescending(e => e.Date)
            .ToListAsync(ct);
    }

    public async Task HydrateExpenseCategoriesAsync(IEnumerable<Expense> expenses, string userId, CancellationToken ct = default)
    {
        var list = expenses as IList<Expense> ?? expenses.ToList();
        if (list.Count == 0)
            return;
        var ids = list.Select(e => e.CategoryId).Distinct().ToList();
        var cats = await _db.FinanceCategories.AsNoTracking()
            .Where(c => c.UserId == userId && ids.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, ct);
        foreach (var e in list)
        {
            e.Category = cats.TryGetValue(e.CategoryId, out var c)
                ? c
                : new Category { Id = e.CategoryId, Name = "?", UserId = userId };
        }
    }

    public async Task<Expense?> GetExpenseAsync(string userId, Guid id, CancellationToken ct = default)
    {
        var e = await _db.FinanceExpenses.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId && x.Id == id, ct);
        if (e == null)
            return null;
        await HydrateExpenseCategoriesAsync(new[] { e }, userId, ct);
        return e;
    }

    public async Task InsertExpenseAsync(Expense entity, CancellationToken ct = default)
    {
        StripExpense(entity);
        await _db.FinanceExpenses.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateExpenseAsync(Expense entity, CancellationToken ct = default)
    {
        StripExpense(entity);
        _db.FinanceExpenses.Update(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteExpenseAsync(string userId, Guid id, CancellationToken ct = default)
    {
        var row = await _db.FinanceExpenses.FirstOrDefaultAsync(e => e.UserId == userId && e.Id == id, ct);
        if (row != null)
        {
            _db.FinanceExpenses.Remove(row);
            await _db.SaveChangesAsync(ct);
        }
    }

    public Task<bool> AnyExpenseForCreditCardAsync(Guid cardId, CancellationToken ct = default) =>
        _db.FinanceExpenses.AnyAsync(e => e.CreditCardId == cardId, ct);

    public async Task<IReadOnlyList<Income>> ListIncomesAsync(string userId, CancellationToken ct = default) =>
        await _db.FinanceIncomes.AsNoTracking().Where(i => i.UserId == userId).OrderByDescending(i => i.ReferenceMonth).ToListAsync(ct);

    public async Task<IReadOnlyList<Income>> ListIncomesFromMonthAsync(string userId, DateTime fromMonthFirstDay, CancellationToken ct = default)
    {
        var ts = fromMonthFirstDay.ToUniversalTime();
        return await _db.FinanceIncomes.AsNoTracking()
            .Where(i => i.UserId == userId && i.ReferenceMonth.ToUniversalTime() >= ts)
            .OrderBy(i => i.ReferenceMonth)
            .ToListAsync(ct);
    }

    public async Task<decimal> SumIncomeForMonthAsync(string userId, DateTime monthStart, DateTime monthEndExclusive, CancellationToken ct = default)
    {
        var a = monthStart.ToUniversalTime();
        var b = monthEndExclusive.ToUniversalTime();
        return await _db.FinanceIncomes.AsNoTracking()
            .Where(i => i.UserId == userId
                && i.ReferenceMonth.ToUniversalTime() >= a
                && i.ReferenceMonth.ToUniversalTime() < b)
            .SumAsync(i => i.Amount, ct);
    }

    public async Task<Income?> GetIncomeAsync(string userId, Guid id, CancellationToken ct = default) =>
        await _db.FinanceIncomes.AsNoTracking().FirstOrDefaultAsync(i => i.UserId == userId && i.Id == id, ct);

    public async Task InsertIncomeAsync(Income entity, CancellationToken ct = default)
    {
        await _db.FinanceIncomes.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateIncomeAsync(Income entity, CancellationToken ct = default)
    {
        _db.FinanceIncomes.Update(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteIncomeAsync(string userId, Guid id, CancellationToken ct = default)
    {
        var row = await _db.FinanceIncomes.FirstOrDefaultAsync(i => i.UserId == userId && i.Id == id, ct);
        if (row != null)
        {
            _db.FinanceIncomes.Remove(row);
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task<IReadOnlyList<CreditCard>> ListCreditCardsAsync(string userId, CancellationToken ct = default) =>
        await _db.FinanceCreditCards.AsNoTracking().Where(c => c.UserId == userId).OrderBy(c => c.Name).ToListAsync(ct);

    public async Task<CreditCard?> GetCreditCardAsync(string userId, Guid id, CancellationToken ct = default) =>
        await _db.FinanceCreditCards.AsNoTracking().FirstOrDefaultAsync(c => c.UserId == userId && c.Id == id, ct);

    public async Task InsertCreditCardAsync(CreditCard entity, CancellationToken ct = default)
    {
        await _db.FinanceCreditCards.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateCreditCardAsync(CreditCard entity, CancellationToken ct = default)
    {
        _db.FinanceCreditCards.Update(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteCreditCardAsync(string userId, Guid id, CancellationToken ct = default)
    {
        var row = await _db.FinanceCreditCards.FirstOrDefaultAsync(c => c.UserId == userId && c.Id == id, ct);
        if (row != null)
        {
            _db.FinanceCreditCards.Remove(row);
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task<bool> CreditCardInUseAsync(Guid cardId, CancellationToken ct = default)
    {
        if (await _db.FinanceExpenses.AnyAsync(e => e.CreditCardId == cardId, ct))
            return true;
        if (await _db.FinanceRecurringExpenses.AnyAsync(r => r.CreditCardId == cardId, ct))
            return true;
        if (await _db.FinanceInstallmentPlans.AnyAsync(p => p.CreditCardId == cardId, ct))
            return true;
        if (await _db.FinanceIncomes.AnyAsync(i => i.CreditCardId == cardId, ct))
            return true;
        return false;
    }

    public async Task<IReadOnlyList<InstallmentPlan>> ListInstallmentPlansAsync(string userId, CancellationToken ct = default)
    {
        var list = await _db.FinanceInstallmentPlans.AsNoTracking()
            .Where(p => p.UserId == userId)
            .Include(p => p.Installments)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);
        foreach (var p in list)
            p.Installments = p.Installments.OrderBy(i => i.SequenceNumber).ToList();
        return list;
    }

    public async Task<InstallmentPlan?> GetInstallmentPlanAsync(string userId, Guid id, bool withInstallments, CancellationToken ct = default)
    {
        IQueryable<InstallmentPlan> q = _db.FinanceInstallmentPlans.AsNoTracking().Where(p => p.UserId == userId && p.Id == id);
        if (withInstallments)
            q = q.Include(p => p.Installments);
        var p = await q.FirstOrDefaultAsync(ct);
        if (p == null)
            return null;
        if (withInstallments)
            p.Installments = p.Installments.OrderBy(i => i.SequenceNumber).ToList();
        await HydratePlanGraphAsync(p, userId, ct);
        return p;
    }

    public async Task InsertInstallmentPlanAsync(InstallmentPlan entity, CancellationToken ct = default)
    {
        StripInstallmentPlan(entity);
        await _db.FinanceInstallmentPlans.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteInstallmentPlanAsync(string userId, Guid id, CancellationToken ct = default)
    {
        var row = await _db.FinanceInstallmentPlans.FirstOrDefaultAsync(p => p.UserId == userId && p.Id == id, ct);
        if (row != null)
        {
            _db.FinanceInstallmentPlans.Remove(row);
            await _db.SaveChangesAsync(ct);
        }
    }

    public Task<bool> AnyInstallmentPlanForCategoryAsync(string userId, Guid categoryId, CancellationToken ct = default) =>
        _db.FinanceInstallmentPlans.AnyAsync(p => p.UserId == userId && p.CategoryId == categoryId, ct);

    public async Task InsertInstallmentAsync(Installment entity, CancellationToken ct = default)
    {
        StripInstallment(entity);
        await _db.FinanceInstallments.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<Installment>> ListInstallmentsForPlanAsync(Guid planId, CancellationToken ct = default) =>
        await _db.FinanceInstallments.AsNoTracking()
            .Where(i => i.InstallmentPlanId == planId)
            .OrderBy(i => i.SequenceNumber)
            .ToListAsync(ct);

    public async Task<Installment?> GetInstallmentWithPlanAsync(Guid installmentId, CancellationToken ct = default)
    {
        var i = await _db.FinanceInstallments.AsNoTracking().FirstOrDefaultAsync(x => x.Id == installmentId, ct);
        if (i == null)
            return null;
        var plan = await _db.FinanceInstallmentPlans.AsNoTracking().FirstOrDefaultAsync(p => p.Id == i.InstallmentPlanId, ct);
        if (plan == null)
            return i;
        await HydratePlanGraphAsync(plan, plan.UserId, ct);
        i.InstallmentPlan = plan;
        return i;
    }

    public async Task UpdateInstallmentAsync(Installment entity, CancellationToken ct = default)
    {
        StripInstallment(entity);
        _db.FinanceInstallments.Update(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteInstallmentAsync(Guid installmentId, CancellationToken ct = default)
    {
        var row = await _db.FinanceInstallments.FirstOrDefaultAsync(i => i.Id == installmentId, ct);
        if (row != null)
        {
            _db.FinanceInstallments.Remove(row);
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task<IReadOnlyList<Installment>> ListUnpaidInstallmentsForUserPlansAsync(string userId, CancellationToken ct = default)
    {
        var planIds = await _db.FinanceInstallmentPlans.Where(p => p.UserId == userId).Select(p => p.Id).ToListAsync(ct);
        return await _db.FinanceInstallments.AsNoTracking()
            .Where(i => planIds.Contains(i.InstallmentPlanId) && !i.IsPaid)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Installment>> ListUnpaidInstallmentsInRangeAsync(string userId, DateTime flowStart, DateTime flowEndExclusive,
        CancellationToken ct = default)
    {
        var unpaid = await ListUnpaidInstallmentsForUserPlansAsync(userId, ct);
        var start = flowStart.Date;
        var end = flowEndExclusive.Date;
        var filtered = unpaid.Where(i => i.DueDate >= start && i.DueDate < end).ToList();
        await HydrateInstallmentPlansAsync(filtered, ct);
        return filtered;
    }

    public async Task<Installment?> FindInstallmentByExpenseIdAsync(Guid expenseId, CancellationToken ct = default) =>
        await _db.FinanceInstallments.AsNoTracking().FirstOrDefaultAsync(i => i.ExpenseId == expenseId, ct);

    public async Task HydrateInstallmentPlansAsync(IEnumerable<Installment> installments, CancellationToken ct = default)
    {
        var list = installments as IList<Installment> ?? installments.ToList();
        var planIds = list.Select(i => i.InstallmentPlanId).Distinct().ToList();
        if (planIds.Count == 0)
            return;
        var plans = await _db.FinanceInstallmentPlans.AsNoTracking().Where(p => planIds.Contains(p.Id)).ToListAsync(ct);
        var byId = plans.ToDictionary(p => p.Id);
        foreach (var i in list)
        {
            if (byId.TryGetValue(i.InstallmentPlanId, out var p))
            {
                await HydratePlanGraphAsync(p, p.UserId, ct);
                i.InstallmentPlan = p;
            }
        }
    }

    public async Task HydratePlanGraphAsync(InstallmentPlan plan, string userId, CancellationToken ct = default)
    {
        plan.Category = await _db.FinanceCategories.AsNoTracking()
                         .FirstOrDefaultAsync(c => c.Id == plan.CategoryId && c.UserId == userId, ct)
                     ?? new Category { Id = plan.CategoryId, Name = "?", UserId = userId };
        if (plan.CreditCardId.HasValue)
        {
            plan.CreditCard = await _db.FinanceCreditCards.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == plan.CreditCardId && c.UserId == userId, ct);
        }
    }

    public async Task<IReadOnlyList<RecurringExpenseAmountSchedule>> ListRecurringAmountSchedulesAsync(string? userId = null,
        Guid? recurringExpenseId = null, CancellationToken ct = default)
    {
        var q = _db.FinanceRecurringAmountSchedules.AsNoTracking().AsQueryable();
        if (userId != null)
            q = q.Where(x => x.UserId == userId);
        if (recurringExpenseId.HasValue)
            q = q.Where(x => x.RecurringExpenseId == recurringExpenseId.Value);
        return await q.OrderBy(x => x.EffectiveFrom).ThenBy(x => x.CreatedAt).ToListAsync(ct);
    }

    public async Task UpsertRecurringAmountScheduleAsync(RecurringExpenseAmountSchedule entity, CancellationToken ct = default)
    {
        var normalizedFrom = NormalizeMonthStartUtc(entity.EffectiveFrom);
        entity.EffectiveFrom = normalizedFrom;

        var dups = await _db.FinanceRecurringAmountSchedules
            .Where(x => x.UserId == entity.UserId && x.RecurringExpenseId == entity.RecurringExpenseId
                && x.EffectiveFrom == normalizedFrom && x.Id != entity.Id)
            .ToListAsync(ct);
        _db.FinanceRecurringAmountSchedules.RemoveRange(dups);

        var tracked = await _db.FinanceRecurringAmountSchedules.FirstOrDefaultAsync(x => x.Id == entity.Id, ct);
        if (tracked == null)
            await _db.FinanceRecurringAmountSchedules.AddAsync(entity, ct);
        else
        {
            tracked.EffectiveFrom = entity.EffectiveFrom;
            tracked.Amount = entity.Amount;
            tracked.RecurringExpenseId = entity.RecurringExpenseId;
            tracked.UserId = entity.UserId;
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> DeleteRecurringAmountScheduleAsync(string userId, Guid scheduleId, CancellationToken ct = default)
    {
        var row = await _db.FinanceRecurringAmountSchedules.FirstOrDefaultAsync(x => x.UserId == userId && x.Id == scheduleId, ct);
        if (row == null)
            return false;
        _db.FinanceRecurringAmountSchedules.Remove(row);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IReadOnlyList<RecurringExpense>> ListRecurringAsync(string userId, CancellationToken ct = default) =>
        await _db.FinanceRecurringExpenses.AsNoTracking().Where(r => r.UserId == userId).OrderBy(r => r.Description).ToListAsync(ct);

    public async Task<RecurringExpense?> GetRecurringAsync(string userId, Guid id, CancellationToken ct = default) =>
        await _db.FinanceRecurringExpenses.AsNoTracking().FirstOrDefaultAsync(r => r.UserId == userId && r.Id == id, ct);

    public async Task InsertRecurringAsync(RecurringExpense entity, CancellationToken ct = default)
    {
        StripRecurring(entity);
        await _db.FinanceRecurringExpenses.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateRecurringAsync(RecurringExpense entity, CancellationToken ct = default)
    {
        StripRecurring(entity);
        _db.FinanceRecurringExpenses.Update(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteRecurringAsync(string userId, Guid id, CancellationToken ct = default)
    {
        var row = await _db.FinanceRecurringExpenses.FirstOrDefaultAsync(r => r.UserId == userId && r.Id == id, ct);
        if (row != null)
        {
            _db.FinanceRecurringExpenses.Remove(row);
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task<IReadOnlyList<RecurringExpense>> ListActiveRecurringForDayOfMonthAsync(int dayOfMonth, CancellationToken ct = default) =>
        await _db.FinanceRecurringExpenses.AsNoTracking()
            .Where(r => r.Active && r.DayOfMonth == dayOfMonth)
            .ToListAsync(ct);

    public async Task<decimal> SumRecurringAsync(string userId, bool activeOnly, RecurringExpenseType type, CancellationToken ct = default)
    {
        var q = _db.FinanceRecurringExpenses.AsNoTracking().Where(r => r.UserId == userId && r.Type == type);
        if (activeOnly)
            q = q.Where(r => r.Active);
        return await q.SumAsync(r => r.Amount, ct);
    }

    public async Task HydrateRecurringCategoriesAsync(IEnumerable<RecurringExpense> items, string userId, CancellationToken ct = default)
    {
        var list = items as IList<RecurringExpense> ?? items.ToList();
        var catIds = list.Select(r => r.CategoryId).Distinct().ToList();
        var cardIds = list.Where(r => r.CreditCardId.HasValue).Select(r => r.CreditCardId!.Value).Distinct().ToList();
        var cats = await _db.FinanceCategories.AsNoTracking()
            .Where(c => c.UserId == userId && catIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, ct);
        var cards = await _db.FinanceCreditCards.AsNoTracking()
            .Where(c => c.UserId == userId && cardIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, ct);
        foreach (var r in list)
        {
            r.Category = cats.TryGetValue(r.CategoryId, out var c)
                ? c
                : new Category { Id = r.CategoryId, Name = "?", UserId = userId };
            if (r.CreditCardId.HasValue && cards.TryGetValue(r.CreditCardId.Value, out var cc))
                r.CreditCard = cc;
        }
    }

    public async Task<IReadOnlyList<Debt>> ListDebtsAsync(string userId, CancellationToken ct = default) =>
        await _db.FinanceDebts.AsNoTracking().Where(d => d.UserId == userId).OrderBy(d => d.Name).ToListAsync(ct);

    public async Task<Debt?> GetDebtAsync(string userId, Guid id, CancellationToken ct = default) =>
        await _db.FinanceDebts.AsNoTracking().FirstOrDefaultAsync(d => d.UserId == userId && d.Id == id, ct);

    public async Task InsertDebtAsync(Debt entity, CancellationToken ct = default)
    {
        await _db.FinanceDebts.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateDebtAsync(Debt entity, CancellationToken ct = default)
    {
        _db.FinanceDebts.Update(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteDebtAsync(string userId, Guid id, CancellationToken ct = default)
    {
        var row = await _db.FinanceDebts.FirstOrDefaultAsync(d => d.UserId == userId && d.Id == id, ct);
        if (row != null)
        {
            _db.FinanceDebts.Remove(row);
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task<decimal> SumDebtBalanceAsync(string userId, CancellationToken ct = default) =>
        await _db.FinanceDebts.AsNoTracking()
            .Where(d => d.UserId == userId)
            .SumAsync(d => d.TotalAmount - d.PaidAmount, ct);

    public async Task<IReadOnlyList<Account>> ListAccountsAsync(string userId, CancellationToken ct = default) =>
        await _db.FinanceAccounts.AsNoTracking().Where(a => a.UserId == userId).OrderBy(a => a.Name).ToListAsync(ct);

    public async Task<Account?> GetAccountAsync(string userId, Guid id, CancellationToken ct = default) =>
        await _db.FinanceAccounts.AsNoTracking().FirstOrDefaultAsync(a => a.UserId == userId && a.Id == id, ct);

    public async Task InsertAccountAsync(Account entity, CancellationToken ct = default)
    {
        await _db.FinanceAccounts.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAccountAsync(Account entity, CancellationToken ct = default)
    {
        _db.FinanceAccounts.Update(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAccountAsync(string userId, Guid id, CancellationToken ct = default)
    {
        var row = await _db.FinanceAccounts.FirstOrDefaultAsync(a => a.UserId == userId && a.Id == id, ct);
        if (row != null)
        {
            _db.FinanceAccounts.Remove(row);
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task<decimal> SumAccountBalancesAsync(string userId, CancellationToken ct = default) =>
        await _db.FinanceAccounts.AsNoTracking().Where(a => a.UserId == userId).SumAsync(a => a.Balance, ct);

    public async Task<PatrimonyMonthlySnapshot?> FindPatrimonySnapshotAsync(string userId, int year, int month, CancellationToken ct = default) =>
        await _db.FinancePatrimonySnapshots.AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId && p.Year == year && p.Month == month, ct);

    public async Task InsertPatrimonySnapshotAsync(PatrimonyMonthlySnapshot entity, CancellationToken ct = default)
    {
        await _db.FinancePatrimonySnapshots.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdatePatrimonySnapshotAsync(PatrimonyMonthlySnapshot entity, CancellationToken ct = default)
    {
        _db.FinancePatrimonySnapshots.Update(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<PatrimonyMonthlySnapshot>> ListPatrimonySnapshotsAsync(string userId, int take, CancellationToken ct = default)
    {
        var chunk = await _db.FinancePatrimonySnapshots.AsNoTracking()
            .Where(p => p.UserId == userId)
            .OrderByDescending(x => x.Year).ThenByDescending(x => x.Month)
            .Take(take)
            .ToListAsync(ct);
        return chunk.OrderBy(x => x.Year).ThenBy(x => x.Month).ToList();
    }
}
