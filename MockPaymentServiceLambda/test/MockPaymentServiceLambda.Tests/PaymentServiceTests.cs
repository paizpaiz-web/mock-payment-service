using Microsoft.Extensions.Logging;
using MockPaymentServiceLambda.Services;
using Moq;

namespace MockPaymentServiceLambda.Tests;

public class PaymentServiceTests
{
    private readonly PaymentService _paymentService;
    private readonly Mock<ILogger<PaymentService>> _loggerMock;

    public PaymentServiceTests()
    {
        _loggerMock = new Mock<ILogger<PaymentService>>();
        _paymentService = new PaymentService(_loggerMock.Object);
    }

    [Fact]
    public async Task ChargeAsync_WithValidAmount_ShouldSucceed()
    {
        // Arrange
        var amount = 100.50m;
        var cardNumber = "4111111111111111";
        var expirationDate = "12/25";
        var cvv = "123";
        var cardholderName = "John Doe";

        // Act
        var result = await _paymentService.ChargeAsync(amount, cardNumber, expirationDate, cvv, cardholderName);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        var response = result.Data as dynamic;
        Assert.Equal(amount, response.Amount);
        Assert.Equal("success", response.Status);
        Assert.NotNull(response.TransactionId);
        Assert.Contains("Payment processed successfully", response.Message);
    }

    [Fact]
    public async Task ChargeAsync_ShouldOccasionallyFail()
    {
        // Arrange - This test might be flaky due to random failure rate
        var amount = 100.00m;
        var cardNumber = "4111111111111111";
        var expirationDate = "12/25";
        var cvv = "123";
        var cardholderName = "John Doe";

        // Act - Run multiple times to increase chance of failure
        var results = new List<bool>();
        for (int i = 0; i < 100; i++)
        {
            var result = await _paymentService.ChargeAsync(amount, cardNumber, expirationDate, cvv, cardholderName);
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
        var transactionId = Guid.NewGuid().ToString();
        var amount = 50.00m;
        var reason = "Customer request";

        // Act
        var result = await _paymentService.RefundAsync(transactionId, amount, reason);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        // Use reflection to check properties since anonymous objects are returned
        var response = result.Data;
        Assert.NotNull(response);
        var amountProp = response.GetType().GetProperty("Amount");
        var statusProp = response.GetType().GetProperty("Status");
        var refundIdProp = response.GetType().GetProperty("RefundId");
        var originalTransactionIdProp = response.GetType().GetProperty("OriginalTransactionId");
        var messageProp = response.GetType().GetProperty("Message");

        Assert.Equal(amount, amountProp?.GetValue(response));
        Assert.Equal("success", statusProp?.GetValue(response));
        Assert.NotNull(refundIdProp?.GetValue(response));
        Assert.Equal(transactionId, originalTransactionIdProp?.GetValue(response));
        Assert.Contains("Refund processed successfully", messageProp?.GetValue(response) as string);
    }

    [Fact]
    public async Task RefundAsync_ShouldOccasionallyFail()
    {
        // Arrange
        var transactionId = Guid.NewGuid().ToString();
        var amount = 75.00m;
        var reason = "Invalid transaction";

        // Act - Run multiple times to increase chance of failure
        var results = new List<bool>();
        for (int i = 0; i < 100; i++)
        {
            var result = await _paymentService.RefundAsync(transactionId, amount, reason);
            results.Add(result.Success);
        }

        // Assert - Should have some failures (90% success rate)
        Assert.Contains(false, results);
        Assert.Contains(true, results);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-50)]
    public async Task ChargeAsync_WithInvalidAmount_ShouldStillProcess(decimal invalidAmount)
    {
        // Arrange
        var cardNumber = "4111111111111111";
        var expirationDate = "12/25";
        var cvv = "123";
        var cardholderName = "John Doe";

        // Act
        var result = await _paymentService.ChargeAsync(invalidAmount, cardNumber, expirationDate, cvv, cardholderName);

        // Assert - Current implementation doesn't validate amount, so it succeeds
        // This test documents the current behavior
        Assert.True(result.Success);
    }

    [Fact]
    public async Task ChargeAsync_WithEmptyCardNumber_ShouldStillProcess()
    {
        // Arrange
        var amount = 100.00m;
        var cardNumber = "";
        var expirationDate = "12/25";
        var cvv = "123";
        var cardholderName = "John Doe";

        // Act
        var result = await _paymentService.ChargeAsync(amount, cardNumber, expirationDate, cvv, cardholderName);

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