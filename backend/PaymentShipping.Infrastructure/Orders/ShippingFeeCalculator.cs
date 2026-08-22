namespace PaymentShipping.Infrastructure.Orders;

/// <summary>
/// Tính phí vận chuyển dựa theo khu vực địa lý (simulated).
/// Logic: cùng quốc gia = 3$, cùng châu lục = 8$, quốc tế = 15$
/// </summary>
public static class ShippingFeeCalculator
{
    // Catalog châu lục theo quốc gia
    private static readonly Dictionary<string, string> CountryContinent = new(StringComparer.OrdinalIgnoreCase)
    {
        // Châu Á
        ["VN"] = "Asia", ["TH"] = "Asia", ["SG"] = "Asia", ["MY"] = "Asia",
        ["ID"] = "Asia", ["PH"] = "Asia", ["CN"] = "Asia", ["JP"] = "Asia",
        ["KR"] = "Asia", ["IN"] = "Asia", ["TW"] = "Asia", ["HK"] = "Asia",
        ["MM"] = "Asia", ["KH"] = "Asia", ["LA"] = "Asia", ["BD"] = "Asia",
        // Châu Âu
        ["DE"] = "Europe", ["FR"] = "Europe", ["GB"] = "Europe", ["IT"] = "Europe",
        ["ES"] = "Europe", ["NL"] = "Europe", ["PL"] = "Europe", ["SE"] = "Europe",
        ["NO"] = "Europe", ["DK"] = "Europe", ["FI"] = "Europe", ["RU"] = "Europe",
        // Châu Mỹ
        ["US"] = "Americas", ["CA"] = "Americas", ["MX"] = "Americas", ["BR"] = "Americas",
        ["AR"] = "Americas", ["CO"] = "Americas", ["CL"] = "Americas", ["PE"] = "Americas",
        // Châu Úc
        ["AU"] = "Oceania", ["NZ"] = "Oceania",
        // Châu Phi
        ["ZA"] = "Africa", ["NG"] = "Africa", ["EG"] = "Africa", ["KE"] = "Africa",
        // Trung Đông
        ["AE"] = "MiddleEast", ["SA"] = "MiddleEast", ["IL"] = "MiddleEast"
    };

    public const string DefaultCountry = "VN";
    public const string DefaultContinent = "Asia";

    /// <summary>Tính phí vận chuyển theo quốc gia người mua, có bonus freeship cho đơn lớn.</summary>
    public static decimal Calculate(string? toCountry, decimal orderSubtotal)
    {
        // Free shipping cho đơn >= 100 USD
        if (orderSubtotal >= 100m)
            return 0m;

        var from = DefaultContinent; // Seller luôn từ VN trong demo
        var to = GetContinent(toCountry ?? DefaultCountry);

        // Same country
        if (string.Equals(toCountry ?? DefaultCountry, DefaultCountry, StringComparison.OrdinalIgnoreCase))
            return 3m;

        // Same continent
        if (from == to)
            return 8m;

        // International
        return 15m;
    }

    /// <summary>Tính phí từ countryCode cụ thể sang countryCode khác.</summary>
    public static (decimal Fee, string Region) CalculateByCountry(string fromCountry, string toCountry)
    {
        if (string.Equals(fromCountry, toCountry, StringComparison.OrdinalIgnoreCase))
            return (3m, "local");

        var fromContinent = GetContinent(fromCountry);
        var toContinent   = GetContinent(toCountry);

        if (fromContinent == toContinent)
            return (8m, "regional");

        return (15m, "international");
    }

    private static string GetContinent(string countryCode) =>
        CountryContinent.TryGetValue(countryCode, out var c) ? c : "Unknown";
}
