namespace PaymentService.Models;

public class Payment
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public Guid UserId { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "NGN";

    public string Provider { get; set; } = "Paystack";

    public string Reference { get; set; } = string.Empty;

    public PaymentStatus Status { get; set; }

    public long? PaystackTransactionId { get; set; }

    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public DateTime? PaidAt { get; set; }
}