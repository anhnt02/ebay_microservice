namespace PaymentShipping.Application.Common;

/// <summary>Các trạng thái đơn hàng</summary>
public static class OrderStatuses
{
    public const string PendingPayment = "pending_payment";
    public const string Paid           = "paid";
    public const string Processing     = "processing";
    public const string Shipped        = "shipped";
    public const string Delivered      = "delivered";
    public const string Cancelled      = "cancelled";
    public const string Completed      = "completed";
}

/// <summary>Các trạng thái thanh toán</summary>
public static class PaymentStatuses
{
    public const string Pending   = "pending";
    public const string Captured  = "captured";
    public const string Cancelled = "cancelled";
    public const string Failed    = "failed";
}

/// <summary>Các phương thức thanh toán</summary>
public static class PaymentMethods
{
    public const string PayPal = "paypal";
    public const string Cod    = "cod";
}

/// <summary>Các trạng thái vận đơn</summary>
public static class ShipmentStatuses
{
    public const string Pending         = "pending";
    public const string InTransit       = "in_transit";
    public const string OutForDelivery  = "out_for_delivery";
    public const string Delivered       = "delivered";
    public const string Exception       = "exception";
}
