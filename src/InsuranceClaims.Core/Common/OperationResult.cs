namespace InsuranceClaims.Core.Common;

/// <summary>Uniform result of a business operation without a return value.</summary>
public class OperationResult
{
    public ResultStatus Status { get; }
    public string? Error { get; }
    public bool IsSuccess => Status == ResultStatus.Success;

    protected OperationResult(ResultStatus status, string? error)
    {
        Status = status;
        Error = error;
    }

    public static OperationResult Success() => new(ResultStatus.Success, null);
    public static OperationResult NotFound(string error) => new(ResultStatus.NotFound, error);
    public static OperationResult Conflict(string error) => new(ResultStatus.Conflict, error);
    public static OperationResult Unauthorized(string error) => new(ResultStatus.Unauthorized, error);
    public static OperationResult Forbidden(string error) => new(ResultStatus.Forbidden, error);
    public static OperationResult ValidationFailed(string error) => new(ResultStatus.ValidationFailed, error);
}

/// <summary>Uniform result of a business operation that returns a value on success.</summary>
public class OperationResult<T> : OperationResult
{
    public T? Value { get; }

    private OperationResult(ResultStatus status, T? value, string? error) : base(status, error)
    {
        Value = value;
    }

    public static OperationResult<T> Success(T value) => new(ResultStatus.Success, value, null);
    public new static OperationResult<T> NotFound(string error) => new(ResultStatus.NotFound, default, error);
    public new static OperationResult<T> Conflict(string error) => new(ResultStatus.Conflict, default, error);
    public new static OperationResult<T> Unauthorized(string error) => new(ResultStatus.Unauthorized, default, error);
    public new static OperationResult<T> Forbidden(string error) => new(ResultStatus.Forbidden, default, error);
    public new static OperationResult<T> ValidationFailed(string error) => new(ResultStatus.ValidationFailed, default, error);
}
