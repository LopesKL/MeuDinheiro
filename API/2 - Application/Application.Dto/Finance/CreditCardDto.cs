namespace Application.Dto.Finance;

public class CreditCardDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int ClosingDay { get; set; }
    public int DueDay { get; set; }
    public bool IsMealVoucher { get; set; }
    public decimal? MealVoucherDailyAmount { get; set; }
    public int? MealVoucherCreditDay { get; set; }
    /// <summary>#RGB ou #RRGGBB</summary>
    public string? ThemeColor { get; set; }
}
