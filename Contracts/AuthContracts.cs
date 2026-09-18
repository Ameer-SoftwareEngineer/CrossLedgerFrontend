namespace CrossLedgerFrontend.Contracts;

// Mirrors CrossLedgerWeb's own wire contracts (src/CrossLedgerWeb.Shared/Auth/) by hand,
// not by referencing that project - this repo is deliberately independent of the backend
// repo's code, the same way CrossLedgerDatabase is. It talks to CrossLedgerWeb purely as
// an HTTP API with a documented contract (see the specification PDF in that repo), which
// is also what a real separately-deployed frontend does against a backend it doesn't
// share a solution with.

// Register is multipart/form-data (it carries a PDF), so there's no RegisterRequest JSON
// record here - AuthApiClient builds a MultipartFormDataContent by hand instead.
public sealed record RegisterResponse(Guid UserId, string Email);

public sealed record LoginRequest(string Email, string Password);

public sealed record TokenResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);

public sealed record RefreshTokenRequest(string RefreshToken);

public sealed record LoginChallengeResponse(
    string ChallengeToken,
    DateTimeOffset ChallengeExpiresAt,
    bool RequiresSetup,
    IReadOnlyList<string> AvailableMethods,
    string? MaskedPhoneNumber);

public sealed record SendLoginSmsCodeRequest(string ChallengeToken);

/// <summary>Method is "Totp" or "Sms".</summary>
public sealed record VerifyTwoFactorLoginRequest(string ChallengeToken, string Method, string Code);

public sealed record BeginTwoFactorLoginTotpSetupRequest(string ChallengeToken);

public sealed record ConfirmTwoFactorLoginTotpSetupRequest(string ChallengeToken, string Secret, string Code);

public sealed record TwoFactorLoginSetupResponse(TokenResponse Tokens, IReadOnlyList<string> RecoveryCodes);
