using Project.Entities.Finance;
using Repositories.Interfaces;

namespace Repositories.Repositories;

/// <summary>Estado partilhado (singleton) para persistência financeira em memória no processo.</summary>
public sealed class FinanceStoreState
{
    internal readonly object Sync = new();
    internal readonly Dictionary<Guid, Category> Categories = new();
    internal readonly Dictionary<Guid, Expense> Expenses = new();
    internal readonly Dictionary<Guid, Income> Incomes = new();
    internal readonly Dictionary<Guid, CreditCard> CreditCards = new();
    internal readonly Dictionary<Guid, InstallmentPlan> InstallmentPlans = new();
    internal readonly Dictionary<Guid, Installment> Installments = new();
    internal readonly Dictionary<Guid, RecurringExpense> RecurringExpenses = new();
    internal readonly Dictionary<Guid, RecurringExpenseAmountSchedule> RecurringAmountSchedules = new();
    internal readonly Dictionary<Guid, Debt> Debts = new();
    internal readonly Dictionary<Guid, Account> Accounts = new();
    internal readonly Dictionary<Guid, PatrimonyMonthlySnapshot> PatrimonySnapshots = new();
}

public sealed class MemoryFinanceStore(FinanceStoreState s) : IFinanceStore
{
    private static Category Clone(Category c) => new()
    {
        Id = c.Id,
        UserId = c.UserId,
        Name = c.Name,
        IsExpense = c.IsExpense,
        CreatedAt = c.CreatedAt
    };

    private static Expense Clone(Expense e) => new()
    {
        Id = e.Id,
        UserId = e.UserId,
        Amount = e.Amount,
        Date = e.Date,
        CategoryId = e.CategoryId,
        Description = e.Description,
        PaymentMethod = e.PaymentMethod,
        StoreLocation = e.StoreLocation,
        CreditCardId = e.CreditCardId,
        InstallmentPlanId = e.InstallmentPlanId,
        RecurringExpenseId = e.RecurringExpenseId,
        ImagePath = e.ImagePath,
        CreationSource = e.CreationSource,
        CreatedAt = e.CreatedAt,
        Category = null!,
        CreditCard = null,
        InstallmentPlan = null
    };

    private static Income Clone(Income i) => new()
    {
        Id = i.Id,
        UserId = i.UserId,
        Amount = i.Amount,
        Source = i.Source,
        ReferenceMonth = i.ReferenceMonth,
        CreatedAt = i.CreatedAt,
        Description = i.Description,
        BatchId = i.BatchId,
        CreditCardId = i.CreditCardId
    };

    private static CreditCard Clone(CreditCard c) => new()
    {
        Id = c.Id,
        UserId = c.UserId,
        Name = c.Name,
        ClosingDay = c.ClosingDay,
        DueDay = c.DueDay,
        IsMealVoucher = c.IsMealVoucher,
        MealVoucherDailyAmount = c.MealVoucherDailyAmount,
        MealVoucherCreditDay = c.MealVoucherCreditDay,
        ThemeColor = c.ThemeColor,
        CreatedAt = c.CreatedAt
    };

    private static InstallmentPlan ClonePlanScalars(InstallmentPlan p) => new()
    {
        Id = p.Id,
        UserId = p.UserId,
        CreditCardId = p.CreditCardId,
        CategoryId = p.CategoryId,
        Description = p.Description,
        TotalAmount = p.TotalAmount,
        InstallmentCount = p.InstallmentCount,
        StartDate = p.StartDate,
        CreatedAt = p.CreatedAt,
        Category = null!,
        CreditCard = null,
        Installments = new List<Installment>()
    };

    private static Installment Clone(Installment i) => new()
    {
        Id = i.Id,
        InstallmentPlanId = i.InstallmentPlanId,
        SequenceNumber = i.SequenceNumber,
        DueDate = i.DueDate,
        Amount = i.Amount,
        IsPaid = i.IsPaid,
        ExpenseId = i.ExpenseId,
        InstallmentPlan = null!,
        Expense = null
    };

