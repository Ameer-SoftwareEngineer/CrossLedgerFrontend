namespace CrossLedgerFrontend.Contracts;

public sealed record ApiResult(bool IsSuccess, string? ErrorCode, string? ErrorMessage)
{
    public static ApiResult Success() => new(true, null, null);
    public static ApiResult Failed(string? code, string message) => new(false, code, message);

    public bool IsStepUpRequired => !IsSuccess && ErrorCode == "STEP_UP_REQUIRED";
}

public sealed record ApiResult<T>(bool IsSuccess, T? Value, string? ErrorCode, string? ErrorMessage)
{
    public static ApiResult<T> Success(T value) => new(true, value, null, null);
    public static ApiResult<T> Failed(string? code, string message) => new(false, default, code, message);

    /// <summary>Specification 6.3: a 403 with this code means the caller can open the
    /// step-up dialog and retry rather than surfacing a dead-end error.</summary>
    public bool IsStepUpRequired => !IsSuccess && ErrorCode == "STEP_UP_REQUIRED";
}
