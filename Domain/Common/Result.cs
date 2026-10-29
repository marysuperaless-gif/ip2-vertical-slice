namespace Domain.Common;
public enum ErrorType
{
    None = 0,
    Validation,
    NotFound,
    Conflict,
    Unauthorized,
    Forbidden,
    Failure
}

public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public Error? Error { get; }

    private Result(T value)
    {
        IsSuccess = true;
        Value = value;
    }

    private Result(Error error)
    {
        IsSuccess = false;
        Error = error;
    }

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(Error error) => new(error);
    public TOut Match<TOut>(Func<T, TOut> OnSuccess, Func<Error, TOut> OnFailure) 
        => IsSuccess ? OnSuccess(Value!) : OnFailure(Error!);

}

public class Result
{
    public bool IsSuccess { get; }
    public Error? Error { get; }

    private Result(bool isSuccess, Error? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, null);
    public static Result Failure(Error error) => new(false, error);
    public TOut Match<TOut>(Func<TOut> onSuccess, Func<Error, TOut> onFailure)
        => IsSuccess ? onSuccess() : onFailure(Error!);
}

public record Error(string Code, string Message, ErrorType Type)
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.None);
    public static Error NotFound(string code, string message)
        => new(code, message, ErrorType.NotFound);
    public static Error Validation(string code, string message)
        => new(code, message, ErrorType.Validation);
    public static Error Conflict(string code, string message)
        => new(code, message, ErrorType.Conflict);
    public static Error Unauthorized(string code, string message)
        => new(code, message,ErrorType.Unauthorized);
    public static Error Forbidden(string code, string message)
        => new(code, message, ErrorType.Forbidden);
    public static Error Failure(string code, string message)
        => new(code, message,ErrorType.Failure);
}
