using System.ComponentModel.DataAnnotations;

namespace FxPayments.Api.Dtos;

// [ApiController] checks these attributes automatically and returns
// 400 Bad Request with the validation errors if any fail.

public record CreatePaymentRequest(
    [Required, StringLength(100)] string Payee,
    [Range(typeof(decimal), "0.01", "1000000")] decimal Amount,
    [Required, RegularExpression("^[A-Za-z]{3}$", ErrorMessage = "Use a 3-letter currency code.")] string FromCurrency,
    [Required, RegularExpression("^[A-Za-z]{3}$", ErrorMessage = "Use a 3-letter currency code.")] string ToCurrency);

public record UpdateRateRequest(
    [Range(typeof(decimal), "0.000001", "1000000")] decimal UnitsPerUsd);

public record QuoteResponse(
    string FromCurrency,
    string ToCurrency,
    decimal Amount,
    decimal Rate,
    decimal ConvertedAmount);
