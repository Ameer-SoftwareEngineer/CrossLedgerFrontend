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

    /// <summary>On success, stores the token pair and immediately notifies the
    /// authentication state provider - the caller can navigate straight to a protected
    /// page without waiting for a page reload to pick up the new identity.</summary>
    public async Task<AuthResult> LoginAsync(string email, string password)
    {
        var response = await HttpCall.TrySendAsync(() => _http.PostAsJsonAsync("api/v1/auth/login", new LoginRequest(email, password)));
        if (response is null)
            return AuthResult.Failed(null, HttpCall.NetworkErrorMessage);

        if (!response.IsSuccessStatusCode)
        {
            var (code, message) = await ApiErrorReader.ReadAsync(response);
            return AuthResult.Failed(code, message);
        }

        var tokens = await response.Content.ReadFromJsonAsync<TokenResponse>()
            ?? throw new InvalidOperationException("Login succeeded but the response body was empty.");

        await _tokenStore.SaveAsync(tokens.AccessToken, tokens.RefreshToken);
        _authStateProvider.NotifyUserChanged();

        return AuthResult.Success();
    }

    public async Task LogoutAsync()
    {
        await _tokenStore.ClearAsync();
        _authStateProvider.NotifyUserChanged();
    }
}
