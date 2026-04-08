namespace Project.Entities.Finance;

public class Installment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid InstallmentPlanId { get; set; }
    public int SequenceNumber { get; set; }
    public DateTime DueDate { get; set; }
    public decimal Amount { get; set; }
    public bool IsPaid { get; set; }
    public Guid? ExpenseId { get; set; }

    public InstallmentPlan InstallmentPlan { get; set; } = null!;
    public Expense? Expense { get; set; }
}
