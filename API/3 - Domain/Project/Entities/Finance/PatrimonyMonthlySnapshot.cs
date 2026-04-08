namespace Project.Entities.Finance;

/// <summary>Total de patrimônio consolidado no fim do mês (atualizado ao alterar contas).</summary>
public class PatrimonyMonthlySnapshot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal TotalBalance { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
