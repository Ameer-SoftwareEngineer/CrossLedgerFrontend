using System.Net.Http.Json;
using CrossLedgerFrontend.Api;
using CrossLedgerFrontend.Contracts;

namespace CrossLedgerFrontend.Admin;

public sealed record KycDocumentFile(byte[] Content, string ContentType, string FileName);

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

    public async Task<ApiResult<IReadOnlyList<PendingRegistrationResponse>>> ListPendingRegistrationsAsync()
    {
        var response = await HttpCall.TrySendAsync(() => _http.GetAsync("api/v1/admin/registrations/pending"));
        if (response is null)
            return ApiResult<IReadOnlyList<PendingRegistrationResponse>>.Failed(null, HttpCall.NetworkErrorMessage);

        if (!response.IsSuccessStatusCode)
        {
            var (code, message) = await ApiErrorReader.ReadAsync(response);
            return ApiResult<IReadOnlyList<PendingRegistrationResponse>>.Failed(code, message);
        }

        var pending = await response.Content.ReadFromJsonAsync<List<PendingRegistrationResponse>>()
            ?? new List<PendingRegistrationResponse>();
        return ApiResult<IReadOnlyList<PendingRegistrationResponse>>.Success(pending);
    }

    public async Task<ApiResult> ApproveRegistrationAsync(Guid userId)
    {
        var response = await HttpCall.TrySendAsync(() => _http.PostAsync($"api/v1/admin/registrations/{userId}/approve", null));
        if (response is null)
            return ApiResult.Failed(null, HttpCall.NetworkErrorMessage);

        if (!response.IsSuccessStatusCode)
        {
            var (code, message) = await ApiErrorReader.ReadAsync(response);
            return ApiResult.Failed(code, message);
        }

        return ApiResult.Success();
    }

    public async Task<ApiResult> RejectRegistrationAsync(Guid userId)
    {
        var response = await HttpCall.TrySendAsync(() => _http.PostAsync($"api/v1/admin/registrations/{userId}/reject", null));
        if (response is null)
            return ApiResult.Failed(null, HttpCall.NetworkErrorMessage);

        if (!response.IsSuccessStatusCode)
        {
            var (code, message) = await ApiErrorReader.ReadAsync(response);
            return ApiResult.Failed(code, message);
        }

        return ApiResult.Success();
    }

    public async Task<ApiResult<KycDocumentFile>> GetKycDocumentAsync(Guid userId)
    {
        var response = await HttpCall.TrySendAsync(() => _http.GetAsync($"api/v1/admin/registrations/{userId}/document"));
        if (response is null)
            return ApiResult<KycDocumentFile>.Failed(null, HttpCall.NetworkErrorMessage);

        if (!response.IsSuccessStatusCode)
        {
            var (code, message) = await ApiErrorReader.ReadAsync(response);
            return ApiResult<KycDocumentFile>.Failed(code, message);
        }

        var bytes = await response.Content.ReadAsByteArrayAsync();
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
        var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
            ?? response.Content.Headers.ContentDisposition?.FileName
            ?? "document.pdf";

        return ApiResult<KycDocumentFile>.Success(new KycDocumentFile(bytes, contentType, fileName.Trim('"')));
    }
}
