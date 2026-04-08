namespace Application.Dto.Finance;

public class CreditCardDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int ClosingDay { get; set; }
    public int DueDay { get; set; }
}
