namespace Application.Dto.Finance;

public class RecurringExpenseDto
{
    public Guid Id { get; set; }
    public int Type { get; set; }
    /// <summary>Valor padrão quando não há ajuste por mês.</summary>
    public decimal Amount { get; set; }
    /// <summary>Valor aplicado no mês corrente (UTC), após vigências.</summary>
    public decimal EffectiveAmount { get; set; }
    public List<RecurringAmountScheduleDto> AmountSchedules { get; set; } = new();
    public Guid CategoryId { get; set; }
    public string Description { get; set; } = string.Empty;
    public int PaymentMethod { get; set; }
    public int DayOfMonth { get; set; }
    public bool Active { get; set; }
    public Guid? CreditCardId { get; set; }
}
