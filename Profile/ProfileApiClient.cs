using System.Net.Http.Json;
using CrossLedgerFrontend.Api;
using CrossLedgerFrontend.Contracts;

namespace CrossLedgerFrontend.Profile;

public sealed class ProfileApiClient
{
    private readonly HttpClient _http;

    public ProfileApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<ApiResult<MyProfileResponse>> GetMyProfileAsync()
    {
        var response = await HttpCall.TrySendAsync(() => _http.GetAsync("api/v1/profile/me"));
        if (response is null)
            return ApiResult<MyProfileResponse>.Failed(null, HttpCall.NetworkErrorMessage);

        if (!response.IsSuccessStatusCode)
        {
            var (code, message) = await ApiErrorReader.ReadAsync(response);
            return ApiResult<MyProfileResponse>.Failed(code, message);
        }

        var body = await response.Content.ReadFromJsonAsync<MyProfileResponse>()
            ?? throw new InvalidOperationException("Profile request succeeded but the response body was empty.");
        return ApiResult<MyProfileResponse>.Success(body);
    }
}
