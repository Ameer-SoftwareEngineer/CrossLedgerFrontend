using System.Net.Http.Json;
using CrossLedgerFrontend.Api;
using CrossLedgerFrontend.Contracts;

namespace CrossLedgerFrontend.Transfers;

public sealed class TransferApiClient
{
    private const string StepUpTokenHeaderName = "X-Step-Up-Token";

    private readonly HttpClient _http;

    public TransferApiClient(HttpClient http)
    {
        _http = http;
    }

    /// <summary>stepUpToken is omitted on the first attempt; if the server responds with
    /// STEP_UP_REQUIRED the caller re-invokes this with the token from the step-up
    /// dialog, reusing the same idempotencyKey so a retry after step-up never double-posts
    /// (specification 2.3 and 6.3 composing together).</summary>
    public async Task<ApiResult<TransferResponse>> CreateAsync(
        CreateTransferRequest request, string idempotencyKey, string? stepUpToken = null)
    {
        var response = await HttpCall.TrySendAsync(async () =>
        {
            using var message = new HttpRequestMessage(HttpMethod.Post, "api/v1/transfers")
            {
                Content = JsonContent.Create(request),
            };
            message.Headers.Add("Idempotency-Key", idempotencyKey);
            if (stepUpToken is not null)
                message.Headers.Add(StepUpTokenHeaderName, stepUpToken);

            return await _http.SendAsync(message);
        });
        if (response is null)
            return ApiResult<TransferResponse>.Failed(null, HttpCall.NetworkErrorMessage);

        if (!response.IsSuccessStatusCode)
        {
            var (code, msg) = await ApiErrorReader.ReadAsync(response);
            return ApiResult<TransferResponse>.Failed(code, msg);
        }

        var transfer = await response.Content.ReadFromJsonAsync<TransferResponse>()
            ?? throw new InvalidOperationException("Transfer posted but the response body was empty.");
        return ApiResult<TransferResponse>.Success(transfer);
    }

    public async Task<ApiResult<IReadOnlyList<LedgerEntryDetailResponse>>> GetLedgerEntriesAsync(Guid transferId)
    {
        var response = await HttpCall.TrySendAsync(() => _http.GetAsync($"api/v1/transfers/{transferId}/ledger-entries"));
        if (response is null)
            return ApiResult<IReadOnlyList<LedgerEntryDetailResponse>>.Failed(null, HttpCall.NetworkErrorMessage);

        if (!response.IsSuccessStatusCode)
        {
            var (code, message) = await ApiErrorReader.ReadAsync(response);
            return ApiResult<IReadOnlyList<LedgerEntryDetailResponse>>.Failed(code, message);
        }

        var entries = await response.Content.ReadFromJsonAsync<List<LedgerEntryDetailResponse>>()
            ?? new List<LedgerEntryDetailResponse>();
        return ApiResult<IReadOnlyList<LedgerEntryDetailResponse>>.Success(entries);
    }
}
