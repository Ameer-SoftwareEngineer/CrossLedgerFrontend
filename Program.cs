using Blazored.LocalStorage;
using CrossLedgerFrontend;
using CrossLedgerFrontend.Api;
using CrossLedgerFrontend.Auth;
using CrossLedgerFrontend.Fx;
using CrossLedgerFrontend.Security;
using CrossLedgerFrontend.Transfers;
using CrossLedgerFrontend.Wallets;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

const string ApiBaseAddress = ApiConfig.BaseAddress;

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

builder.Services.AddScoped<WalletApiClient>();
builder.Services.AddScoped<QuoteApiClient>();
builder.Services.AddScoped<TransferApiClient>();
builder.Services.AddScoped<TwoFactorApiClient>();
builder.Services.AddScoped<FxRatesApiClient>();

await builder.Build().RunAsync();
