using System.Net.Http.Json;
using CrossLedgerFrontend.Api;
using CrossLedgerFrontend.Contracts;

namespace CrossLedgerFrontend.Wallets;

public sealed class WalletApiClient
{
    private readonly HttpClient _http;

    public WalletApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<ApiResult<IReadOnlyList<WalletSummaryResponse>>> ListMineAsync()
    {
        var response = await HttpCall.TrySendAsync(() => _http.GetAsync("api/v1/wallets"));
        if (response is null)
            return ApiResult<IReadOnlyList<WalletSummaryResponse>>.Failed(null, HttpCall.NetworkErrorMessage);

        if (!response.IsSuccessStatusCode)
        {
            var (code, message) = await ApiErrorReader.ReadAsync(response);
            return ApiResult<IReadOnlyList<WalletSummaryResponse>>.Failed(code, message);
        }

        var wallets = await response.Content.ReadFromJsonAsync<List<WalletSummaryResponse>>()
            ?? new List<WalletSummaryResponse>();
        return ApiResult<IReadOnlyList<WalletSummaryResponse>>.Success(wallets);
    }

    public async Task<ApiResult<CreateWalletResponse>> CreateAsync(Guid ownerId, string currency)
    {
        var response = await HttpCall.TrySendAsync(() => _http.PostAsJsonAsync("api/v1/wallets", new CreateWalletRequest(ownerId, currency)));
        if (response is null)
            return ApiResult<CreateWalletResponse>.Failed(null, HttpCall.NetworkErrorMessage);

        if (!response.IsSuccessStatusCode)
        {
            var (code, message) = await ApiErrorReader.ReadAsync(response);
            return ApiResult<CreateWalletResponse>.Failed(code, message);
        }

        var created = await response.Content.ReadFromJsonAsync<CreateWalletResponse>()
            ?? throw new InvalidOperationException("Wallet created but the response body was empty.");
        return ApiResult<CreateWalletResponse>.Success(created);
    }
}
