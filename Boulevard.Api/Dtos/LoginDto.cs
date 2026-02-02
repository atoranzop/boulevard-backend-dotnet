public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public string Access { get; set; } = string.Empty;
    public UserResponse User {get; set; } = null!;
}