namespace MockPaymentServiceLambda.Services;

public interface IJwtService
{
    string GenerateAccessToken(int userId, string email);
    string GenerateRefreshToken();
    bool ValidateRefreshToken(string token);
}