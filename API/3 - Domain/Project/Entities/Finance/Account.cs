namespace Project.Entities.Finance;

public class Account
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public AccountType Type { get; set; }
    public decimal Balance { get; set; }
    public string Currency { get; set; } = "BRL";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
