using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MockPaymentService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentController : ControllerBase
{
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(ILogger<PaymentController> logger)
    {
        _logger = logger;
    }

    [HttpPost("charge")]
    public async Task<IActionResult> Charge([FromBody] ChargeRequest request)
    {
        _logger.LogInformation("Processing charge request for amount {Amount} with card {CardNumber}",
            request.Amount, request.CardNumber?.Substring(Math.Max(0, request.CardNumber.Length - 4)));

        // Mock payment processing
        await Task.Delay(100); // Simulate processing time

        var transactionId = Guid.NewGuid().ToString();
        var success = Random.Shared.Next(0, 100) > 5; // 95% success rate

        if (success)
        {
            _logger.LogInformation("Charge successful for transaction {TransactionId}", transactionId);
            return Ok(new ChargeResponse
            {
                TransactionId = transactionId,
                Status = "success",
                Amount = request.Amount,
                Message = "Payment processed successfully"
            });
        }
        else
        {
            _logger.LogWarning("Charge failed for transaction {TransactionId}", transactionId);
            return BadRequest(new ChargeResponse
            {
                TransactionId = transactionId,
                Status = "failed",
                Amount = request.Amount,
                Message = "Payment failed due to insufficient funds"
            });
        }
    }

    [HttpPost("refund")]
    public async Task<IActionResult> Refund([FromBody] RefundRequest request)
    {
        _logger.LogInformation("Processing refund request for transaction {TransactionId}",
            request.TransactionId);

        // Mock refund processing
        await Task.Delay(100); // Simulate processing time

        var refundId = Guid.NewGuid().ToString();
        var success = Random.Shared.Next(0, 100) > 10; // 90% success rate

        if (success)
        {
            _logger.LogInformation("Refund successful for transaction {TransactionId}, refund {RefundId}",
                request.TransactionId, refundId);
            return Ok(new RefundResponse
            {
                RefundId = refundId,
                OriginalTransactionId = request.TransactionId,
                Status = "success",
                Amount = request.Amount,
                Message = "Refund processed successfully"
            });
        }
        else
        {
            _logger.LogWarning("Refund failed for transaction {TransactionId}", request.TransactionId);
            return BadRequest(new RefundResponse
            {
                RefundId = refundId,
                OriginalTransactionId = request.TransactionId,
                Status = "failed",
                Amount = request.Amount,
                Message = "Refund failed due to invalid transaction"
            });
        }
    }
}

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