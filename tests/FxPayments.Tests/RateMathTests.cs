using FxPayments.Api.Services;

namespace FxPayments.Tests;

public class RateMathTests
{
    [Fact]
    public void CrossRate_SameCurrency_IsOne()
    {
        Assert.Equal(1m, RateMath.CrossRate(0.92m, 0.92m));
    }

    [Fact]
    public void CrossRate_FromUsd_IsTheTargetRate()
    {
        Assert.Equal(0.92m, RateMath.CrossRate(1.00m, 0.92m));
    }

    [Fact]
    public void CrossRate_BetweenTwoNonUsdCurrencies_GoesThroughUsd()
    {
        // EUR -> GBP = 0.79 / 0.92 = 0.858695..., rounded to 6 places
        Assert.Equal(0.858696m, RateMath.CrossRate(0.92m, 0.79m));
    }

    [Theory]
    [InlineData(100, 0.92, 92.00)]
    [InlineData(1.005, 1, 1.01)]     // midpoint rounds away from zero
    [InlineData(250, 84, 21000.00)]
    public void Convert_RoundsToCents(decimal amount, decimal rate, decimal expected)
    {
        Assert.Equal(expected, RateMath.Convert(amount, rate));
    }
}
