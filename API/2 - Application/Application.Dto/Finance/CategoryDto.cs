namespace Application.Dto.Finance;

public class CategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsExpense { get; set; }
}
