namespace Illumin360.SharedKernel;

/// <summary>
/// Outcome of an operation that may fail in an expected way. Preferred over throwing
/// for anticipated failures (charter Part 18: <c>Result&lt;T&gt;</c> over exceptions).
/// </summary>
/// <typeparam name="T">The success payload type.</typeparam>
public readonly record struct Result<T>
{
    private Result(bool isSuccess, T? value, Error? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    /// <summary>Whether the operation succeeded.</summary>
    public bool IsSuccess { get; }

    /// <summary>Whether the operation failed.</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>The success value. Only valid when <see cref="IsSuccess"/> is true.</summary>
    public T? Value { get; }

    /// <summary>The error. Only valid when <see cref="IsFailure"/> is true.</summary>
    public Error? Error { get; }

    /// <summary>Creates a successful result.</summary>
    public static Result<T> Success(T value) => new(true, value, null);

    /// <summary>Creates a failed result.</summary>
    public static Result<T> Failure(Error error) => new(false, default, error);

    /// <summary>Implicitly wraps a value into a successful result.</summary>
    public static implicit operator Result<T>(T value) => Success(value);

    /// <summary>Implicitly wraps an error into a failed result.</summary>
    public static implicit operator Result<T>(Error error) => Failure(error);
}

/// <summary>A machine-readable error with a stable code and human-readable message.</summary>
/// <param name="Code">Stable, dotted error code (e.g. <c>candidate.not_found</c>).</param>
/// <param name="Message">Human-readable description.</param>
/// <param name="Type">The category of failure, used to map to HTTP status codes.</param>
public sealed record Error(string Code, string Message, ErrorType Type = ErrorType.Failure)
{
    /// <summary>A "not found" error for the given entity/key.</summary>
    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);

    /// <summary>A validation error.</summary>
    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);

    /// <summary>A conflict error (e.g. concurrency / uniqueness).</summary>
    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);
}

/// <summary>Failure category used to translate <see cref="Error"/> into transport status codes.</summary>
public enum ErrorType
{
    /// <summary>Generic, unexpected failure.</summary>
    Failure,
    /// <summary>Input validation failure.</summary>
    Validation,
    /// <summary>Requested resource was not found.</summary>
    NotFound,
    /// <summary>State conflict (concurrency / uniqueness).</summary>
    Conflict,
}
