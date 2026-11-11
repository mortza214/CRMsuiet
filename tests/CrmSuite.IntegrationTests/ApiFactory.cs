using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace CrmSuite.IntegrationTests;

/// <summary>
/// Starts the real host without MySQL/Hangfire dependencies. Database-backed tests will use
/// Testcontainers and swap the connection string in a later module slice.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Hangfire:Enabled"] = "false",
                ["HealthChecks:MySql:Enabled"] = "false",
                ["Swagger:Enabled"] = "false",
                ["Http:UseHttpsRedirection"] = "false",
                ["Jwt:Key"] = "TEST_ONLY_KEY_32_BYTES_MINIMUM_0123456789",
                ["ConnectionStrings:MySql"] =
                    "Server=127.0.0.1;Port=3306;Database=crm_suite_test;User=root;",
            });
        });
    }
}
