using PaymentShipping.Contracts.Shipping;

namespace PaymentShipping.Contracts.Orders;

// ── Requests ─────────────────────────────────────────────────────

public record CreateOrderRequest(
    List<OrderItemRequest> Items,
    int? AddressId,
    string? CouponCode,
    string PaymentMethod   // "paypal" | "cod"
);

public record OrderItemRequest(
    int ProductId,
    int Quantity
);

public record CalculateOrderRequest(
    List<OrderItemRequest> Items,
    int? AddressId,
    string? CouponCode
);

// ── Responses ────────────────────────────────────────────────────

public record OrderDto(
    int Id,
    int BuyerId,
    string? BuyerName,
    string Status,
    string PaymentMethod,
    decimal SubtotalAmount,
    decimal ShippingFee,
    decimal DiscountAmount,
    decimal TotalPrice,
    string? CouponCode,
    DateTime OrderDate,
    DateTime? PaidAt,
    AddressDto? Address,
    List<OrderItemDto> Items,
    PaymentSummaryDto? LatestPayment,
    ShippingInfoDto? ShippingInfo
);

public record OrderItemDto(
    int Id,
    int ProductId,
    int SellerId,
    string? ProductTitle,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal
);

public record AddressDto(
    int Id,
    string? FullName,
    string? Street,
    string? City,
    string? Province,
    string? PostalCode,
    string? Country,
    string? Phone
);

public record PaymentSummaryDto(
    int Id,
    string Provider,
    string Status,
    decimal Amount,
    string? TransactionId,
    DateTime? PaidAt
);

public record OrderTotalDto(
    decimal SubtotalAmount,
    decimal ShippingFee,
    decimal DiscountAmount,
    decimal TotalPrice,
    string? AppliedCouponCode
);

public record PagedResponse<T>(
    List<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages
);
