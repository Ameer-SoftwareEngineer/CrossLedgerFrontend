using System.Net.Http.Json;
using CrossLedgerFrontend.Api;
using CrossLedgerFrontend.Contracts;

namespace CrossLedgerFrontend.Fx;

public sealed class FxRatesApiClient
{
    private readonly HttpClient _http;

    public FxRatesApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<ApiResult<IReadOnlyList<FxRateOhlcPointResponse>>> GetOhlcAsync(
        string fromCurrency, string toCurrency, DateOnly fromDate, DateOnly toDate)
    {
        var query = $"api/v1/fx-rates/ohlc?fromCurrency={fromCurrency}&toCurrency={toCurrency}" +
            $"&fromDate={fromDate:yyyy-MM-dd}&toDate={toDate:yyyy-MM-dd}";

        var response = await HttpCall.TrySendAsync(() => _http.GetAsync(query));
        if (response is null)
            return ApiResult<IReadOnlyList<FxRateOhlcPointResponse>>.Failed(null, HttpCall.NetworkErrorMessage);

        if (!response.IsSuccessStatusCode)
        {
            var (code, message) = await ApiErrorReader.ReadAsync(response);
            return ApiResult<IReadOnlyList<FxRateOhlcPointResponse>>.Failed(code, message);
        }

        var points = await response.Content.ReadFromJsonAsync<List<FxRateOhlcPointResponse>>()
            ?? new List<FxRateOhlcPointResponse>();
        return ApiResult<IReadOnlyList<FxRateOhlcPointResponse>>.Success(points);
    }
}
