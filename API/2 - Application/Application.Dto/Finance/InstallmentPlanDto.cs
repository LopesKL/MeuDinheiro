namespace Application.Dto.Finance;

public class InstallmentPlanDto
{
    public Guid Id { get; set; }
    public Guid? CreditCardId { get; set; }
    public Guid CategoryId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int InstallmentCount { get; set; }
    public DateTime StartDate { get; set; }
    public List<InstallmentDto> Installments { get; set; } = new();
}
