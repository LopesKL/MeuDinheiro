namespace Application.Dto.Finance;

public class IncomeDto
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public string Source { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime ReferenceMonth { get; set; }
    public Guid? BatchId { get; set; }
}
