namespace Application.Dto.Finance;

public class PatrimonyHistoryPointDto
{
    public string Label { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal TotalBalance { get; set; }
}
