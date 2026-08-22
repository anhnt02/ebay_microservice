namespace PaymentShipping.Contracts.Shipping;

// ── Requests ─────────────────────────────────────────────────────

public record CreateShipmentRequest(
    int OrderId,
    string? Carrier,
    string? Notes
);

public record UpdateShipmentStatusRequest(
    int ShippingInfoId,
    string Status,      // in_transit | out_for_delivery | delivered | exception
    string? Description,
    string? Location,
    DateTime? EventTime
);

// ── Responses ────────────────────────────────────────────────────

public record ShippingInfoDto(
    int Id,
    int OrderId,
    string TrackingCode,
    string Carrier,
    string Status,
    string? LastCheckpoint,
    DateTime? EstimatedArrival,
    DateTime? ShippedAt,
    DateTime? DeliveredAt,
    List<TrackingEventDto> TrackingEvents
);

public record TrackingEventDto(
    int Id,
    string? Status,
    string? Description,
    string? Location,
    string Provider,
    DateTime EventTime
);

public record ShippingFeeDto(
    decimal Fee,
    string Region,      // local | regional | international
    string FromCountry,
    string ToCountry
);
