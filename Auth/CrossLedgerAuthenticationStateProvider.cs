using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;

namespace CrossLedgerFrontend.Auth;

/// <summary>Parses the access token's own claims to build a ClaimsPrincipal for Blazor's
/// AuthorizeView/CascadingAuthenticationState - purely a UI convenience for deciding what
/// to render. It never validates the token's signature (the client has no reason to
/// distrust a token it just received from the server over HTTPS); every real
/// authorization decision still happens server-side, on every request, exactly as
/// before - a client-side claim is a display hint, never a security boundary.</summary>
public sealed class CrossLedgerAuthenticationStateProvider : AuthenticationStateProvider
{
    private static readonly ClaimsPrincipal Anonymous = new(new ClaimsIdentity());

    private readonly TokenStore _tokenStore;

    public CrossLedgerAuthenticationStateProvider(TokenStore tokenStore)
    {
        _tokenStore = tokenStore;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await _tokenStore.GetAccessTokenAsync();

        if (string.IsNullOrWhiteSpace(token))
            return new AuthenticationState(Anonymous);

        var identity = new ClaimsIdentity(ParseClaims(token), authenticationType: "jwt");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    /// <summary>Call after login/logout/refresh so every &lt;AuthorizeView&gt; and
    /// CascadingAuthenticationState consumer on the page re-renders against the new
    /// state immediately, rather than waiting for the next navigation.</summary>
    public void NotifyUserChanged() => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());

    private static IEnumerable<Claim> ParseClaims(string jwt)
    {
        var payload = jwt.Split('.')[1];
        var json = Encoding.UTF8.GetString(Base64UrlDecode(payload));
        var root = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)
            ?? throw new InvalidOperationException("The access token's payload could not be parsed.");

        foreach (var (key, value) in root)
        {
            if (value.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in value.EnumerateArray())
                    yield return new Claim(key, item.ToString());
            }
            else
            {
                yield return new Claim(key, value.ToString());
            }
        }
    }

    private static byte[] Base64UrlDecode(string input)
    {
        var padded = input.Replace('-', '+').Replace('_', '/');
        padded += (padded.Length % 4) switch
        {
            2 => "==",
            3 => "=",
            _ => string.Empty,
        };
        return Convert.FromBase64String(padded);
    }
}
