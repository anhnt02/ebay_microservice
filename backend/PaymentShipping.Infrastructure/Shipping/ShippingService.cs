using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentShipping.Application.Common;
using PaymentShipping.Application.Orders;
using PaymentShipping.Application.Shipping;
using PaymentShipping.Contracts.Shipping;
using PaymentShipping.Domain.Entities;
using PaymentShipping.Domain.Exceptions;
using PaymentShipping.Infrastructure.Orders;
using PaymentShipping.Infrastructure.Persistence;

namespace PaymentShipping.Infrastructure.Shipping;

/// <summary>
/// Quản lý vận chuyển:
/// - Tạo mã vận đơn (simulated)
/// - Cập nhật trạng thái giao hàng
/// - Gửi email khi trạng thái thay đổi
/// 🔁 Retry logic cho SimulatedShippingApiClient
/// </summary>
public sealed class ShippingService : IShippingService
{
    private readonly AppDbContext _db;
    private readonly SimulatedShippingApiClient _shippingClient;
    private readonly IOrderEmailService _emailService;
    private readonly ILogger<ShippingService> _logger;
    private readonly ITransactionContextAccessor _txContext;

    public ShippingService(
        AppDbContext db,
        SimulatedShippingApiClient shippingClient,
        IOrderEmailService emailService,
        ILogger<ShippingService> logger,
        ITransactionContextAccessor txContext)
    {
        _db = db;
        _shippingClient = shippingClient;
        _emailService = emailService;
        _logger = logger;
        _txContext = txContext;
    }

    public async Task<ShippingInfoDto> CreateShipmentAsync(
        int orderId, CreateShipmentRequest req, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "CreateShipment started | cid={cid} | tx={tx} | orderId={id}",
            _txContext.CorrelationId, _txContext.TransactionId, orderId);

        var order = await _db.Orders
            .Include(o => o.Buyer)
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
            .Include(o => o.ShippingInfo)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        if (order == null)
            throw new NotFoundException("Order not found", "ORDER_NOT_FOUND");

        if (order.Status != OrderStatuses.Paid && order.Status != OrderStatuses.Processing)
            throw new ValidationException(
                "Only paid or processing orders can be shipped", "ORDER_INVALID_STATUS");

        // Gọi simulated shipping API để lấy tracking code (có retry)
        var trackingCode = await _shippingClient.RegisterShipmentAsync(orderId, req.Carrier ?? "SimShip", ct);
        var estimatedArrival = DateTime.UtcNow.AddDays(5); // Simulated 5-day delivery

        ShippingInfo? shipping;
        if (order.ShippingInfo != null)
        {
            shipping = order.ShippingInfo;
            shipping.TrackingCode = trackingCode;
            shipping.Carrier = req.Carrier ?? "SimShip";
            shipping.Status = ShipmentStatuses.InTransit;
            shipping.ShippedAt = DateTime.UtcNow;
            shipping.EstimatedArrival = estimatedArrival;
            shipping.LastCheckpoint = "Shipment registered";
            shipping.LastSyncedAt = DateTime.UtcNow;
        }
        else
        {
            shipping = new ShippingInfo
            {
                OrderId = orderId,
                TrackingCode = trackingCode,
                Carrier = req.Carrier ?? "SimShip",
                Status = ShipmentStatuses.InTransit,
                ShippedAt = DateTime.UtcNow,
                EstimatedArrival = estimatedArrival,
                LastCheckpoint = "Shipment registered",
                CreatedAt = DateTime.UtcNow,
                LastSyncedAt = DateTime.UtcNow
            };
            _db.ShippingInfos.Add(shipping);
        }

        // Add initial tracking event
        var trackingEvent = new ShippingTrackingEvent
        {
            ShippingInfo = shipping,
            Status = ShipmentStatuses.InTransit,
            Description = "Shipment registered and picked up",
            Location = "Warehouse",
            Provider = "SIMSHIP",
            EventTime = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        _db.ShippingTrackingEvents.Add(trackingEvent);
        shipping.TrackingEvents.Add(trackingEvent);

        var oldStatus = order.Status;
        order.Status = OrderStatuses.Shipped;

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "CreateShipment succeeded | cid={cid} | tx={tx} | orderId={id} | trackingCode={tc}",
            _txContext.CorrelationId, _txContext.TransactionId, orderId, trackingCode);

