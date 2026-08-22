using PaymentShipping.Contracts.Payments;

namespace PaymentShipping.Application.Payments;

// ─── Value objects ────────────────────────────────────────────────

/// <summary>Yêu cầu xử lý thanh toán gửi đến payment provider.</summary>
public record PaymentRequest(
    int OrderId,
    decimal Amount,
    string Currency,
    string BuyerEmail,
    string OrderDescription
);

/// <summary>Kết quả từ payment provider trả về.</summary>
public record PaymentProviderResult(
    bool Success,
    string TransactionId,
    string Status,
    string Message,
    string? ApproveUrl = null,
    string? ProviderRawResponse = null
);

// ─── Interfaces ───────────────────────────────────────────────────

/// <summary>
/// Interface plug-in cho payment provider (PayPal, COD...).
/// Dễ dàng thêm provider mới mà không cần sửa core logic.
/// </summary>
public interface IPaymentProvider
{
    string ProviderName { get; }  // "paypal" | "cod"

    /// <summary>Khởi tạo thanh toán — trả về approve URL (PayPal) hoặc confirm trực tiếp (COD).</summary>
    Task<PaymentProviderResult> InitiateAsync(PaymentRequest request, CancellationToken ct = default);

    /// <summary>Xác nhận / capture thanh toán sau khi user approve (chủ yếu dùng cho PayPal).</summary>
    Task<PaymentProviderResult> CaptureAsync(string providerOrderId, CancellationToken ct = default);
}

/// <summary>Service điều phối thanh toán — chọn đúng provider và lưu DB.</summary>
public interface IPaymentService
{
    Task<PaymentResultDto> ProcessAsync(int buyerId, ProcessPaymentRequest request, CancellationToken ct = default);
    Task<PaymentResultDto> CapturePayPalAsync(int buyerId, int orderId, string paypalOrderId, CancellationToken ct = default);
    Task<PaymentResultDto> ConfirmCodAsync(int buyerId, int orderId, CancellationToken ct = default);
}
