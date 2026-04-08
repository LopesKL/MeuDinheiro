namespace Application.Dto.Finance;

public class CreditCardInvoiceDto
{
    public Guid CreditCardId { get; set; }
    public string CreditCardName { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal Total { get; set; }
    public List<ExpenseDto> Expenses { get; set; } = new();
}
