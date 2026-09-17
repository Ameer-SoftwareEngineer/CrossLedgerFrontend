using Blazored.LocalStorage;
using CrossLedgerFrontend;
using CrossLedgerFrontend.Auth;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// The API runs on its own origin in development (its own launchSettings:
// http://localhost:5011) - the WASM app cannot just point HttpClient at its own
// BaseAddress like the default template does, since it isn't the API's host.
const string ApiBaseAddress = "http://localhost:5011/";

builder.Services.AddMudServices();
builder.Services.AddBlazoredLocalStorage();

builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<CrossLedgerAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<CrossLedgerAuthenticationStateProvider>());
builder.Services.AddScoped<TokenStore>();
builder.Services.AddScoped<AuthorizationMessageHandler>();

// Two named clients: "Anonymous" for register/login (must never carry a stale token),
// "Authenticated" for everything else (AuthorizationMessageHandler attaches the current
// one automatically, so feature pages never touch the Authorization header themselves).
builder.Services.AddHttpClient("Anonymous", client => client.BaseAddress = new Uri(ApiBaseAddress));
builder.Services.AddHttpClient("Authenticated", client => client.BaseAddress = new Uri(ApiBaseAddress))
    .AddHttpMessageHandler<AuthorizationMessageHandler>();

builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("Authenticated"));

builder.Services.AddScoped<AuthApiClient>(sp => new AuthApiClient(
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("Anonymous"),
    sp.GetRequiredService<TokenStore>(),
    sp.GetRequiredService<CrossLedgerAuthenticationStateProvider>()));

await builder.Build().RunAsync();
