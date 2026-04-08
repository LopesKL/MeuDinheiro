namespace Project.Entities.Finance;

/// <summary>
/// Renda do usuário; ReferenceMonth indica o mês de competência (primeiro dia do mês).
/// </summary>
public class Income
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Source { get; set; } = string.Empty;
    public string? Description { get; set; }
    /// <summary>Primeiro dia do mês de referência (UTC).</summary>
    public DateTime ReferenceMonth { get; set; }
    /// <summary>Quando preenchido, identifica rendas criadas no mesmo lançamento em lote (ex.: intervalo mensal).</summary>
    public Guid? BatchId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
