namespace Project.Entities.Finance;

public class Expense
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public Guid CategoryId { get; set; }
    public string Description { get; set; } = string.Empty;
    public PaymentMethod PaymentMethod { get; set; }
    public string? StoreLocation { get; set; }
    public Guid? CreditCardId { get; set; }
    public Guid? InstallmentPlanId { get; set; }
    /// <summary>Preenchido quando a despesa foi gerada automaticamente a partir de um recorrente.</summary>
    public Guid? RecurringExpenseId { get; set; }
    public string? ImagePath { get; set; }
    public ExpenseCreationSource CreationSource { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Category Category { get; set; } = null!;
    public CreditCard? CreditCard { get; set; }
    public InstallmentPlan? InstallmentPlan { get; set; }
}
