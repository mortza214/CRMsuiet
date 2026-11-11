namespace CrmSuite.BuildingBlocks.Domain.Results;

/// <summary>
/// Explicit success/failure result used by handlers instead of throwing for expected outcomes.
/// </summary>
public class Result
{
    protected Result(bool isSuccess, string? message, IReadOnlyList<string> errors)
    {
        IsSuccess = isSuccess;
        Message = message;
        Errors = errors;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public string? Message { get; }

    public IReadOnlyList<string> Errors { get; }

    public static Result Success(string? message = null) =>
        new(true, message, Array.Empty<string>());

    public static Result Failure(string message, params string[] errors) =>
        new(false, message, Normalize(errors, message));

    public static Result Failure(IEnumerable<string> errors, string? message = null) =>
        new(false, message, Normalize(errors, message));

    public static Result<T> Success<T>(T value, string? message = null) =>
        Result<T>.Success(value, message);

    public static Result<T> Failure<T>(string message, params string[] errors) =>
        Result<T>.Failure(message, errors);

    public static Result<T> Failure<T>(IEnumerable<string> errors, string? message = null) =>
        Result<T>.Failure(errors, message);

    private static IReadOnlyList<string> Normalize(IEnumerable<string>? errors, string? message)
    {
        var normalized = errors?
            .Where(error => !string.IsNullOrWhiteSpace(error))
            .Distinct(StringComparer.Ordinal)
            .ToArray() ?? [];

        return normalized.Length > 0
            ? normalized
            : string.IsNullOrWhiteSpace(message) ? ["خطای نامشخص"] : [message];
    }
}

/// <summary>A result carrying a value when successful.</summary>
public sealed class Result<T> : Result
{
    private Result(bool isSuccess, T? value, string? message, IReadOnlyList<string> errors)
        : base(isSuccess, message, errors)
    {
        Value = value;
    }

    public T? Value { get; }

    public bool HasValue => IsSuccess && Value is not null;

    public static Result<T> Success(T value, string? message = null) =>
        new(true, value, message, Array.Empty<string>());

    public new static Result<T> Failure(string message, params string[] errors)
    {
        var normalized = errors
            .Where(error => !string.IsNullOrWhiteSpace(error))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return new(false, default, message, normalized.Length > 0 ? normalized : [message]);
    }

    public new static Result<T> Failure(IEnumerable<string> errors, string? message = null)
    {
        var normalized = errors
            .Where(error => !string.IsNullOrWhiteSpace(error))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return new(false, default, message, normalized.Length > 0 ? normalized : [message ?? "خطای نامشخص"]);
    }
}
