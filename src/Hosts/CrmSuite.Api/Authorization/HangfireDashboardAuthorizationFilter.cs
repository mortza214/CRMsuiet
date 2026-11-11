using CrmSuite.BuildingBlocks.Application.Security;
using Hangfire.Dashboard;

namespace CrmSuite.Api.Authorization;

/// <summary>The Hangfire dashboard is never anonymous, even in development.</summary>
public sealed class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        return httpContext.User.Identity?.IsAuthenticated == true
            && (httpContext.User.IsInRole(Roles.SuperAdmin) || httpContext.User.IsInRole(Roles.SystemAdmin));
    }
}
