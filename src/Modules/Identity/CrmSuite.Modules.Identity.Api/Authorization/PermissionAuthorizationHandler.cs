using CrmSuite.BuildingBlocks.Application.Security;
using Microsoft.AspNetCore.Authorization;

namespace CrmSuite.Modules.Identity.Api.Authorization;

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return Task.CompletedTask;
        }

        var granted =
            context.User.IsInRole(Roles.SuperAdmin)
            || context.User.HasClaim(CustomClaimTypes.Permission, Permissions.Wildcard)
            || context.User.HasClaim(CustomClaimTypes.Permission, requirement.Permission);

        if (granted)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
