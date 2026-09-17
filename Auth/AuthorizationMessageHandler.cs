using System.Net.Http.Headers;

namespace CrossLedgerFrontend.Auth;

/// <summary>Attaches the stored access token to every outgoing request on the
/// "Authenticated" named HttpClient, so feature pages never handle the Authorization
/// header themselves. Deliberately does not attempt a refresh-and-retry on a 401 here -
/// that's the next increment's concern, once there's a protected page to actually prove
/// it against; for now a 401 surfaces to the caller like any other failed request.</summary>
public sealed class AuthorizationMessageHandler : DelegatingHandler
{
    private readonly TokenStore _tokenStore;

    public AuthorizationMessageHandler(TokenStore tokenStore)
    {
        _tokenStore = tokenStore;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _tokenStore.GetAccessTokenAsync();

        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await base.SendAsync(request, cancellationToken);
    }
}
