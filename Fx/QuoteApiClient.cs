using System.Net.Http.Json;
using CrossLedgerFrontend.Api;
using CrossLedgerFrontend.Contracts;

namespace CrossLedgerFrontend.Fx;

public sealed class QuoteApiClient
{
    private readonly HttpClient _http;

    public QuoteApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<ApiResult<QuoteResponse>> CreateAsync(string fromCurrency, string toCurrency, decimal amount)
    {
        var response = await HttpCall.TrySendAsync(
            () => _http.PostAsJsonAsync("api/v1/quotes", new CreateQuoteRequest(fromCurrency, toCurrency, amount)));
        if (response is null)
            return ApiResult<QuoteResponse>.Failed(null, HttpCall.NetworkErrorMessage);

        if (!response.IsSuccessStatusCode)
        {
            var (code, message) = await ApiErrorReader.ReadAsync(response);
            return ApiResult<QuoteResponse>.Failed(code, message);
        }

        var quote = await response.Content.ReadFromJsonAsync<QuoteResponse>()
            ?? throw new InvalidOperationException("Quote created but the response body was empty.");
        return ApiResult<QuoteResponse>.Success(quote);
    }
}
