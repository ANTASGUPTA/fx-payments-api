using System.ComponentModel.DataAnnotations;
using FxPayments.Api.Data;
using FxPayments.Api.Dtos;
using FxPayments.Api.Models;
using FxPayments.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FxPayments.Api.Controllers;

[ApiController]
[Route("api/rates")]
public class RatesController(AppDbContext db, ICurrencyConverter converter) : ControllerBase
{
    // GET /api/rates
    [HttpGet]
    public async Task<IEnumerable<ExchangeRate>> GetAll(CancellationToken cancellationToken) =>
        await db.ExchangeRates.OrderBy(r => r.Currency).ToListAsync(cancellationToken);

    // PUT /api/rates/EUR  { "unitsPerUsd": 0.93 } — adds or updates a rate
    [HttpPut("{currency}")]
    public async Task<ActionResult<ExchangeRate>> Upsert(
        [RegularExpression("^[A-Za-z]{3}$")] string currency,
        UpdateRateRequest request,
        CancellationToken cancellationToken)
    {
        var code = currency.ToUpperInvariant();
        var rate = await db.ExchangeRates.FindAsync([code], cancellationToken);
        if (rate is null)
        {
            rate = new ExchangeRate { Currency = code };
            db.ExchangeRates.Add(rate);
        }

        rate.UnitsPerUsd = request.UnitsPerUsd;
        rate.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return rate;
    }

    // GET /api/rates/quote?from=USD&to=EUR&amount=100 — preview a conversion without saving it
    [HttpGet("quote")]
    public async Task<ActionResult<QuoteResponse>> Quote(
        [Required, RegularExpression("^[A-Za-z]{3}$")] string from,
        [Required, RegularExpression("^[A-Za-z]{3}$")] string to,
        [Range(typeof(decimal), "0.01", "1000000")] decimal amount,
        CancellationToken cancellationToken)
    {
        try
        {
            var conversion = await converter.ConvertAsync(amount, from, to, cancellationToken);
            return new QuoteResponse(from.ToUpperInvariant(), to.ToUpperInvariant(),
                amount, conversion.Rate, conversion.ConvertedAmount);
        }
        catch (UnknownCurrencyException ex)
        {
            ModelState.AddModelError("currency", ex.Message);
            return ValidationProblem(ModelState);
        }
    }
}
