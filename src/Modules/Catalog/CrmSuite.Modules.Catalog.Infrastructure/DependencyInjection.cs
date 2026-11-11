using CrmSuite.Modules.Catalog.Application.Abstractions;
using CrmSuite.Modules.Catalog.Application.Mapping;
using CrmSuite.Modules.Catalog.Application.Services;
using CrmSuite.Modules.Catalog.Infrastructure.Persistence;
using CrmSuite.Modules.Catalog.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace CrmSuite.Modules.Catalog.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCatalogInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IServiceRepository, ServiceRepository>();
        services.AddScoped<ICatalogReadRepository, CatalogReadRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IAddonRepository, AddonRepository>();
        services.AddScoped<IDataCenterItemRepository, DataCenterItemRepository>();
        services.AddScoped<IServiceUsageGuard, ServiceUsageGuard>();
        services.AddScoped<IServiceFormLinkRepository, ServiceFormLinkRepository>();
        services.AddScoped<ICatalogUnitOfWork, EfCatalogUnitOfWork>();
        services.AddScoped<CatalogDtoMapper>();
        services.AddScoped<IServicePricingService, ServicePricingService>();

        return services;
    }
}
