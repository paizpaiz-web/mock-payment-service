using MockPaymentServiceLambda.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace MockPaymentServiceLambda.Tests;

public class JwtServiceTests
{
    private readonly JwtService _jwtService;

    public JwtServiceTests()
    {
        // Use test configuration
        _jwtService = new JwtService();
    }

    [Fact]
    public void GenerateAccessToken_ShouldCreateValidToken()
    {
        // Arrange
        var userId = 123;
        var email = "test@example.com";

        // Act
        var token = _jwtService.GenerateAccessToken(userId, email);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);

        // Verify token structure
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        Assert.Equal("MockPaymentServiceLambda", jwtToken.Issuer);
        Assert.Equal("MockPaymentServiceLambda", jwtToken.Audiences.First());
        Assert.Contains(jwtToken.Claims, c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == userId.ToString());
        Assert.Contains(jwtToken.Claims, c => c.Type == JwtRegisteredClaimNames.Email && c.Value == email);
    }

    [Fact]
    public void GenerateRefreshToken_ShouldCreateRandomToken()
    {
        // Act
        var token1 = _jwtService.GenerateRefreshToken();
        var token2 = _jwtService.GenerateRefreshToken();

        // Assert
        Assert.NotNull(token1);
        Assert.NotNull(token2);
        Assert.NotEmpty(token1);
        Assert.NotEmpty(token2);
        Assert.NotEqual(token1, token2); // Should be different

        // Should be base64 encoded (typical for refresh tokens)
        Assert.True(IsBase64String(token1));
        Assert.True(IsBase64String(token2));
    }

    [Fact]
    public void GenerateAccessToken_ShouldHaveExpiration()
    {
        // Arrange
        var userId = 456;
        var email = "expire@example.com";

        // Act
        var token = _jwtService.GenerateAccessToken(userId, email);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        Assert.NotNull(jwtToken.ValidTo);
        var expectedExpiry = DateTime.UtcNow.AddMinutes(15); // Default 15 minutes
        var timeDifference = Math.Abs((jwtToken.ValidTo - expectedExpiry).TotalSeconds);
        Assert.True(timeDifference < 10); // Within 10 seconds tolerance
    }

    [Fact]
    public void GenerateRefreshToken_ShouldBeProperLength()
    {
        // Act
        var token = _jwtService.GenerateRefreshToken();

        // Assert
        // Base64 encoded 64 bytes should be around 86 characters (64 * 4/3)
        Assert.True(token.Length >= 80); // Reasonable length for security
    }

    private static bool IsBase64String(string base64)
    {
        try
        {
            Convert.FromBase64String(base64);
            return true;
        }
        catch
        {
            return false;
        }
    }
}