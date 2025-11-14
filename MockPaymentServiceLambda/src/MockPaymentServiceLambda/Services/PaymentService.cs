using Microsoft.Extensions.Logging;

namespace MockPaymentServiceLambda.Services;

public class PaymentService : IPaymentService
{
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(ILogger<PaymentService> logger)
    {
        _logger = logger;
    }

    public async Task<(bool Success, string Message, object? Data)> ChargeAsync(decimal amount, string? cardNumber, string? expirationDate, string? cvv, string? cardholderName)
    {
        try
        {
            _logger.LogInformation("Processing charge request for amount {Amount} with card {CardNumber}",
                amount, cardNumber?.Substring(Math.Max(0, cardNumber.Length - 4)));

            // Mock payment processing
            await Task.Delay(100); // Simulate processing time

            var transactionId = Guid.NewGuid().ToString();
            var success = true; // Always succeed for mock

            if (success)
            {
                _logger.LogInformation("Charge successful for transaction {TransactionId}", transactionId);
                var result = new ChargeResponse
                {
                    TransactionId = transactionId,
                    Status = "success",
                    Amount = amount,
                    Message = "Payment processed successfully"
                };
                return (true, "Charge successful", result);
            }
            else
            {
                _logger.LogWarning("Charge failed for transaction {TransactionId}", transactionId);
                var result = new ChargeResponse
                {
                    TransactionId = transactionId,
                    Status = "failed",
                    Amount = amount,
                    Message = "Payment failed due to insufficient funds"
                };
                return (false, "Charge failed", result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing charge");
            return (false, $"Charge failed: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, object? Data)> RefundAsync(string transactionId, decimal amount, string? reason)
    {
        try
        {
            _logger.LogInformation("Processing refund request for transaction {TransactionId}", transactionId);

            // Mock refund processing
            await Task.Delay(100); // Simulate processing time

            var refundId = Guid.NewGuid().ToString();
            var success = true; // Always succeed for mock

            if (success)
            {
                _logger.LogInformation("Refund successful for transaction {TransactionId}, refund {RefundId}", transactionId, refundId);
                var result = new RefundResponse
                {
                    RefundId = refundId,
                    OriginalTransactionId = transactionId,
                    Status = "success",
                    Amount = amount,
                    Message = "Refund processed successfully"
                };
                return (true, "Refund successful", result);
            }
            else
            {
                _logger.LogWarning("Refund failed for transaction {TransactionId}", transactionId);
                var result = new RefundResponse
                {
                    RefundId = refundId,
                    OriginalTransactionId = transactionId,
                    Status = "failed",
                    Amount = amount,
                    Message = "Refund failed due to invalid transaction"
                };
                return (false, "Refund failed", result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing refund");
            return (false, $"Refund failed: {ex.Message}", null);
        }
    }
}

// DTOs for responses
public class ChargeResponse
{
    public string? TransactionId { get; set; }
    public string? Status { get; set; }
    public decimal Amount { get; set; }
    public string? Message { get; set; }
}

public class RefundResponse
{
    public string? RefundId { get; set; }
    public string? OriginalTransactionId { get; set; }
    public string? Status { get; set; }
    public decimal Amount { get; set; }
    public string? Message { get; set; }
}