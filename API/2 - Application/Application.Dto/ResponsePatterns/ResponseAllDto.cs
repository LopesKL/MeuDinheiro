namespace Application.Dto.ResponsePatterns;

public class ResponseAllDto<T>
{
    public bool Success { get; set; }
    public List<T> Data { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public string Message { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();
}
