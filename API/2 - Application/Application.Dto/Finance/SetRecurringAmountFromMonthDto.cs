namespace Application.Dto.Finance;

public class SetRecurringAmountFromMonthDto
{
    /// <summary>Qualquer data dentro do mês inicial; será normalizado para o 1º dia (UTC).</summary>
    public DateTime EffectiveFrom { get; set; }
    public decimal Amount { get; set; }
}
