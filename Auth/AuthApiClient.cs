using System.Net.Http.Json;
using CrossLedgerFrontend.Api;
using CrossLedgerFrontend.Contracts;
using Microsoft.AspNetCore.Components.Forms;

namespace CrossLedgerFrontend.Auth;

public sealed record AuthResult(bool IsSuccess, string? ErrorCode, string? Error)
{
    public static AuthResult Success() => new(true, null, null);
    public static AuthResult Failed(string? errorCode, string error) => new(false, errorCode, error);

    public bool IsAccountPendingApproval => !IsSuccess && ErrorCode == "ACCOUNT_PENDING_APPROVAL";
    public bool IsAccountRegistrationRejected => !IsSuccess && ErrorCode == "ACCOUNT_REGISTRATION_REJECTED";
}

/// <summary>Everything RegisterAsync needs beyond email/password - the KYC profile a
/// registration now collects (specification 9's Admin Console approval gate) plus the
/// proof-of-address PDF, which is why registration is multipart/form-data rather than
/// JSON like every other request this client sends.</summary>
public sealed record RegistrationDetails(
    string Email,
    string Password,
    string FullName,
    string PhoneNumber,
    DateOnly DateOfBirth,
    string Address,
    string PermanentAddress,
    string City,
    string StateProvince,
    string Country,
    string ProofOfAddressDocumentType,
    IBrowserFile ProofOfAddressFile);

/// <summary>Login is now two steps (specification 9's mandatory 2FA): credentials earn a
/// short-lived challenge, not tokens - this client uses the "Anonymous" HttpClient for
/// every step of it, same as Register, deliberately never the "Authenticated" one. A
/// wrong 2FA code legitimately comes back as a 401, and AuthorizationMessageHandler
/// (wired only onto "Authenticated") would misread that as an expired access token and
/// try to refresh-and-redirect instead of just surfacing "wrong code".</summary>
public sealed class AuthApiClient
{
    private const long MaxDocumentSizeBytes = 5 * 1024 * 1024;

    private readonly HttpClient _http;
    private readonly TokenStore _tokenStore;
    private readonly CrossLedgerAuthenticationStateProvider _authStateProvider;

    public AuthApiClient(HttpClient http, TokenStore tokenStore, CrossLedgerAuthenticationStateProvider authStateProvider)
    {
        _http = http;
        _tokenStore = tokenStore;
        _authStateProvider = authStateProvider;
    }

    public async Task<AuthResult> RegisterAsync(RegistrationDetails details)
    {
        using var content = new MultipartFormDataContent
        {
            { new StringContent(details.Email), "Email" },
            { new StringContent(details.Password), "Password" },
            { new StringContent(details.FullName), "FullName" },
            { new StringContent(details.PhoneNumber), "PhoneNumber" },
            { new StringContent(details.DateOfBirth.ToString("yyyy-MM-dd")), "DateOfBirth" },
            { new StringContent(details.Address), "Address" },
            { new StringContent(details.PermanentAddress), "PermanentAddress" },
            { new StringContent(details.City), "City" },
            { new StringContent(details.StateProvince), "StateProvince" },
            { new StringContent(details.Country), "Country" },
            { new StringContent(details.ProofOfAddressDocumentType), "ProofOfAddressDocumentType" },
        };

        await using var fileStream = details.ProofOfAddressFile.OpenReadStream(MaxDocumentSizeBytes);
        using var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
            string.IsNullOrWhiteSpace(details.ProofOfAddressFile.ContentType) ? "application/pdf" : details.ProofOfAddressFile.ContentType);
        content.Add(fileContent, "ProofOfAddress", details.ProofOfAddressFile.Name);

        var response = await HttpCall.TrySendAsync(() => _http.PostAsync("api/v1/auth/register", content));
        if (response is null)
            return AuthResult.Failed(null, HttpCall.NetworkErrorMessage);

        if (response.IsSuccessStatusCode)
            return AuthResult.Success();

