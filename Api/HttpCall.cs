namespace CrossLedgerFrontend.Api;

/// <summary>A WASM fetch failure (offline, DNS, a CORS block, the dev server not running
/// yet) surfaces as an HttpRequestException before any HttpResponseMessage exists - found
/// live when a slow quote request briefly failed to connect and crashed the component's
/// render tree instead of showing an error. Every API client routes its request through
/// this so that failure becomes an ApiResult like any other, never an unhandled
/// exception.</summary>
internal static class HttpCall
{
    public const string NetworkErrorMessage = "Could not reach the server. Check your connection and try again.";

    public static async Task<HttpResponseMessage?> TrySendAsync(Func<Task<HttpResponseMessage>> send)
    {
        try
        {
            return await send();
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (TaskCanceledException)
        {
            return null;
        }
    }
}
