namespace CrossLedgerFrontend.Contracts;

public sealed record IssueCardRequest(Guid LinkedWalletId, string CardholderName);

public sealed record CardResponse(
    Guid CardId,
    Guid LinkedWalletId,
    string Currency,
    string CardholderName,
    string Last4,
    int ExpiryMonth,
    int ExpiryYear,
    string Status,
    decimal? SpendingLimit,
    DateTimeOffset IssuedAt);

public sealed record RevealCardDetailsResponse(string Pan, string Cvv, int ExpiryMonth, int ExpiryYear);

public sealed record SetCardSpendingLimitRequest(decimal? Limit);

public sealed record SimulateCardPurchaseRequest(string MerchantName, decimal Amount);

public sealed record CardPurchaseResponse(Guid PurchaseId, Guid TransferId, string MerchantName, decimal Amount, string Currency, DateTimeOffset PostedAt);
