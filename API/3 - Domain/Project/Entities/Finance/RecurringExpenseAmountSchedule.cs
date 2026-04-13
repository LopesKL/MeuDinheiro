namespace Project.Entities.Finance;

/// <summary>
/// Valor efetivo do gasto recorrente a partir de um mês (UTC). Meses sem entrada usam <see cref="RecurringExpense.Amount"/>.</summary>
public class RecurringExpenseAmountSchedule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public Guid RecurringExpenseId { get; set; }
    /// <summary>Primeiro dia do mês em que o valor passa a valer (UTC).</summary>
    public DateTime EffectiveFrom { get; set; }
    public decimal Amount { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
