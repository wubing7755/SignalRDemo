namespace SignalRDemo.Application.Results;

public readonly struct Result<T>
{
    public T? Value { get; }
    public string? Error { get; }
    public string? ErrorCode { get; }
    public bool IsSuccess => Error == null;

    private Result(T? value, string? error, string? errorCode)
    {
        Value = value;
        Error = error;
        ErrorCode = errorCode;
    }

    public static Result<T> Success(T value) => new(value, null, null);
    public static Result<T> Failure(string error, string? errorCode = null) => new(default, error, errorCode);

    public R Match<R>(Func<T, R> onSuccess, Func<string, R> onFailure)
        => IsSuccess ? onSuccess(Value!) : onFailure(Error!);
}

public readonly struct Result
{
    public bool IsSuccess { get; }
    public string? Error { get; }
    public string? ErrorCode { get; }

    private Result(bool isSuccess, string? error, string? errorCode)
    {
        IsSuccess = isSuccess;
        Error = error;
        ErrorCode = errorCode;
    }

    public static Result Success() => new(true, null, null);
    public static Result Failure(string error, string? errorCode = null) => new(false, error, errorCode);
}
