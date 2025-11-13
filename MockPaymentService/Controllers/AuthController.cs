using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MockPaymentService.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace MockPaymentService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly AppDbContext _context;

    public AuthController(IConfiguration configuration, AppDbContext context)
    {
        _configuration = configuration;
        _context = context;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
        {
            return BadRequest(new { message = "User already exists" });
        }

        var user = new User
        {
            Email = request.Email
        };
        user.SetPassword(request.Password);

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok(new { message = "User registered successfully" });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null || !user.VerifyPassword(request.Password))
        {
            return Unauthorized(new { message = "Invalid credentials" });
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var tokens = GenerateTokens(user);
        return Ok(tokens);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.RefreshToken == request.RefreshToken);
        if (user == null || user.RefreshTokenExpiry < DateTime.UtcNow)
        {
            return Unauthorized(new { message = "Invalid refresh token" });
        }

        var tokens = GenerateTokens(user);
        return Ok(tokens);
    }

    private TokenResponse GenerateTokens(User user)
    {
        var accessToken = GenerateAccessToken(user);
        var refreshToken = GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(_configuration.GetValue<int>("Jwt:RefreshTokenExpiryDays", 7));
        _context.SaveChanges();

        return new TokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken
        };
    }

    private string GenerateAccessToken(User user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? "default-secret-key"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_configuration.GetValue<int>("Jwt:AccessTokenExpiryMinutes", 15)),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}

public class RegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RefreshRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class TokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}