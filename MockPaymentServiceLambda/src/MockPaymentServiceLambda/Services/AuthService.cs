using MockPaymentServiceLambda.Models;

namespace MockPaymentServiceLambda.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly IJwtService _jwtService;

    public AuthService(AppDbContext context, IJwtService jwtService)
    {
        _context = context;
        _jwtService = jwtService;
    }

    public async Task<(bool Success, string Message, object? Data)> RegisterAsync(string email, string password)
    {
        try
        {
            if (await _context.Users.AnyAsync(u => u.Email == email))
            {
                return (false, "User already exists", null);
            }

            var user = new User { Email = email };
            user.SetPassword(password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return (true, "User registered successfully", null);
        }
        catch (Exception ex)
        {
            return (false, $"Registration failed: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, object? Data)> LoginAsync(string email, string password)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null || !user.VerifyPassword(password))
            {
                return (false, "Invalid credentials", null);
            }

            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var accessToken = _jwtService.GenerateAccessToken(user.Id, user.Email);
            var refreshToken = _jwtService.GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7); // 7 days
            await _context.SaveChangesAsync();

            var tokens = new
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };

            return (true, "Login successful", tokens);
        }
        catch (Exception ex)
        {
            return (false, $"Login failed: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, object? Data)> RefreshTokenAsync(string refreshToken)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);
            if (user == null || user.RefreshTokenExpiry < DateTime.UtcNow)
            {
                return (false, "Invalid refresh token", null);
            }

            var accessToken = _jwtService.GenerateAccessToken(user.Id, user.Email);
            var newRefreshToken = _jwtService.GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
            await _context.SaveChangesAsync();

            var tokens = new
            {
                AccessToken = accessToken,
                RefreshToken = newRefreshToken
            };

            return (true, "Token refreshed successfully", tokens);
        }
        catch (Exception ex)
        {
            return (false, $"Token refresh failed: {ex.Message}", null);
        }
    }
}