using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaymentService.Data;
using PaymentService.Models;

namespace PaymentService.Controllers;

[ApiController]
[Route("api/webhook")]
public class WebhookController : ControllerBase
{
    private readonly PaymentDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    public WebhookController(
        PaymentDbContext db,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
    {
        _db = db;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    [HttpPost("paystack")]
    public async Task<IActionResult> PaystackWebhook()
    {
        var signature = Request.Headers["x-paystack-signature"].ToString();
        if (string.IsNullOrWhiteSpace(signature))
        {
            return Unauthorized();
        }

        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
        var rawBody = await reader.ReadToEndAsync();
        Request.Body.Position = 0;

        var secretKey = _configuration["Paystack:SecretKey"]
            ?? throw new InvalidOperationException("Paystack secret key not configured.");

        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(secretKey));
        var computedHash = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(rawBody))).ToLowerInvariant();

        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(signature.Trim()),
                Encoding.UTF8.GetBytes(computedHash)))
        {
            return Unauthorized();
        }

        var payload = JsonSerializer.Deserialize<PaystackWebhookPayload>(rawBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (payload is null || payload.Data is null || string.IsNullOrWhiteSpace(payload.Data.Reference))
        {
            return BadRequest();
        }

        var payment = await _db.Payments
            .FirstOrDefaultAsync(p => p.Reference == payload.Data.Reference);

        if (payment is null)
        {
            return NotFound();
        }

        var paymentSucceeded = payload.Event == "charge.success" && payload.Data.Status == "success";

        if (paymentSucceeded)
        {
            payment.Status = PaymentStatus.Successful;
            payment.PaidAt = DateTime.UtcNow;
            payment.PaystackTransactionId = payload.Data.Id;
            await _db.SaveChangesAsync();

            var orderClient = _httpClientFactory.CreateClient("OrderService");
            var internalSecret = _configuration["InternalApi:Secret"]
                ?? "internal-secret";

            var updateOrderRequest = new HttpRequestMessage(
                HttpMethod.Patch,
                $"api/v1/Orders/{payment.OrderId}/status"
            )
            {
                Content = JsonContent.Create(new UpdateOrderStatusRequest
                {
                    Status = "Paid"
                })
            };

            updateOrderRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", internalSecret);
            updateOrderRequest.Headers.Add("X-Internal-Secret", internalSecret);

            var orderResponse = await orderClient.SendAsync(updateOrderRequest);
            if (!orderResponse.IsSuccessStatusCode)
            {
                return StatusCode(StatusCodes.Status502BadGateway, new
                {
                    message = "Payment was successful but updating the order status failed."
                });
            }

            return Ok(new { status = "received" });
        }

        if (payload.Data.Status == "failed")
        {
            payment.Status = PaymentStatus.Failed;
            await _db.SaveChangesAsync();
        }

        return Ok(new { status = "received" });
    }
}


public class PaystackWebhookPayload
{
    public string Event { get; set; } = string.Empty;
    public PaystackWebhookData Data { get; set; } = new();
}

public class PaystackWebhookData
{
    public long Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public long Amount { get; set; }
}

public class UpdateOrderStatusRequest
{
    public string Status { get; set; } = string.Empty;
}
