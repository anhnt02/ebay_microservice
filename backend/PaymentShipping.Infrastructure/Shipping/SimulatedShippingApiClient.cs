using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace PaymentShipping.Infrastructure.Shipping;

/// <summary>
/// Simulated Shipping API Client — giả lập việc kết nối API vận chuyển bên ngoài.
/// 🔁 Có retry policy (Polly) nếu request thất bại.
/// Trong thực tế, đây sẽ là client của GHN, GHTK, 17Track, v.v.
/// </summary>
public sealed class SimulatedShippingApiClient
{
    private readonly ILogger<SimulatedShippingApiClient> _logger;
    private readonly AsyncRetryPolicy _retryPolicy;
    private static readonly Random _rng = new();

    public SimulatedShippingApiClient(ILogger<SimulatedShippingApiClient> logger)
    {
        _logger = logger;

        // 🔁 Retry 3 lần, chờ exponential backoff: 1s, 2s, 4s
        _retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt - 1)),
                onRetry: (ex, wait, attempt, ctx) =>
                {
                    logger.LogWarning(
                        "ShippingApi retry #{attempt} after {wait}ms | error={msg}",
                        attempt, (int)wait.TotalMilliseconds, ex.Message);
                });
    }

    /// <summary>
    /// Đăng ký vận đơn với hãng vận chuyển, lấy tracking code.
    /// Simulates random failure 20% để test retry.
    /// </summary>
    public async Task<string> RegisterShipmentAsync(
        int orderId, string carrier, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "ShippingApi.RegisterShipment | orderId={id} | carrier={carrier}",
            orderId, carrier);

        var trackingCode = await _retryPolicy.ExecuteAsync(async () =>
        {
            await Task.Delay(50, ct); // Simulate network latency

            // Simulate 20% failure rate to test retry
            if (_rng.NextDouble() < 0.20)
                throw new HttpRequestException("Simulated shipping API connection error");

            return GenerateTrackingCode(carrier, orderId);
        });

        _logger.LogInformation(
            "ShippingApi.RegisterShipment succeeded | orderId={id} | trackingCode={tc}",
            orderId, trackingCode);

        return trackingCode;
    }

    /// <summary>Lấy trạng thái vận đơn từ carrier API.</summary>
    public async Task<string?> GetTrackingStatusAsync(string trackingCode, CancellationToken ct = default)
    {
        _logger.LogInformation("ShippingApi.GetTrackingStatus | trackingCode={tc}", trackingCode);

        return await _retryPolicy.ExecuteAsync(async () =>
        {
            await Task.Delay(30, ct);
            // Simulated — in reality would call carrier's API
            return "in_transit";
        });
    }

    private static string GenerateTrackingCode(string carrier, int orderId)
    {
        var prefix = carrier.ToUpperInvariant() switch
        {
            "SIMSHIP" => "SS",
            "GHN"     => "GH",
            "GHTK"    => "GK",
            "VNPOST"  => "VP",
            _         => "XX"
        };
        var timestamp = DateTime.UtcNow.ToString("yyMMddHHmm");
        var suffix = _rng.Next(1000, 9999);
        return $"{prefix}{orderId:D6}{timestamp}{suffix}";
    }
}
