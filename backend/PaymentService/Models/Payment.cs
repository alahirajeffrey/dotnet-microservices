using Microsoft.EntityFrameworkCore;

namespace PaymentService.Models;

[Index(nameof(Reference), IsUnique = true)]
[Index(nameof(OrderId), nameof(Status))]
public class Payment
{
    public Guid Id { get; set; } = Guid.NewGuid();

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