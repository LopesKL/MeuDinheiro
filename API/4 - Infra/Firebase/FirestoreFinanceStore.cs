using System.Globalization;
using Google.Cloud.Firestore;
using Project.Entities.Finance;
using Repositories.Interfaces;

namespace Firebase;

public sealed class FirestoreFinanceStore(FirestoreDb db) : IFinanceStore
{
    private const string Cat = "fin_categories";
    private const string Inc = "fin_incomes";
    private const string Exp = "fin_expenses";
    private const string Cc = "fin_credit_cards";
    private const string Plan = "fin_installment_plans";
    private const string Inst = "fin_installments";
    private const string DebtC = "fin_debts";
    private const string Rec = "fin_recurring_expenses";
    private const string Acc = "fin_accounts";
    private const string Pat = "fin_patrimony_snapshots";

    private static string D(decimal v) => v.ToString(CultureInfo.InvariantCulture);
    private static decimal R(string s) => decimal.Parse(s, CultureInfo.InvariantCulture);

    private CollectionReference C(string name) => db.Collection(name);

    public async Task<IReadOnlyList<Category>> ListCategoriesAsync(string userId, CancellationToken ct = default)
    {
        var snap = await C(Cat).WhereEqualTo("UserId", userId).GetSnapshotAsync(ct);
        return snap.Documents.Select(CategoryFromDoc).OrderBy(x => x.Name).ToList();
    }

    public async Task<IReadOnlyList<Category>> ListExpenseCategoriesAsync(string userId, CancellationToken ct = default)
    {
        var list = await ListCategoriesAsync(userId, ct);
        return list.Where(c => c.IsExpense).OrderBy(c => c.Name).ToList();
    }

    public async Task<Category?> GetCategoryAsync(string userId, Guid id, CancellationToken ct = default)
    {
        var d = await C(Cat).Document(id.ToString()).GetSnapshotAsync(ct);
        if (!d.Exists) return null;
        var c = CategoryFromDoc(d);
        return c.UserId == userId ? c : null;
    }

    public Task InsertCategoryAsync(Category entity, CancellationToken ct = default) =>
        C(Cat).Document(entity.Id.ToString()).SetAsync(CategoryToDict(entity), cancellationToken: ct);

    public Task UpdateCategoryAsync(Category entity, CancellationToken ct = default) =>
        InsertCategoryAsync(entity, ct);

    public Task DeleteCategoryAsync(string userId, Guid id, CancellationToken ct = default) =>
        C(Cat).Document(id.ToString()).DeleteAsync(cancellationToken: ct);

    public async Task<bool> CategoryInUseAsync(string userId, Guid categoryId, CancellationToken ct = default)
    {
        var e = await C(Exp).WhereEqualTo("UserId", userId).WhereEqualTo("CategoryId", categoryId.ToString()).Limit(1).GetSnapshotAsync(ct);
        if (e.Count > 0) return true;
        var r = await C(Rec).WhereEqualTo("UserId", userId).WhereEqualTo("CategoryId", categoryId.ToString()).Limit(1).GetSnapshotAsync(ct);
        if (r.Count > 0) return true;
        var p = await C(Plan).WhereEqualTo("UserId", userId).WhereEqualTo("CategoryId", categoryId.ToString()).Limit(1).GetSnapshotAsync(ct);
        return p.Count > 0;
    }

    public async Task<IReadOnlyList<Expense>> ListExpensesAsync(string userId, DateTime? from = null, DateTime? toExclusive = null,
        CancellationToken ct = default)
    {
        Query q = C(Exp).WhereEqualTo("UserId", userId);
        if (from.HasValue)
            q = q.WhereGreaterThanOrEqualTo("Date", Timestamp.FromDateTime(from.Value.ToUniversalTime()));
        if (toExclusive.HasValue)
            q = q.WhereLessThan("Date", Timestamp.FromDateTime(toExclusive.Value.ToUniversalTime()));
        var snap = await q.GetSnapshotAsync(ct);
        return snap.Documents.Select(ExpenseFromDoc).ToList();
    }

    public async Task<IReadOnlyList<Expense>> ListExpensesForCardAsync(string userId, Guid cardId, DateTime from, DateTime toExclusive,
        CancellationToken ct = default)
    {
        var snap = await C(Exp)
            .WhereEqualTo("UserId", userId)
            .WhereEqualTo("CreditCardId", cardId.ToString())
            .WhereGreaterThanOrEqualTo("Date", Timestamp.FromDateTime(from.ToUniversalTime()))
            .WhereLessThan("Date", Timestamp.FromDateTime(toExclusive.ToUniversalTime()))
            .GetSnapshotAsync(ct);
        return snap.Documents.Select(ExpenseFromDoc).OrderByDescending(x => x.Date).ToList();
    }