        var (code, message) = await ApiErrorReader.ReadAsync(response);
        return AuthResult.Failed(code, message);
    }

    /// <summary>Credentials alone never return tokens any more - this returns the 2FA
    /// challenge that TwoFactorLoginAsync/SendTwoFactorSmsAsync/the TOTP setup pair
    /// consume to actually finish signing in.</summary>
    public async Task<ApiResult<LoginChallengeResponse>> LoginAsync(string email, string password)
    {
        var response = await HttpCall.TrySendAsync(() => _http.PostAsJsonAsync("api/v1/auth/login", new LoginRequest(email, password)));
        if (response is null)
            return ApiResult<LoginChallengeResponse>.Failed(null, HttpCall.NetworkErrorMessage);

        if (!response.IsSuccessStatusCode)
        {
            var (code, message) = await ApiErrorReader.ReadAsync(response);
            return ApiResult<LoginChallengeResponse>.Failed(code, message);
        }

        var challenge = await response.Content.ReadFromJsonAsync<LoginChallengeResponse>()
            ?? throw new InvalidOperationException("Login succeeded but the response body was empty.");
        return ApiResult<LoginChallengeResponse>.Success(challenge);
    }

    public async Task<ApiResult> SendTwoFactorSmsAsync(string challengeToken)
    {
        var response = await HttpCall.TrySendAsync(
            () => _http.PostAsJsonAsync("api/v1/auth/2fa/login/send-sms", new SendLoginSmsCodeRequest(challengeToken)));
        if (response is null)
            return ApiResult.Failed(null, HttpCall.NetworkErrorMessage);

        if (!response.IsSuccessStatusCode)
        {
            var (code, message) = await ApiErrorReader.ReadAsync(response);
            return ApiResult.Failed(code, message);
        }

        return ApiResult.Success();
    }

    /// <summary>Verifies a code from an already-enrolled method (method is "Totp" or
    /// "Sms") and, on success, stores the token pair - but deliberately does NOT notify
    /// the authentication state provider here. Login.razor may still have more of its own
    /// UI to show (e.g. recovery codes) before actually leaving /login, and
    /// AuthorizeRouteView remounts the routed component the instant auth state changes -
    /// notifying too early wipes whatever local step state Login.razor was mid-render
    /// with. The caller notifies itself, right before it navigates away.</summary>
    public async Task<ApiResult> VerifyTwoFactorAsync(string challengeToken, string method, string code)
    {
        var response = await HttpCall.TrySendAsync(
            () => _http.PostAsJsonAsync("api/v1/auth/2fa/login/verify", new VerifyTwoFactorLoginRequest(challengeToken, method, code)));
        if (response is null)
            return ApiResult.Failed(null, HttpCall.NetworkErrorMessage);

        if (!response.IsSuccessStatusCode)
        {
            var (errorCode, message) = await ApiErrorReader.ReadAsync(response);
            return ApiResult.Failed(errorCode, message);
        }

        var tokens = await response.Content.ReadFromJsonAsync<TokenResponse>()
            ?? throw new InvalidOperationException("Verification succeeded but the response body was empty.");

        await _tokenStore.SaveAsync(tokens.AccessToken, tokens.RefreshToken);

        return ApiResult.Success();
    }

    public async Task<ApiResult<BeginTotpEnrollmentResponse>> BeginTwoFactorTotpSetupAsync(string challengeToken)
    {
        var response = await HttpCall.TrySendAsync(
            () => _http.PostAsJsonAsync("api/v1/auth/2fa/login/totp/begin", new BeginTwoFactorLoginTotpSetupRequest(challengeToken)));
        if (response is null)
            return ApiResult<BeginTotpEnrollmentResponse>.Failed(null, HttpCall.NetworkErrorMessage);

        if (!response.IsSuccessStatusCode)
        {
            var (code, message) = await ApiErrorReader.ReadAsync(response);
            return ApiResult<BeginTotpEnrollmentResponse>.Failed(code, message);
        }

        var body = await response.Content.ReadFromJsonAsync<BeginTotpEnrollmentResponse>()
            ?? throw new InvalidOperationException("Setup began but the response body was empty.");
        return ApiResult<BeginTotpEnrollmentResponse>.Success(body);
    }

    /// <summary>Enrols the authenticator credential and, on success, stores the token
    /// pair like VerifyTwoFactorAsync - first-time setup and the login it was blocking
    /// both complete together. The recovery codes are shown once here and never again, so
    /// (see VerifyTwoFactorAsync's note) the auth state provider is deliberately not
    /// notified yet - Login.razor still needs to render the recovery-codes step first.</summary>
    public async Task<ApiResult<IReadOnlyList<string>>> ConfirmTwoFactorTotpSetupAsync(string challengeToken, string secret, string code)
    {
        var response = await HttpCall.TrySendAsync(() => _http.PostAsJsonAsync(
            "api/v1/auth/2fa/login/totp/confirm", new ConfirmTwoFactorLoginTotpSetupRequest(challengeToken, secret, code)));
        if (response is null)
            return ApiResult<IReadOnlyList<string>>.Failed(null, HttpCall.NetworkErrorMessage);

        if (!response.IsSuccessStatusCode)
        {
            var (errorCode, message) = await ApiErrorReader.ReadAsync(response);
            return ApiResult<IReadOnlyList<string>>.Failed(errorCode, message);
        }

        var body = await response.Content.ReadFromJsonAsync<TwoFactorLoginSetupResponse>()
            ?? throw new InvalidOperationException("Setup succeeded but the response body was empty.");

        await _tokenStore.SaveAsync(body.Tokens.AccessToken, body.Tokens.RefreshToken);

        return ApiResult<IReadOnlyList<string>>.Success(body.RecoveryCodes);
    }

    /// <summary>Tells every &lt;AuthorizeView&gt;/AuthorizeRouteView consumer to
    /// re-evaluate against the freshly-saved tokens. Callers that still have their own UI
    /// left to show after VerifyTwoFactorAsync/ConfirmTwoFactorTotpSetupAsync succeeds
    /// (Login.razor's recovery-codes step) must call this themselves, right before they
    /// navigate away - see those methods' notes for why it can't happen any earlier.</summary>
    public void NotifyAuthenticated() => _authStateProvider.NotifyUserChanged();

    public async Task LogoutAsync()
    {
        await _tokenStore.ClearAsync();
        _authStateProvider.NotifyUserChanged();
    }
}