    private static RecurringExpense Clone(RecurringExpense r) => new()
    {
        Id = r.Id,
        UserId = r.UserId,
        Type = r.Type,
        Amount = r.Amount,
        CategoryId = r.CategoryId,
        Description = r.Description,
        PaymentMethod = r.PaymentMethod,
        DayOfMonth = r.DayOfMonth,
        Active = r.Active,
        CreatedAt = r.CreatedAt,
        CreditCardId = r.CreditCardId,
        LastGeneratedMonth = r.LastGeneratedMonth,
        Category = null!,
        CreditCard = null
    };

    private static RecurringExpenseAmountSchedule CloneSchedule(RecurringExpenseAmountSchedule x) => new()
    {
        Id = x.Id,
        UserId = x.UserId,
        RecurringExpenseId = x.RecurringExpenseId,
        EffectiveFrom = x.EffectiveFrom,
        Amount = x.Amount,
        CreatedAt = x.CreatedAt
    };

    private static DateTime NormalizeMonthStartUtc(DateTime any)
    {
        var d = any.Kind == DateTimeKind.Utc ? any : any.ToUniversalTime();
        return new DateTime(d.Year, d.Month, 1, 0, 0, 0, DateTimeKind.Utc);
    }

    private static Debt Clone(Debt d) => new()
    {
        Id = d.Id,
        UserId = d.UserId,
        Name = d.Name,
        TotalAmount = d.TotalAmount,
        PaidAmount = d.PaidAmount,
        CreatedAt = d.CreatedAt,
        DueDate = d.DueDate,
        MonthlyPayment = d.MonthlyPayment
    };

    private static Account Clone(Account a) => new()
    {
        Id = a.Id,
        UserId = a.UserId,
        Name = a.Name,
        Type = a.Type,
        Balance = a.Balance,
        Currency = a.Currency,
        CreatedAt = a.CreatedAt
    };

    private static PatrimonyMonthlySnapshot Clone(PatrimonyMonthlySnapshot p) => new()
    {
        Id = p.Id,
        UserId = p.UserId,
        Year = p.Year,
        Month = p.Month,
        TotalBalance = p.TotalBalance,
        UpdatedAt = p.UpdatedAt
    };

    private void HydrateExpenseCategoriesUnsafe(IEnumerable<Expense> expenses, string userId)
    {
        foreach (var e in expenses)
        {
            e.Category = s.Categories.TryGetValue(e.CategoryId, out var c) && c.UserId == userId
                ? Clone(c)
                : new Category { Id = e.CategoryId, Name = "?", UserId = userId };
        }
    }

    private void HydratePlanGraphUnsafe(InstallmentPlan plan, string userId)
    {
        plan.Category = s.Categories.TryGetValue(plan.CategoryId, out var c) && c.UserId == userId
            ? Clone(c)
            : new Category { Id = plan.CategoryId, Name = "?", UserId = userId };
        if (plan.CreditCardId.HasValue && s.CreditCards.TryGetValue(plan.CreditCardId.Value, out var cc) && cc.UserId == userId)
            plan.CreditCard = Clone(cc);
    }

    public Task<IReadOnlyList<Category>> ListCategoriesAsync(string userId, CancellationToken ct = default)
    {
        lock (s.Sync)
        {
            var list = s.Categories.Values.Where(c => c.UserId == userId).OrderBy(c => c.Name).Select(Clone).ToList();
            return Task.FromResult<IReadOnlyList<Category>>(list);
        }
    }

    public Task<IReadOnlyList<Category>> ListExpenseCategoriesAsync(string userId, CancellationToken ct = default)
    {
        lock (s.Sync)
        {
            var list = s.Categories.Values.Where(c => c.UserId == userId && c.IsExpense).OrderBy(c => c.Name).Select(Clone).ToList();
            return Task.FromResult<IReadOnlyList<Category>>(list);
        }
    }

    public Task<Category?> GetCategoryAsync(string userId, Guid id, CancellationToken ct = default)
    {
        lock (s.Sync)
        {
            if (!s.Categories.TryGetValue(id, out var c) || c.UserId != userId)
                return Task.FromResult<Category?>(null);
            return Task.FromResult<Category?>(Clone(c));
        }
    }

