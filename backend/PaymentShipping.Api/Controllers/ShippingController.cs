using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaymentShipping.Application.Shipping;
using PaymentShipping.Contracts;
using PaymentShipping.Contracts.Shipping;

namespace PaymentShipping.Api.Controllers;

[Authorize]
public sealed class ShippingController : BaseController
{
    private readonly IShippingService _shipping;

    public ShippingController(IShippingService shipping) => _shipping = shipping;

    [HttpPost("{orderId}/shipments")]
    public async Task<IActionResult> CreateShipment(int orderId, CreateShipmentRequest req, CancellationToken ct)
    {
        var result = await _shipping.CreateShipmentAsync(orderId, req, ct);
        return Ok(ApiResponse<ShippingInfoDto>.Ok(result, CurrentCorrelationId, "Shipment created"));
    }

    [HttpPost("status")]
    public async Task<IActionResult> UpdateStatus(UpdateShipmentStatusRequest req, CancellationToken ct)
    {
        var result = await _shipping.UpdateStatusAsync(req, ct);
        return Ok(ApiResponse<ShippingInfoDto>.Ok(result, CurrentCorrelationId, "Status updated"));
    }

    [HttpGet("orders/{orderId}")]
    public async Task<IActionResult> GetByOrder(int orderId, CancellationToken ct)
    {
        var result = await _shipping.GetByOrderIdAsync(orderId, ct);
        return Ok(ApiResponse<ShippingInfoDto>.Ok(result, CurrentCorrelationId));
    }

    [HttpGet("fee")]
    [AllowAnonymous]
    public async Task<IActionResult> CalculateFee([FromQuery] string from = "VN", [FromQuery] string to = "VN", CancellationToken ct = default)
    {
        var result = await _shipping.CalculateFeeAsync(from, to, ct);
        return Ok(ApiResponse<ShippingFeeDto>.Ok(result, CurrentCorrelationId));
    }
}
