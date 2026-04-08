namespace Application.Dto.Finance;

public class ParseExpenseRequestDto
{
    public string Text { get; set; } = string.Empty;
}

public class ParseExpenseResultDto
{
    public decimal? Amount { get; set; }
    public string? Description { get; set; }
    public int? PaymentMethod { get; set; }
    public Guid? SuggestedCategoryId { get; set; }
    public string? SuggestedCategoryName { get; set; }
}
