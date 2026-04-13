using Application.Dto.Finance;
using Project.Entities.Finance;
using Repositories.Interfaces;

namespace Application.Finance;

public class ProjectionService
{
    private readonly IFinanceStore _finance;

    public ProjectionService(IFinanceStore finance)
    {
        _finance = finance;
    }

    public async Task<ProjectionResultDto> ProjectForUserAsync(string userId, int monthsAhead = 12)
    {
        monthsAhead = Math.Clamp(monthsAhead, 1, 60);
        var now = DateTime.UtcNow;
        var start = new DateTime(now.Year, now.Month, 1);

        var lastMonths = 6;
        var incomeSum = 0m;
        var incomeCount = 0;
        for (var i = 0; i < lastMonths; i++)
        {
            var m = start.AddMonths(-i);
            var mEnd = m.AddMonths(1);
            var inc = await _finance.SumIncomeForMonthAsync(userId, m, mEnd);
            if (inc > 0)
            {
                incomeSum += inc;
                incomeCount++;
            }
        }

        var avgIncome = incomeCount > 0 ? incomeSum / incomeCount : 0m;

        var recs = await _finance.ListRecurringAsync(userId);
        var schedules = await _finance.ListRecurringAmountSchedulesAsync(userId);
        var byRec = schedules.ToLookup(s => s.RecurringExpenseId);

        (decimal recurringFixed, decimal recurringVar) RecurringForMonth(DateTime monthFirst)
        {
            var m = RecurringAmountResolver.NormalizeMonthStartUtc(monthFirst);
            decimal rf = 0, rv = 0;
            foreach (var r in recs.Where(x => x.Active))
            {
                var a = RecurringAmountResolver.EffectiveAmount(r, m, byRec[r.Id]);
                if (r.Type == RecurringExpenseType.Fixed)
                    rf += a;
                else
                    rv += a;
            }

            return (rf, rv);
        }

        var installmentsDue = await _finance.ListUnpaidInstallmentsForUserPlansAsync(userId);

        var installmentMonthly = installmentsDue
            .GroupBy(i => new { i.DueDate.Year, i.DueDate.Month })
            .ToDictionary(g => $"{g.Key.Year}-{g.Key.Month:00}", g => g.Sum(x => x.Amount));

        var debts = await _finance.ListDebtsAsync(userId);
        var debtMonthly = debts.Sum(d => d.MonthlyPayment ?? 0m);

        var patrimony = await _finance.SumAccountBalancesAsync(userId);

        return BuildProjection(monthsAhead, start, avgIncome, RecurringForMonth,
            installmentMonthly, debtMonthly, patrimony);
    }

    public ProjectionResultDto ProjectSandbox(SandboxProjectionRequestDto req)
    {
        var monthsAhead = Math.Clamp(req.MonthsAhead, 1, 60);
        var now = DateTime.UtcNow;
        var start = new DateTime(now.Year, now.Month, 1);
        var installmentSim = new Dictionary<string, decimal>();
        if (req.OutstandingInstallmentsTotal > 0)
        {
            var perMonth = req.OutstandingInstallmentsTotal / monthsAhead;
            for (var i = 0; i < monthsAhead; i++)
            {
                var d = start.AddMonths(i);
                installmentSim[$"{d.Year}-{d.Month:00}"] = perMonth;
            }
        }

        (decimal, decimal) SandboxRecurring(DateTime _) =>
            (req.MonthlyFixedExpenses, req.MonthlyVariableExpensesAverage);

        return BuildProjection(monthsAhead, start, req.MonthlyIncomeAverage, SandboxRecurring,
            installmentSim, req.DebtMonthlyPayments, req.CurrentLiquidPatrimony);
    }

    private static ProjectionResultDto BuildProjection(
        int monthsAhead,
        DateTime startMonth,
        decimal avgIncome,
        Func<DateTime, (decimal recurringFixed, decimal recurringVariable)> recurringForMonth,
        IReadOnlyDictionary<string, decimal> installmentsByMonth,
        decimal debtMonthly,
        decimal startingPatrimony)
    {
        var months = new List<ProjectionMonthDto>();
        var balance = startingPatrimony;
        var cumInc = 0m;
        var cumExp = 0m;

        for (var i = 0; i < monthsAhead; i++)
        {
            var d = startMonth.AddMonths(i);
            var key = $"{d.Year}-{d.Month:00}";
            installmentsByMonth.TryGetValue(key, out var instMonth);
            var (recurringFixed, recurringVariableEstimate) = recurringForMonth(d);
            var expenseMonth = recurringFixed + recurringVariableEstimate / 2 + instMonth + debtMonthly;
            var incomeMonth = avgIncome;
            balance += incomeMonth - expenseMonth;
            cumInc += incomeMonth;
            cumExp += expenseMonth;
            months.Add(new ProjectionMonthDto
            {
                Label = $"{d.Month:00}/{d.Year}",
                ProjectedBalance = decimal.Round(balance, 2),
                CumulativeIncome = decimal.Round(cumInc, 2),
                CumulativeExpense = decimal.Round(cumExp, 2)
            });
        }

        return new ProjectionResultDto { Months = months };
    }
}
