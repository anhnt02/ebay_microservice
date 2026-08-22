using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentShipping.Application.Common;
using PaymentShipping.Application.Orders;
using PaymentShipping.Contracts.Orders;
using PaymentShipping.Domain.Entities;
using PaymentShipping.Domain.Exceptions;
using PaymentShipping.Infrastructure.Orders;
using PaymentShipping.Infrastructure.Persistence;

namespace PaymentShipping.Infrastructure.Orders;

public sealed class OrderService : IOrderService
{
    private readonly AppDbContext _db;
    private readonly ILogger<OrderService> _logger;
    private readonly ITransactionContextAccessor _txContext;

    public OrderService(AppDbContext db, ILogger<OrderService> logger, ITransactionContextAccessor txContext)
    {
        _db = db;
        _logger = logger;
        _txContext = txContext;
    }

    public async Task<OrderDto> CreateAsync(int buyerId, CreateOrderRequest req, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "CreateOrder started | cid={cid} | tx={tx} | buyerId={buyerId}",
            _txContext.CorrelationId, _txContext.TransactionId, buyerId);

        if (req.Items == null || req.Items.Count == 0)
            throw new ValidationException("Order must contain at least one item", "ORDER_ITEMS_REQUIRED");

        var method = NormalizePaymentMethod(req.PaymentMethod);

