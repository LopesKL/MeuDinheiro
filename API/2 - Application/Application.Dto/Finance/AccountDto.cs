namespace Application.Dto.Finance;

public class AccountDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Type { get; set; }
    public decimal Balance { get; set; }
    public string Currency { get; set; } = "BRL";
}
