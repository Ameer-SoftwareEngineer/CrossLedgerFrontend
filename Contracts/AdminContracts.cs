namespace CrossLedgerFrontend.Contracts;

public sealed record UserSummaryResponse(Guid Id, string Email, IReadOnlyList<string> Roles, bool IsLockedOut);

public sealed record SetUserRolesRequest(IReadOnlyList<string> Roles);

public sealed record ProviderHealthResponse(string ProviderCode, string Status, DateTimeOffset CheckedAt);

public sealed record WebhookEventResponse(string ProviderCode, string EventId, DateTimeOffset ProcessedAt);

public sealed record WebhookEventPageResponse(IReadOnlyList<WebhookEventResponse> Entries, int TotalCount, int PageNumber, int PageSize);
