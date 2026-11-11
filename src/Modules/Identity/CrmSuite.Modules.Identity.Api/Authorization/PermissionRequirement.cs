using Microsoft.AspNetCore.Authorization;

namespace CrmSuite.Modules.Identity.Api.Authorization;

public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public PermissionRequirement(string permission)
    {
        Permission = permission;
    }

    public string Permission { get; }
}
