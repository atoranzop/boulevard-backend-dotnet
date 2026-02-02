using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthController(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterUserRequest request)
    {
        if(await _context.Users.AnyAsync(u => u.Email == request.Email))
            return BadRequest("El correo ya está registrado");
        
        if(await _context.Users.AnyAsync(u => u.Email == request.Email))
            return BadRequest("Nombre de usuario ya en uso");
        
        string salt = BCrypt.Net.BCrypt.GenerateSalt(12);
        string hashedPassword =  BCrypt.Net.BCrypt.HashPassword(request.Password, salt);

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = hashedPassword
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok(new UserResponse
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

        if(user == null)
            return Unauthorized("Credenciales inválidas");
        
        bool IsValidPassword = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);

        if(!IsValidPassword)
            return Unauthorized("Credenciales inválidas");
        
        var token = new TokenService(_configuration).GenerateToken(user);

        return Ok(new LoginResponse
        {
            Access = token,
            User = new UserResponse
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email
            }
        });
    }
}