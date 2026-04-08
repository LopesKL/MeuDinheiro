namespace Application.Dto.ResponsePatterns;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();

    public static ApiResponse<T> SuccessResult(T data, string message = "Success")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message,
            Errors = new List<string>()
        };
    }

    public static ApiResponse<T> ErrorResult(string message, List<string> errors)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Data = default,
            Message = message,
            Errors = errors
        };
    }
}
