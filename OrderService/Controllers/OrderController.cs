using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.DTOs;
using OrderService.Models;
using OrderService.Services;

namespace OrderService.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly OrderDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly RabbitMqLogPublisher _logPublisher;

    public OrdersController(
        OrderDbContext db,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        RabbitMqLogPublisher logPublisher)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logPublisher = logPublisher;
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder(
        CreateOrderRequest request)
    {
        var userId = Guid.Parse(
            User.FindFirst("sub")!.Value
        );

        if (request.Items.Count == 0)
        {
            return BadRequest("Order must contain items.");
        }

        var client = _httpClientFactory.CreateClient("ProductService");
        var authorizationToken = HttpContext.Request.Headers.Authorization.ToString();

        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = OrderStatus.PendingPayment,
            Currency = "NGN"
        };

        var productIds = request.Items
            .Select(i => i.ProductId)
            .Distinct()
            .ToList();

        var bulkProductRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "api/v1/Product/by-ids"
        )
        {
            Content = JsonContent.Create(productIds)
        };

        if (!string.IsNullOrWhiteSpace(authorizationToken))
        {
            bulkProductRequest.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                authorizationToken.Replace("Bearer ", string.Empty, StringComparison.OrdinalIgnoreCase)
            );
        }

        var bulkProductResponse = await client.SendAsync(bulkProductRequest);
        if (!bulkProductResponse.IsSuccessStatusCode)
        {
            return BadRequest("Unable to load product details for the requested products.");
        }

        var products = await bulkProductResponse.Content.ReadFromJsonAsync<List<ProductCatalogItem>>();
        if (products is null || products.Count == 0)
        {
            return BadRequest("No valid products were returned from ProductService.");
        }

        var productLookup = products
            .GroupBy(p => p.Id)
            .ToDictionary(g => g.Key, g => g.First());

        foreach (var item in request.Items)
        {
            if (!productLookup.TryGetValue(item.ProductId, out var product))
            {
                return BadRequest($"Unable to find product with id {item.ProductId}.");
            }

            if (product.Quantity < item.Quantity)
            {
                return BadRequest($"Product '{product.Name}' does not have enough stock.");
            }

            order.Items.Add(new OrderItem
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                ProductId = product.Id,
                ProductName = product.Name,
                UnitPrice = product.Price,
                Quantity = item.Quantity
            });
        }

        order.TotalAmount = order.Items.Sum(
            item => item.UnitPrice * item.Quantity
        );

        _db.Orders.Add(order);

        await _db.SaveChangesAsync();

        await _logPublisher.PublishAsync(
            "OrderService",
            "OrderCreated",
            $"Order {order.Id} created for user {userId}",
            new { orderId = order.Id, userId, totalAmount = order.TotalAmount, itemCount = order.Items.Count });

        return CreatedAtAction(
            nameof(GetOrder),
            new { id = order.Id },
            order
        );
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetOrder(Guid id)
    {
        var userId = Guid.Parse(
            User.FindFirst("sub")!.Value
        );

        var order = await _db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(
                o => o.Id == id &&
                     o.UserId == userId
            );

        if (order == null)
            return NotFound();

        return Ok(order);
    }

    [HttpGet]
    public async Task<IActionResult> GetOrders(
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

        var userId = Guid.Parse(
            User.FindFirst("sub")!.Value
        );

        var query = _db.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt);

        var totalCount = await query.CountAsync();

        var orders = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var pagedResult = new PagedResult<Order>
        {
            Items = orders,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalCount == 0
                ? 0
                : (int)Math.Ceiling(totalCount / (double)pageSize)
        };

        return Ok(pagedResult);
    }

    [HttpPatch("{id:guid}/status")]
    [AllowAnonymous]
    public async Task<IActionResult> UpdateOrderStatus(Guid id, [FromBody] UpdateOrderStatusRequest request)
    {
        var internalSecret = HttpContext.Request.Headers["X-Internal-Secret"].ToString();
        var expectedSecret = _configuration["InternalApi:Secret"] ?? "internal-secret";

        if (!string.IsNullOrWhiteSpace(expectedSecret) && !string.Equals(internalSecret, expectedSecret, StringComparison.Ordinal))
        {
            return Unauthorized();
        }

        var order = await _db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null)
        {
            return NotFound();
        }

        if (!Enum.TryParse<OrderStatus>(request.Status, true, out var newStatus))
        {
            return BadRequest("Invalid order status value.");
        }

        order.Status = newStatus;
        await _db.SaveChangesAsync();

        await _logPublisher.PublishAsync(
            "OrderService",
            "OrderStatusUpdated",
            $"Order {order.Id} status updated to {newStatus}",
            new { orderId = order.Id, status = newStatus.ToString(), userId = order.UserId });

        if (newStatus == OrderStatus.Paid)
        {
            var productClient = _httpClientFactory.CreateClient("ProductService");
            var stockUpdates = order.Items
                .Select(item => new ReduceProductStockRequest
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity
                })
                .ToList();

            var productRequest = new HttpRequestMessage(
                HttpMethod.Post,
                "api/v1/Product/reduce-stock"
            )
            {
                Content = JsonContent.Create(stockUpdates)
            };

            productRequest.Headers.Add("X-Internal-Secret", expectedSecret);

            var productResponse = await productClient.SendAsync(productRequest);
            if (!productResponse.IsSuccessStatusCode)
            {
                return StatusCode(StatusCodes.Status502BadGateway, new
                {
                    message = "Order was marked as paid but inventory could not be reduced."
                });
            }
        }

        return Ok(order);
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

public class ProductCatalogItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}

public class UpdateOrderStatusRequest
{
    public string Status { get; set; } = string.Empty;
}

public class ReduceProductStockRequest
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}