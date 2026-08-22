using PaymentShipping.Application.Notifications;
using PaymentShipping.Application.Orders;
using PaymentShipping.Domain.Entities;

namespace PaymentShipping.Infrastructure.Orders;

/// <summary>
/// Gửi email thông báo thanh toán thành công và thay đổi trạng thái đơn hàng.
/// </summary>
public sealed class OrderEmailService : IOrderEmailService
{
    private readonly IEmailService _emailService;
    private readonly string _frontendUrl;

    public OrderEmailService(IEmailService emailService, IConfiguration config)
    {
        _emailService = emailService;
        _frontendUrl = config["Frontend:BaseUrl"] ?? "http://localhost:5173";
    }

    public async Task SendPaymentSuccessEmailAsync(Order order, CancellationToken ct = default)
    {
        if (order.Buyer?.Email == null) return;

        var subtotal  = order.SubtotalAmount;
        var shipping  = order.ShippingFee;
        var discount  = order.DiscountAmount;
        var total     = order.TotalPrice;

        var itemRows = string.Join("", order.OrderItems.Select(i =>
            $"""
            <tr>
              <td style="padding:8px;border-bottom:1px solid #f3f4f6">{i.Product?.Title ?? "Product"} (x{i.Quantity})</td>
              <td style="padding:8px;border-bottom:1px solid #f3f4f6;text-align:right">${i.LineTotal:F2}</td>
            </tr>
            """));

        var discountRow = discount > 0
            ? $"""<tr><td style="padding:8px;text-align:right;color:#059669">Discount ({order.CouponCode}):</td><td style="padding:8px;text-align:right;color:#059669">-${discount:F2}</td></tr>"""
            : "";

        var subject = $"✅ Payment Confirmed — Order #{order.Id}";
        var body = $"""
<div style="font-family:Arial,sans-serif;max-width:600px;margin:auto;padding:24px;border:1px solid #e5e7eb;border-radius:10px">
  <h2 style="color:#0053a0;margin-bottom:4px">Payment Confirmed!</h2>
  <p>Hello <strong>{order.Address?.FullName ?? order.Buyer?.FullName ?? order.Buyer?.Username ?? "Valued Customer"}</strong>,</p>
  <p>Your payment for order <strong>#{order.Id}</strong> has been successfully processed. We are now preparing your shipment.</p>
  <table style="width:100%;border-collapse:collapse;margin-top:20px">
    <thead>
      <tr style="background:#f3f4f6">
        <th style="text-align:left;padding:10px;border-bottom:2px solid #e5e7eb">Item</th>
        <th style="text-align:right;padding:10px;border-bottom:2px solid #e5e7eb">Price</th>
      </tr>
    </thead>
    <tbody>{itemRows}</tbody>
    <tfoot>
      <tr><td style="padding:8px;text-align:right">Subtotal:</td><td style="padding:8px;text-align:right">${subtotal:F2}</td></tr>
      <tr><td style="padding:8px;text-align:right">Shipping:</td><td style="padding:8px;text-align:right">${shipping:F2}</td></tr>
      {discountRow}
      <tr style="font-weight:bold;font-size:1.1rem">
        <td style="padding:10px;text-align:right">Total Paid:</td>
        <td style="padding:10px;text-align:right;color:#0053a0">${total:F2}</td>
      </tr>
    </tfoot>
  </table>
  <p style="margin-top:30px">
    <a href="{_frontendUrl}/orders/{order.Id}" style="background:#0053a0;color:white;padding:12px 24px;text-decoration:none;border-radius:6px;display:inline-block">View Order Details</a>
  </p>
  <p style="color:#6b7280;font-size:0.85rem;margin-top:30px">Thank you for your purchase!</p>
</div>
""";

        await _emailService.SendAsync(order.Buyer.Email, subject, body, ct);
    }

    public async Task SendOrderStatusChangedEmailAsync(
        Order order, string oldStatus, string newStatus, CancellationToken ct = default)
    {
        if (order.Buyer?.Email == null) return;

        var statusDisplay = newStatus.Replace("_", " ").ToUpperInvariant();

        var (message, ctaLabel) = newStatus.ToLower() switch
        {
            "shipped"   => ("Great news! Your package is on its way.", "Track Order"),
            "delivered" => ("Your package has been delivered! We hope you enjoy your purchase.", "View Order"),
            "cancelled" => ("Your order has been cancelled. If this was unexpected, please contact support.", "Contact Support"),
            _           => ($"Your order status has changed to: {statusDisplay}", "View Order")
        };

        var subject = $"📦 Order #{order.Id} Update: {statusDisplay}";
        var body = $"""
<div style="font-family:Arial,sans-serif;max-width:600px;margin:auto;padding:24px;border:1px solid #e5e7eb;border-radius:10px">
  <h2 style="color:#0053a0">Order Update: {statusDisplay}</h2>
  <p>Hello <strong>{order.Buyer.FullName ?? order.Buyer.Username ?? "Valued Customer"}</strong>,</p>
  <p>{message}</p>
  <div style="background:#f9fafb;padding:16px;border-radius:6px;margin:20px 0">
    <p style="margin:0">Order ID: <strong>#{order.Id}</strong></p>
    <p style="margin:8px 0 0">Status: <span style="color:#059669;font-weight:bold">{statusDisplay}</span></p>
  </div>
  <p style="margin-top:30px">
    <a href="{_frontendUrl}/orders/{order.Id}" style="background:#0053a0;color:white;padding:12px 24px;text-decoration:none;border-radius:6px;display:inline-block">{ctaLabel}</a>
  </p>
  <p style="color:#6b7280;font-size:0.8rem;margin-top:40px;border-top:1px solid #e5e7eb;padding-top:20px">
    If you have questions, please contact our support team.
  </p>
</div>
""";

        await _emailService.SendAsync(order.Buyer.Email, subject, body, ct);
    }
}
