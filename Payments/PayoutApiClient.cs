using System.Net.Http.Json;
using CrossLedgerFrontend.Api;
using CrossLedgerFrontend.Contracts;

namespace CrossLedgerFrontend.Payments;

public sealed class PayoutApiClient
{
    private readonly HttpClient _http;

    public PayoutApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<ApiResult<PayoutResponse>> CreateAsync(CreatePayoutRequest request, string idempotencyKey)
    {
        var response = await HttpCall.TrySendAsync(async () =>
        {
            using var message = new HttpRequestMessage(HttpMethod.Post, "api/v1/payouts")
            {
                Content = JsonContent.Create(request),
            };
            message.Headers.Add("Idempotency-Key", idempotencyKey);
            return await _http.SendAsync(message);
        });
        if (response is null)
            return ApiResult<PayoutResponse>.Failed(null, HttpCall.NetworkErrorMessage);

        if (!response.IsSuccessStatusCode)
        {
            var (code, message) = await ApiErrorReader.ReadAsync(response);
            return ApiResult<PayoutResponse>.Failed(code, message);
        }

        var payout = await response.Content.ReadFromJsonAsync<PayoutResponse>()
            ?? throw new InvalidOperationException("Payout created but the response body was empty.");
        return ApiResult<PayoutResponse>.Success(payout);
    }

    public async Task<ApiResult<IReadOnlyList<RoutingDecisionEntryResponse>>> GetRoutingDecisionAsync(Guid payoutId)
    {
        var response = await HttpCall.TrySendAsync(() => _http.GetAsync($"api/v1/payouts/{payoutId}/routing-decision"));
        if (response is null)
            return ApiResult<IReadOnlyList<RoutingDecisionEntryResponse>>.Failed(null, HttpCall.NetworkErrorMessage);

        if (!response.IsSuccessStatusCode)
        {
            var (code, message) = await ApiErrorReader.ReadAsync(response);
            return ApiResult<IReadOnlyList<RoutingDecisionEntryResponse>>.Failed(code, message);
        }

        var entries = await response.Content.ReadFromJsonAsync<List<RoutingDecisionEntryResponse>>()
            ?? new List<RoutingDecisionEntryResponse>();
        return ApiResult<IReadOnlyList<RoutingDecisionEntryResponse>>.Success(entries);
    }
}
