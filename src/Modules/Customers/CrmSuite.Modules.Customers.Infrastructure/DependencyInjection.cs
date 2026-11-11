using CrmSuite.Modules.Customers.Application.Abstractions;
using CrmSuite.Modules.Customers.Application.Mapping;
using CrmSuite.Modules.Customers.Application.Options;
using CrmSuite.Modules.Customers.Infrastructure.Identity;
using CrmSuite.Modules.Customers.Infrastructure.Otp;
using CrmSuite.Modules.Customers.Infrastructure.Persistence;
using CrmSuite.Modules.Customers.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CrmSuite.Modules.Customers.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCustomersInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<OtpOptions>(configuration.GetSection(OtpOptions.SectionName));

        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ICustomerReadRepository, CustomerReadRepository>();
        services.AddScoped<ICustomerGroupRepository, CustomerGroupRepository>();
        services.AddScoped<ICustomerTagRepository, CustomerTagRepository>();
        services.AddScoped<ICustomerOtpRepository, CustomerOtpRepository>();
        services.AddScoped<ICustomerUnitOfWork, EfCustomerUnitOfWork>();
        services.AddScoped<ICustomerIdentityService, CustomerIdentityService>();
        services.AddScoped<LoggingOtpSender>();
        services.AddScoped<IppanelOtpSender>();
        services.AddScoped<IOtpSender>(provider =>
            provider.GetRequiredService<IppanelOtpSender>());
        services.AddScoped<CustomerDtoMapper>();

        return services;
    }
}
