namespace Application.Dto.Finance;

public class CreditCardInvoiceDto
{
    public Guid CreditCardId { get; set; }
    public string CreditCardName { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal Total { get; set; }
    public List<ExpenseDto> Expenses { get; set; } = new();

    public bool IsMealVoucher { get; set; }
    public decimal? MealVoucherDailyAmount { get; set; }
    public int? MealVoucherCreditDay { get; set; }
    /// <summary>Segunda a sexta no mês de referência.</summary>
    public int BusinessDaysInMonth { get; set; }
    /// <summary><see cref="MealVoucherDailyAmount"/> × <see cref="BusinessDaysInMonth"/>.</summary>
    public decimal ExpectedMonthlyCredit { get; set; }
}