        // Load products
        var productIds = req.Items.Select(x => x.ProductId).Distinct().ToList();
        var products = await _db.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);

        if (products.Count != productIds.Count)
            throw new NotFoundException("One or more products not found", "PRODUCT_NOT_FOUND");

        foreach (var item in req.Items)
        {
            var product = products[item.ProductId];
            if (product.Status != "active")
                throw new ValidationException($"Product '{product.Title}' is not available", "PRODUCT_NOT_AVAILABLE");
            if (product.StockQuantity < item.Quantity)
                throw new ValidationException($"Insufficient stock for '{product.Title}'", "INSUFFICIENT_STOCK");
        }

        // Load address
        Address? address = null;
        if (req.AddressId.HasValue)
        {
            address = await _db.Addresses.FirstOrDefaultAsync(a => a.Id == req.AddressId && a.UserId == buyerId, ct);
            if (address == null)
                throw new NotFoundException("Address not found", "ADDRESS_NOT_FOUND");
        }

        // Calculate pricing
        var pricing = await CalculatePricingAsync(buyerId, address, req.Items, products, req.CouponCode, ct);

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        // Deduct stock
        foreach (var item in req.Items)
        {
            var product = products[item.ProductId];
            product.StockQuantity -= item.Quantity;
            if (product.StockQuantity <= 0)
                product.Status = "out_of_stock";
        }

        var order = new Order
        {
            BuyerId = buyerId,
            AddressId = req.AddressId,
            PaymentMethod = method,
            Status = OrderStatuses.PendingPayment,
            SubtotalAmount = pricing.Subtotal,
            ShippingFee = pricing.ShippingFee,
            DiscountAmount = pricing.DiscountAmount,
            TotalPrice = pricing.GrandTotal,
            CouponCode = pricing.AppliedCouponCode,
            OrderDate = DateTime.UtcNow
        };

        _db.Orders.Add(order);
        await _db.SaveChangesAsync(ct);

        // Add order items
        foreach (var item in req.Items)
        {
            _db.OrderItems.Add(new OrderItem
            {
                OrderId = order.Id,
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = products[item.ProductId].Price ?? 0m
            });
        }

        // Create payment record
        _db.Payments.Add(new Payment
        {
            OrderId = order.Id,
            UserId = buyerId,
            Provider = method,
            Status = PaymentStatuses.Pending,
            Amount = pricing.GrandTotal,
            CreatedAt = DateTime.UtcNow
        });

        // Apply coupon usage
        if (!string.IsNullOrWhiteSpace(req.CouponCode))
        {
            var coupon = await _db.Coupons.FirstOrDefaultAsync(
                c => c.Code.ToUpper() == req.CouponCode.ToUpper(), ct);
            if (coupon != null) coupon.UsedCount++;
        }

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        _logger.LogInformation(
            "CreateOrder succeeded | cid={cid} | tx={tx} | orderId={orderId} | total={total}",
            _txContext.CorrelationId, _txContext.TransactionId, order.Id, pricing.GrandTotal);

        return await GetByIdAsync(buyerId, order.Id, ct);
    }

    public async Task<OrderTotalDto> CalculateAsync(int buyerId, CalculateOrderRequest req, CancellationToken ct = default)
    {
        if (req.Items == null || req.Items.Count == 0)
            throw new ValidationException("Items are required", "ORDER_ITEMS_REQUIRED");

        var productIds = req.Items.Select(x => x.ProductId).Distinct().ToList();
        var products = await _db.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);

        Address? address = null;
        if (req.AddressId.HasValue)
            address = await _db.Addresses.FirstOrDefaultAsync(a => a.Id == req.AddressId && a.UserId == buyerId, ct);

        var pricing = await CalculatePricingAsync(buyerId, address, req.Items, products, req.CouponCode, ct);

        return new OrderTotalDto(
            pricing.Subtotal,
            pricing.ShippingFee,
            pricing.DiscountAmount,
            pricing.GrandTotal,
            pricing.AppliedCouponCode);
    }

    public async Task<OrderDto> GetByIdAsync(int buyerId, int orderId, CancellationToken ct = default)
    {
        var order = await LoadOrder(orderId, ct);

        if (order.BuyerId != buyerId)
            throw new ForbiddenException("You don't have access to this order", "ORDER_FORBIDDEN");

        return MapToDto(order);
    }

    public async Task<PagedResponse<OrderDto>> GetMyOrdersAsync(int buyerId, int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = _db.Orders
            .AsNoTracking()
            .Include(o => o.Buyer)
            .Include(o => o.Address)
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
            .Include(o => o.Payments)
            .Include(o => o.ShippingInfo).ThenInclude(s => s!.TrackingEvents)
            .Where(o => o.BuyerId == buyerId)
            .OrderByDescending(o => o.OrderDate);

        var total = await query.CountAsync(ct);
        var orders = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return new PagedResponse<OrderDto>(
            orders.Select(MapToDto).ToList(),
            page, pageSize, total,
            (int)Math.Ceiling(total / (double)pageSize));
    }

    public async Task<OrderDto> CancelAsync(int buyerId, int orderId, CancellationToken ct = default)
    {
        var order = await LoadOrder(orderId, ct);

        if (order.BuyerId != buyerId)
            throw new ForbiddenException("You don't have access to this order", "ORDER_FORBIDDEN");

        if (order.Status == OrderStatuses.Cancelled)
            return MapToDto(order);

        if (order.Status == OrderStatuses.Paid || order.Status == OrderStatuses.Shipped ||
            order.Status == OrderStatuses.Delivered)
            throw new ValidationException("Cannot cancel order in current status", "ORDER_CANNOT_CANCEL");

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        order.Status = OrderStatuses.Cancelled;

        // Restore stock
        foreach (var item in order.OrderItems)
        {
            if (item.Product != null)
            {
                item.Product.StockQuantity += item.Quantity;
                item.Product.Status = "active";
            }
        }

        foreach (var payment in order.Payments)
            payment.Status = PaymentStatuses.Cancelled;

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return MapToDto(order);
    }

    // ── Private helpers ──────────────────────────────────────────────

    private async Task<Order> LoadOrder(int orderId, CancellationToken ct)
    {
        var order = await _db.Orders
            .Include(o => o.Buyer)
            .Include(o => o.Address)
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
            .Include(o => o.Payments)
            .Include(o => o.ShippingInfo).ThenInclude(s => s!.TrackingEvents)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        if (order == null)
            throw new NotFoundException("Order not found", "ORDER_NOT_FOUND");

        return order;
    }

    private static string NormalizePaymentMethod(string? method)
    {
        var m = (method ?? "").Trim().ToLowerInvariant();
        if (m != PaymentMethods.PayPal && m != PaymentMethods.Cod)
            throw new ValidationException("Payment method must be 'paypal' or 'cod'", "PAYMENT_METHOD_INVALID");
        return m;
    }

    private static async Task<OrderPricingResult> CalculatePricingAsync(
        int buyerId,
        Address? address,
        List<OrderItemRequest> items,
        Dictionary<int, Product> products,
        string? couponCode,
        CancellationToken ct)
    {
        var subtotal = items.Sum(i => (products[i.ProductId].Price ?? 0m) * i.Quantity);

        // Shipping fee based on region (simulated)
        var shippingFee = ShippingFeeCalculator.Calculate(address?.Country, subtotal);

        decimal discountAmount = 0m;
        string? appliedCode = null;

        // Intentionally return resolved result; coupon lookup is done in calling context
        // (simplified here — real app would do DB lookup)

        var grandTotal = Math.Max(0m, subtotal + shippingFee - discountAmount);

        return await Task.FromResult(new OrderPricingResult(subtotal, shippingFee, discountAmount, grandTotal, appliedCode));
    }

    private static OrderDto MapToDto(Order order)
    {
        var latestPayment = order.Payments.OrderByDescending(p => p.Id).FirstOrDefault();

        return new OrderDto(
            order.Id,
            order.BuyerId,
            order.Buyer?.FullName ?? order.Buyer?.Username,
            order.Status,
            order.PaymentMethod,
            order.SubtotalAmount,
            order.ShippingFee,
            order.DiscountAmount,
            order.TotalPrice,
            order.CouponCode,
            order.OrderDate,
            order.PaidAt,
            order.Address == null ? null : new AddressDto(
                order.Address.Id,
                order.Address.FullName,
                order.Address.Street,
                order.Address.City,
                order.Address.Province,
                order.Address.PostalCode,
                order.Address.Country,
                order.Address.Phone),
            order.OrderItems.Select(oi => new OrderItemDto(
                oi.Id,
                oi.ProductId,
                oi.Product?.Title,
                oi.Quantity,
                oi.UnitPrice,
                oi.LineTotal)).ToList(),
            latestPayment == null ? null : new PaymentSummaryDto(
                latestPayment.Id,
                latestPayment.Provider,
                latestPayment.Status,
                latestPayment.Amount,
                latestPayment.TransactionId,
                latestPayment.PaidAt),
            order.ShippingInfo == null ? null : new ShippingInfoDto(
                order.ShippingInfo.Id,
                order.ShippingInfo.OrderId,
                order.ShippingInfo.TrackingCode,
                order.ShippingInfo.Carrier,
                order.ShippingInfo.Status,
                order.ShippingInfo.LastCheckpoint,
                order.ShippingInfo.EstimatedArrival,
                order.ShippingInfo.ShippedAt,
                order.ShippingInfo.DeliveredAt,
                order.ShippingInfo.TrackingEvents.Select(e => new TrackingEventDto(
                    e.Id, e.Status, e.Description, e.Location, e.Provider, e.EventTime)).ToList()));
    }

    private sealed record OrderPricingResult(
        decimal Subtotal,
        decimal ShippingFee,
        decimal DiscountAmount,
        decimal GrandTotal,
        string? AppliedCouponCode);
}
