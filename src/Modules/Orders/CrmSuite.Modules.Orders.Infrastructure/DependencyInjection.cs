using CrmSuite.Modules.Orders.Application.Abstractions;
using CrmSuite.Modules.Orders.Application.Mapping;
using CrmSuite.Modules.Orders.Infrastructure.Persistence;
using CrmSuite.Modules.Orders.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace CrmSuite.Modules.Orders.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddOrdersInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOrderReadRepository, OrderReadRepository>();
        services.AddScoped<IOrderUnitOfWork, EfOrderUnitOfWork>();
        services.AddScoped<ICustomerServiceRepository, CustomerServiceRepository>();
        services.AddScoped<OrderDtoMapper>();

        return services;
    }
}
