using System.Net.Http.Json;
using CrossLedgerFrontend.Api;
using CrossLedgerFrontend.Contracts;

namespace CrossLedgerFrontend.Cards;

public sealed class CardApiClient
{
    private const string StepUpTokenHeaderName = "X-Step-Up-Token";

    private readonly HttpClient _http;

    public CardApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<ApiResult<CardResponse>> IssueAsync(IssueCardRequest request)
    {
        var response = await HttpCall.TrySendAsync(() => _http.PostAsJsonAsync("api/v1/cards", request));
        if (response is null)
            return ApiResult<CardResponse>.Failed(null, HttpCall.NetworkErrorMessage);

        if (!response.IsSuccessStatusCode)
        {
            var (code, message) = await ApiErrorReader.ReadAsync(response);
            return ApiResult<CardResponse>.Failed(code, message);
        }

        var card = await response.Content.ReadFromJsonAsync<CardResponse>()
            ?? throw new InvalidOperationException("Card issued but the response body was empty.");
        return ApiResult<CardResponse>.Success(card);
    }

    public async Task<ApiResult<IReadOnlyList<CardResponse>>> ListMineAsync()
    {
        var response = await HttpCall.TrySendAsync(() => _http.GetAsync("api/v1/cards"));
        if (response is null)
            return ApiResult<IReadOnlyList<CardResponse>>.Failed(null, HttpCall.NetworkErrorMessage);

        if (!response.IsSuccessStatusCode)
        {
            var (code, message) = await ApiErrorReader.ReadAsync(response);
            return ApiResult<IReadOnlyList<CardResponse>>.Failed(code, message);
        }

        var cards = await response.Content.ReadFromJsonAsync<List<CardResponse>>() ?? new List<CardResponse>();
        return ApiResult<IReadOnlyList<CardResponse>>.Success(cards);
    }

    /// <summary>Always step-up gated server-side (StepUpOperation.RevealCardDetails, no
    /// threshold) - stepUpToken is omitted on the first attempt, same retry-with-token
    /// pattern as every other step-up-gated call in this client.</summary>
    public async Task<ApiResult<RevealCardDetailsResponse>> RevealAsync(Guid cardId, string? stepUpToken = null)
    {
        var response = await HttpCall.TrySendAsync(async () =>
        {
            using var message = new HttpRequestMessage(HttpMethod.Get, $"api/v1/cards/{cardId}/reveal");
            if (stepUpToken is not null)
                message.Headers.Add(StepUpTokenHeaderName, stepUpToken);

            return await _http.SendAsync(message);
        });
        if (response is null)
            return ApiResult<RevealCardDetailsResponse>.Failed(null, HttpCall.NetworkErrorMessage);

        if (!response.IsSuccessStatusCode)
        {
            var (code, message) = await ApiErrorReader.ReadAsync(response);
            return ApiResult<RevealCardDetailsResponse>.Failed(code, message);
        }

        var details = await response.Content.ReadFromJsonAsync<RevealCardDetailsResponse>()
            ?? throw new InvalidOperationException("Reveal succeeded but the response body was empty.");
        return ApiResult<RevealCardDetailsResponse>.Success(details);
    }

    public async Task<ApiResult> FreezeAsync(Guid cardId) => await PostActionAsync($"api/v1/cards/{cardId}/freeze");

    public async Task<ApiResult> UnfreezeAsync(Guid cardId) => await PostActionAsync($"api/v1/cards/{cardId}/unfreeze");

    public async Task<ApiResult> CloseAsync(Guid cardId) => await PostActionAsync($"api/v1/cards/{cardId}/close");

    /// <summary>Always step-up gated server-side (StepUpOperation.ChangeSpendingLimits,
    /// no threshold).</summary>
    public async Task<ApiResult> SetSpendingLimitAsync(Guid cardId, decimal? limit, string? stepUpToken = null)
    {
        var response = await HttpCall.TrySendAsync(async () =>
        {
            using var message = new HttpRequestMessage(HttpMethod.Put, $"api/v1/cards/{cardId}/spending-limit")
            {
                Content = JsonContent.Create(new SetCardSpendingLimitRequest(limit)),
            };
            if (stepUpToken is not null)
                message.Headers.Add(StepUpTokenHeaderName, stepUpToken);

            return await _http.SendAsync(message);
        });
        if (response is null)
            return ApiResult.Failed(null, HttpCall.NetworkErrorMessage);

        if (!response.IsSuccessStatusCode)
        {
            var (code, message) = await ApiErrorReader.ReadAsync(response);
            return ApiResult.Failed(code, message);
        }

        return ApiResult.Success();
    }

