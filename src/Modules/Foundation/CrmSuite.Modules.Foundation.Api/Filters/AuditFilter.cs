using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Foundation.Application.Auditing;
using CrmSuite.Modules.Foundation.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace CrmSuite.Modules.Foundation.Api.Filters;

/// <summary>
/// Writes an audit record after a successful POST/PUT/PATCH/DELETE action. Handlers can put the
/// original entity into <see cref="IAuditScope"/>; the request arguments become NewValues.
/// </summary>
public sealed class AuditFilter : IAsyncActionFilter
{
    private static readonly Action<ILogger, string, string, Exception?> AuditWriteFailed =
        LoggerMessage.Define<string, string>(
            LogLevel.Warning,
            new EventId(2001, "AuditWriteFailed"),
            "Could not write audit record for {EntityName}/{EntityId}.");

    private readonly IAuditService _auditService;
    private readonly IAuditScope _auditScope;
    private readonly ILogger<AuditFilter> _logger;

    public AuditFilter(
        IAuditService auditService,
        IAuditScope auditScope,
        ILogger<AuditFilter> logger)
    {
        _auditService = auditService;
        _auditScope = auditScope;
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var executed = await next();

        var action = MapAction(context.HttpContext.Request.Method);
        if (action is null)
        {
            return;
        }

        if (executed.Exception is not null && !executed.ExceptionHandled)
        {
            return;
        }

        if (executed.Result is ObjectResult { StatusCode: >= 400 })
        {
            return;
        }

        var entityName = ResolveEntityName(context.Controller);
        var entityId = _auditScope.EntityId
            ?? ResolveEntityId(context)
            ?? "unknown";

        var newValues = context.ActionArguments
            .Where(pair => pair.Value is not CancellationToken && pair.Value is not HttpContext)
            .ToDictionary(pair => pair.Key, pair => pair.Value);

        try
        {
            await _auditService.RecordAsync(
                new AuditEntry(
                    entityName,
                    entityId,
                    action.Value,
                    _auditScope.OldValues,
                    newValues),
                context.HttpContext.RequestAborted);
        }
        catch (Exception exception)
        {
            // A failed audit write must not turn a successful business operation into a 500.
            AuditWriteFailed(_logger, entityName, entityId, exception);
        }
    }

    private static AuditAction? MapAction(string method) => method.ToUpperInvariant() switch
    {
        "POST" => AuditAction.Create,
        "PUT" or "PATCH" => AuditAction.Update,
        "DELETE" => AuditAction.Delete,
        _ => null,
    };

    private static string ResolveEntityName(object controller)
    {
        var name = controller.GetType().Name;
        if (name.EndsWith("Controller", StringComparison.Ordinal))
        {
            name = name[..^"Controller".Length];
        }

        return name.EndsWith('s') ? name[..^1] : name;
    }

    private string? ResolveEntityId(ActionExecutingContext context)
    {
        foreach (var key in new[] { "id", "key" })
        {
            if (context.RouteData.Values.TryGetValue(key, out var value)
                && value is not null)
            {
                return Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture);
            }

            if (context.ActionArguments.TryGetValue(key, out var argument)
                && argument is not null)
            {
                return Convert.ToString(argument, System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        return _auditScope.EntityId;
    }
}
