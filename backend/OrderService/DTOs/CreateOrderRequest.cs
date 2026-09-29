using System.ComponentModel.DataAnnotations;

namespace OrderService.DTOs;

public class CreateOrderRequest
{
    public List<CreateOrderItem> Items { get; set; } = new();
}

public class CreateOrderItem
{
    [Required]
    public Guid ProductId { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage ="Quantity must be a positive number")]
    public int Quantity { get; set; }
}