# FX Payments API

A small full-stack app for creating cross-currency payments. An ASP.NET Core Web API converts amounts between currencies, locks in the exchange rate on each payment, and stores everything in a database through Entity Framework Core. A plain JavaScript front end lets you get a quote, send a payment and see recent payments.

**Stack:** C# / .NET 8, ASP.NET Core Web API, Entity Framework Core (SQLite), JavaScript, HTML/CSS, xUnit, Swagger/OpenAPI, GitHub Actions

> Exchange rates are illustrative sample values seeded into the database, not live market data.

## Run it

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```bash
cd src/FxPayments.Api
dotnet run
```

Then open:
- **http://localhost:5019** for the app
- **http://localhost:5019/swagger** for the interactive API docs

The SQLite database file (`fxpayments.db`) is created and seeded automatically on first run. Delete it to start fresh.

## Run the tests

```bash
dotnet test
```

14 tests:
- **Unit tests** (`RateMathTests`): cross-rate math and rounding to cents.
- **API tests** (`ApiTests`): start the real API in memory against a throwaway database and call it over HTTP. They cover creating and fetching payments, validation errors (bad currency, zero or negative amounts), 404s, quotes and rate updates.

GitHub Actions runs restore, build and test on every push and pull request (`.github/workflows/ci.yml`).

## API

| Method | Route | What it does |
|---|---|---|
| GET | `/api/rates` | List exchange rates (units per 1 USD) |
| PUT | `/api/rates/{currency}` | Add or update a rate, e.g. `{ "unitsPerUsd": 0.93 }` |
| GET | `/api/rates/quote?from=USD&to=EUR&amount=100` | Preview a conversion without saving it |
| GET | `/api/payments` | List recent payments, newest first |
| GET | `/api/payments/{id}` | Get one payment |
| POST | `/api/payments` | Create a payment |

Example request body for `POST /api/payments`:

```json
{ "payee": "Acme Supplies", "amount": 100, "fromCurrency": "USD", "toCurrency": "EUR" }
```

Invalid input returns `400 Bad Request` with standard ProblemDetails validation errors.

## How it works

- **Rates use a USD base.** Each currency stores how many units 1 USD buys. Converting between two non-USD currencies uses the cross rate `toRate / fromRate`, rounded to 6 decimal places. Converted amounts are rounded to cents.
- **`decimal`, not `double`,** for all money and rate values, to avoid floating-point errors.
- **The rate is locked in on each payment**, so later rate changes don't alter past payments.
- **Rate math lives in `RateMath`** as pure functions, so it can be unit tested without a database.
- **Timestamps are stored and returned as UTC.** SQLite has no time-zone type, so a value converter marks them as UTC when they're read back.
- **The front end builds table rows with `textContent`,** not `innerHTML`, so user input such as payee names can't inject HTML.

## Project layout

```
src/FxPayments.Api/
  Controllers/   PaymentsController, RatesController
  Data/          AppDbContext (EF Core model, sample rate seed data)
  Dtos/          Request and response records with validation attributes
  Models/        Payment, ExchangeRate
  Services/      CurrencyConverter, RateMath
  wwwroot/       index.html, app.js, styles.css
tests/FxPayments.Tests/
  RateMathTests.cs, ApiTests.cs
```

## Ideas for next steps

- Switch the database to SQL Server (`Microsoft.EntityFrameworkCore.SqlServer`) and use EF Core migrations instead of `EnsureCreated`.
- Pull live rates from a public FX rates API on a schedule.
- Add a payment status (Pending → Sent → Settled) and an endpoint to update it.
- Add authentication so only signed-in users can create payments.
- Port the front end to TypeScript.
