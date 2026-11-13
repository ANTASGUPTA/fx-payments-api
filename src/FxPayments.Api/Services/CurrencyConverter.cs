using FxPayments.Api.Data;

namespace FxPayments.Api.Services;

public record Conversion(decimal Rate, decimal ConvertedAmount);

public class UnknownCurrencyException(string currency)
    : Exception($"Unknown currency '{currency}'.");

public interface ICurrencyConverter
{
    Task<Conversion> ConvertAsync(decimal amount, string fromCurrency, string toCurrency,
        CancellationToken cancellationToken = default);
}

/// <summary>Looks up both currencies' USD rates and converts between them.</summary>
public class CurrencyConverter(AppDbContext db) : ICurrencyConverter
{
    public async Task<Conversion> ConvertAsync(decimal amount, string fromCurrency, string toCurrency,
        CancellationToken cancellationToken = default)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be greater than zero.");

        var fromRate = await GetUnitsPerUsdAsync(fromCurrency, cancellationToken);
        var toRate = await GetUnitsPerUsdAsync(toCurrency, cancellationToken);

        var rate = RateMath.CrossRate(fromRate, toRate);
        return new Conversion(rate, RateMath.Convert(amount, rate));
    }

    private async Task<decimal> GetUnitsPerUsdAsync(string currency, CancellationToken cancellationToken)
    {
        var code = currency.ToUpperInvariant();
        var rate = await db.ExchangeRates.FindAsync([code], cancellationToken);
        return rate?.UnitsPerUsd ?? throw new UnknownCurrencyException(code);
    }
}

/// <summary>Pure rate arithmetic, kept separate so it is easy to unit test.</summary>
public static class RateMath
{
    // Both rates are "units per 1 USD", so 1 unit of FROM buys (toRate / fromRate) units of TO.
    public static decimal CrossRate(decimal fromUnitsPerUsd, decimal toUnitsPerUsd) =>
        Math.Round(toUnitsPerUsd / fromUnitsPerUsd, 6, MidpointRounding.AwayFromZero);

    // Money is rounded to cents.
    public static decimal Convert(decimal amount, decimal rate) =>
        Math.Round(amount * rate, 2, MidpointRounding.AwayFromZero);
}
