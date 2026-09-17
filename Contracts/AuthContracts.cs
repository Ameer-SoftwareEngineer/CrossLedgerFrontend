namespace CrossLedgerFrontend.Contracts;

// Mirrors CrossLedgerWeb's own wire contracts (src/CrossLedgerWeb.Shared/Auth/) by hand,
// not by referencing that project - this repo is deliberately independent of the backend
// repo's code, the same way CrossLedgerDatabase is. It talks to CrossLedgerWeb purely as
// an HTTP API with a documented contract (see the specification PDF in that repo), which
// is also what a real separately-deployed frontend does against a backend it doesn't
// share a solution with.

public sealed record RegisterRequest(string Email, string Password);

public sealed record RegisterResponse(Guid UserId, string Email);

public sealed record LoginRequest(string Email, string Password);

public sealed record TokenResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);

public sealed record RefreshTokenRequest(string RefreshToken);