    public async Task HydrateExpenseCategoriesAsync(IEnumerable<Expense> expenses, string userId, CancellationToken ct = default)
    {
        foreach (var e in expenses)
        {
            e.Category = (await GetCategoryAsync(userId, e.CategoryId, ct)) ?? new Category { Id = e.CategoryId, Name = "?", UserId = userId };
        }
    }

    public async Task<Expense?> GetExpenseAsync(string userId, Guid id, CancellationToken ct = default)
    {
        var d = await C(Exp).Document(id.ToString()).GetSnapshotAsync(ct);
        if (!d.Exists) return null;
        var e = ExpenseFromDoc(d);
        if (e.UserId != userId) return null;
        await HydrateExpenseCategoriesAsync(new[] { e }, userId, ct);
        return e;
    }

    public Task InsertExpenseAsync(Expense entity, CancellationToken ct = default) =>
        C(Exp).Document(entity.Id.ToString()).SetAsync(ExpenseToDict(entity), cancellationToken: ct);

    public Task UpdateExpenseAsync(Expense entity, CancellationToken ct = default) => InsertExpenseAsync(entity, ct);

    public Task DeleteExpenseAsync(string userId, Guid id, CancellationToken ct = default) =>
        C(Exp).Document(id.ToString()).DeleteAsync(cancellationToken: ct);

    public async Task<bool> AnyExpenseForCreditCardAsync(Guid cardId, CancellationToken ct = default)
    {
        var s = await C(Exp).WhereEqualTo("CreditCardId", cardId.ToString()).Limit(1).GetSnapshotAsync(ct);
        return s.Count > 0;
    }

    public async Task<IReadOnlyList<Income>> ListIncomesAsync(string userId, CancellationToken ct = default)
    {
        var snap = await C(Inc).WhereEqualTo("UserId", userId).GetSnapshotAsync(ct);
        return snap.Documents.Select(IncomeFromDoc).OrderByDescending(x => x.ReferenceMonth).ToList();
    }

    public async Task<IReadOnlyList<Income>> ListIncomesFromMonthAsync(string userId, DateTime fromMonthFirstDay, CancellationToken ct = default)
    {
        var ts = Timestamp.FromDateTime(fromMonthFirstDay.ToUniversalTime());
        var snap = await C(Inc).WhereEqualTo("UserId", userId).WhereGreaterThanOrEqualTo("ReferenceMonth", ts).GetSnapshotAsync(ct);
        return snap.Documents.Select(IncomeFromDoc).OrderBy(x => x.ReferenceMonth).ToList();
    }

    public async Task<decimal> SumIncomeForMonthAsync(string userId, DateTime monthStart, DateTime monthEndExclusive, CancellationToken ct = default)
    {
        var list = await C(Inc)
            .WhereEqualTo("UserId", userId)
            .WhereGreaterThanOrEqualTo("ReferenceMonth", Timestamp.FromDateTime(monthStart.ToUniversalTime()))
            .WhereLessThan("ReferenceMonth", Timestamp.FromDateTime(monthEndExclusive.ToUniversalTime()))
            .GetSnapshotAsync(ct);
        return list.Documents.Sum(d => R(d.GetValue<string>("Amount")));
    }

    public async Task<Income?> GetIncomeAsync(string userId, Guid id, CancellationToken ct = default)
    {
        var d = await C(Inc).Document(id.ToString()).GetSnapshotAsync(ct);
        if (!d.Exists) return null;
        var i = IncomeFromDoc(d);
        return i.UserId == userId ? i : null;
    }

    public Task InsertIncomeAsync(Income entity, CancellationToken ct = default) =>
        C(Inc).Document(entity.Id.ToString()).SetAsync(IncomeToDict(entity), cancellationToken: ct);

    public Task UpdateIncomeAsync(Income entity, CancellationToken ct = default) => InsertIncomeAsync(entity, ct);

    public Task DeleteIncomeAsync(string userId, Guid id, CancellationToken ct = default) =>
        C(Inc).Document(id.ToString()).DeleteAsync(cancellationToken: ct);

    public async Task<IReadOnlyList<CreditCard>> ListCreditCardsAsync(string userId, CancellationToken ct = default)
    {
        var snap = await C(Cc).WhereEqualTo("UserId", userId).GetSnapshotAsync(ct);
        return snap.Documents.Select(CreditCardFromDoc).OrderBy(x => x.Name).ToList();
    }

    public async Task<CreditCard?> GetCreditCardAsync(string userId, Guid id, CancellationToken ct = default)
    {
        var d = await C(Cc).Document(id.ToString()).GetSnapshotAsync(ct);
        if (!d.Exists) return null;
        var c = CreditCardFromDoc(d);
        return c.UserId == userId ? c : null;
    }

