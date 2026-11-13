namespace FxPayments.Api.Models;

/// <summary>A cross-currency payment, with the rate locked in when it was created.</summary>
public class Payment
{
    public int Id { get; set; }
    public string Payee { get; set; } = "";
    public decimal Amount { get; set; }
    public string FromCurrency { get; set; } = "";
    public string ToCurrency { get; set; } = "";
    public decimal Rate { get; set; }
    public decimal ConvertedAmount { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
