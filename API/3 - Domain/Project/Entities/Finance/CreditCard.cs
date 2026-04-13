namespace Project.Entities.Finance;

public class CreditCard
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int ClosingDay { get; set; } = 1;
    public int DueDay { get; set; } = 10;
    /// <summary>Vale alimentação: sem ciclo de fatura; usa valor diário × dias úteis do mês.</summary>
    public bool IsMealVoucher { get; set; }
    /// <summary>Valor creditado por dia útil (somente vale alimentação).</summary>
    public decimal? MealVoucherDailyAmount { get; set; }
    /// <summary>Dia do mês em que o benefício é creditado (1–31).</summary>
    public int? MealVoucherCreditDay { get; set; }
    /// <summary>Cor de identificação no formato #RGB ou #RRGGBB (opcional).</summary>
    public string? ThemeColor { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
