namespace Application.Dto.Finance;

public class InstallmentDto
{
    public Guid Id { get; set; }
    public Guid InstallmentPlanId { get; set; }
    public int SequenceNumber { get; set; }
    public DateTime DueDate { get; set; }
    public decimal Amount { get; set; }
    public bool IsPaid { get; set; }
    public Guid? ExpenseId { get; set; }
}
