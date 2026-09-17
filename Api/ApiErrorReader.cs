using System.Net.Http.Json;

namespace CrossLedgerFrontend.Api;

/// <summary>Two error shapes come back from CrossLedgerWeb: ASP.NET's ProblemDetails
/// (title/detail, with the domain error code merged into the JSON root by its converter)
/// from DomainExceptionHandler, and the flat { code, message } object RequireStepUpFilter
/// writes directly. Reading both through the same optional-property record covers each
/// without the caller needing to know which endpoint produced it.</summary>
internal static class ApiErrorReader
{
    public static async Task<(string? Code, string Message)> ReadAsync(HttpResponseMessage response)
    {
        try
        {
            var payload = await response.Content.ReadFromJsonAsync<ErrorPayload>();
            var message = payload?.Detail ?? payload?.Message ?? payload?.Title;
            return (payload?.Code, message ?? $"Request failed ({(int)response.StatusCode}).");
        }
        catch
        {
            return (null, $"Request failed ({(int)response.StatusCode}).");
        }
    }

    private sealed record ErrorPayload(string? Title, string? Detail, string? Code, string? Message);
}
