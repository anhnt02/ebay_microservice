using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PaymentShipping.Application.Common;
using PaymentShipping.Application.Payments;

namespace PaymentShipping.Infrastructure.Payments;

/// <summary>
/// Simulated PayPal provider — gọi PayPal Sandbox API.
/// Nếu không có credentials thật, trả về simulated response.
/// 🔐 Được bảo vệ bằng PayPal OAuth2 client credentials.
/// ⚡ Timeout 2 giây theo yêu cầu.
/// </summary>
public sealed class SimulatedPayPalProvider : IPaymentProvider
{
    public string ProviderName => "paypal";

    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<SimulatedPayPalProvider> _logger;
    private readonly ITransactionContextAccessor _txContext;

    private string BaseUrl     => _config["PayPal:BaseUrl"]      ?? "https://api-m.sandbox.paypal.com";
    private string ClientId    => _config["PayPal:ClientId"]     ?? "SIMULATED_CLIENT_ID";
    private string ClientSecret => _config["PayPal:ClientSecret"] ?? "SIMULATED_CLIENT_SECRET";
    private string Currency    => _config["PayPal:Currency"]     ?? "USD";
    private bool   IsSimulated => _config.GetValue<bool>("PayPal:Simulated", true);

    public SimulatedPayPalProvider(
        HttpClient http,
        IConfiguration config,
        ILogger<SimulatedPayPalProvider> logger,
        ITransactionContextAccessor txContext)
    {
        _http = http;
        _config = config;
        _logger = logger;
        _txContext = txContext;
    }

    public async Task<PaymentProviderResult> InitiateAsync(PaymentRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "PayPal InitiatePayment | cid={cid} | tx={tx} | orderId={orderId} | amount={amount} {currency}",
            _txContext.CorrelationId, _txContext.TransactionId,
            request.OrderId, request.Amount, request.Currency);

        if (IsSimulated)
            return SimulateInitiate(request);

        try
        {
            var accessToken = await GetAccessTokenAsync(ct);
            var paypalOrderId = await CreatePayPalOrderAsync(request, accessToken, ct);
            var approveUrl = $"{BaseUrl}/checkoutnow?token={paypalOrderId}";

            _logger.LogInformation(
                "PayPal order created | cid={cid} | tx={tx} | paypalOrderId={pid}",
                _txContext.CorrelationId, _txContext.TransactionId, paypalOrderId);

            return new PaymentProviderResult(
                Success: true,
                TransactionId: paypalOrderId,
                Status: "CREATED",
                Message: "PayPal order created. Redirect user to approve URL.",
                ApproveUrl: approveUrl,
                ProviderRawResponse: paypalOrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "PayPal InitiatePayment failed | cid={cid} | tx={tx} | orderId={orderId}",
                _txContext.CorrelationId, _txContext.TransactionId, request.OrderId);

            return new PaymentProviderResult(
                Success: false,
                TransactionId: "",
                Status: "FAILED",
                Message: $"PayPal error: {ex.Message}");
        }
    }

    public async Task<PaymentProviderResult> CaptureAsync(string providerOrderId, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "PayPal CapturePayment | cid={cid} | tx={tx} | paypalOrderId={pid}",
            _txContext.CorrelationId, _txContext.TransactionId, providerOrderId);

        if (IsSimulated)
            return SimulateCapture(providerOrderId);

        try
        {
            var accessToken = await GetAccessTokenAsync(ct);
            var (success, status, raw) = await CapturePayPalOrderAsync(providerOrderId, accessToken, ct);

            return new PaymentProviderResult(
                Success: success,
                TransactionId: providerOrderId,
                Status: status,
                Message: success ? "Payment captured successfully" : "Capture failed",
                ProviderRawResponse: raw);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "PayPal CapturePayment failed | cid={cid} | tx={tx} | paypalOrderId={pid}",
                _txContext.CorrelationId, _txContext.TransactionId, providerOrderId);

            return new PaymentProviderResult(
                Success: false, TransactionId: providerOrderId,
                Status: "FAILED", Message: ex.Message);
        }
    }

    // ── Simulated responses ───────────────────────────────────────────

    private static PaymentProviderResult SimulateInitiate(PaymentRequest request)
    {
        var fakeOrderId = $"PAYPAL-SIM-{request.OrderId}-{DateTime.UtcNow.Ticks}";
        return new PaymentProviderResult(
            Success: true,
            TransactionId: fakeOrderId,
            Status: "CREATED",
            Message: "[SIMULATED] PayPal order created",
            ApproveUrl: $"http://localhost:5173/payment/paypal/approve?token={fakeOrderId}",
            ProviderRawResponse: $"{{\"id\":\"{fakeOrderId}\",\"status\":\"CREATED\"}}");
    }

    private static PaymentProviderResult SimulateCapture(string providerOrderId)
    {
        return new PaymentProviderResult(
            Success: true,
            TransactionId: providerOrderId,
            Status: "COMPLETED",
            Message: "[SIMULATED] PayPal payment captured",
            ProviderRawResponse: $"{{\"id\":\"{providerOrderId}\",\"status\":\"COMPLETED\"}}");
    }

    // ── Real PayPal API calls ─────────────────────────────────────────

    private async Task<string> GetAccessTokenAsync(CancellationToken ct)
    {
        var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{ClientId}:{ClientSecret}"));
        using var req = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/v1/oauth2/token");
        req.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);
        req.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials"
        });

        using var resp = await _http.SendAsync(req, ct);
        var json = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogError("PayPal auth failed | status={s} | body={b}",
                (int)resp.StatusCode, json[..Math.Min(json.Length, 500)]);
            throw new InvalidOperationException("Failed to get PayPal access token");
        }

        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("access_token").GetString()
               ?? throw new InvalidOperationException("PayPal access_token missing");
    }

    private async Task<string> CreatePayPalOrderAsync(PaymentRequest request, string accessToken, CancellationToken ct)
    {
        var body = new
        {
            intent = "CAPTURE",
            purchase_units = new[]
            {
                new
                {
                    reference_id = request.OrderId.ToString(),
                    description  = request.OrderDescription,
                    amount       = new
                    {
                        currency_code = Currency,
                        value = request.Amount.ToString("0.00", CultureInfo.InvariantCulture)
                    }
                }
            },
            application_context = new
            {
                shipping_preference = "NO_SHIPPING",
                user_action         = "PAY_NOW"
            }
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/v2/checkout/orders");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        req.Headers.Add("Prefer", "return=representation");
        req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        using var resp = await _http.SendAsync(req, ct);
        var json = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"PayPal create order failed: {json[..Math.Min(json.Length, 500)]}");

        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("id").GetString()
               ?? throw new InvalidOperationException("PayPal order ID missing");
    }

    private async Task<(bool Success, string Status, string Raw)> CapturePayPalOrderAsync(
        string paypalOrderId, string accessToken, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(
            HttpMethod.Post, $"{BaseUrl}/v2/checkout/orders/{paypalOrderId}/capture");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        req.Headers.Add("Prefer", "return=representation");
        req.Content = new StringContent("{}", Encoding.UTF8, "application/json");

        using var resp = await _http.SendAsync(req, ct);
        var json = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
            return (false, "FAILED", json);

        using var doc = JsonDocument.Parse(json);
        var status = doc.RootElement.TryGetProperty("status", out var s) ? s.GetString() ?? "" : "";
        var success = status is "COMPLETED" or "APPROVED";
        return (success, status, json);
    }
}
