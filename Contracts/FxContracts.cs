namespace CrossLedgerFrontend.Contracts;

public sealed record CreateQuoteRequest(string FromCurrency, string ToCurrency, decimal Amount);

public sealed record QuoteResponse(Guid QuoteId, string FromCurrency, string ToCurrency, decimal Rate, DateTimeOffset ExpiresAt);

public sealed record FxRateUpdate(string FromCurrency, string ToCurrency, decimal MidMarketRate, DateTimeOffset AsOf, bool IsStale);

public sealed record FxRateOhlcPointResponse(DateOnly Date, decimal Open, decimal High, decimal Low, decimal Close);
