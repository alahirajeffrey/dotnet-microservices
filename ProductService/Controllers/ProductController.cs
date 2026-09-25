using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductService.Data;
using ProductService.DTOs;
using ProductService.Models;
using ProductService.Services;

namespace ProductService.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class ProductController : ControllerBase
{
    private readonly ProductDbContext _context;
    private readonly RedisService _redisService;

    public ProductController(ProductDbContext context, RedisService redisService)
    {
        _context = context;
        _redisService = redisService;
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<Product>> CreateProduct([FromBody] CreateProductRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var product = new Product
        {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            Quantity = request.Quantity
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var cacheKey = $"product:{product.Id}";
        await _redisService.SetAsync(cacheKey, product, TimeSpan.FromHours(1));

        return CreatedAtAction(nameof(GetProductById), new { id = product.Id }, product);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Product>> GetProductById(Guid id)
    {
        var cacheKey = $"product:{id}";
        var cachedProduct = await _redisService.GetAsync<Product>(cacheKey);

        if (cachedProduct is not null)
        {
            return Ok(cachedProduct);
        }

        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product is null)
        {
            return NotFound();
        }

        await _redisService.SetAsync(cacheKey, product, TimeSpan.FromHours(1));

        return Ok(product);
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<Product>>> GetProducts(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        if (pageNumber < 1)
        {
            return BadRequest("Page number must be greater than or equal to 1.");
        }

        if (pageSize < 1 || pageSize > 100)
        {
            return BadRequest("Page size must be between 1 and 100.");
        }

        var query = _context.Products
            .AsNoTracking()
            .OrderByDescending(p => p.CreatedAt);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var response = new PagedResult<Product>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };

        return Ok(response);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> DeleteProduct(Guid id)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);

        if (product is null)
        {
            return NotFound();
        }

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        var cacheKey = $"product:{id}";
        await _redisService.RemoveAsync(cacheKey);

        return NoContent();
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<Product>> UpdateProduct(Guid id, [FromBody] UpdateProduct request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);

        if (product is null)
        {
            return NotFound();
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            product.Name = request.Name;
        }

        if (!string.IsNullOrWhiteSpace(request.Description))
        {
            product.Description = request.Description;
        }

        if(request.Price.HasValue)
        {
            product.Price = request.Price.Value;
        }
        if(request.Quantity.HasValue)
        {
            product.Quantity = request.Quantity.Value;
        }
        product.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var cacheKey = $"product:{id}";
        await _redisService.SetAsync(cacheKey, product, TimeSpan.FromHours(1));

        return Ok(product);
    }
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}
