namespace Project.Entities.Finance;

public class RecurringExpense
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public RecurringExpenseType Type { get; set; }
    public decimal Amount { get; set; }
    public Guid CategoryId { get; set; }
    public string Description { get; set; } = string.Empty;
    public PaymentMethod PaymentMethod { get; set; }
    public int DayOfMonth { get; set; } = 1;
    public bool Active { get; set; } = true;
    public Guid? CreditCardId { get; set; }
    public DateTime? LastGeneratedMonth { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Category Category { get; set; } = null!;
    public CreditCard? CreditCard { get; set; }
}
