using Blazored.LocalStorage;
using Blazored.SessionStorage;

namespace CrossLedgerFrontend.Auth;

/// <summary>Persists the JWT token pair (specification 3.1) - in local storage if the
/// user checked "Remember me" (survives closing the browser), otherwise in session
/// storage (cleared when the tab/browser closes). Per-viewer only, never seen by the
/// server directly.</summary>
public sealed class TokenStore
{
    private const string AccessTokenKey = "cl_access_token";
    private const string RefreshTokenKey = "cl_refresh_token";
    private const string TrustedDeviceTokenKey = "cl_trusted_device_token";

    private readonly ILocalStorageService _localStorage;
    private readonly ISessionStorageService _sessionStorage;

    public TokenStore(ILocalStorageService localStorage, ISessionStorageService sessionStorage)
    {
        _localStorage = localStorage;
        _sessionStorage = sessionStorage;
    }

    public async Task SaveAsync(string accessToken, string refreshToken, bool remember)
    {
        if (remember)
        {
            await _localStorage.SetItemAsStringAsync(AccessTokenKey, accessToken);
            await _localStorage.SetItemAsStringAsync(RefreshTokenKey, refreshToken);
            // A prior non-remembered session must not leave a stale token an attacker
            // on a shared machine could still find in session storage.
            await _sessionStorage.RemoveItemAsync(AccessTokenKey);
            await _sessionStorage.RemoveItemAsync(RefreshTokenKey);
        }
        else
        {
            await _sessionStorage.SetItemAsStringAsync(AccessTokenKey, accessToken);
            await _sessionStorage.SetItemAsStringAsync(RefreshTokenKey, refreshToken);
            await _localStorage.RemoveItemAsync(AccessTokenKey);
            await _localStorage.RemoveItemAsync(RefreshTokenKey);
        }
    }

    /// <summary>Whether the current token pair (if any) was saved with "Remember me" -
    /// AuthorizationMessageHandler's silent refresh uses this to keep writing the
    /// refreshed pair to the same store the user originally chose.</summary>
    public async Task<bool> IsRememberedAsync() =>
        !string.IsNullOrWhiteSpace(await _localStorage.GetItemAsStringAsync(AccessTokenKey));

    public async Task<string?> GetAccessTokenAsync() =>
        await _localStorage.GetItemAsStringAsync(AccessTokenKey) ?? await _sessionStorage.GetItemAsStringAsync(AccessTokenKey);

    public async Task<string?> GetRefreshTokenAsync() =>
        await _localStorage.GetItemAsStringAsync(RefreshTokenKey) ?? await _sessionStorage.GetItemAsStringAsync(RefreshTokenKey);

    public async Task ClearAsync()
    {
        await _localStorage.RemoveItemAsync(AccessTokenKey);
        await _localStorage.RemoveItemAsync(RefreshTokenKey);
        await _sessionStorage.RemoveItemAsync(AccessTokenKey);
        await _sessionStorage.RemoveItemAsync(RefreshTokenKey);
    }

    // The trusted-device grant is deliberately independent of "Remember me" (always
    // local storage, never cleared by LogoutAsync) - its whole point is to still be
    // there the next time this browser logs in, session-persistence choice or manual
    // logout notwithstanding.
    public async Task SaveTrustedDeviceTokenAsync(string token) =>
        await _localStorage.SetItemAsStringAsync(TrustedDeviceTokenKey, token);

    public async Task<string?> GetTrustedDeviceTokenAsync() =>
        await _localStorage.GetItemAsStringAsync(TrustedDeviceTokenKey);

    public async Task ClearTrustedDeviceTokenAsync() =>
        await _localStorage.RemoveItemAsync(TrustedDeviceTokenKey);
}
