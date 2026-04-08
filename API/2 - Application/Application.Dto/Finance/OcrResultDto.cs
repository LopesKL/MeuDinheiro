namespace Application.Dto.Finance;

public class OcrResultDto
{
    public decimal? DetectedAmount { get; set; }
    public string? MerchantName { get; set; }
    public string RawHint { get; set; } = string.Empty;
}
