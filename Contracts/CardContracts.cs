namespace CrossLedgerFrontend.Contracts;

public sealed record CardAddressDto(string Street, string Number, string State, string Postcode, string City, string Country);

/// <summary>ShippingAddress null means "same as registered".</summary>
public sealed record IssueCardRequest(
    string Currency,
    string CardType,
    string CardholderName,
    string SourceOfFunds,
    decimal EstimatedGrossAnnualIncome,
    decimal EstimatedNetWorth,
    bool IsActivelyEmployed,
    string? EmployerCompanyName,
    string? JobTitle,
    CardAddressDto RegisteredAddress,
    CardAddressDto? ShippingAddress);

public sealed record CardResponse(
    Guid CardId,
    Guid CardWalletId,
    string Currency,
    string CardType,
    string CardholderName,
    string Last4,
    int ExpiryMonth,
    int ExpiryYear,
    string Status,
    decimal? SpendingLimit,
    decimal Balance,
    DateTimeOffset IssuedAt);

public sealed record RevealCardDetailsResponse(string Pan, string Cvv, int ExpiryMonth, int ExpiryYear);

public sealed record SetCardSpendingLimitRequest(decimal? Limit);

public sealed record TopUpCardRequest(Guid SourceWalletId, decimal Amount);

public sealed record TopUpCardResponse(Guid TransferId, decimal Amount, string Currency, decimal NewCardBalance, DateTimeOffset PostedAt);

public sealed record SimulateCardPurchaseRequest(string MerchantName, decimal Amount);

public sealed record CardPurchaseResponse(Guid PurchaseId, Guid TransferId, string MerchantName, decimal Amount, string Currency, DateTimeOffset PostedAt);
