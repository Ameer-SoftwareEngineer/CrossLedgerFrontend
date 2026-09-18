using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CrossLedgerFrontend.Contracts;
using Microsoft.AspNetCore.Components;

namespace CrossLedgerFrontend.Auth;

/// <summary>Attaches the stored access token to every outgoing request on the
/// "Authenticated" named HttpClient, so feature pages never handle the Authorization
/// header themselves. On a 401 (the access token has expired - it's short-lived by
/// design), it transparently exchanges the refresh token for a new pair and retries the
/// request once, so a page load doesn't surface a raw 401 just because the token aged out
/// mid-session. Concurrent 401s from several in-flight requests share a single refresh via
/// the semaphore instead of racing the endpoint. If the refresh token has also expired, the
/// stored tokens are cleared and the user is sent back to /login.</summary>
public sealed class AuthorizationMessageHandler : DelegatingHandler
{
    private readonly TokenStore _tokenStore;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly CrossLedgerAuthenticationStateProvider _authStateProvider;
    private readonly NavigationManager _navigation;
    private static readonly SemaphoreSlim RefreshLock = new(1, 1);

    public AuthorizationMessageHandler(
        TokenStore tokenStore,
        IHttpClientFactory httpClientFactory,
        CrossLedgerAuthenticationStateProvider authStateProvider,
        NavigationManager navigation)
    {
        _tokenStore = tokenStore;
        _httpClientFactory = httpClientFactory;
        _authStateProvider = authStateProvider;
        _navigation = navigation;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _tokenStore.GetAccessTokenAsync();
        Attach(request, token);

        var response = await base.SendAsync(request, cancellationToken);
        if (response.StatusCode != HttpStatusCode.Unauthorized)
            return response;

        var refreshedToken = await TryRefreshAsync(token, cancellationToken);
        if (refreshedToken is null)
        {
            await _tokenStore.ClearAsync();
            _authStateProvider.NotifyUserChanged();
            _navigation.NavigateTo("/login", forceLoad: false);
            return response;
        }

        response.Dispose();
        var retryRequest = await CloneAsync(request, refreshedToken);
        return await base.SendAsync(retryRequest, cancellationToken);
    }

    private async Task<string?> TryRefreshAsync(string? tokenUsedForFailedRequest, CancellationToken cancellationToken)
    {
        await RefreshLock.WaitAsync(cancellationToken);
        try
        {
            // Another in-flight request may have already refreshed while we waited.
            var current = await _tokenStore.GetAccessTokenAsync();
            if (!string.IsNullOrWhiteSpace(current) && current != tokenUsedForFailedRequest)
                return current;

            var refreshToken = await _tokenStore.GetRefreshTokenAsync();
            if (string.IsNullOrWhiteSpace(refreshToken))
                return null;

            var client = _httpClientFactory.CreateClient("Anonymous");
            var response = await client.PostAsJsonAsync("api/v1/auth/refresh", new RefreshTokenRequest(refreshToken), cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;

            var tokens = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: cancellationToken);
            if (tokens is null)
                return null;

            await _tokenStore.SaveAsync(tokens.AccessToken, tokens.RefreshToken);
            _authStateProvider.NotifyUserChanged();
            return tokens.AccessToken;
        }
        catch
        {
            return null;
        }
        finally
        {
            RefreshLock.Release();
        }
    }

    private static void Attach(HttpRequestMessage request, string? token)
    {
        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private static async Task<HttpRequestMessage> CloneAsync(HttpRequestMessage request, string token)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri) { Version = request.Version };

        if (request.Content is not null)
        {
            var buffer = await request.Content.ReadAsByteArrayAsync();
            clone.Content = new ByteArrayContent(buffer);
            foreach (var header in request.Content.Headers)
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        foreach (var header in request.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

        Attach(clone, token);
        return clone;
    }
}
