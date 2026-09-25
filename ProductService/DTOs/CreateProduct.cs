using System.ComponentModel.DataAnnotations;

namespace ProductService.DTOs;

public class CreateProductRequest
{
    [Required]
    public string Name {get; set;} = string.Empty;
    
    [Required]
    public string Description {get; set;} = string.Empty;
    [Required]
    [Range(1, int.MaxValue, ErrorMessage ="Price must be a positive number")]
    public decimal Price { get; set; }
    [Required]
    [Range(1, int.MaxValue, ErrorMessage ="Quantity must be a positive number")]
    public int Quantity { get; set; }
}