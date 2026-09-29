using System.ComponentModel.DataAnnotations;

namespace ProductService.DTOs;

public class UpdateProduct
{
    public string? Name { get; set; } = string.Empty;
    public string? Description { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Price must be a positive number")]
    public decimal? Price { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be a positive number")]
    public int? Quantity { get; set; }
}