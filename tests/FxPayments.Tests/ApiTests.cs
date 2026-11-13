using System.Net;
using System.Net.Http.Json;
using FxPayments.Api.Data;
using FxPayments.Api.Dtos;
using FxPayments.Api.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FxPayments.Tests;

/// <summary>Starts the real API in memory, pointed at a throwaway SQLite database file.</summary>
public class FxApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"fxpayments-test-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options => options.UseSqlite($"Data Source={_dbPath}"));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
    }
}

public class ApiTests(FxApiFactory factory) : IClassFixture<FxApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task CreatePayment_ValidRequest_Returns201WithConvertedAmount()
    {
        var response = await _client.PostAsJsonAsync("/api/payments",
            new CreatePaymentRequest("Acme Supplies", 100m, "usd", "EUR"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var payment = await response.Content.ReadFromJsonAsync<Payment>();
        Assert.NotNull(payment);
        Assert.Equal("USD", payment.FromCurrency);
        Assert.Equal(0.92m, payment.Rate);
        Assert.Equal(92.00m, payment.ConvertedAmount);
    }

    [Fact]
    public async Task CreatePayment_IsReturnedByGetById()
    {
        var created = await (await _client.PostAsJsonAsync("/api/payments",
            new CreatePaymentRequest("Globex", 50m, "GBP", "CAD"))).Content.ReadFromJsonAsync<Payment>();

        var fetched = await _client.GetFromJsonAsync<Payment>($"/api/payments/{created!.Id}");

        Assert.Equal("Globex", fetched!.Payee);
        Assert.Equal(created.ConvertedAmount, fetched.ConvertedAmount);
        // Timestamps read back from SQLite must still be UTC (serialized with a trailing "Z").
        Assert.Equal(DateTimeKind.Utc, fetched.CreatedAtUtc.Kind);
        Assert.Equal(created.CreatedAtUtc, fetched.CreatedAtUtc);
    }

    [Fact]
    public async Task CreatePayment_UnknownCurrency_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/payments",
            new CreatePaymentRequest("Acme Supplies", 100m, "USD", "XYZ"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-25)]
    public async Task CreatePayment_NonPositiveAmount_Returns400(decimal amount)
    {
        var response = await _client.PostAsJsonAsync("/api/payments",
            new CreatePaymentRequest("Acme Supplies", amount, "USD", "EUR"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetPayment_Missing_Returns404()
    {
        var response = await _client.GetAsync("/api/payments/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Quote_CrossCurrency_UsesUsdBase()
    {
        var quote = await _client.GetFromJsonAsync<QuoteResponse>("/api/rates/quote?from=EUR&to=GBP&amount=100");

        Assert.Equal(0.858696m, quote!.Rate);
        Assert.Equal(85.87m, quote.ConvertedAmount);
    }

    [Fact]
    public async Task UpdateRate_ChangesFutureQuotes()
    {
        var update = await _client.PutAsJsonAsync("/api/rates/CHF", new UpdateRateRequest(0.88m));
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var quote = await _client.GetFromJsonAsync<QuoteResponse>("/api/rates/quote?from=USD&to=CHF&amount=10");

        Assert.Equal(8.80m, quote!.ConvertedAmount);
    }
}