    /// <summary>Step-up gated the same way a regular transfer is (above
    /// Limits:StepUpAbove) - same retry-with-token pattern as every other step-up-gated
    /// call in this client.</summary>
    public async Task<ApiResult<TopUpCardResponse>> TopUpAsync(Guid cardId, Guid sourceWalletId, decimal amount, string idempotencyKey, string? stepUpToken = null)
    {
        var response = await HttpCall.TrySendAsync(async () =>
        {
            using var message = new HttpRequestMessage(HttpMethod.Post, $"api/v1/cards/{cardId}/top-up")
            {
                Content = JsonContent.Create(new TopUpCardRequest(sourceWalletId, amount)),
            };
            message.Headers.Add("Idempotency-Key", idempotencyKey);
            if (stepUpToken is not null)
                message.Headers.Add(StepUpTokenHeaderName, stepUpToken);

            return await _http.SendAsync(message);
        });
        if (response is null)
            return ApiResult<TopUpCardResponse>.Failed(null, HttpCall.NetworkErrorMessage);

        if (!response.IsSuccessStatusCode)
        {
            var (code, message) = await ApiErrorReader.ReadAsync(response);
            return ApiResult<TopUpCardResponse>.Failed(code, message);
        }

        var topUp = await response.Content.ReadFromJsonAsync<TopUpCardResponse>()
            ?? throw new InvalidOperationException("Top-up succeeded but the response body was empty.");
        return ApiResult<TopUpCardResponse>.Success(topUp);
    }

    public async Task<ApiResult<CardPurchaseResponse>> SimulatePurchaseAsync(Guid cardId, string merchantName, decimal amount, string idempotencyKey)
    {
        var response = await HttpCall.TrySendAsync(async () =>
        {
            using var message = new HttpRequestMessage(HttpMethod.Post, $"api/v1/cards/{cardId}/purchases")
            {
                Content = JsonContent.Create(new SimulateCardPurchaseRequest(merchantName, amount)),
            };
            message.Headers.Add("Idempotency-Key", idempotencyKey);

            return await _http.SendAsync(message);
        });
        if (response is null)
            return ApiResult<CardPurchaseResponse>.Failed(null, HttpCall.NetworkErrorMessage);

        if (!response.IsSuccessStatusCode)
        {
            var (code, message) = await ApiErrorReader.ReadAsync(response);
            return ApiResult<CardPurchaseResponse>.Failed(code, message);
        }

        var purchase = await response.Content.ReadFromJsonAsync<CardPurchaseResponse>()
            ?? throw new InvalidOperationException("Purchase posted but the response body was empty.");
        return ApiResult<CardPurchaseResponse>.Success(purchase);
    }

    public async Task<ApiResult<IReadOnlyList<CardPurchaseResponse>>> ListPurchasesAsync(Guid cardId)
    {
        var response = await HttpCall.TrySendAsync(() => _http.GetAsync($"api/v1/cards/{cardId}/purchases"));
        if (response is null)
            return ApiResult<IReadOnlyList<CardPurchaseResponse>>.Failed(null, HttpCall.NetworkErrorMessage);

        if (!response.IsSuccessStatusCode)
        {
            var (code, message) = await ApiErrorReader.ReadAsync(response);
            return ApiResult<IReadOnlyList<CardPurchaseResponse>>.Failed(code, message);
        }

        var purchases = await response.Content.ReadFromJsonAsync<List<CardPurchaseResponse>>() ?? new List<CardPurchaseResponse>();
        return ApiResult<IReadOnlyList<CardPurchaseResponse>>.Success(purchases);
    }

    private async Task<ApiResult> PostActionAsync(string path)
    {
        var response = await HttpCall.TrySendAsync(() => _http.PostAsync(path, null));
        if (response is null)
            return ApiResult.Failed(null, HttpCall.NetworkErrorMessage);

        if (!response.IsSuccessStatusCode)
        {
            var (code, message) = await ApiErrorReader.ReadAsync(response);
            return ApiResult.Failed(code, message);
        }

        return ApiResult.Success();
    }
}
