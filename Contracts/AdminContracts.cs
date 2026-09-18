namespace CrossLedgerFrontend.Contracts;

public sealed record UserSummaryResponse(Guid Id, string Email, IReadOnlyList<string> Roles, bool IsLockedOut);

public sealed record SetUserRolesRequest(IReadOnlyList<string> Roles);

public sealed record ProviderHealthResponse(string ProviderCode, string Status, DateTimeOffset CheckedAt);

public sealed record WebhookEventResponse(string ProviderCode, string EventId, DateTimeOffset ProcessedAt);

public sealed record WebhookEventPageResponse(IReadOnlyList<WebhookEventResponse> Entries, int TotalCount, int PageNumber, int PageSize);

public sealed record PendingRegistrationResponse(
    Guid Id,
    string Email,
    string FullName,
    string PhoneNumber,
    DateOnly DateOfBirth,
    string Address,
    string PermanentAddress,
    string City,
    string StateProvince,
    string Country,
    string ProofOfAddressDocumentType,
    string ProofOfAddressFileName,
    DateTimeOffset SubmittedAt);
