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
        var response = (ChargeResponse)result.Data;
        Assert.Equal(amount, response.Amount);
        Assert.Equal("success", response.Status);
        Assert.NotNull(response.TransactionId);
        Assert.Contains("Payment processed successfully", response.Message);
    }

    [Fact]
    public async Task ChargeAsync_ShouldAlwaysSucceed()
    {
        // Arrange
        var amount = 100.00m;
        var cardNumber = "4111111111111111";
        var expirationDate = "12/25";
        var cvv = "123";
        var cardholderName = "John Doe";

        // Act - Run multiple times
        var results = new List<bool>();
        for (int i = 0; i < 10; i++)
        {
            var result = await _paymentService.ChargeAsync(amount, cardNumber, expirationDate, cvv, cardholderName);
            results.Add(result.Success);
        }

        // Assert - Should always succeed
        Assert.All(results, r => Assert.True(r));
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
        var response = (RefundResponse)result.Data;
        Assert.Equal(amount, response.Amount);
        Assert.Equal("success", response.Status);
        Assert.NotNull(response.RefundId);
        Assert.Equal(transactionId, response.OriginalTransactionId);
        Assert.Contains("Refund processed successfully", response.Message);
    }

    [Fact]
    public async Task RefundAsync_ShouldAlwaysSucceed()
    {
        // Arrange
        var transactionId = Guid.NewGuid().ToString();
        var amount = 75.00m;
        var reason = "Customer request";

        // Act - Run multiple times
        var results = new List<bool>();
        for (int i = 0; i < 10; i++)
        {
            var result = await _paymentService.RefundAsync(transactionId, amount, reason);
            results.Add(result.Success);
        }

        // Assert - Should always succeed
        Assert.All(results, r => Assert.True(r));
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
