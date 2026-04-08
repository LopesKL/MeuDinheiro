namespace Application.Dto.Finance;

public class IncomeMonthlyHistoryDto
{
    public string Label { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal Total { get; set; }
    public List<IncomeDto> Items { get; set; } = new();
}
