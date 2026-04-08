namespace Application.Dto.RequestPatterns;

public class RequestAllDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Search { get; set; }
    public string? OrderBy { get; set; }
    public bool OrderDesc { get; set; } = false;
}
