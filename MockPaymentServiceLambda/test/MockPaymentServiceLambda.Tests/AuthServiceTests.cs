using Microsoft.EntityFrameworkCore;
using MockPaymentServiceLambda.Models;
using MockPaymentServiceLambda.Services;
using Moq;

namespace MockPaymentServiceLambda.Tests;

public class AuthServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Mock<IJwtService> _jwtServiceMock;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        // Create in-memory database
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _jwtServiceMock = new Mock<IJwtService>();

        // Setup JWT service mocks
        _jwtServiceMock.Setup(x => x.GenerateAccessToken(It.IsAny<int>(), It.IsAny<string>()))
            .Returns("mock-access-token");
        _jwtServiceMock.Setup(x => x.GenerateRefreshToken())
            .Returns("mock-refresh-token");

        _authService = new AuthService(_context, _jwtServiceMock.Object);
    }

    [Fact]
    public async Task RegisterAsync_WithValidData_ShouldCreateUser()
    {
        // Arrange
        var email = "test@example.com";
        var password = "SecurePass123!";

        // Act
        var result = await _authService.RegisterAsync(email, password);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("User registered successfully", result.Message);
        Assert.Single(_context.Users);
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ShouldFail()
    {
        // Arrange
        var email = "existing@example.com";
        var user = new User { Email = email };
        user.SetPassword("password");
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _authService.RegisterAsync(email, "newpassword");

        // Assert
        Assert.False(result.Success);
        Assert.Equal("User already exists", result.Message);
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ShouldReturnTokens()
    {
        // Arrange
        var email = "user@example.com";
        var password = "SecurePass123!";
        var user = new User { Email = email };
        user.SetPassword(password);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _authService.LoginAsync(email, password);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Login successful", result.Message);
        Assert.NotNull(result.Data);

        // Verify JWT service was called
        _jwtServiceMock.Verify(x => x.GenerateAccessToken(It.IsAny<int>(), email), Times.Once);
        _jwtServiceMock.Verify(x => x.GenerateRefreshToken(), Times.Once);

        // Verify user was updated
        var updatedUser = await _context.Users.FirstAsync();
        Assert.NotNull(updatedUser.LastLoginAt);
        Assert.Equal("mock-refresh-token", updatedUser.RefreshToken);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidEmail_ShouldFail()
    {
        // Act
        var result = await _authService.LoginAsync("nonexistent@example.com", "password");

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Invalid credentials", result.Message);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ShouldFail()
    {
        // Arrange
        var email = "user@example.com";
        var user = new User { Email = email };
        user.SetPassword("correctpassword");
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _authService.LoginAsync(email, "wrongpassword");

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Invalid credentials", result.Message);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithValidToken_ShouldReturnNewTokens()
    {
        // Arrange
        var refreshToken = "valid-refresh-token";
        var user = new User
        {
            Email = "user@example.com",
            RefreshToken = refreshToken,
            RefreshTokenExpiry = DateTime.UtcNow.AddDays(1)
        };
        user.SetPassword("password");
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _authService.RefreshTokenAsync(refreshToken);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Token refreshed successfully", result.Message);
        Assert.NotNull(result.Data);

        // Verify JWT service was called
        _jwtServiceMock.Verify(x => x.GenerateAccessToken(It.IsAny<int>(), user.Email), Times.Once);
        _jwtServiceMock.Verify(x => x.GenerateRefreshToken(), Times.Once);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithExpiredToken_ShouldFail()
    {
        // Arrange
        var refreshToken = "expired-refresh-token";
        var user = new User
        {
            Email = "user@example.com",
            RefreshToken = refreshToken,
            RefreshTokenExpiry = DateTime.UtcNow.AddDays(-1) // Expired
        };
        user.SetPassword("password");
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _authService.RefreshTokenAsync(refreshToken);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Invalid refresh token", result.Message);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithInvalidToken_ShouldFail()
    {
        // Act
        var result = await _authService.RefreshTokenAsync("invalid-token");

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Invalid refresh token", result.Message);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}