namespace Application.Dto.Finance;

public class RecurringAmountScheduleDto
{
    public Guid Id { get; set; }
    public Guid RecurringExpenseId { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public decimal Amount { get; set; }
}
