namespace Application.Dto.Finance;

public class RecurringExpenseDto
{
    public Guid Id { get; set; }
    public int Type { get; set; }
    public decimal Amount { get; set; }
    public Guid CategoryId { get; set; }
    public string Description { get; set; } = string.Empty;
    public int PaymentMethod { get; set; }
    public int DayOfMonth { get; set; }
    public bool Active { get; set; }
    public Guid? CreditCardId { get; set; }
}
