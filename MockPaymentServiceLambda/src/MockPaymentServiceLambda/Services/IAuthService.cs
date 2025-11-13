namespace MockPaymentServiceLambda.Services;

public interface IAuthService
{
    Task<(bool Success, string Message, object? Data)> RegisterAsync(string email, string password);
    Task<(bool Success, string Message, object? Data)> LoginAsync(string email, string password);
    Task<(bool Success, string Message, object? Data)> RefreshTokenAsync(string refreshToken);
}