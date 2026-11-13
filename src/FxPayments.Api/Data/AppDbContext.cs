using FxPayments.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FxPayments.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<ExchangeRate> ExchangeRates => Set<ExchangeRate>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // SQLite has no time-zone-aware type, so values come back with an unspecified kind.
        // Mark every DateTime read from the database as UTC so the API returns "...Z" timestamps.
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
    }

    private class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
        toDb => toDb.ToUniversalTime(),
        fromDb => DateTime.SpecifyKind(fromDb, DateTimeKind.Utc));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ExchangeRate>(rate =>
        {
            rate.HasKey(r => r.Currency);
            rate.Property(r => r.Currency).HasMaxLength(3);
            rate.HasData(SampleRates);
        });

        modelBuilder.Entity<Payment>(payment =>
        {
            payment.Property(p => p.Payee).HasMaxLength(100).IsRequired();
            payment.Property(p => p.FromCurrency).HasMaxLength(3).IsRequired();
            payment.Property(p => p.ToCurrency).HasMaxLength(3).IsRequired();
        });
    }

    // Illustrative sample rates (units per 1 USD), not live market data.
    private static readonly DateTime SeededAt = new(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);

    private static readonly ExchangeRate[] SampleRates =
    [
        new() { Currency = "USD", UnitsPerUsd = 1.00m,   UpdatedAtUtc = SeededAt },
        new() { Currency = "EUR", UnitsPerUsd = 0.92m,   UpdatedAtUtc = SeededAt },
        new() { Currency = "GBP", UnitsPerUsd = 0.79m,   UpdatedAtUtc = SeededAt },
        new() { Currency = "CAD", UnitsPerUsd = 1.37m,   UpdatedAtUtc = SeededAt },
        new() { Currency = "MXN", UnitsPerUsd = 18.50m,  UpdatedAtUtc = SeededAt },
        new() { Currency = "INR", UnitsPerUsd = 84.00m,  UpdatedAtUtc = SeededAt },
        new() { Currency = "JPY", UnitsPerUsd = 150.00m, UpdatedAtUtc = SeededAt },
    ];
}
