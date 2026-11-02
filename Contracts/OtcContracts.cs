namespace CrossLedgerFrontend.Contracts;

public sealed record CreateOtcRequestRequest(Guid SourceWalletId, string FromCurrency, string ToCurrency, decimal Amount);

public sealed record OtcRequestResponse(
    Guid OtcRequestId,
    Guid QuoteId,
    Guid SourceWalletId,
    string FromCurrency,
    string ToCurrency,
    decimal RequestedAmount,
    decimal DeskRate,
    DateTimeOffset RequestedAt,
    DateTimeOffset OfferExpiresAt,
    string Status,
    Guid? TransferId);

public sealed record AcceptOtcRequestRequest(Guid TargetWalletId, decimal Amount);

public sealed record AcceptOtcRequestResponse(
    Guid TransferId,
    decimal SourceAmount,
    string SourceCurrency,
    decimal TargetAmount,
    string TargetCurrency,
    DateTimeOffset PostedAt);
