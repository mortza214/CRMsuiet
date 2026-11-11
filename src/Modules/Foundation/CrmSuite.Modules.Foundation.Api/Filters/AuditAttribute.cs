using Microsoft.AspNetCore.Mvc;

namespace CrmSuite.Modules.Foundation.Api.Filters;

/// <summary>Marks a controller/action whose successful write must be audited.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class AuditAttribute : ServiceFilterAttribute
{
    public AuditAttribute()
        : base(typeof(AuditFilter))
    {
    }
}
