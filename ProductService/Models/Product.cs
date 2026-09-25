namespace ProductService.Models;

public class Product
{
    public Guid Id {get; set;} = Guid.NewGuid();
    public required string Name {get; set;}
    public required string Description {get; set;}
    public decimal Price {get; set;}
    public int Quantity {get; set;}
    public DateTime CreatedAt {get; private set;} = DateTime.UtcNow; // private field restricts modification
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}