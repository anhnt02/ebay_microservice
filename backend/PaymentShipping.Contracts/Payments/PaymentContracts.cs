namespace PaymentShipping.Contracts.Payments;

// ── Requests ─────────────────────────────────────────────────────

public record ProcessPaymentRequest(
    int OrderId,
    string Provider   // "paypal" | "cod"
);

public record CapturePayPalRequest(
    string PayPalOrderId
);

// ── Responses ────────────────────────────────────────────────────

public record PaymentResultDto(
    bool Success,
    string TransactionId,
    string Status,
    string Message,
    string? ApproveUrl = null,      // For PayPal redirect
    string? ProviderRawResponse = null
);
