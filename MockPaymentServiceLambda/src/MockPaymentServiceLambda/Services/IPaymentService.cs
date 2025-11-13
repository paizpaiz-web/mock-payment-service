namespace MockPaymentServiceLambda.Services;

public interface IPaymentService
{
    Task<(bool Success, string Message, object? Data)> ChargeAsync(decimal amount, string? cardNumber, string? expirationDate, string? cvv, string? cardholderName);
    Task<(bool Success, string Message, object? Data)> RefundAsync(string transactionId, decimal amount, string? reason);
}