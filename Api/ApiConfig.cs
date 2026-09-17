namespace CrossLedgerFrontend.Api;

/// <summary>The API runs on its own origin in development (its own launchSettings:
/// http://localhost:5011) - the WASM app cannot just point at its own BaseAddress like
/// the default template does, since it isn't the API's host. Shared here so the SignalR
/// hub connection (built directly with HubConnectionBuilder, not an HttpClient) can reach
/// the same host without duplicating the literal.</summary>
public static class ApiConfig
{
    public const string BaseAddress = "http://localhost:5011/";
}