        // Send email
        try
        {
            await _emailService.SendOrderStatusChangedEmailAsync(order, oldStatus, order.Status, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send shipment email | orderId={id}", orderId);
        }

        return MapToDto(shipping);
    }

    public async Task<ShippingInfoDto> UpdateStatusAsync(
        UpdateShipmentStatusRequest req, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "UpdateShipmentStatus | cid={cid} | tx={tx} | shippingInfoId={id} | newStatus={status}",
            _txContext.CorrelationId, _txContext.TransactionId, req.ShippingInfoId, req.Status);

        var shipping = await _db.ShippingInfos
            .Include(s => s.Order).ThenInclude(o => o!.Buyer)
            .Include(s => s.TrackingEvents)
            .FirstOrDefaultAsync(s => s.Id == req.ShippingInfoId, ct);

        if (shipping == null)
            throw new NotFoundException("Shipping info not found", "SHIPPING_NOT_FOUND");

        var normalizedStatus = NormalizeStatus(req.Status);
        var order = shipping.Order;
        var oldOrderStatus = order?.Status;

        shipping.Status = normalizedStatus;
        shipping.LastCheckpoint = req.Description ?? normalizedStatus;
        shipping.LastSyncedAt = DateTime.UtcNow;

        if (req.EventTime.HasValue)
            shipping.LastSyncedAt = req.EventTime.Value;

        if (normalizedStatus == ShipmentStatuses.Delivered)
        {
            shipping.DeliveredAt = req.EventTime ?? DateTime.UtcNow;
            if (order != null) order.Status = OrderStatuses.Delivered;
        }

        // Add tracking event
        var trackingEvent = new ShippingTrackingEvent
        {
            ShippingInfoId = shipping.Id,
            Status = normalizedStatus,
            Description = req.Description ?? normalizedStatus,
            Location = req.Location,
            Provider = "SIMSHIP",
            EventTime = req.EventTime ?? DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        _db.ShippingTrackingEvents.Add(trackingEvent);
        shipping.TrackingEvents.Add(trackingEvent);

        await _db.SaveChangesAsync(ct);

        // Send email if order status changed
        if (order != null && oldOrderStatus != order.Status)
        {
            try
            {
                await _emailService.SendOrderStatusChangedEmailAsync(
                    order, oldOrderStatus!, order.Status, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send status change email | orderId={id}", order.Id);
            }
        }

        return MapToDto(shipping);
    }

    public async Task<ShippingInfoDto> GetByOrderIdAsync(int orderId, CancellationToken ct = default)
    {
        var shipping = await _db.ShippingInfos
            .AsNoTracking()
            .Include(s => s.TrackingEvents)
            .FirstOrDefaultAsync(s => s.OrderId == orderId, ct);

        if (shipping == null)
            throw new NotFoundException("Shipping info not found for this order", "SHIPPING_NOT_FOUND");

        return MapToDto(shipping);
    }

    public Task<ShippingFeeDto> CalculateFeeAsync(string fromCountry, string toCountry, CancellationToken ct = default)
    {
        var (fee, region) = ShippingFeeCalculator.CalculateByCountry(fromCountry, toCountry);
        return Task.FromResult(new ShippingFeeDto(fee, region, fromCountry, toCountry));
    }

    // ── Private helpers ───────────────────────────────────────────────

    private static string NormalizeStatus(string status) =>
        status.ToLower() switch
        {
            "in_transit"       or "intransit"       => ShipmentStatuses.InTransit,
            "out_for_delivery" or "outfordelivery"  => ShipmentStatuses.OutForDelivery,
            "delivered"                             => ShipmentStatuses.Delivered,
            "exception"                             => ShipmentStatuses.Exception,
            _ => throw new ValidationException($"Invalid shipping status: {status}", "INVALID_SHIPPING_STATUS")
        };

    private static ShippingInfoDto MapToDto(ShippingInfo s) =>
        new(s.Id, s.OrderId, s.TrackingCode, s.Carrier, s.Status,
            s.LastCheckpoint, s.EstimatedArrival, s.ShippedAt, s.DeliveredAt,
            s.TrackingEvents.OrderByDescending(e => e.EventTime)
                .Select(e => new TrackingEventDto(e.Id, e.Status, e.Description, e.Location, e.Provider, e.EventTime))
                .ToList());
}