    public Task InsertCategoryAsync(Category entity, CancellationToken ct = default)
    {
        lock (s.Sync)
        {
            s.Categories[entity.Id] = Clone(entity);
            return Task.CompletedTask;
        }
    }

    public Task UpdateCategoryAsync(Category entity, CancellationToken ct = default) => InsertCategoryAsync(entity, ct);

    public Task DeleteCategoryAsync(string userId, Guid id, CancellationToken ct = default)
    {
        lock (s.Sync)
        {
            s.Categories.Remove(id);
            return Task.CompletedTask;
        }
    }

    public Task<bool> CategoryInUseAsync(string userId, Guid categoryId, CancellationToken ct = default)
    {
        lock (s.Sync)
        {
            if (s.Expenses.Values.Any(e => e.UserId == userId && e.CategoryId == categoryId))
                return Task.FromResult(true);
            if (s.RecurringExpenses.Values.Any(r => r.UserId == userId && r.CategoryId == categoryId))
                return Task.FromResult(true);
            if (s.InstallmentPlans.Values.Any(p => p.UserId == userId && p.CategoryId == categoryId))
                return Task.FromResult(true);
            return Task.FromResult(false);
        }
    }

    public Task<IReadOnlyList<Expense>> ListExpensesAsync(string userId, DateTime? from = null, DateTime? toExclusive = null,
        CancellationToken ct = default)
    {
        lock (s.Sync)
        {
            var q = s.Expenses.Values.Where(e => e.UserId == userId);
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

            return Task.FromResult<IReadOnlyList<Expense>>(q.Select(Clone).ToList());
        }
    }

    public Task<IReadOnlyList<Expense>> ListExpensesForCardAsync(string userId, Guid cardId, DateTime from, DateTime toExclusive,
        CancellationToken ct = default)
    {
        lock (s.Sync)
        {
            var f = from.ToUniversalTime();
            var t = toExclusive.ToUniversalTime();
            var list = s.Expenses.Values
                .Where(e => e.UserId == userId && e.CreditCardId == cardId
                    && e.Date.ToUniversalTime() >= f && e.Date.ToUniversalTime() < t)
                .OrderByDescending(e => e.Date)
                .Select(Clone)
                .ToList();
            return Task.FromResult<IReadOnlyList<Expense>>(list);
        }
    }

    public Task HydrateExpenseCategoriesAsync(IEnumerable<Expense> expenses, string userId, CancellationToken ct = default)
    {
        lock (s.Sync)
        {
            HydrateExpenseCategoriesUnsafe(expenses, userId);
            return Task.CompletedTask;
        }
    }

    public Task<Expense?> GetExpenseAsync(string userId, Guid id, CancellationToken ct = default)
    {
        lock (s.Sync)
        {
            if (!s.Expenses.TryGetValue(id, out var e) || e.UserId != userId)
                return Task.FromResult<Expense?>(null);
            var copy = Clone(e);
            HydrateExpenseCategoriesUnsafe(new[] { copy }, userId);
            return Task.FromResult<Expense?>(copy);
        }
    }

    public Task InsertExpenseAsync(Expense entity, CancellationToken ct = default)
    {
        lock (s.Sync)
        {
            s.Expenses[entity.Id] = Clone(entity);
            return Task.CompletedTask;
        }
    }

    public Task UpdateExpenseAsync(Expense entity, CancellationToken ct = default) => InsertExpenseAsync(entity, ct);

    public Task DeleteExpenseAsync(string userId, Guid id, CancellationToken ct = default)
    {
        lock (s.Sync)
        {
            s.Expenses.Remove(id);
            return Task.CompletedTask;
        }
    }

    public Task<bool> AnyExpenseForCreditCardAsync(Guid cardId, CancellationToken ct = default)
    {
        lock (s.Sync)
        {
            return Task.FromResult(s.Expenses.Values.Any(e => e.CreditCardId == cardId));
        }
    }

