using System.Net.Http.Json;
using CrossLedgerFrontend.Api;
using CrossLedgerFrontend.Contracts;

namespace CrossLedgerFrontend.Auth;

public sealed record AuthResult(bool IsSuccess, string? Error)
{
    public static AuthResult Success() => new(true, null);
    public static AuthResult Failed(string error) => new(false, error);
}

public sealed class AuthApiClient
{
    private readonly HttpClient _http;
    private readonly TokenStore _tokenStore;
    private readonly CrossLedgerAuthenticationStateProvider _authStateProvider;

    public AuthApiClient(HttpClient http, TokenStore tokenStore, CrossLedgerAuthenticationStateProvider authStateProvider)
    {
        _http = http;
        _tokenStore = tokenStore;
        _authStateProvider = authStateProvider;
    }

    public async Task<AuthResult> RegisterAsync(string email, string password)
    {
        var response = await HttpCall.TrySendAsync(() => _http.PostAsJsonAsync("api/v1/auth/register", new RegisterRequest(email, password)));
        if (response is null)
            return AuthResult.Failed(HttpCall.NetworkErrorMessage);

        return response.IsSuccessStatusCode ? AuthResult.Success() : AuthResult.Failed(await ReadErrorAsync(response));
    }

    /// <summary>On success, stores the token pair and immediately notifies the
    /// authentication state provider - the caller can navigate straight to a protected
    /// page without waiting for a page reload to pick up the new identity.</summary>
    public async Task<AuthResult> LoginAsync(string email, string password)
    {
        var response = await HttpCall.TrySendAsync(() => _http.PostAsJsonAsync("api/v1/auth/login", new LoginRequest(email, password)));
        if (response is null)
            return AuthResult.Failed(HttpCall.NetworkErrorMessage);

        if (!response.IsSuccessStatusCode)
            return AuthResult.Failed(await ReadErrorAsync(response));

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

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();
            return problem?.Detail ?? problem?.Title ?? $"Request failed ({(int)response.StatusCode}).";
        }
        catch
        {
            return $"Request failed ({(int)response.StatusCode}).";
        }
    }

    private sealed record ProblemDetailsResponse(string? Title, string? Detail);
}
