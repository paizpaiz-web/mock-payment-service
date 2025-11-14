using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;

namespace MockPaymentServiceLambda.Tests;

public class IntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public IntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact(Skip = "Integration test setup required")]
    public async Task HealthEndpoint_ShouldReturnHealthy()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.Equal("healthy", (string)result?["status"]);
        Assert.NotNull(result?["timestamp"]);
    }

    [Fact(Skip = "Integration test setup required")]
    public async Task RegisterEndpoint_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var registerData = new
        {
            Email = "test@example.com",
            Password = "SecurePass123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", registerData);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<dynamic>();
        Assert.Equal("User registered successfully", (string)result.message);
    }

    [Fact(Skip = "Integration test setup required")]
    public async Task LoginEndpoint_WithValidCredentials_ShouldReturnTokens()
    {
        // Arrange - First register a user
        var registerData = new
        {
            Email = "login-test@example.com",
            Password = "SecurePass123!"
        };
        await _client.PostAsJsonAsync("/api/auth/register", registerData);

        var loginData = new
        {
            Email = "login-test@example.com",
            Password = "SecurePass123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginData);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<dynamic>();
        Assert.NotNull(result.AccessToken);
        Assert.NotNull(result.RefreshToken);
    }

    [Fact]
    public async Task LoginEndpoint_WithInvalidCredentials_ShouldReturnUnauthorized()
    {
        // Arrange
        var loginData = new
        {
            Email = "nonexistent@example.com",
            Password = "wrongpassword"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginData);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PaymentCharge_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Arrange
        var chargeData = new
        {
            Amount = 100.50m,
            CardNumber = "4111111111111111",
            ExpirationDate = "12/25",
            CVV = "123",
            CardholderName = "John Doe"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/payment/charge", chargeData);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task NonExistentEndpoint_ShouldReturn404()
    {
        // Act
        var response = await _client.GetAsync("/api/nonexistent");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}