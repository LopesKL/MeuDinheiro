namespace Application.Dto.Finance;

public class DebtDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal Balance { get; set; }
    public DateTime? DueDate { get; set; }
    public decimal? MonthlyPayment { get; set; }
}
