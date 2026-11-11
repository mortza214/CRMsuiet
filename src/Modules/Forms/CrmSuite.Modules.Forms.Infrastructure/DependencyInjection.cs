using CrmSuite.Modules.Forms.Application.Abstractions;
using CrmSuite.Modules.Forms.Application.Mapping;
using CrmSuite.Modules.Forms.Infrastructure.Persistence;
using CrmSuite.Modules.Forms.Infrastructure.Persistence.Repositories;
using CrmSuite.Modules.Forms.Infrastructure.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace CrmSuite.Modules.Forms.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddFormsInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IFormTemplateRepository, FormTemplateRepository>();
        services.AddScoped<IFormSubmissionRepository, FormSubmissionRepository>();
        services.AddScoped<IFormSubmissionReadRepository, FormSubmissionReadRepository>();
        services.AddScoped<IFormUnitOfWork, EfFormUnitOfWork>();
        services.AddScoped<IFormSchemaValidator, JsonSchemaValidator>();
        services.AddScoped<FormDtoMapper>();
        services.AddScoped<FormsSeedService>();

        return services;
    }
}
