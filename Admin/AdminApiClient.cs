using System.Net.Http.Json;
using CrossLedgerFrontend.Api;
using CrossLedgerFrontend.Contracts;

namespace CrossLedgerFrontend.Admin;

public sealed class AdminApiClient
{
    private readonly HttpClient _http;

    public AdminApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<ApiResult<IReadOnlyList<UserSummaryResponse>>> ListUsersAsync()
    {
        var response = await HttpCall.TrySendAsync(() => _http.GetAsync("api/v1/admin/users"));
        if (response is null)
            return ApiResult<IReadOnlyList<UserSummaryResponse>>.Failed(null, HttpCall.NetworkErrorMessage);

        if (!response.IsSuccessStatusCode)
        {
            var (code, message) = await ApiErrorReader.ReadAsync(response);
            return ApiResult<IReadOnlyList<UserSummaryResponse>>.Failed(code, message);
        }

        var users = await response.Content.ReadFromJsonAsync<List<UserSummaryResponse>>()
            ?? new List<UserSummaryResponse>();
        return ApiResult<IReadOnlyList<UserSummaryResponse>>.Success(users);
    }

    public async Task<ApiResult> SetUserRolesAsync(Guid userId, IReadOnlyList<string> roles)
    {
        var response = await HttpCall.TrySendAsync(
            () => _http.PutAsJsonAsync($"api/v1/admin/users/{userId}/roles", new SetUserRolesRequest(roles)));
        if (response is null)
            return ApiResult.Failed(null, HttpCall.NetworkErrorMessage);

        if (!response.IsSuccessStatusCode)
        {
            var (code, message) = await ApiErrorReader.ReadAsync(response);
            return ApiResult.Failed(code, message);
        }

        return ApiResult.Success();
    }

    public async Task<ApiResult<IReadOnlyList<ProviderHealthResponse>>> GetProviderHealthAsync()
    {
        var response = await HttpCall.TrySendAsync(() => _http.GetAsync("api/v1/admin/provider-health"));
        if (response is null)
            return ApiResult<IReadOnlyList<ProviderHealthResponse>>.Failed(null, HttpCall.NetworkErrorMessage);

        if (!response.IsSuccessStatusCode)
        {
            var (code, message) = await ApiErrorReader.ReadAsync(response);
            return ApiResult<IReadOnlyList<ProviderHealthResponse>>.Failed(code, message);
        }

        var health = await response.Content.ReadFromJsonAsync<List<ProviderHealthResponse>>()
            ?? new List<ProviderHealthResponse>();
        return ApiResult<IReadOnlyList<ProviderHealthResponse>>.Success(health);
    }

    public async Task<ApiResult<WebhookEventPageResponse>> ListWebhookEventsAsync(int pageNumber, int pageSize)
    {
        var response = await HttpCall.TrySendAsync(
            () => _http.GetAsync($"api/v1/admin/webhook-events?pageNumber={pageNumber}&pageSize={pageSize}"));
        if (response is null)
            return ApiResult<WebhookEventPageResponse>.Failed(null, HttpCall.NetworkErrorMessage);

        if (!response.IsSuccessStatusCode)
        {
            var (code, message) = await ApiErrorReader.ReadAsync(response);
            return ApiResult<WebhookEventPageResponse>.Failed(code, message);
        }

        var page = await response.Content.ReadFromJsonAsync<WebhookEventPageResponse>()
            ?? throw new InvalidOperationException("Webhook events request succeeded but the response body was empty.");
        return ApiResult<WebhookEventPageResponse>.Success(page);
    }
}
