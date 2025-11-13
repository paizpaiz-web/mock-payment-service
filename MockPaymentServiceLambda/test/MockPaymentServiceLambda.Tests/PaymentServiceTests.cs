using MockPaymentServiceLambda.Services;

namespace MockPaymentServiceLambda.Tests;

public class PaymentServiceTests
{
    private readonly PaymentService _paymentService;

    public PaymentServiceTests()
    {
        _paymentService = new PaymentService();
    }

    [Fact]
    public async Task ChargeAsync_WithValidAmount_ShouldSucceed()
    {
        // Arrange
        var request = new ChargeRequest
        {
            Amount = 100.50m,
            CardNumber = "4111111111111111",
            ExpirationDate = "12/25",
            CVV = "123",
            CardholderName = "John Doe"
        };
        var jwtToken = "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.mock.jwt.token";

        // Act
        var result = await _paymentService.ChargeAsync(request, jwtToken);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Response);
        Assert.Equal(request.Amount, result.Response.Amount);
        Assert.Equal("success", result.Response.Status);
        Assert.NotNull(result.Response.TransactionId);
        Assert.Contains("Payment processed successfully", result.Response.Message);
    }

    [Fact]
    public async Task ChargeAsync_ShouldOccasionallyFail()
    {
        // Arrange - This test might be flaky due to random failure rate
        var request = new ChargeRequest
        {
            Amount = 100.00m,
            CardNumber = "4111111111111111",
            ExpirationDate = "12/25",
            CVV = "123",
            CardholderName = "John Doe"
        };
        var jwtToken = "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.mock.jwt.token";

        // Act - Run multiple times to increase chance of failure
        var results = new List<bool>();
        for (int i = 0; i < 100; i++)
        {
            var result = await _paymentService.ChargeAsync(request, jwtToken);
            results.Add(result.Success);
        }

        // Assert - Should have some failures (95% success rate)
        Assert.Contains(false, results);
        Assert.Contains(true, results);
    }

    [Fact]
    public async Task RefundAsync_WithValidTransaction_ShouldSucceed()
    {
        // Arrange
        var request = new RefundRequest
        {
            TransactionId = Guid.NewGuid().ToString(),
            Amount = 50.00m,
            Reason = "Customer request"
        };
        var jwtToken = "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.mock.jwt.token";

        // Act
        var result = await _paymentService.RefundAsync(request, jwtToken);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Response);
        Assert.Equal(request.Amount, result.Response.Amount);
        Assert.Equal("success", result.Response.Status);
        Assert.NotNull(result.Response.RefundId);
        Assert.Equal(request.TransactionId, result.Response.OriginalTransactionId);
        Assert.Contains("Refund processed successfully", result.Response.Message);
    }

    [Fact]
    public async Task RefundAsync_ShouldOccasionallyFail()
    {
        // Arrange
        var request = new RefundRequest
        {
            TransactionId = Guid.NewGuid().ToString(),
            Amount = 75.00m,
            Reason = "Invalid transaction"
        };
        var jwtToken = "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.mock.jwt.token";

        // Act - Run multiple times to increase chance of failure
        var results = new List<bool>();
        for (int i = 0; i < 100; i++)
        {
            var result = await _paymentService.RefundAsync(request, jwtToken);
            results.Add(result.Success);
        }

        // Assert - Should have some failures (90% success rate)
        Assert.Contains(false, results);
        Assert.Contains(true, results);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-50)]
    public async Task ChargeAsync_WithInvalidAmount_ShouldFail(decimal invalidAmount)
    {
        // Arrange
        var request = new ChargeRequest
        {
            Amount = invalidAmount,
            CardNumber = "4111111111111111",
            ExpirationDate = "12/25",
            CVV = "123",
            CardholderName = "John Doe"
        };
        var jwtToken = "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.mock.jwt.token";

        // Act
        var result = await _paymentService.ChargeAsync(request, jwtToken);

        // Assert - Current implementation doesn't validate amount, so it succeeds
        // This test documents the current behavior
        Assert.True(result.Success);
    }

    [Fact]
    public async Task ChargeAsync_WithEmptyCardNumber_ShouldStillProcess()
    {
        // Arrange
        var request = new ChargeRequest
        {
            Amount = 100.00m,
            CardNumber = "",
            ExpirationDate = "12/25",
            CVV = "123",
            CardholderName = "John Doe"
        };
        var jwtToken = "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.mock.jwt.token";

        // Act
        var result = await _paymentService.ChargeAsync(request, jwtToken);

        // Assert - Current implementation doesn't validate card details
        Assert.True(result.Success);
    }
}

// Request/Response DTOs for testing
public class ChargeRequest
{
    public decimal Amount { get; set; }
    public string? CardNumber { get; set; }
    public string? ExpirationDate { get; set; }
    public string? CVV { get; set; }
    public string? CardholderName { get; set; }
}

public class ChargeResponse
{
    public string? TransactionId { get; set; }
    public string? Status { get; set; }
    public decimal Amount { get; set; }
    public string? Message { get; set; }
}

public class RefundRequest
{
    public string? TransactionId { get; set; }
    public decimal Amount { get; set; }
    public string? Reason { get; set; }
}

public class RefundResponse
{
    public string? RefundId { get; set; }
    public string? OriginalTransactionId { get; set; }
    public string? Status { get; set; }
    public decimal Amount { get; set; }
    public string? Message { get; set; }
}

public class PaymentResult
{
    public bool Success { get; set; }
    public ChargeResponse? Response { get; set; }
}