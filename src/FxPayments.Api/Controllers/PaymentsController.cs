using FxPayments.Api.Data;
using FxPayments.Api.Dtos;
using FxPayments.Api.Models;
using FxPayments.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FxPayments.Api.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentsController(AppDbContext db, ICurrencyConverter converter) : ControllerBase
{
    // GET /api/payments — newest first
    [HttpGet]
    public async Task<IEnumerable<Payment>> GetAll(CancellationToken cancellationToken) =>
        await db.Payments.OrderByDescending(p => p.Id).Take(100).ToListAsync(cancellationToken);

    // GET /api/payments/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Payment>> GetById(int id, CancellationToken cancellationToken)
    {
        var payment = await db.Payments.FindAsync([id], cancellationToken);
        if (payment is null) return NotFound();
        return payment;
    }

    // POST /api/payments — converts at the current rate and saves the payment
    [HttpPost]
    public async Task<ActionResult<Payment>> Create(CreatePaymentRequest request, CancellationToken cancellationToken)
    {
        Conversion conversion;
        try
        {
            conversion = await converter.ConvertAsync(
                request.Amount, request.FromCurrency, request.ToCurrency, cancellationToken);
        }
        catch (UnknownCurrencyException ex)
        {
            ModelState.AddModelError("currency", ex.Message);
            return ValidationProblem(ModelState);
        }

        var payment = new Payment
        {
            Payee = request.Payee.Trim(),
            Amount = request.Amount,
            FromCurrency = request.FromCurrency.ToUpperInvariant(),
            ToCurrency = request.ToCurrency.ToUpperInvariant(),
            Rate = conversion.Rate,
            ConvertedAmount = conversion.ConvertedAmount,
            CreatedAtUtc = DateTime.UtcNow,
        };

        db.Payments.Add(payment);
        await db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = payment.Id }, payment);
    }
}
