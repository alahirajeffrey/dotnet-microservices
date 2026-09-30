using Microsoft.EntityFrameworkCore;

namespace OrderService.Models;

[Index(nameof(UserId), nameof(CreatedAt))]
public class Order
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public decimal TotalAmount { get; set; }

    public string Currency { get; set; } = "NGN";

    public OrderStatus Status { get; set; }

    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public List<OrderItem> Items { get; set; } = new();
}