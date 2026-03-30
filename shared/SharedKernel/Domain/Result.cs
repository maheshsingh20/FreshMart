namespace SharedKernel.Domain;

/// <summary>
/// Generic Result pattern — replaces exceptions for business logic failures.
///
/// WHY: Throwing exceptions for expected failures (wrong password, duplicate email)
/// is expensive and makes control flow hard to follow. Result makes success/failure
/// explicit in the method signature.
///
/// USAGE:
///   return Result&lt;T&gt;.Success(value);   // happy path
///   return Result&lt;T&gt;.Failure("msg");   // business rule violation
///
/// Callers check result.IsSuccess before accessing result.Value.
/// </summary>
public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }
    public IEnumerable<string> Errors { get; } = [];

    private Result(bool isSuccess, T? value, string? error, IEnumerable<string>? errors = null)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
        Errors = errors ?? [];
    }

    // Factory methods — constructor is private to force explicit intent
    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(string error) => new(false, default, error);
    public static Result<T> Failure(IEnumerable<string> errors) => new(false, default, null, errors);
}

/// <summary>
/// Non-generic Result for commands that don't return a value (void operations).
/// Used by commands like SendOtp, VerifyEmail, ResetPassword.
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public string? Error { get; }

    private Result(bool isSuccess, string? error) { IsSuccess = isSuccess; Error = error; }

    public static Result Success() => new(true, null);
    public static Result Failure(string error) => new(false, error);
}