    public Task<IReadOnlyList<Income>> ListIncomesAsync(string userId, CancellationToken ct = default)
    {
        lock (s.Sync)
        {
            var list = s.Incomes.Values.Where(i => i.UserId == userId).OrderByDescending(i => i.ReferenceMonth).Select(Clone).ToList();
            return Task.FromResult<IReadOnlyList<Income>>(list);
        }
    }

    public Task<IReadOnlyList<Income>> ListIncomesFromMonthAsync(string userId, DateTime fromMonthFirstDay, CancellationToken ct = default)
    {
        lock (s.Sync)
        {
            var ts = fromMonthFirstDay.ToUniversalTime();
            var list = s.Incomes.Values
                .Where(i => i.UserId == userId && i.ReferenceMonth.ToUniversalTime() >= ts)
                .OrderBy(i => i.ReferenceMonth)
                .Select(Clone)
                .ToList();
            return Task.FromResult<IReadOnlyList<Income>>(list);
        }
    }

    public Task<decimal> SumIncomeForMonthAsync(string userId, DateTime monthStart, DateTime monthEndExclusive, CancellationToken ct = default)
    {
        lock (s.Sync)
        {
            var a = monthStart.ToUniversalTime();
            var b = monthEndExclusive.ToUniversalTime();
            var sum = s.Incomes.Values
                .Where(i => i.UserId == userId
                    && i.ReferenceMonth.ToUniversalTime() >= a
                    && i.ReferenceMonth.ToUniversalTime() < b)
                .Sum(i => i.Amount);
            return Task.FromResult(sum);
        }
    }

    public Task<Income?> GetIncomeAsync(string userId, Guid id, CancellationToken ct = default)
    {
        lock (s.Sync)
        {
            if (!s.Incomes.TryGetValue(id, out var i) || i.UserId != userId)
                return Task.FromResult<Income?>(null);
            return Task.FromResult<Income?>(Clone(i));
        }
    }

    public Task InsertIncomeAsync(Income entity, CancellationToken ct = default)
    {
        lock (s.Sync)
        {
            s.Incomes[entity.Id] = Clone(entity);
            return Task.CompletedTask;
        }
    }

    public Task UpdateIncomeAsync(Income entity, CancellationToken ct = default) => InsertIncomeAsync(entity, ct);

    public Task DeleteIncomeAsync(string userId, Guid id, CancellationToken ct = default)
    {
        lock (s.Sync)
        {
            s.Incomes.Remove(id);
            return Task.CompletedTask;
        }
    }

    public Task<IReadOnlyList<CreditCard>> ListCreditCardsAsync(string userId, CancellationToken ct = default)
    {
        lock (s.Sync)
        {
            var list = s.CreditCards.Values.Where(c => c.UserId == userId).OrderBy(c => c.Name).Select(Clone).ToList();
            return Task.FromResult<IReadOnlyList<CreditCard>>(list);
        }
    }

    public Task<CreditCard?> GetCreditCardAsync(string userId, Guid id, CancellationToken ct = default)
    {
        lock (s.Sync)
        {
            if (!s.CreditCards.TryGetValue(id, out var c) || c.UserId != userId)
                return Task.FromResult<CreditCard?>(null);
            return Task.FromResult<CreditCard?>(Clone(c));
        }
    }

    public Task InsertCreditCardAsync(CreditCard entity, CancellationToken ct = default)
    {
        lock (s.Sync)
        {
            s.CreditCards[entity.Id] = Clone(entity);
            return Task.CompletedTask;
        }
    }

    public Task UpdateCreditCardAsync(CreditCard entity, CancellationToken ct = default) => InsertCreditCardAsync(entity, ct);

    public Task DeleteCreditCardAsync(string userId, Guid id, CancellationToken ct = default)
    {
        lock (s.Sync)
        {
            s.CreditCards.Remove(id);
            return Task.CompletedTask;
        }
    }

    public Task<bool> CreditCardInUseAsync(Guid cardId, CancellationToken ct = default)
    {
        lock (s.Sync)
        {
            if (s.Expenses.Values.Any(e => e.CreditCardId == cardId))
                return Task.FromResult(true);
            if (s.RecurringExpenses.Values.Any(r => r.CreditCardId == cardId))
                return Task.FromResult(true);
            if (s.InstallmentPlans.Values.Any(p => p.CreditCardId == cardId))
                return Task.FromResult(true);
            if (s.Incomes.Values.Any(i => i.CreditCardId == cardId))
                return Task.FromResult(true);
            return Task.FromResult(false);
        }
    }

