using Blazored.LocalStorage;

namespace CrossLedgerFrontend.Auth;

/// <summary>Persists the JWT access token across page refreshes (specification 3.1) via
/// browser local storage - per-viewer only, never seen by the server directly.</summary>
public sealed class TokenStore
{
    private const string AccessTokenKey = "cl_access_token";
    private const string RefreshTokenKey = "cl_refresh_token";

    private readonly ILocalStorageService _localStorage;

    public TokenStore(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    public async Task SaveAsync(string accessToken, string refreshToken)
    {
        await _localStorage.SetItemAsStringAsync(AccessTokenKey, accessToken);
        await _localStorage.SetItemAsStringAsync(RefreshTokenKey, refreshToken);
    }

    public async Task<string?> GetAccessTokenAsync() =>
        await _localStorage.GetItemAsStringAsync(AccessTokenKey);

    public async Task<string?> GetRefreshTokenAsync() =>
        await _localStorage.GetItemAsStringAsync(RefreshTokenKey);

    public async Task ClearAsync()
    {
        await _localStorage.RemoveItemAsync(AccessTokenKey);
        await _localStorage.RemoveItemAsync(RefreshTokenKey);
    }
}
