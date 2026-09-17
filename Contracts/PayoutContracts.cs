namespace CrossLedgerFrontend.Contracts;

public sealed record CreatePayoutRequest(Guid SourceWalletId, string TargetCurrency, string DestinationCountry, decimal Amount, string Preference);

public sealed record PayoutResponse(Guid PayoutId, Guid TransferId, string State, string? ProviderCode, string? ProviderReference);

public sealed record RoutingDecisionEntryResponse(
    string ProviderCode, int Rank, decimal Score, decimal FeeAmount, string FeeCurrency, double EstimatedSettlementMinutes, DateTimeOffset RecordedAt);
