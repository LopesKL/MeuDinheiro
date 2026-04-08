namespace Application.Dto.Finance;

public class ExpenseDto
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public Guid CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string Description { get; set; } = string.Empty;
    public int PaymentMethod { get; set; }
    public string? StoreLocation { get; set; }
    public Guid? CreditCardId { get; set; }
    public Guid? InstallmentPlanId { get; set; }
    public string? ImagePath { get; set; }
    /// <summary>0 = não informado, 1 = lançamento rápido, 2 = upload de comprovante.</summary>
    public int CreationSource { get; set; }
}
