using Application.Dto.Finance;
using Project.Entities.Finance;
using Repositories.Interfaces;

namespace Application.Finance;

public class DashboardService
{
    private readonly IFinanceStore _finance;
    private readonly PatrimonySnapshotService _snapshots;

    public DashboardService(IFinanceStore finance, PatrimonySnapshotService snapshots)
    {
        _finance = finance;
        _snapshots = snapshots;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(string userId, int? year = null, int? month = null)
    {
        var now = DateTime.UtcNow;
        var y = year ?? now.Year;
        var mo = month ?? now.Month;
        if (mo is < 1 or > 12)
            mo = now.Month;
        if (y < 2000 || y > 2100)
            y = now.Year;

        var start = new DateTime(y, mo, 1);
        var end = start.AddMonths(1);

        var monthIncome = await _finance.SumIncomeForMonthAsync(userId, start, end);

        var flowStart = start.AddMonths(-1);
        var flowEndExclusive = start.AddMonths(2);

        var expensesWide = await _finance.ListExpensesAsync(userId, flowStart, flowEndExclusive);
        await _finance.HydrateExpenseCategoriesAsync(expensesWide, userId);

        var installmentsWide = await _finance.ListUnpaidInstallmentsInRangeAsync(userId, flowStart, flowEndExclusive);

        var recurrings = await _finance.ListRecurringAsync(userId);
        await _finance.HydrateRecurringCategoriesAsync(recurrings, userId);
        var recurringSchedules = await _finance.ListRecurringAmountSchedulesAsync(userId);
        var schedulesByRec = recurringSchedules.ToLookup(s => s.RecurringExpenseId);

        var monthExpenseList = expensesWide.Where(e => e.Date >= start && e.Date < end).ToList();
        var monthInstallments = installmentsWide.Where(i => i.DueDate >= start && i.DueDate < end).ToList();

        var monthExpenseFromTable = monthExpenseList.Sum(e => e.Amount);
        var monthExpenseFromInstallments = monthInstallments.Sum(i => i.Amount);
        var monthExpenseFromRecurringVirtual = SumActiveRecurringNotBooked(recurrings, monthExpenseList, start, schedulesByRec);
        var monthExpense = monthExpenseFromTable + monthExpenseFromInstallments + monthExpenseFromRecurringVirtual;

        var monthExpensesByCategory = BuildCategoryBreakdown(monthExpenseList, monthInstallments, recurrings, start, schedulesByRec);

        var patrimony = await _finance.SumAccountBalancesAsync(userId);
        var debtBalance = await _finance.SumDebtBalanceAsync(userId);

        var flow = new List<MonthlyFlowDto>();
        for (var i = -1; i <= 1; i++)
        {
            var m = start.AddMonths(i);
            var mEnd = m.AddMonths(1);
            var inc = await _finance.SumIncomeForMonthAsync(userId, m, mEnd);

            var monthEx = expensesWide.Where(e => e.Date >= m && e.Date < mEnd).ToList();
            var exp = monthEx.Sum(e => e.Amount)
                + installmentsWide.Where(x => x.DueDate >= m && x.DueDate < mEnd).Sum(x => x.Amount)
                + SumActiveRecurringNotBooked(recurrings, monthEx, m, schedulesByRec);

            flow.Add(new MonthlyFlowDto
            {
                Label = $"{m.Month:00}/{m.Year}",
                Income = inc,
                Expense = exp
            });
        }

        var summary = new DashboardSummaryDto
        {
            MonthIncome = monthIncome,
            MonthExpense = monthExpense,
            MonthBalance = monthIncome - monthExpense,
            TotalPatrimony = patrimony,
            TotalDebtBalance = debtBalance,
            LastMonthsFlow = flow,
            MonthExpensesByCategory = monthExpensesByCategory
        };

        await _snapshots.RefreshCurrentMonthAsync(userId);
        return summary;
    }

    private static string CategoryLabel(string? name) =>
        string.IsNullOrWhiteSpace(name) ? "Outros" : name;

    private static bool RecurringHasBookedExpense(RecurringExpense r, List<Expense> monthExpenses) =>
        monthExpenses.Exists(e =>
            e.RecurringExpenseId == r.Id
            || (e.RecurringExpenseId == null
                && e.CategoryId == r.CategoryId
                && e.Description == r.Description + " (recorrente)"));

    private static decimal SumActiveRecurringNotBooked(
        IReadOnlyList<RecurringExpense> recurrings,
        List<Expense> monthExpenses,
        DateTime monthStartUtc,
        ILookup<Guid, RecurringExpenseAmountSchedule> schedulesByRec)
    {
        var m = RecurringAmountResolver.NormalizeMonthStartUtc(monthStartUtc);
        decimal s = 0;
        foreach (var r in recurrings)
        {
            if (!r.Active)
                continue;
            if (RecurringHasBookedExpense(r, monthExpenses))
                continue;
            s += RecurringAmountResolver.EffectiveAmount(r, m, schedulesByRec[r.Id]);
        }

        return s;
    }

    private static List<MonthExpenseCategorySliceDto> BuildCategoryBreakdown(
        List<Expense> monthExpenses,
        List<Installment> monthInstallments,
        IReadOnlyList<RecurringExpense> recurrings,
        DateTime monthStartUtc,
        ILookup<Guid, RecurringExpenseAmountSchedule> schedulesByRec)
    {
        var buckets = new Dictionary<string, CategoryBreakdownBucket>(StringComparer.OrdinalIgnoreCase);

        void AddLine(string? rawCategoryName, MonthExpenseCategoryLineDto line)
        {
            var key = CategoryLabel(rawCategoryName);
            if (!buckets.TryGetValue(key, out var b))
            {
                b = new CategoryBreakdownBucket();
                buckets[key] = b;
            }

            b.Total += line.Amount;
            b.Items.Add(line);
        }

        foreach (var e in monthExpenses)
        {
            AddLine(e.Category?.Name, new MonthExpenseCategoryLineDto
            {
                Kind = "expense",
                Title = string.IsNullOrWhiteSpace(e.Description) ? "Despesa" : e.Description.Trim(),
                Amount = e.Amount,
                Date = e.Date
            });
        }

        foreach (var ins in monthInstallments)
        {
            var plan = ins.InstallmentPlan;
            var baseTitle = string.IsNullOrWhiteSpace(plan.Description) ? "Parcelamento" : plan.Description.Trim();
            AddLine(plan.Category?.Name, new MonthExpenseCategoryLineDto
            {
                Kind = "installment",
                Title = $"{baseTitle} — parcela {ins.SequenceNumber}/{plan.InstallmentCount}",
                Amount = ins.Amount,
                Date = ins.DueDate
            });
        }

        var m = RecurringAmountResolver.NormalizeMonthStartUtc(monthStartUtc);
        foreach (var r in recurrings)
        {
            if (!r.Active || RecurringHasBookedExpense(r, monthExpenses))
                continue;
            var title = string.IsNullOrWhiteSpace(r.Description) ? "Recorrente" : r.Description.Trim();
            var amt = RecurringAmountResolver.EffectiveAmount(r, m, schedulesByRec[r.Id]);
            AddLine(r.Category?.Name, new MonthExpenseCategoryLineDto
            {
                Kind = "recurring",
                Title = $"{title} (previsto no mês)",
                Amount = amt,
                Date = null
            });
        }

        return buckets
            .Select(kv => new MonthExpenseCategorySliceDto
            {
                CategoryName = kv.Key,
                Amount = kv.Value.Total,
                Items = kv.Value.Items
                    .OrderByDescending(x => x.Date ?? DateTime.MinValue)
                    .ThenBy(x => x.Kind)
                    .ToList()
            })
            .OrderByDescending(x => x.Amount)
            .ToList();
    }

    private sealed class CategoryBreakdownBucket
    {
        public decimal Total { get; set; }
        public List<MonthExpenseCategoryLineDto> Items { get; } = new();
    }
}
