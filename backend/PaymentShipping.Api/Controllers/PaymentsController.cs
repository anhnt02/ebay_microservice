using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaymentShipping.Application.Payments;
using PaymentShipping.Contracts;
using PaymentShipping.Contracts.Payments;

namespace PaymentShipping.Api.Controllers;

[Authorize]
public sealed class PaymentsController : BaseController
{
    private readonly IPaymentService _payment;

    public PaymentsController(IPaymentService payment) => _payment = payment;

    [HttpPost("process")]
    public async Task<IActionResult> Process(ProcessPaymentRequest req, CancellationToken ct)
    {
        var result = await _payment.ProcessAsync(CurrentUserId, req, ct);
        return Ok(ApiResponse<PaymentResultDto>.Ok(result, CurrentCorrelationId, result.Message));
    }

    [HttpPost("capture/paypal/{orderId}")]
    public async Task<IActionResult> CapturePayPal(int orderId, [FromBody] CapturePayPalRequest req, CancellationToken ct)
    {
        // Require X-Payment-Secret-Key handled by middleware
        var result = await _payment.CapturePayPalAsync(CurrentUserId, orderId, req.PayPalOrderId, ct);
        return Ok(ApiResponse<PaymentResultDto>.Ok(result, CurrentCorrelationId, result.Message));
    }

    [HttpPost("confirm-cod/{orderId}")]
    public async Task<IActionResult> ConfirmCod(int orderId, CancellationToken ct)
    {
        // Require X-Payment-Secret-Key handled by middleware
        var result = await _payment.ConfirmCodAsync(CurrentUserId, orderId, ct);
        return Ok(ApiResponse<PaymentResultDto>.Ok(result, CurrentCorrelationId, result.Message));
    }
}
