namespace CrossLedgerFrontend.Contracts;

public sealed record WalletSummaryResponse(Guid WalletId, string Currency, decimal Balance);

public sealed record CreateWalletRequest(Guid OwnerId, string Currency);

public sealed record CreateWalletResponse(Guid WalletId, Guid OwnerId, string Currency);

public sealed record TransactionHistoryEntryResponse(
    Guid Id, Guid TransferId, string Direction, decimal Amount, string Currency, decimal SignedAmount, DateTimeOffset PostedAt);

public sealed record TransactionHistoryPageResponse(
    IReadOnlyList<TransactionHistoryEntryResponse> Entries, int TotalCount, int PageNumber, int PageSize);
