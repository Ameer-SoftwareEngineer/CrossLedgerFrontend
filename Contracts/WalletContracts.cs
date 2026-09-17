namespace CrossLedgerFrontend.Contracts;

public sealed record WalletSummaryResponse(Guid WalletId, string Currency, decimal Balance);

public sealed record CreateWalletRequest(Guid OwnerId, string Currency);

public sealed record CreateWalletResponse(Guid WalletId, Guid OwnerId, string Currency);
