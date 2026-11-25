namespace CrossLedgerFrontend.Api;

/// <summary>The API runs on its own origin in development (its own launchSettings:
/// http://localhost:5011) - the WASM app cannot just point at its own BaseAddress like
/// the default template does, since it isn't the API's host. Shared here so the SignalR
/// hub connection (built directly with HubConnectionBuilder, not an HttpClient) can reach
/// the same host without duplicating the literal.
///
/// Set once in Program.cs from the standard Blazor WASM configuration mechanism
/// (wwwroot/appsettings.json for dev, wwwroot/appsettings.Production.json for a real
/// deploy - WebAssemblyHostBuilder.CreateDefault loads the right one automatically based
/// on build configuration), not hardcoded - a hardcoded http://localhost value here would
/// ship into every build, dev and "production" alike, with no way to ever point this app
/// at a real https:// host without editing and recompiling source.</summary>
public static class ApiConfig
{
    public static string BaseAddress { get; set; } = "http://localhost:5011/";
}
