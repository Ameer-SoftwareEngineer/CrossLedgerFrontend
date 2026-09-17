namespace CrossLedgerFrontend.Contracts;

public sealed record BeginTotpEnrollmentResponse(string Secret, string QrCodeUri);

public sealed record ConfirmTotpEnrollmentRequest(string Secret, string Code);

public sealed record ConfirmTotpEnrollmentResponse(IReadOnlyList<string> RecoveryCodes);

public sealed record RequestStepUpTokenRequest(string Operation, string Code);

public sealed record StepUpTokenResponse(string StepUpToken, DateTimeOffset ExpiresAt);
