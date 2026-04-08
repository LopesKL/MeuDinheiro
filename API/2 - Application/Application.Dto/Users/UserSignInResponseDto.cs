namespace Application.Dto.Users;

public class UserSignInResponseDto
{
    public string Token { get; set; } = string.Empty;
    public UserDto User { get; set; } = null!;
    public List<string> Roles { get; set; } = new();
}
