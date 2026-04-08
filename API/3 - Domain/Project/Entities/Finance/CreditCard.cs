namespace Project.Entities.Finance;

public class CreditCard
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int ClosingDay { get; set; } = 1;
    public int DueDay { get; set; } = 10;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
