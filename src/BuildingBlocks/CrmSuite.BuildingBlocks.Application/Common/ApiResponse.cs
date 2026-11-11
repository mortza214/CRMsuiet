namespace CrmSuite.BuildingBlocks.Application.Common;

/// <summary>
/// Standard API envelope: { success, data, message, errors, traceId }.
/// </summary>
public sealed class ApiResponse<T>
{
    private ApiResponse(bool success, T? data, string? message, IReadOnlyList<string> errors, string traceId)
    {
        Success = success;
        Data = data;
        Message = message;
        Errors = errors;
        TraceId = traceId;
    }

    public bool Success { get; }

    public T? Data { get; }

    public string? Message { get; }

    public IReadOnlyList<string> Errors { get; }

    public string TraceId { get; }

    public static ApiResponse<T> Ok(T data, string? message = null, string? traceId = null) =>
        new(true, data, message, [], traceId ?? string.Empty);

    public static ApiResponse<T> Ok(string? message = null, string? traceId = null) =>
        new(true, default, message, [], traceId ?? string.Empty);

    public static ApiResponse<T> Fail(
        string message,
        IEnumerable<string>? errors = null,
        string? traceId = null)
    {
        var normalized = errors?
            .Where(error => !string.IsNullOrWhiteSpace(error))
            .Distinct(StringComparer.Ordinal)
            .ToArray() ?? [];

        return new(
            false,
            default,
            message,
            normalized.Length > 0 ? normalized : [message],
            traceId ?? string.Empty);
    }
}