    public Task InsertCreditCardAsync(CreditCard entity, CancellationToken ct = default) =>
        C(Cc).Document(entity.Id.ToString()).SetAsync(CreditCardToDict(entity), cancellationToken: ct);

    public Task UpdateCreditCardAsync(CreditCard entity, CancellationToken ct = default) => InsertCreditCardAsync(entity, ct);

    public Task DeleteCreditCardAsync(string userId, Guid id, CancellationToken ct = default) =>
        C(Cc).Document(id.ToString()).DeleteAsync(cancellationToken: ct);

    public async Task<bool> CreditCardInUseAsync(Guid cardId, CancellationToken ct = default)
    {
        if (await AnyExpenseForCreditCardAsync(cardId, ct)) return true;
        var r = await C(Rec).WhereEqualTo("CreditCardId", cardId.ToString()).Limit(1).GetSnapshotAsync(ct);
        if (r.Count > 0) return true;
        var p = await C(Plan).WhereEqualTo("CreditCardId", cardId.ToString()).Limit(1).GetSnapshotAsync(ct);
        return p.Count > 0;
    }

    public async Task<IReadOnlyList<InstallmentPlan>> ListInstallmentPlansAsync(string userId, CancellationToken ct = default)
    {
        var snap = await C(Plan).WhereEqualTo("UserId", userId).GetSnapshotAsync(ct);
        var list = snap.Documents.Select(InstallmentPlanFromDoc).ToList();
        foreach (var p in list)
            p.Installments = (await ListInstallmentsForPlanAsync(p.Id, ct)).ToList();
        return list.OrderByDescending(p => p.CreatedAt).ToList();
    }

    public async Task<InstallmentPlan?> GetInstallmentPlanAsync(string userId, Guid id, bool withInstallments, CancellationToken ct = default)
    {
        var d = await C(Plan).Document(id.ToString()).GetSnapshotAsync(ct);
        if (!d.Exists) return null;
        var p = InstallmentPlanFromDoc(d);
        if (p.UserId != userId) return null;
        if (withInstallments)
            p.Installments = (await ListInstallmentsForPlanAsync(id, ct)).ToList();
        await HydratePlanGraphAsync(p, userId, ct);
        return p;
    }

    public Task InsertInstallmentPlanAsync(InstallmentPlan entity, CancellationToken ct = default) =>
        C(Plan).Document(entity.Id.ToString()).SetAsync(InstallmentPlanToDict(entity), cancellationToken: ct);

    public async Task DeleteInstallmentPlanAsync(string userId, Guid id, CancellationToken ct = default)
    {
        foreach (var i in await ListInstallmentsForPlanAsync(id, ct))
            await C(Inst).Document(i.Id.ToString()).DeleteAsync(cancellationToken: ct);
        await C(Plan).Document(id.ToString()).DeleteAsync(cancellationToken: ct);
    }

    public async Task<bool> AnyInstallmentPlanForCategoryAsync(string userId, Guid categoryId, CancellationToken ct = default)
    {
        var s = await C(Plan).WhereEqualTo("UserId", userId).WhereEqualTo("CategoryId", categoryId.ToString()).Limit(1).GetSnapshotAsync(ct);
        return s.Count > 0;
    }

    public Task InsertInstallmentAsync(Installment entity, CancellationToken ct = default) =>
        C(Inst).Document(entity.Id.ToString()).SetAsync(InstallmentToDict(entity), cancellationToken: ct);

    public async Task<IReadOnlyList<Installment>> ListInstallmentsForPlanAsync(Guid planId, CancellationToken ct = default)
    {
        var snap = await C(Inst).WhereEqualTo("InstallmentPlanId", planId.ToString()).GetSnapshotAsync(ct);
        return snap.Documents.Select(InstallmentFromDoc).OrderBy(x => x.SequenceNumber).ToList();
    }

    public async Task<Installment?> GetInstallmentWithPlanAsync(Guid installmentId, CancellationToken ct = default)
    {
        var d = await C(Inst).Document(installmentId.ToString()).GetSnapshotAsync(ct);
        if (!d.Exists) return null;
        var i = InstallmentFromDoc(d);
        var planDoc = await C(Plan).Document(i.InstallmentPlanId.ToString()).GetSnapshotAsync(ct);
        if (!planDoc.Exists) return i;
        i.InstallmentPlan = InstallmentPlanFromDoc(planDoc);
        await HydratePlanGraphAsync(i.InstallmentPlan, i.InstallmentPlan.UserId, ct);
        return i;
    }

