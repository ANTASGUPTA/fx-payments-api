namespace FxPayments.Api.Models;

/// <summary>
/// An exchange rate stored against a USD base: how many units of
/// <see cref="Currency"/> one US dollar buys (e.g. EUR = 0.92).
/// </summary>
public class ExchangeRate
{
    public string Currency { get; set; } = "";   // ISO 4217 code, e.g. "EUR" (primary key)
    public decimal UnitsPerUsd { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
