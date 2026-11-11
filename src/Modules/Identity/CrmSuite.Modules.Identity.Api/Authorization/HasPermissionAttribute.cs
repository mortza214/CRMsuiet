using Microsoft.AspNetCore.Authorization;

namespace CrmSuite.Modules.Identity.Api.Authorization;

/// <summary>Declarative permission check, e.g. [HasPermission(Permissions.Identity.User.Create)].</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(string code)
    {
        Policy = $"{PermissionPolicyPrefix.Value}{code}";
    }
}
