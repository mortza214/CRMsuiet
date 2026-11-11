using System.Diagnostics;
using System.Text.Json;
using CrmSuite.BuildingBlocks.Application.Common;
using CrmSuite.BuildingBlocks.Domain.Exceptions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CrmSuite.Api.Middleware;

/// <summary>
/// Converts every unhandled exception into the standard API envelope and keeps internal
/// details out of production responses. The trace identifier is the correlation key for logs.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private static readonly Action<ILogger, int, string, Exception?> RequestWarning =
        LoggerMessage.Define<int, string>(
            LogLevel.Warning,
            new EventId(1001, "RequestWarning"),
            "Request failed with {StatusCode}. TraceId: {TraceId}");

    private static readonly Action<ILogger, int, string, Exception?> RequestError =
        LoggerMessage.Define<int, string>(
            LogLevel.Error,
            new EventId(1002, "RequestError"),
            "Request failed with {StatusCode}. TraceId: {TraceId}");

    private static readonly Action<ILogger, Exception?> ResponseAlreadyStarted =
        LoggerMessage.Define(
            LogLevel.Warning,
            new EventId(1003, "ResponseAlreadyStarted"),
            "The response has already started; the error envelope cannot be written.");

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // The client disconnected; there is no response to write.
        }
        catch (Exception exception)
        {
            await WriteErrorAsync(context, exception);
        }
    }

    private async Task WriteErrorAsync(HttpContext context, Exception exception)
    {
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
        var (statusCode, message, errors, logLevel) = Map(exception);

        if (logLevel == LogLevel.Warning)
        {
            RequestWarning(_logger, statusCode, traceId, exception);
        }
        else
        {
            RequestError(_logger, statusCode, traceId, exception);
        }

        if (context.Response.HasStarted)
        {
            ResponseAlreadyStarted(_logger, null);
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json; charset=utf-8";

        var payload = ApiResponse<object?>.Fail(message, errors, traceId);
        await context.Response.WriteAsJsonAsync(payload, context.RequestAborted);
    }

    private static (int StatusCode, string Message, string[] Errors, LogLevel LogLevel) Map(
        Exception exception)
    {
        switch (exception)
        {
            case DomainException domain:
                return (
                    StatusCodes.Status400BadRequest,
                    domain.Message,
                    domain.Errors.SelectMany(pair => pair.Value).ToArray(),
                    LogLevel.Warning);

            case ValidationException validation:
                return (
                    StatusCodes.Status400BadRequest,
                    "داده‌های ورودی معتبر نیستند.",
                    validation.Errors
                        .Select(error => string.IsNullOrWhiteSpace(error.PropertyName)
                            ? error.ErrorMessage
                            : $"{error.PropertyName}: {error.ErrorMessage}")
                        .ToArray(),
                    LogLevel.Warning);

            case BadHttpRequestException:
                return (
                    StatusCodes.Status400BadRequest,
                    "درخواست ارسال‌شده معتبر نیست.",
                    [],
                    LogLevel.Warning);

            case JsonException:
                return (
                    StatusCodes.Status400BadRequest,
                    "ساختار JSON درخواست معتبر نیست.",
                    [],
                    LogLevel.Warning);

            case UnauthorizedAccessException:
                return (
                    StatusCodes.Status403Forbidden,
                    "شما اجازه انجام این عملیات را ندارید.",
                    [],
                    LogLevel.Warning);

            case KeyNotFoundException:
                return (
                    StatusCodes.Status404NotFound,
                    "مورد درخواستی یافت نشد.",
                    [],
                    LogLevel.Warning);

            case DbUpdateConcurrencyException:
                return (
                    StatusCodes.Status409Conflict,
                    "اطلاعات توسط کاربر دیگری تغییر کرده است. لطفاً دوباره تلاش کنید.",
                    [],
                    LogLevel.Warning);

            case DbUpdateException:
                return (
                    StatusCodes.Status409Conflict,
                    "ذخیره اطلاعات به دلیل تعارض داده انجام نشد.",
                    [],
                    LogLevel.Error);

            case NotImplementedException:
                return (
                    StatusCodes.Status501NotImplemented,
                    "این قابلیت هنوز پیاده‌سازی نشده است.",
                    [],
                    LogLevel.Warning);

            default:
                return (
                    StatusCodes.Status500InternalServerError,
                    "خطای داخلی سرور رخ داده است.",
                    [],
                    LogLevel.Error);
        }
    }
}
