using CrmSuite.Modules.Foundation.Application.Abstractions;

namespace CrmSuite.Modules.Foundation.Application.Auditing;

/// <summary>Registered as scoped, so a handler and its filter share the same instance.</summary>
public sealed class AuditScope : IAuditScope
{
    public object? OldValues { get; set; }

    public string? EntityId { get; set; }
}
