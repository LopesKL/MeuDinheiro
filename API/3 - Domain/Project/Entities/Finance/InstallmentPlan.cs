namespace Project.Entities.Finance;

public class InstallmentPlan
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public Guid? CreditCardId { get; set; }
    public Guid CategoryId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int InstallmentCount { get; set; }
    public DateTime StartDate { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public CreditCard? CreditCard { get; set; }
    public Category Category { get; set; } = null!;
    public ICollection<Installment> Installments { get; set; } = new List<Installment>();
}
