using Project.Entities.Finance;

namespace Application.Finance;

public static class RecurringAmountResolver
{
    public static DateTime NormalizeMonthStartUtc(DateTime any)
    {
        var d = any.Kind == DateTimeKind.Utc ? any : any.ToUniversalTime();
        return new DateTime(d.Year, d.Month, 1, 0, 0, 0, DateTimeKind.Utc);
    }

    /// <summary>Valor do recorrente no mês de <paramref name="monthFirstDayUtc"/> (1º dia UTC).</summary>
    public static decimal EffectiveAmount(
        RecurringExpense r,
        DateTime monthFirstDayUtc,
        IEnumerable<RecurringExpenseAmountSchedule> schedulesForRecurring)
    {
        var m = NormalizeMonthStartUtc(monthFirstDayUtc);
        var hit = schedulesForRecurring
            .Where(s => NormalizeMonthStartUtc(s.EffectiveFrom) <= m)
            .OrderByDescending(s => NormalizeMonthStartUtc(s.EffectiveFrom))
            .FirstOrDefault();
        return hit?.Amount ?? r.Amount;
    }
}
