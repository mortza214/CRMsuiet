using CrmSuite.Modules.Foundation.Api.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace CrmSuite.Modules.Foundation.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddFoundationApi(this IServiceCollection services)
    {
        services.AddScoped<AuditFilter>();
        return services;
    }
}
