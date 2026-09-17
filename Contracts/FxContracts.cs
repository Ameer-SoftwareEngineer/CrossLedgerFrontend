namespace CrossLedgerFrontend.Contracts;

public sealed record CreateQuoteRequest(string FromCurrency, string ToCurrency, decimal Amount);

public sealed record QuoteResponse(Guid QuoteId, string FromCurrency, string ToCurrency, decimal Rate, DateTimeOffset ExpiresAt);
