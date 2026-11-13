// Small vanilla-JavaScript client for the FX Payments API.

const $ = (id) => document.getElementById(id);

async function request(path, options = {}) {
  const response = await fetch(path, {
    headers: { "Content-Type": "application/json" },
    ...options,
  });
  const body = response.status === 204 ? null : await response.json();
  if (!response.ok) throw body; // ProblemDetails from the API
  return body;
}

function formValues() {
  return {
    payee: $("payee").value.trim(),
    amount: Number($("amount").value),
    fromCurrency: $("from").value,
    toCurrency: $("to").value,
  };
}

function money(value, currency) {
  return `${Number(value).toFixed(2)} ${currency}`;
}

function showError(problem) {
  // Validation errors arrive as { errors: { field: [messages] } }
  const messages = problem?.errors
    ? Object.values(problem.errors).flat()
    : [problem?.title ?? "Something went wrong."];
  $("error").textContent = messages.join(" ");
  $("error").hidden = false;
}

function clearMessages() {
  $("error").hidden = true;
  $("quote").hidden = true;
}

async function loadRates() {
  const rates = await request("/api/rates");
  for (const select of [$("from"), $("to")]) {
    select.replaceChildren(...rates.map((r) => new Option(r.currency, r.currency)));
  }
  $("from").value = "USD";
  $("to").value = "EUR";
}

async function loadPayments() {
  const payments = await request("/api/payments");
  const rows = payments.map((p) => {
    const row = document.createElement("tr");
    const cells = [
      p.id,
      p.payee,
      money(p.amount, p.fromCurrency),
      p.rate,
      money(p.convertedAmount, p.toCurrency),
      new Date(p.createdAtUtc).toISOString().slice(0, 16).replace("T", " "),
    ];
    for (const value of cells) {
      const cell = document.createElement("td");
      cell.textContent = value; // textContent, not innerHTML, so payee names can't inject HTML
      row.appendChild(cell);
    }
    return row;
  });
  $("payments").replaceChildren(...rows);
}

async function getQuote() {
  clearMessages();
  const { amount, fromCurrency, toCurrency } = formValues();
  try {
    const params = new URLSearchParams({ from: fromCurrency, to: toCurrency, amount });
    const quote = await request(`/api/rates/quote?${params}`);
    $("quote").textContent =
      `${money(quote.amount, quote.fromCurrency)} = ${money(quote.convertedAmount, quote.toCurrency)} ` +
      `(rate ${quote.rate})`;
    $("quote").hidden = false;
  } catch (problem) {
    showError(problem);
  }
}

async function sendPayment(event) {
  event.preventDefault();
  clearMessages();
  try {
    await request("/api/payments", { method: "POST", body: JSON.stringify(formValues()) });
    $("payment-form").reset();
    $("from").value = "USD";
    $("to").value = "EUR";
    await loadPayments();
  } catch (problem) {
    showError(problem);
  }
}

$("quote-btn").addEventListener("click", getQuote);
$("payment-form").addEventListener("submit", sendPayment);

loadRates().then(loadPayments).catch(showError);