    public Task<IReadOnlyList<InstallmentPlan>> ListInstallmentPlansAsync(string userId, CancellationToken ct = default)
    {
        lock (s.Sync)
        {
            var list = s.InstallmentPlans.Values.Where(p => p.UserId == userId).Select(ClonePlanScalars).ToList();
            foreach (var p in list)
            {
                p.Installments = s.Installments.Values
                    .Where(i => i.InstallmentPlanId == p.Id)
                    .OrderBy(i => i.SequenceNumber)
                    .Select(Clone)
                    .ToList();
            }

            return Task.FromResult<IReadOnlyList<InstallmentPlan>>(
                list.OrderByDescending(p => p.CreatedAt).ToList());
        }
    }

    public Task<InstallmentPlan?> GetInstallmentPlanAsync(string userId, Guid id, bool withInstallments, CancellationToken ct = default)
    {
        lock (s.Sync)
        {
            if (!s.InstallmentPlans.TryGetValue(id, out var src) || src.UserId != userId)
                return Task.FromResult<InstallmentPlan?>(null);
            var p = ClonePlanScalars(src);
            if (withInstallments)
            {
                p.Installments = s.Installments.Values
                    .Where(i => i.InstallmentPlanId == id)
                    .OrderBy(i => i.SequenceNumber)
                    .Select(Clone)
                    .ToList();
            }

            HydratePlanGraphUnsafe(p, userId);
            return Task.FromResult<InstallmentPlan?>(p);
        }
    }

    public Task InsertInstallmentPlanAsync(InstallmentPlan entity, CancellationToken ct = default)
    {
        lock (s.Sync)
        {
            s.InstallmentPlans[entity.Id] = ClonePlanScalars(entity);
            return Task.CompletedTask;
        }
    }

    public Task DeleteInstallmentPlanAsync(string userId, Guid id, CancellationToken ct = default)
    {
        lock (s.Sync)
        {
            foreach (var i in s.Installments.Values.Where(x => x.InstallmentPlanId == id).ToList())
                s.Installments.Remove(i.Id);
            s.InstallmentPlans.Remove(id);
            return Task.CompletedTask;
        }
    }

    public Task<bool> AnyInstallmentPlanForCategoryAsync(string userId, Guid categoryId, CancellationToken ct = default)
    {
        lock (s.Sync)
        {
            return Task.FromResult(s.InstallmentPlans.Values.Any(p => p.UserId == userId && p.CategoryId == categoryId));
        }
    }

    public Task InsertInstallmentAsync(Installment entity, CancellationToken ct = default)
    {
        lock (s.Sync)
        {
            s.Installments[entity.Id] = Clone(entity);
            return Task.CompletedTask;
        }
    }

    public Task<IReadOnlyList<Installment>> ListInstallmentsForPlanAsync(Guid planId, CancellationToken ct = default)
    {
        lock (s.Sync)
        {
            var list = s.Installments.Values
                .Where(i => i.InstallmentPlanId == planId)
                .OrderBy(i => i.SequenceNumber)
                .Select(Clone)
                .ToList();
            return Task.FromResult<IReadOnlyList<Installment>>(list);
        }
    }

    public Task<Installment?> GetInstallmentWithPlanAsync(Guid installmentId, CancellationToken ct = default)
    {
        lock (s.Sync)
        {
            if (!s.Installments.TryGetValue(installmentId, out var i))
                return Task.FromResult<Installment?>(null);
            var copy = Clone(i);
            if (s.InstallmentPlans.TryGetValue(i.InstallmentPlanId, out var planSrc))
            {
                var plan = ClonePlanScalars(planSrc);
                HydratePlanGraphUnsafe(plan, plan.UserId);
                copy.InstallmentPlan = plan;
            }

            return Task.FromResult<Installment?>(copy);
        }
    }

