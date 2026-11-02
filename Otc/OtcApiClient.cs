using System.Net.Http.Json;
using CrossLedgerFrontend.Api;
using CrossLedgerFrontend.Contracts;

namespace CrossLedgerFrontend.Otc;

public sealed class OtcApiClient
{
    private const string StepUpTokenHeaderName = "X-Step-Up-Token";

    private readonly HttpClient _http;

    public OtcApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<ApiResult<OtcRequestResponse>> CreateAsync(Guid sourceWalletId, string fromCurrency, string toCurrency, decimal amount)
    {
        var response = await HttpCall.TrySendAsync(
            () => _http.PostAsJsonAsync("api/v1/otc/requests", new CreateOtcRequestRequest(sourceWalletId, fromCurrency, toCurrency, amount)));
        if (response is null)
            return ApiResult<OtcRequestResponse>.Failed(null, HttpCall.NetworkErrorMessage);

        if (!response.IsSuccessStatusCode)
        {
            var (code, message) = await ApiErrorReader.ReadAsync(response);
            return ApiResult<OtcRequestResponse>.Failed(code, message);
        }

        var offer = await response.Content.ReadFromJsonAsync<OtcRequestResponse>()
            ?? throw new InvalidOperationException("OTC request created but the response body was empty.");
        return ApiResult<OtcRequestResponse>.Success(offer);
    }

    /// <summary>stepUpToken is omitted on the first attempt; if the server responds with
    /// STEP_UP_REQUIRED the caller re-invokes this with the token from the step-up
    /// dialog, reusing the same idempotencyKey so a retry after step-up never double-posts -
    /// same pattern as TransferApiClient.CreateAsync, since accepting an offer posts a
    /// transfer exactly like Send Money does.</summary>
    public async Task<ApiResult<AcceptOtcRequestResponse>> AcceptAsync(
        Guid otcRequestId, Guid targetWalletId, decimal amount, string idempotencyKey, string? stepUpToken = null)
    {
        var response = await HttpCall.TrySendAsync(async () =>
        {
            using var message = new HttpRequestMessage(HttpMethod.Post, $"api/v1/otc/requests/{otcRequestId}/accept")
            {
                Content = JsonContent.Create(new AcceptOtcRequestRequest(targetWalletId, amount)),
            };
            message.Headers.Add("Idempotency-Key", idempotencyKey);
            if (stepUpToken is not null)
                message.Headers.Add(StepUpTokenHeaderName, stepUpToken);

            return await _http.SendAsync(message);
        });
        if (response is null)
            return ApiResult<AcceptOtcRequestResponse>.Failed(null, HttpCall.NetworkErrorMessage);

        if (!response.IsSuccessStatusCode)
        {
            var (code, message) = await ApiErrorReader.ReadAsync(response);
            return ApiResult<AcceptOtcRequestResponse>.Failed(code, message);
        }

        var accepted = await response.Content.ReadFromJsonAsync<AcceptOtcRequestResponse>()
            ?? throw new InvalidOperationException("OTC request accepted but the response body was empty.");
        return ApiResult<AcceptOtcRequestResponse>.Success(accepted);
    }

    public async Task<ApiResult> RejectAsync(Guid otcRequestId)
    {
        var response = await HttpCall.TrySendAsync(() => _http.PostAsync($"api/v1/otc/requests/{otcRequestId}/reject", null));
        if (response is null)
            return ApiResult.Failed(null, HttpCall.NetworkErrorMessage);

        if (!response.IsSuccessStatusCode)
        {
            var (code, message) = await ApiErrorReader.ReadAsync(response);
            return ApiResult.Failed(code, message);
        }

        return ApiResult.Success();
    }

    public async Task<ApiResult<IReadOnlyList<OtcRequestResponse>>> ListMineAsync()
    {
        var response = await HttpCall.TrySendAsync(() => _http.GetAsync("api/v1/otc/requests"));
        if (response is null)
            return ApiResult<IReadOnlyList<OtcRequestResponse>>.Failed(null, HttpCall.NetworkErrorMessage);

        if (!response.IsSuccessStatusCode)
        {
            var (code, message) = await ApiErrorReader.ReadAsync(response);
            return ApiResult<IReadOnlyList<OtcRequestResponse>>.Failed(code, message);
        }

        var requests = await response.Content.ReadFromJsonAsync<List<OtcRequestResponse>>()
            ?? new List<OtcRequestResponse>();
        return ApiResult<IReadOnlyList<OtcRequestResponse>>.Success(requests);
    }
}
