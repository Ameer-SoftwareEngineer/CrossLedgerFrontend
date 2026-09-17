using System.Net.Http.Json;
using CrossLedgerFrontend.Api;
using CrossLedgerFrontend.Contracts;

namespace CrossLedgerFrontend.Security;

public sealed class TwoFactorApiClient
{
    private const string StepUpTokenHeaderName = "X-Step-Up-Token";

    private readonly HttpClient _http;

    public TwoFactorApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<ApiResult<BeginTotpEnrollmentResponse>> BeginEnrollmentAsync()
    {
        var response = await HttpCall.TrySendAsync(() => _http.PostAsync("api/v1/auth/2fa/enroll/begin", content: null));
        if (response is null)
            return ApiResult<BeginTotpEnrollmentResponse>.Failed(null, HttpCall.NetworkErrorMessage);

        if (!response.IsSuccessStatusCode)
        {
            var (code, message) = await ApiErrorReader.ReadAsync(response);
            return ApiResult<BeginTotpEnrollmentResponse>.Failed(code, message);
        }

        var body = await response.Content.ReadFromJsonAsync<BeginTotpEnrollmentResponse>()
            ?? throw new InvalidOperationException("Enrollment began but the response body was empty.");
        return ApiResult<BeginTotpEnrollmentResponse>.Success(body);
    }

    public async Task<ApiResult<ConfirmTotpEnrollmentResponse>> ConfirmEnrollmentAsync(string secret, string code)
    {
        var response = await HttpCall.TrySendAsync(
            () => _http.PostAsJsonAsync("api/v1/auth/2fa/enroll/confirm", new ConfirmTotpEnrollmentRequest(secret, code)));
        if (response is null)
            return ApiResult<ConfirmTotpEnrollmentResponse>.Failed(null, HttpCall.NetworkErrorMessage);

        if (!response.IsSuccessStatusCode)
        {
            var (errorCode, message) = await ApiErrorReader.ReadAsync(response);
            return ApiResult<ConfirmTotpEnrollmentResponse>.Failed(errorCode, message);
        }

        var body = await response.Content.ReadFromJsonAsync<ConfirmTotpEnrollmentResponse>()
            ?? throw new InvalidOperationException("Enrollment confirmed but the response body was empty.");
        return ApiResult<ConfirmTotpEnrollmentResponse>.Success(body);
    }

    public async Task<ApiResult<StepUpTokenResponse>> RequestStepUpAsync(string operation, string code)
    {
        var response = await HttpCall.TrySendAsync(
            () => _http.PostAsJsonAsync("api/v1/auth/2fa/step-up", new RequestStepUpTokenRequest(operation, code)));
        if (response is null)
            return ApiResult<StepUpTokenResponse>.Failed(null, HttpCall.NetworkErrorMessage);

        if (!response.IsSuccessStatusCode)
        {
            var (errorCode, message) = await ApiErrorReader.ReadAsync(response);
            return ApiResult<StepUpTokenResponse>.Failed(errorCode, message);
        }

        var body = await response.Content.ReadFromJsonAsync<StepUpTokenResponse>()
            ?? throw new InvalidOperationException("Step-up succeeded but the response body was empty.");
        return ApiResult<StepUpTokenResponse>.Success(body);
    }

    public async Task<ApiResult> DisableAsync(string stepUpToken)
    {
        var response = await HttpCall.TrySendAsync(async () =>
        {
            using var message = new HttpRequestMessage(HttpMethod.Post, "api/v1/auth/2fa/disable");
            message.Headers.Add(StepUpTokenHeaderName, stepUpToken);
            return await _http.SendAsync(message);
        });
        if (response is null)
            return ApiResult.Failed(null, HttpCall.NetworkErrorMessage);

        if (!response.IsSuccessStatusCode)
        {
            var (code, msg) = await ApiErrorReader.ReadAsync(response);
            return ApiResult.Failed(code, msg);
        }

        return ApiResult.Success();
    }
}
