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
    public async Task<IActionResult> Create(CreateOrderRequest req, CancellationToken ct)
    {
        var result = await _orderService.CreateAsync(CurrentUserId, req, ct);
        return Ok(ApiResponse<OrderDto>.Ok(result, CurrentCorrelationId, "Order created successfully"));
    }

    [HttpPost("calculate")]
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
}
