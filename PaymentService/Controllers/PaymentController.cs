using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaymentService.Data;
using PaymentService.Models;
using PaymentService.Services;

namespace PaymentService.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class PaymentController : ControllerBase
{
    private readonly PaymentDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly RabbitMqLogPublisher _logPublisher;

    public PaymentController(
        PaymentDbContext db,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        RabbitMqLogPublisher logPublisher)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logPublisher = logPublisher;
    }

    [HttpPost("paysatck/initialize")]
    public async Task<IActionResult> InitializePayment([FromBody] InitializePaymentRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value ?? throw new InvalidOperationException("User id not found."));

        var orderClient = _httpClientFactory.CreateClient("OrderService");
        var orderRequest = new HttpRequestMessage(
            HttpMethod.Get,
            $"api/v1/Orders/{request.OrderId}"
        );

        var authHeader = Request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(authHeader))
        {
            orderRequest.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                authHeader.Replace("Bearer ", string.Empty, StringComparison.OrdinalIgnoreCase)
            );
        }

        var orderResponse = await orderClient.SendAsync(orderRequest);
        if (!orderResponse.IsSuccessStatusCode)
        {
            return BadRequest("Unable to retrieve order details from OrderService.");
        }

        var order = await orderResponse.Content.ReadFromJsonAsync<OrderSummary>();
        if (order is null)
        {
            return BadRequest("The order details could not be loaded.");
        }

        if (order.UserId != userId)
        {
            return Forbid();
        }

        if (order.TotalAmount <= 0)
        {
            return BadRequest("The order total must be greater than zero.");
        }

        var existingPayment = await _db.Payments
            .FirstOrDefaultAsync(p => p.OrderId == order.Id && p.Status == PaymentStatus.Pending);

        if (existingPayment is not null)
        {
            return Ok(new
            {
                paymentId = existingPayment.Id,
                reference = existingPayment.Reference,
                status = existingPayment.Status.ToString(),
                amount = existingPayment.Amount,
                currency = existingPayment.Currency
            });
        }

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            UserId = userId,
            Amount = order.TotalAmount,
            Currency = string.IsNullOrWhiteSpace(order.Currency) ? "NGN" : order.Currency,
            Provider = "Paystack",
            Status = PaymentStatus.Pending,
            Reference = $"PS_{Guid.NewGuid():N}"
        };

        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();

        var paystackClient = _httpClientFactory.CreateClient("Paystack");
        var paystackSecret = _configuration["Paystack:SecretKey"]
            ?? throw new InvalidOperationException("Paystack secret key not configured.");

        var paystackRequest = new
        {
            email = request.Email,
            amount = (int)Math.Round(payment.Amount * 100m),
            currency = payment.Currency,
            reference = payment.Reference,
            callback_url = _configuration["Paystack:CallbackUrl"] ?? "http://localhost:5000/api/webhook/paystack",
            metadata = new
            {
                order_id = payment.OrderId,
                payment_id = payment.Id,
                user_id = payment.UserId
            }
        };

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "transaction/initialize")
        {
            Content = JsonContent.Create(paystackRequest)
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", paystackSecret);

        var paystackResponse = await paystackClient.SendAsync(httpRequest);
        var paystackPayload = await paystackResponse.Content.ReadFromJsonAsync<PaystackInitializeResponse>();

        if (!paystackResponse.IsSuccessStatusCode || paystackPayload is null || paystackPayload.Data is null)
        {
            payment.Status = PaymentStatus.Failed;
            await _db.SaveChangesAsync();

            return StatusCode(StatusCodes.Status502BadGateway, new
            {
                message = "Paystack initialization failed.",
                details = paystackPayload?.Message ?? "Unknown error"
            });
        }

        payment.Reference = paystackPayload.Data.Reference ?? payment.Reference;
        await _db.SaveChangesAsync();

        await _logPublisher.PublishAsync(
            "PaymentService",
            "PaymentInitialized",
            $"Payment initialized for order {order.Id}",
            new { paymentId = payment.Id, orderId = order.Id, amount = payment.Amount, currency = payment.Currency, reference = payment.Reference });

        return Ok(new
        {
            paymentId = payment.Id,
            reference = payment.Reference,
            authorizationUrl = paystackPayload.Data.AuthorizationUrl,
            accessCode = paystackPayload.Data.AccessCode,
            amount = payment.Amount,
            currency = payment.Currency,
            status = payment.Status.ToString()
        });
    }
}

public class InitializePaymentRequest
{
    public Guid OrderId { get; set; }
    public string Email { get; set; } = string.Empty;
}

public class OrderSummary
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "NGN";
}

public class PaystackInitializeResponse
{
    public bool Status { get; set; }
    public string? Message { get; set; }
    public PaystackInitializeData? Data { get; set; }
}

public class PaystackInitializeData
{
    public string? AuthorizationUrl { get; set; }
    public string? AccessCode { get; set; }
    public string? Reference { get; set; }
}
