namespace CrossLedgerFrontend.Contracts;

public sealed record UserSummaryResponse(Guid Id, string Email, IReadOnlyList<string> Roles, bool IsLockedOut);

public sealed record SetUserRolesRequest(IReadOnlyList<string> Roles);

public sealed record ProviderHealthResponse(string ProviderCode, string Status, DateTimeOffset CheckedAt);

public sealed record WebhookEventResponse(string ProviderCode, string EventId, DateTimeOffset ProcessedAt);

public sealed record WebhookEventPageResponse(IReadOnlyList<WebhookEventResponse> Entries, int TotalCount, int PageNumber, int PageSize);

public sealed record SecurityAuditEventResponse(
    Guid? UserId,
    string Subject,
    string EventType,
    bool Succeeded,
    string? FailureReason,
    string? IpAddress,
    string? UserAgent,
    DateTimeOffset OccurredAt);

public sealed record SecurityAuditEventPageResponse(IReadOnlyList<SecurityAuditEventResponse> Entries, int TotalCount, int PageNumber, int PageSize);

public sealed record LedgerImbalanceResponse(string CurrencyCode, decimal NetAmount);

public sealed record WalletBalanceResponse(string Currency, decimal Balance);

public sealed record ManualTransferRequest(Guid SourceWalletId, Guid TargetWalletId, decimal SourceAmount, decimal TargetAmount);

public sealed record ManualTransferResponse(Guid TransferId, DateTimeOffset PostedAt);

public sealed record PendingRegistrationResponse(
    Guid Id,
    string Email,
    string FirstName,
    string? MiddleName,
    string LastName,
    string PhoneNumber,
    DateOnly DateOfBirth,
    string Address,
    string PermanentAddress,
    string City,
    string StateProvince,
    string Country,
    string ProofOfAddressDocumentType,
    string ProofOfAddressFileName,
    DateTimeOffset SubmittedAt)
{
    public string FullName => string.IsNullOrWhiteSpace(MiddleName) ? $"{FirstName} {LastName}" : $"{FirstName} {MiddleName} {LastName}";
}
