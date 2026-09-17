namespace CrossLedgerFrontend.Contracts;

public sealed record CreateTransferRequest(Guid QuoteId, Guid SourceWalletId, Guid TargetWalletId, decimal SourceAmount);

public sealed record TransferResponse(
    Guid TransferId,
    decimal SourceAmount,
    string SourceCurrency,
    decimal TargetAmount,
    string TargetCurrency,
    DateTimeOffset PostedAt);

public sealed record LedgerEntryDetailResponse(
    Guid Id, Guid TransferId, Guid WalletId, string Direction, decimal Amount, string Currency, decimal SignedAmount, DateTimeOffset PostedAt);