    public Task UpdateInstallmentAsync(Installment entity, CancellationToken ct = default) => InsertInstallmentAsync(entity, ct);

    public Task DeleteInstallmentAsync(Guid installmentId, CancellationToken ct = default) =>
        C(Inst).Document(installmentId.ToString()).DeleteAsync(cancellationToken: ct);

    public async Task<IReadOnlyList<Installment>> ListUnpaidInstallmentsForUserPlansAsync(string userId, CancellationToken ct = default)
    {
        var plans = await C(Plan).WhereEqualTo("UserId", userId).GetSnapshotAsync(ct);
        var ids = plans.Documents.Select(x => Guid.Parse(x.Id)).ToList();
        var all = new List<Installment>();
        foreach (var pid in ids)
        {
            var snap = await C(Inst).WhereEqualTo("InstallmentPlanId", pid.ToString()).WhereEqualTo("IsPaid", false).GetSnapshotAsync(ct);
            all.AddRange(snap.Documents.Select(InstallmentFromDoc));
        }

        return all;
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

    public async Task<Installment?> FindInstallmentByExpenseIdAsync(Guid expenseId, CancellationToken ct = default)
    {
        var snap = await C(Inst).WhereEqualTo("ExpenseId", expenseId.ToString()).Limit(1).GetSnapshotAsync(ct);
        var d = snap.Documents.FirstOrDefault();
        return d == null ? null : InstallmentFromDoc(d);
    }

    public async Task HydrateInstallmentPlansAsync(IEnumerable<Installment> installments, CancellationToken ct = default)
    {
        var seen = new Dictionary<Guid, InstallmentPlan>();
        foreach (var i in installments)
        {
            if (seen.TryGetValue(i.InstallmentPlanId, out var p))
            {
                i.InstallmentPlan = p;
                continue;
            }

            var d = await C(Plan).Document(i.InstallmentPlanId.ToString()).GetSnapshotAsync(ct);
            if (!d.Exists) continue;
            p = InstallmentPlanFromDoc(d);
            seen[i.InstallmentPlanId] = p;
            i.InstallmentPlan = p;
            await HydratePlanGraphAsync(p, p.UserId, ct);
        }
    }

    public async Task HydratePlanGraphAsync(InstallmentPlan plan, string userId, CancellationToken ct = default)
    {
        plan.Category = (await GetCategoryAsync(userId, plan.CategoryId, ct)) ?? new Category { Id = plan.CategoryId, Name = "?", UserId = userId };
        if (plan.CreditCardId.HasValue)
            plan.CreditCard = await GetCreditCardAsync(userId, plan.CreditCardId.Value, ct);
    }

    public async Task<IReadOnlyList<RecurringExpense>> ListRecurringAsync(string userId, CancellationToken ct = default)
    {
        var snap = await C(Rec).WhereEqualTo("UserId", userId).GetSnapshotAsync(ct);
        return snap.Documents.Select(RecurringFromDoc).OrderBy(x => x.Description).ToList();
    }

    public async Task<RecurringExpense?> GetRecurringAsync(string userId, Guid id, CancellationToken ct = default)
    {
        var d = await C(Rec).Document(id.ToString()).GetSnapshotAsync(ct);
        if (!d.Exists) return null;
        var r = RecurringFromDoc(d);
        return r.UserId == userId ? r : null;
    }

    public Task InsertRecurringAsync(RecurringExpense entity, CancellationToken ct = default) =>
        C(Rec).Document(entity.Id.ToString()).SetAsync(RecurringToDict(entity), cancellationToken: ct);

    public Task UpdateRecurringAsync(RecurringExpense entity, CancellationToken ct = default) => InsertRecurringAsync(entity, ct);

    public Task DeleteRecurringAsync(string userId, Guid id, CancellationToken ct = default) =>
        C(Rec).Document(id.ToString()).DeleteAsync(cancellationToken: ct);

    public async Task<IReadOnlyList<RecurringExpense>> ListActiveRecurringForDayOfMonthAsync(int dayOfMonth, CancellationToken ct = default)
    {
        var snap = await C(Rec).WhereEqualTo("Active", true).WhereEqualTo("DayOfMonth", dayOfMonth).GetSnapshotAsync(ct);
        return snap.Documents.Select(RecurringFromDoc).ToList();
    }

    public async Task<decimal> SumRecurringAsync(string userId, bool activeOnly, RecurringExpenseType type, CancellationToken ct = default)
    {
        Query q = C(Rec).WhereEqualTo("UserId", userId).WhereEqualTo("Type", (long)type);
        if (activeOnly)
            q = q.WhereEqualTo("Active", true);
        var snap = await q.GetSnapshotAsync(ct);
        return snap.Documents.Sum(d => R(d.GetValue<string>("Amount")));
    }

    public async Task HydrateRecurringCategoriesAsync(IEnumerable<RecurringExpense> items, string userId, CancellationToken ct = default)
    {
        foreach (var r in items)
        {
            r.Category = (await GetCategoryAsync(userId, r.CategoryId, ct)) ?? new Category { Id = r.CategoryId, Name = "?", UserId = userId };
            if (r.CreditCardId.HasValue)
                r.CreditCard = await GetCreditCardAsync(userId, r.CreditCardId.Value, ct);
        }
    }

    public async Task<IReadOnlyList<Debt>> ListDebtsAsync(string userId, CancellationToken ct = default)
    {
        var snap = await C(DebtC).WhereEqualTo("UserId", userId).GetSnapshotAsync(ct);
        return snap.Documents.Select(DebtFromDoc).OrderBy(x => x.Name).ToList();
    }

    public async Task<Debt?> GetDebtAsync(string userId, Guid id, CancellationToken ct = default)
    {
        var d = await C(DebtC).Document(id.ToString()).GetSnapshotAsync(ct);
        if (!d.Exists) return null;
        var x = DebtFromDoc(d);
        return x.UserId == userId ? x : null;
    }

    public Task InsertDebtAsync(Debt entity, CancellationToken ct = default) =>
        C(DebtC).Document(entity.Id.ToString()).SetAsync(DebtToDict(entity), cancellationToken: ct);

    public Task UpdateDebtAsync(Debt entity, CancellationToken ct = default) => InsertDebtAsync(entity, ct);

    public Task DeleteDebtAsync(string userId, Guid id, CancellationToken ct = default) =>
        C(DebtC).Document(id.ToString()).DeleteAsync(cancellationToken: ct);

    public async Task<decimal> SumDebtBalanceAsync(string userId, CancellationToken ct = default)
    {
        var list = await ListDebtsAsync(userId, ct);
        return list.Sum(d => d.TotalAmount - d.PaidAmount);
    }

    public async Task<IReadOnlyList<Account>> ListAccountsAsync(string userId, CancellationToken ct = default)
    {
        var snap = await C(Acc).WhereEqualTo("UserId", userId).GetSnapshotAsync(ct);
        return snap.Documents.Select(AccountFromDoc).OrderBy(x => x.Name).ToList();
    }

    public async Task<Account?> GetAccountAsync(string userId, Guid id, CancellationToken ct = default)
    {
        var d = await C(Acc).Document(id.ToString()).GetSnapshotAsync(ct);
        if (!d.Exists) return null;
        var a = AccountFromDoc(d);
        return a.UserId == userId ? a : null;
    }

    public Task InsertAccountAsync(Account entity, CancellationToken ct = default) =>
        C(Acc).Document(entity.Id.ToString()).SetAsync(AccountToDict(entity), cancellationToken: ct);

    public Task UpdateAccountAsync(Account entity, CancellationToken ct = default) => InsertAccountAsync(entity, ct);

    public Task DeleteAccountAsync(string userId, Guid id, CancellationToken ct = default) =>
        C(Acc).Document(id.ToString()).DeleteAsync(cancellationToken: ct);

    public async Task<decimal> SumAccountBalancesAsync(string userId, CancellationToken ct = default)
    {
        var list = await ListAccountsAsync(userId, ct);
        return list.Sum(a => a.Balance);
    }

    public async Task<PatrimonyMonthlySnapshot?> FindPatrimonySnapshotAsync(string userId, int year, int month, CancellationToken ct = default)
    {
        var snap = await C(Pat)
            .WhereEqualTo("UserId", userId)
            .WhereEqualTo("Year", year)
            .WhereEqualTo("Month", month)
            .Limit(1)
            .GetSnapshotAsync(ct);
        var d = snap.Documents.FirstOrDefault();
        return d == null ? null : PatrimonyFromDoc(d);
    }

    public Task InsertPatrimonySnapshotAsync(PatrimonyMonthlySnapshot entity, CancellationToken ct = default) =>
        C(Pat).Document(entity.Id.ToString()).SetAsync(PatrimonyToDict(entity), cancellationToken: ct);

    public Task UpdatePatrimonySnapshotAsync(PatrimonyMonthlySnapshot entity, CancellationToken ct = default) =>
        InsertPatrimonySnapshotAsync(entity, ct);

    public async Task<IReadOnlyList<PatrimonyMonthlySnapshot>> ListPatrimonySnapshotsAsync(string userId, int take, CancellationToken ct = default)
    {
        var snap = await C(Pat).WhereEqualTo("UserId", userId).GetSnapshotAsync(ct);
        return snap.Documents
            .Select(PatrimonyFromDoc)
            .OrderByDescending(x => x.Year).ThenByDescending(x => x.Month)
            .Take(take)
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToList();
    }

    private static Dictionary<string, object> CategoryToDict(Category c) => new()
    {
        { "UserId", c.UserId },
        { "Name", c.Name },
        { "IsExpense", c.IsExpense },
        { "CreatedAt", Timestamp.FromDateTimeOffset(c.CreatedAt) }
    };

    private static Category CategoryFromDoc(DocumentSnapshot d) => new()
    {
        Id = Guid.Parse(d.Id),
        UserId = d.GetValue<string>("UserId"),
        Name = d.GetValue<string>("Name"),
        IsExpense = d.GetValue<bool>("IsExpense"),
        CreatedAt = d.GetValue<Timestamp>("CreatedAt").ToDateTimeOffset()
    };

    private static Dictionary<string, object> ExpenseToDict(Expense e)
    {
        var dict = new Dictionary<string, object>
        {
            { "UserId", e.UserId },
            { "Amount", D(e.Amount) },
            { "Date", Timestamp.FromDateTime(e.Date.ToUniversalTime()) },
            { "CategoryId", e.CategoryId.ToString() },
            { "Description", e.Description },
            { "PaymentMethod", (long)e.PaymentMethod },
            { "CreationSource", (long)e.CreationSource },
            { "CreatedAt", Timestamp.FromDateTimeOffset(e.CreatedAt) }
        };
        if (e.StoreLocation != null) dict["StoreLocation"] = e.StoreLocation;
        if (e.CreditCardId.HasValue) dict["CreditCardId"] = e.CreditCardId.Value.ToString();
        if (e.InstallmentPlanId.HasValue) dict["InstallmentPlanId"] = e.InstallmentPlanId.Value.ToString();
        if (e.ImagePath != null) dict["ImagePath"] = e.ImagePath;
        return dict;
    }

    private static Expense ExpenseFromDoc(DocumentSnapshot d)
    {
        var e = new Expense
        {
            Id = Guid.Parse(d.Id),
            UserId = d.GetValue<string>("UserId"),
            Amount = R(d.GetValue<string>("Amount")),
            Date = d.GetValue<Timestamp>("Date").ToDateTime(),
            CategoryId = Guid.Parse(d.GetValue<string>("CategoryId")),
            Description = d.GetValue<string>("Description"),
            PaymentMethod = (PaymentMethod)(int)d.GetValue<long>("PaymentMethod"),
            CreationSource = d.ContainsField("CreationSource")
                ? (ExpenseCreationSource)(int)d.GetValue<long>("CreationSource")
                : ExpenseCreationSource.Unspecified,
            CreatedAt = d.ContainsField("CreatedAt")
                ? d.GetValue<Timestamp>("CreatedAt").ToDateTimeOffset()
                : DateTimeOffset.UtcNow
        };
        if (d.ContainsField("StoreLocation")) e.StoreLocation = d.GetValue<string>("StoreLocation");
        if (d.ContainsField("CreditCardId")) e.CreditCardId = Guid.Parse(d.GetValue<string>("CreditCardId"));
        if (d.ContainsField("InstallmentPlanId")) e.InstallmentPlanId = Guid.Parse(d.GetValue<string>("InstallmentPlanId"));
        if (d.ContainsField("ImagePath")) e.ImagePath = d.GetValue<string>("ImagePath");
        return e;
    }

    private static Dictionary<string, object> IncomeToDict(Income i)
    {
        var dict = new Dictionary<string, object>
        {
            { "UserId", i.UserId },
            { "Amount", D(i.Amount) },
            { "Source", i.Source },
            { "ReferenceMonth", Timestamp.FromDateTime(i.ReferenceMonth.ToUniversalTime()) },
            { "CreatedAt", Timestamp.FromDateTimeOffset(i.CreatedAt) }
        };
        if (i.Description != null) dict["Description"] = i.Description;
        if (i.BatchId.HasValue) dict["BatchId"] = i.BatchId.Value.ToString();
        return dict;
    }

    private static Income IncomeFromDoc(DocumentSnapshot d)
    {
        var i = new Income
        {
            Id = Guid.Parse(d.Id),
            UserId = d.GetValue<string>("UserId"),
            Amount = R(d.GetValue<string>("Amount")),
            Source = d.GetValue<string>("Source"),
            ReferenceMonth = d.GetValue<Timestamp>("ReferenceMonth").ToDateTime(),
            CreatedAt = d.ContainsField("CreatedAt")
                ? d.GetValue<Timestamp>("CreatedAt").ToDateTimeOffset()
                : DateTimeOffset.UtcNow
        };
        if (d.ContainsField("Description")) i.Description = d.GetValue<string>("Description");
        if (d.ContainsField("BatchId")) i.BatchId = Guid.Parse(d.GetValue<string>("BatchId"));
        return i;
    }

    private static Dictionary<string, object> CreditCardToDict(CreditCard c) => new()
    {
        { "UserId", c.UserId },
        { "Name", c.Name },
        { "ClosingDay", c.ClosingDay },
        { "DueDay", c.DueDay },
        { "CreatedAt", Timestamp.FromDateTimeOffset(c.CreatedAt) }
    };

    private static CreditCard CreditCardFromDoc(DocumentSnapshot d) => new()
    {
        Id = Guid.Parse(d.Id),
        UserId = d.GetValue<string>("UserId"),
        Name = d.GetValue<string>("Name"),
        ClosingDay = (int)d.GetValue<long>("ClosingDay"),
        DueDay = (int)d.GetValue<long>("DueDay"),
        CreatedAt = d.GetValue<Timestamp>("CreatedAt").ToDateTimeOffset()
    };

    private static Dictionary<string, object> InstallmentPlanToDict(InstallmentPlan p)
    {
        var dict = new Dictionary<string, object>
        {
            { "UserId", p.UserId },
            { "CategoryId", p.CategoryId.ToString() },
            { "Description", p.Description },
            { "TotalAmount", D(p.TotalAmount) },
            { "InstallmentCount", p.InstallmentCount },
            { "StartDate", Timestamp.FromDateTime(p.StartDate.ToUniversalTime()) },
            { "CreatedAt", Timestamp.FromDateTimeOffset(p.CreatedAt) }
        };
        if (p.CreditCardId.HasValue) dict["CreditCardId"] = p.CreditCardId.Value.ToString();
        return dict;
    }

    private static InstallmentPlan InstallmentPlanFromDoc(DocumentSnapshot d)
    {
        var p = new InstallmentPlan
        {
            Id = Guid.Parse(d.Id),
            UserId = d.GetValue<string>("UserId"),
            CategoryId = Guid.Parse(d.GetValue<string>("CategoryId")),
            Description = d.GetValue<string>("Description"),
            TotalAmount = R(d.GetValue<string>("TotalAmount")),
            InstallmentCount = (int)d.GetValue<long>("InstallmentCount"),
            StartDate = d.GetValue<Timestamp>("StartDate").ToDateTime(),
            CreatedAt = d.GetValue<Timestamp>("CreatedAt").ToDateTimeOffset()
        };
        if (d.ContainsField("CreditCardId")) p.CreditCardId = Guid.Parse(d.GetValue<string>("CreditCardId"));
        return p;
    }

    private static Dictionary<string, object> InstallmentToDict(Installment i)
    {
        var dict = new Dictionary<string, object>
        {
            { "InstallmentPlanId", i.InstallmentPlanId.ToString() },
            { "SequenceNumber", i.SequenceNumber },
            { "DueDate", Timestamp.FromDateTime(i.DueDate.ToUniversalTime()) },
            { "Amount", D(i.Amount) },
            { "IsPaid", i.IsPaid }
        };
        if (i.ExpenseId.HasValue) dict["ExpenseId"] = i.ExpenseId.Value.ToString();
        return dict;
    }

    private static Installment InstallmentFromDoc(DocumentSnapshot d)
    {
        var i = new Installment
        {
            Id = Guid.Parse(d.Id),
            InstallmentPlanId = Guid.Parse(d.GetValue<string>("InstallmentPlanId")),
            SequenceNumber = (int)d.GetValue<long>("SequenceNumber"),
            DueDate = d.GetValue<Timestamp>("DueDate").ToDateTime(),
            Amount = R(d.GetValue<string>("Amount")),
            IsPaid = d.GetValue<bool>("IsPaid")
        };
        if (d.ContainsField("ExpenseId")) i.ExpenseId = Guid.Parse(d.GetValue<string>("ExpenseId"));
        return i;
    }

    private static Dictionary<string, object> RecurringToDict(RecurringExpense r)
    {
        var dict = new Dictionary<string, object>
        {
            { "UserId", r.UserId },
            { "Type", (long)r.Type },
            { "Amount", D(r.Amount) },
            { "CategoryId", r.CategoryId.ToString() },
            { "Description", r.Description },
            { "PaymentMethod", (long)r.PaymentMethod },
            { "DayOfMonth", r.DayOfMonth },
            { "Active", r.Active },
            { "CreatedAt", Timestamp.FromDateTimeOffset(r.CreatedAt) }
        };
        if (r.CreditCardId.HasValue) dict["CreditCardId"] = r.CreditCardId.Value.ToString();
        if (r.LastGeneratedMonth.HasValue)
            dict["LastGeneratedMonth"] = Timestamp.FromDateTime(r.LastGeneratedMonth.Value.ToUniversalTime());
        return dict;
    }

    private static RecurringExpense RecurringFromDoc(DocumentSnapshot d)
    {
        var r = new RecurringExpense
        {
            Id = Guid.Parse(d.Id),
            UserId = d.GetValue<string>("UserId"),
            Type = (RecurringExpenseType)(int)d.GetValue<long>("Type"),
            Amount = R(d.GetValue<string>("Amount")),
            CategoryId = Guid.Parse(d.GetValue<string>("CategoryId")),
            Description = d.GetValue<string>("Description"),
            PaymentMethod = (PaymentMethod)(int)d.GetValue<long>("PaymentMethod"),
            DayOfMonth = (int)d.GetValue<long>("DayOfMonth"),
            Active = d.GetValue<bool>("Active"),
            CreatedAt = d.GetValue<Timestamp>("CreatedAt").ToDateTimeOffset()
        };
        if (d.ContainsField("CreditCardId")) r.CreditCardId = Guid.Parse(d.GetValue<string>("CreditCardId"));
        if (d.ContainsField("LastGeneratedMonth"))
            r.LastGeneratedMonth = d.GetValue<Timestamp>("LastGeneratedMonth").ToDateTime();
        return r;
    }

    private static Dictionary<string, object> DebtToDict(Debt x)
    {
        var dict = new Dictionary<string, object>
        {
            { "UserId", x.UserId },
            { "Name", x.Name },
            { "TotalAmount", D(x.TotalAmount) },
            { "PaidAmount", D(x.PaidAmount) },
            { "CreatedAt", Timestamp.FromDateTimeOffset(x.CreatedAt) }
        };
        if (x.DueDate.HasValue) dict["DueDate"] = Timestamp.FromDateTime(x.DueDate.Value.ToUniversalTime());
        if (x.MonthlyPayment.HasValue) dict["MonthlyPayment"] = D(x.MonthlyPayment.Value);
        return dict;
    }

    private static Debt DebtFromDoc(DocumentSnapshot d)
    {
        var x = new Debt
        {
            Id = Guid.Parse(d.Id),
            UserId = d.GetValue<string>("UserId"),
            Name = d.GetValue<string>("Name"),
            TotalAmount = R(d.GetValue<string>("TotalAmount")),
            PaidAmount = R(d.GetValue<string>("PaidAmount")),
            CreatedAt = d.GetValue<Timestamp>("CreatedAt").ToDateTimeOffset()
        };
        if (d.ContainsField("DueDate")) x.DueDate = d.GetValue<Timestamp>("DueDate").ToDateTime();
        if (d.ContainsField("MonthlyPayment")) x.MonthlyPayment = R(d.GetValue<string>("MonthlyPayment"));
        return x;
    }

    private static Dictionary<string, object> AccountToDict(Account a) => new()
    {
        { "UserId", a.UserId },
        { "Name", a.Name },
        { "Type", (long)a.Type },
        { "Balance", D(a.Balance) },
        { "Currency", a.Currency },
        { "CreatedAt", Timestamp.FromDateTimeOffset(a.CreatedAt) }
    };

    private static Account AccountFromDoc(DocumentSnapshot d) => new()
    {
        Id = Guid.Parse(d.Id),
        UserId = d.GetValue<string>("UserId"),
        Name = d.GetValue<string>("Name"),
        Type = (AccountType)(int)d.GetValue<long>("Type"),
        Balance = R(d.GetValue<string>("Balance")),
        Currency = d.GetValue<string>("Currency"),
        CreatedAt = d.GetValue<Timestamp>("CreatedAt").ToDateTimeOffset()
    };

    private static Dictionary<string, object> PatrimonyToDict(PatrimonyMonthlySnapshot p) => new()
    {
        { "UserId", p.UserId },
        { "Year", p.Year },
        { "Month", p.Month },
        { "TotalBalance", D(p.TotalBalance) },
        { "UpdatedAt", Timestamp.FromDateTimeOffset(p.UpdatedAt) }
    };

    private static PatrimonyMonthlySnapshot PatrimonyFromDoc(DocumentSnapshot d) => new()
    {
        Id = Guid.Parse(d.Id),
        UserId = d.GetValue<string>("UserId"),
        Year = (int)d.GetValue<long>("Year"),
        Month = (int)d.GetValue<long>("Month"),
        TotalBalance = R(d.GetValue<string>("TotalBalance")),
        UpdatedAt = d.GetValue<Timestamp>("UpdatedAt").ToDateTimeOffset()
    };
}