    public Task UpdateInstallmentAsync(Installment entity, CancellationToken ct = default) => InsertInstallmentAsync(entity, ct);

    public Task DeleteInstallmentAsync(Guid installmentId, CancellationToken ct = default)
    {
        lock (s.Sync)
        {
            s.Installments.Remove(installmentId);
            return Task.CompletedTask;
        }
    }

    public Task<IReadOnlyList<Installment>> ListUnpaidInstallmentsForUserPlansAsync(string userId, CancellationToken ct = default)
    {
        lock (s.Sync)
        {
            var planIds = s.InstallmentPlans.Values.Where(p => p.UserId == userId).Select(p => p.Id).ToHashSet();
            var all = s.Installments.Values
                .Where(i => planIds.Contains(i.InstallmentPlanId) && !i.IsPaid)
                .Select(Clone)
                .ToList();
            return Task.FromResult<IReadOnlyList<Installment>>(all);
        }
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

    public Task<Installment?> FindInstallmentByExpenseIdAsync(Guid expenseId, CancellationToken ct = default)
    {
        lock (s.Sync)
        {
            var i = s.Installments.Values.FirstOrDefault(x => x.ExpenseId == expenseId);
            return Task.FromResult(i == null ? null : Clone(i));
        }
    }

    public Task HydrateInstallmentPlansAsync(IEnumerable<Installment> installments, CancellationToken ct = default)
    {
        lock (s.Sync)
        {
            var seen = new Dictionary<Guid, InstallmentPlan>();
            foreach (var i in installments)
            {
                if (seen.TryGetValue(i.InstallmentPlanId, out var p))
                {
                    i.InstallmentPlan = p;
                    continue;
                }

                if (!s.InstallmentPlans.TryGetValue(i.InstallmentPlanId, out var planSrc))
                    continue;
                p = ClonePlanScalars(planSrc);
                seen[i.InstallmentPlanId] = p;
                i.InstallmentPlan = p;
                HydratePlanGraphUnsafe(p, p.UserId);
            }

            return Task.CompletedTask;
        }
    }

    public Task HydratePlanGraphAsync(InstallmentPlan plan, string userId, CancellationToken ct = default)
    {
        lock (s.Sync)
        {
            HydratePlanGraphUnsafe(plan, userId);
            return Task.CompletedTask;
        }
    }

    public Task<IReadOnlyList<RecurringExpense>> ListRecurringAsync(string userId, CancellationToken ct = default)
    {
        lock (s.Sync)
        {
            var list = s.RecurringExpenses.Values.Where(r => r.UserId == userId).OrderBy(r => r.Description).Select(Clone).ToList();
            return Task.FromResult<IReadOnlyList<RecurringExpense>>(list);
        }
    }

    public Task<RecurringExpense?> GetRecurringAsync(string userId, Guid id, CancellationToken ct = default)
    {
        lock (s.Sync)
        {
            if (!s.RecurringExpenses.TryGetValue(id, out var r) || r.UserId != userId)
                return Task.FromResult<RecurringExpense?>(null);
            return Task.FromResult<RecurringExpense?>(Clone(r));
        }
    }

    public Task InsertRecurringAsync(RecurringExpense entity, CancellationToken ct = default)
    {
        lock (s.Sync)
        {
            s.RecurringExpenses[entity.Id] = Clone(entity);
            return Task.CompletedTask;
        }
    }

    public Task UpdateRecurringAsync(RecurringExpense entity, CancellationToken ct = default) => InsertRecurringAsync(entity, ct);

    public Task<IReadOnlyList<RecurringExpenseAmountSchedule>> ListRecurringAmountSchedulesAsync(string? userId = null,
        Guid? recurringExpenseId = null, CancellationToken ct = default)
    {
        lock (s.Sync)
        {
            var q = s.RecurringAmountSchedules.Values.AsEnumerable();
            if (userId != null)
                q = q.Where(x => x.UserId == userId);
            if (recurringExpenseId.HasValue)
                q = q.Where(x => x.RecurringExpenseId == recurringExpenseId.Value);
            var list = q.OrderBy(x => x.EffectiveFrom).ThenBy(x => x.CreatedAt).Select(CloneSchedule).ToList();
            return Task.FromResult<IReadOnlyList<RecurringExpenseAmountSchedule>>(list);
        }
    }

    public Task UpsertRecurringAmountScheduleAsync(RecurringExpenseAmountSchedule entity, CancellationToken ct = default)
    {
        lock (s.Sync)
        {
            var normalizedFrom = NormalizeMonthStartUtc(entity.EffectiveFrom);
            var dup = s.RecurringAmountSchedules.Values.FirstOrDefault(x =>
                x.UserId == entity.UserId
                && x.RecurringExpenseId == entity.RecurringExpenseId
                && NormalizeMonthStartUtc(x.EffectiveFrom) == normalizedFrom
                && x.Id != entity.Id);
            if (dup != null)
                s.RecurringAmountSchedules.Remove(dup.Id);

            entity.EffectiveFrom = normalizedFrom;
            s.RecurringAmountSchedules[entity.Id] = CloneSchedule(entity);
            return Task.CompletedTask;
        }
    }

    public Task<bool> DeleteRecurringAmountScheduleAsync(string userId, Guid scheduleId, CancellationToken ct = default)
    {
        lock (s.Sync)
        {
            if (!s.RecurringAmountSchedules.TryGetValue(scheduleId, out var x) || x.UserId != userId)
                return Task.FromResult(false);
            s.RecurringAmountSchedules.Remove(scheduleId);
            return Task.FromResult(true);
        }
    }

    public Task DeleteRecurringAsync(string userId, Guid id, CancellationToken ct = default)
    {
        lock (s.Sync)
        {
            s.RecurringExpenses.Remove(id);
            foreach (var sid in s.RecurringAmountSchedules.Values.Where(x => x.RecurringExpenseId == id && x.UserId == userId).Select(x => x.Id).ToList())
                s.RecurringAmountSchedules.Remove(sid);
            return Task.CompletedTask;
        }
    }

    public Task<IReadOnlyList<RecurringExpense>> ListActiveRecurringForDayOfMonthAsync(int dayOfMonth, CancellationToken ct = default)
    {
        lock (s.Sync)
        {
            var list = s.RecurringExpenses.Values.Where(r => r.Active && r.DayOfMonth == dayOfMonth).Select(Clone).ToList();
            return Task.FromResult<IReadOnlyList<RecurringExpense>>(list);
        }
    }

    public Task<decimal> SumRecurringAsync(string userId, bool activeOnly, RecurringExpenseType type, CancellationToken ct = default)
    {
        lock (s.Sync)
        {
            var q = s.RecurringExpenses.Values.Where(r => r.UserId == userId && r.Type == type);
            if (activeOnly)
                q = q.Where(r => r.Active);
            return Task.FromResult(q.Sum(r => r.Amount));
        }
    }

    public Task HydrateRecurringCategoriesAsync(IEnumerable<RecurringExpense> items, string userId, CancellationToken ct = default)
    {
        lock (s.Sync)
        {
            foreach (var r in items)
            {
                r.Category = s.Categories.TryGetValue(r.CategoryId, out var c) && c.UserId == userId
                    ? Clone(c)
                    : new Category { Id = r.CategoryId, Name = "?", UserId = userId };
                if (r.CreditCardId.HasValue && s.CreditCards.TryGetValue(r.CreditCardId.Value, out var cc) && cc.UserId == userId)
                    r.CreditCard = Clone(cc);
            }

            return Task.CompletedTask;
        }
    }

    public Task<IReadOnlyList<Debt>> ListDebtsAsync(string userId, CancellationToken ct = default)
    {
        lock (s.Sync)
        {
            var list = s.Debts.Values.Where(d => d.UserId == userId).OrderBy(d => d.Name).Select(Clone).ToList();
            return Task.FromResult<IReadOnlyList<Debt>>(list);
        }
    }

    public Task<Debt?> GetDebtAsync(string userId, Guid id, CancellationToken ct = default)
    {
        lock (s.Sync)
        {
            if (!s.Debts.TryGetValue(id, out var d) || d.UserId != userId)
                return Task.FromResult<Debt?>(null);
            return Task.FromResult<Debt?>(Clone(d));
        }
    }

    public Task InsertDebtAsync(Debt entity, CancellationToken ct = default)
    {
        lock (s.Sync)
        {
            s.Debts[entity.Id] = Clone(entity);
            return Task.CompletedTask;
        }
    }

    public Task UpdateDebtAsync(Debt entity, CancellationToken ct = default) => InsertDebtAsync(entity, ct);

    public Task DeleteDebtAsync(string userId, Guid id, CancellationToken ct = default)
    {
        lock (s.Sync)
        {
            s.Debts.Remove(id);
            return Task.CompletedTask;
        }
    }

    public Task<decimal> SumDebtBalanceAsync(string userId, CancellationToken ct = default)
    {
        lock (s.Sync)
        {
            var sum = s.Debts.Values.Where(d => d.UserId == userId).Sum(d => d.TotalAmount - d.PaidAmount);
            return Task.FromResult(sum);
        }
    }

    public Task<IReadOnlyList<Account>> ListAccountsAsync(string userId, CancellationToken ct = default)
    {
        lock (s.Sync)
        {
            var list = s.Accounts.Values.Where(a => a.UserId == userId).OrderBy(a => a.Name).Select(Clone).ToList();
            return Task.FromResult<IReadOnlyList<Account>>(list);
        }
    }

    public Task<Account?> GetAccountAsync(string userId, Guid id, CancellationToken ct = default)
    {
        lock (s.Sync)
        {
            if (!s.Accounts.TryGetValue(id, out var a) || a.UserId != userId)
                return Task.FromResult<Account?>(null);
            return Task.FromResult<Account?>(Clone(a));
        }
    }

    public Task InsertAccountAsync(Account entity, CancellationToken ct = default)
    {
        lock (s.Sync)
        {
            s.Accounts[entity.Id] = Clone(entity);
            return Task.CompletedTask;
        }
    }

    public Task UpdateAccountAsync(Account entity, CancellationToken ct = default) => InsertAccountAsync(entity, ct);

    public Task DeleteAccountAsync(string userId, Guid id, CancellationToken ct = default)
    {
        lock (s.Sync)
        {
            s.Accounts.Remove(id);
            return Task.CompletedTask;
        }
    }

    public Task<decimal> SumAccountBalancesAsync(string userId, CancellationToken ct = default)
    {
        lock (s.Sync)
        {
            return Task.FromResult(s.Accounts.Values.Where(a => a.UserId == userId).Sum(a => a.Balance));
        }
    }

    public Task<PatrimonyMonthlySnapshot?> FindPatrimonySnapshotAsync(string userId, int year, int month, CancellationToken ct = default)
    {
        lock (s.Sync)
        {
            var hit = s.PatrimonySnapshots.Values.FirstOrDefault(p => p.UserId == userId && p.Year == year && p.Month == month);
            return Task.FromResult(hit == null ? null : Clone(hit));
        }
    }

    public Task InsertPatrimonySnapshotAsync(PatrimonyMonthlySnapshot entity, CancellationToken ct = default)
    {
        lock (s.Sync)
        {
            s.PatrimonySnapshots[entity.Id] = Clone(entity);
            return Task.CompletedTask;
        }
    }

    public Task UpdatePatrimonySnapshotAsync(PatrimonyMonthlySnapshot entity, CancellationToken ct = default) =>
        InsertPatrimonySnapshotAsync(entity, ct);

    public Task<IReadOnlyList<PatrimonyMonthlySnapshot>> ListPatrimonySnapshotsAsync(string userId, int take, CancellationToken ct = default)
    {
        lock (s.Sync)
        {
            var list = s.PatrimonySnapshots.Values
                .Where(p => p.UserId == userId)
                .OrderByDescending(x => x.Year).ThenByDescending(x => x.Month)
                .Take(take)
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .Select(Clone)
                .ToList();
            return Task.FromResult<IReadOnlyList<PatrimonyMonthlySnapshot>>(list);
        }
    }
}
