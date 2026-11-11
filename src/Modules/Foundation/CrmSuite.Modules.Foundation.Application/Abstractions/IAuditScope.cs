namespace CrmSuite.Modules.Foundation.Application.Abstractions;

/// <summary>
/// Request-scoped hand-off between a handler and <c>AuditFilter</c>. A handler can set the
/// original entity and the final entity id; the filter persists the audit record.
/// </summary>
public interface IAuditScope
{
    object? OldValues { get; set; }

    string? EntityId { get; set; }
}
