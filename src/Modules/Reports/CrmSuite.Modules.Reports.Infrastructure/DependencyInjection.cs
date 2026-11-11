using CrmSuite.Modules.Reports.Application.Abstractions;
using CrmSuite.Modules.Reports.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace CrmSuite.Modules.Reports.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddReportsInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IReportRepository, ReportRepository>();
        return services;
    }
}
