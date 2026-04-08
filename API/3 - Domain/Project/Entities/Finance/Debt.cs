namespace Project.Entities.Finance;

public class Debt
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public DateTime? DueDate { get; set; }
    public decimal? MonthlyPayment { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public decimal Balance => TotalAmount - PaidAmount;
}
