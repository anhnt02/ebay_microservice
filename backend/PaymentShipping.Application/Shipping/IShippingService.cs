using PaymentShipping.Contracts.Shipping;

namespace PaymentShipping.Application.Shipping;

/// <summary>Service quản lý vận chuyển: tạo vận đơn, cập nhật tracking.</summary>
public interface IShippingService
{
    Task<ShippingInfoDto> CreateShipmentAsync(int orderId, CreateShipmentRequest request, CancellationToken ct = default);
    Task<ShippingInfoDto> UpdateStatusAsync(UpdateShipmentStatusRequest request, CancellationToken ct = default);
    Task<ShippingInfoDto> GetByOrderIdAsync(int orderId, CancellationToken ct = default);
    Task<ShippingFeeDto> CalculateFeeAsync(string fromCountry, string toCountry, CancellationToken ct = default);
}
