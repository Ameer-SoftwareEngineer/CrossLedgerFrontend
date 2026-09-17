using System.Net.Http.Json;

namespace CrossLedgerFrontend.Api;

/// <summary>Three error shapes come back from CrossLedgerWeb: ASP.NET's ProblemDetails
/// (title/detail, with the domain error code merged into the JSON root by its converter)
/// from DomainExceptionHandler, its ValidationProblemDetails variant (title plus an
/// "errors" field-name -> messages dictionary, no top-level detail) from FluentValidation
/// failures, and the flat { code, message } object RequireStepUpFilter writes directly.
/// Reading all three through the same optional-property record covers each without the
/// caller needing to know which endpoint produced it - without the "errors" dictionary,
/// a validation failure surfaced only as the generic, unhelpful "One or more validation
/// errors occurred." with no indication of which field or why.</summary>
internal static class ApiErrorReader
{
    public static async Task<(string? Code, string Message)> ReadAsync(HttpResponseMessage response)
    {
        try
        {
            var payload = await response.Content.ReadFromJsonAsync<ErrorPayload>();

            if (payload?.Errors is { Count: > 0 })
                return (payload.Code, string.Join(" ", payload.Errors.Values.SelectMany(messages => messages)));

            var message = payload?.Detail ?? payload?.Message ?? payload?.Title;
            return (payload?.Code, message ?? $"Request failed ({(int)response.StatusCode}).");
        }
        catch
        {
            return (null, $"Request failed ({(int)response.StatusCode}).");
        }
    }

    private sealed record ErrorPayload(string? Title, string? Detail, string? Code, string? Message, Dictionary<string, string[]>? Errors);
}
