using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaymentShipping.Application.Orders;
using PaymentShipping.Contracts;
using PaymentShipping.Contracts.Orders;

namespace PaymentShipping.Api.Controllers;

[Authorize]
public sealed class OrdersController : BaseController
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService) => _orderService = orderService;

    [HttpPost]
    [HttpPost("checkout")]
    public async Task<IActionResult> Create(CreateOrderRequest req, CancellationToken ct)
    {
        var result = await _orderService.CreateAsync(CurrentUserId, req, ct);
        return Ok(ApiResponse<OrderDto>.Ok(result, CurrentCorrelationId, "Order created successfully"));
    }

    [HttpPost("calculate")]
    [HttpPost("checkout/preview")]
    public async Task<IActionResult> Calculate(CalculateOrderRequest req, CancellationToken ct)
    {
        var result = await _orderService.CalculateAsync(CurrentUserId, req, ct);
        return Ok(ApiResponse<OrderTotalDto>.Ok(result, CurrentCorrelationId, "Calculation done"));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id, CancellationToken ct)
    {
        var result = await _orderService.GetByIdAsync(CurrentUserId, id, ct);
        return Ok(ApiResponse<OrderDto>.Ok(result, CurrentCorrelationId));
    }

    [HttpGet]
    [HttpGet("my")]
    public async Task<IActionResult> GetMyOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        var result = await _orderService.GetMyOrdersAsync(CurrentUserId, page, pageSize, ct);
        return Ok(ApiResponse<PagedResponse<OrderDto>>.Ok(result, CurrentCorrelationId));
    }

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(int id, CancellationToken ct)
    {
        var result = await _orderService.CancelAsync(CurrentUserId, id, ct);
        return Ok(ApiResponse<OrderDto>.Ok(result, CurrentCorrelationId, "Order cancelled successfully"));
    }

    [HttpGet("my-coupons")]
    public IActionResult GetMyCoupons()
    {
        var coupons = new[]
        {
            new { Id = 1, Code = "SAVE10", DiscountPercent = 10, Description = "10% off for all orders" },
            new { Id = 2, Code = "SAVE20", DiscountPercent = 20, Description = "20% off for all orders" }
        };
        return Ok(ApiResponse<object>.Ok(coupons, CurrentCorrelationId));
    }

    [HttpPost("{id}/pay")]
    public async Task<IActionResult> PayOrder(int id, [FromServices] PaymentShipping.Application.Payments.IPaymentService paymentService, CancellationToken ct)
    {
        var req = new PaymentShipping.Contracts.Payments.ProcessPaymentRequest(id, "paypal");
        var result = await paymentService.ProcessAsync(CurrentUserId, req, ct);
        // The frontend expects PaypalOrderId in the response to open the popup.
        return Ok(ApiResponse<object>.Ok(new { Id = id, Status = "paid", Message = "Payment simulated successfully", PaypalOrderId = result.TransactionId ?? $"PAYPAL-{Guid.NewGuid()}" }, CurrentCorrelationId, "Paid"));
    }

    [HttpPost("{id}/pay/capture")]
    public async Task<IActionResult> CapturePayPalOrder(int id, [FromBody] System.Text.Json.JsonElement payload, [FromServices] PaymentShipping.Application.Payments.IPaymentService paymentService, CancellationToken ct)
    {
        string? paypalOrderId = null;
        if (payload.TryGetProperty("paypalOrderId", out var prop))
        {
            paypalOrderId = prop.GetString();
        }
        
        if (string.IsNullOrEmpty(paypalOrderId))
        {
            return BadRequest(ApiResponse<object>.Fail("paypalOrderId is required", "BAD_REQUEST", CurrentCorrelationId));
        }

        var result = await paymentService.CapturePayPalAsync(CurrentUserId, id, paypalOrderId, ct);
        return Ok(ApiResponse<object>.Ok(new { Id = id, Status = "paid", Message = "PayPal payment captured" }, CurrentCorrelationId, "Captured"));
    }

    [HttpPut("{id}/address")]
    public IActionResult UpdateOrderAddress(int id, [FromBody] object payload)
    {
        return Ok(ApiResponse<object>.Ok(new { Id = id }, CurrentCorrelationId, "Address updated"));
    }
}
