using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Catalog.Domain.Entities;
using CrmSuite.Modules.Catalog.Domain.Enums;
using CrmSuite.Modules.Customers.Domain.Entities;
using CrmSuite.Modules.Identity.Domain.Entities;
using CrmSuite.Modules.Orders.Domain.Entities;
using CrmSuite.Modules.Workflow.Application.Commands;
using CrmSuite.Modules.Workflow.Application.Dtos;
using CrmSuite.Modules.Workflow.Application.Queries;
using CrmSuite.Modules.Workflow.Application.Sales;
using CrmSuite.Modules.Workflow.Domain.Entities;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.IntegrationTests.Workflow;

public sealed class BandwidthIntakeMySqlTests
{
    [MySqlFact]
    public async Task Real_mysql_intake_and_self_report_return_to_sales_and_rollback_all_test_data()
    {
        await using var factory = new Factory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var currentUser = (TestUser)scope.ServiceProvider.GetRequiredService<FoundationCurrentUser>();
        currentUser.UserId = await db.Set<User>().Where(user => user.Username == "superadmin").Select(user => user.Id).SingleAsync();
        var customerId = await db.Set<Customer>().Select(customer => customer.Id).FirstAsync();
        var definitionId = await db.Set<WorkflowDefinition>().Where(item => item.Code == BandwidthIntake.Code).Select(item => item.Id).SingleAsync();
        var serviceId = await db.Set<Service>().Where(service => service.ServiceType == ServiceType.BandwidthDedicated
            && service.IsActive && service.WorkflowDefinitionId == definitionId && service.Prices.Any(price => price.EffectiveTo == null))
            .Select(service => service.Id).FirstAsync();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        Guid orderId = Guid.Empty;
        await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync();
            try
            {
                var created = await mediator.Send(new CreateServiceRequestCommand(customerId, serviceId,
                    """{"cooperation_type":"invoice","billing_period_months":"3","bandwidth_mb":20,"extra_ip_count":0,"substrate":"fiber"}"""));
                created.IsSuccess.Should().BeTrue(string.Join(" | ", created.Errors));
                orderId = created.Value!.OrderId;
                var instanceId = created.Value.WorkflowInstanceId!.Value;
                var workflow = await mediator.Send(new GetWorkflowInstanceQuery(instanceId));
                workflow.Value!.CurrentStateCode.Should().Be("FEASIBILITY_TRIAGE");
                var decision = await mediator.Send(new ExecuteWorkflowActionCommand(instanceId,
                    workflow.Value.AvailableTransitions.Single().Id, """{"needs_visit":"no"}"""));
                decision.IsSuccess.Should().BeTrue(string.Join(" | ", decision.Errors));
                workflow = await mediator.Send(new GetWorkflowInstanceQuery(instanceId));
                workflow.Value!.CurrentStateCode.Should().Be("FEASIBILITY_REPORT");
                var report = await mediator.Send(new ExecuteWorkflowActionCommand(instanceId,
                    workflow.Value.AvailableTransitions.Single().Id,
                    """{"is_feasible":"no","required_equipment":"تست بدون ذخیره دائمی","needs_tower":"no","pipe_meters":0,"cable_meters":0}"""));
                report.IsSuccess.Should().BeTrue(string.Join(" | ", report.Errors));
                workflow = await mediator.Send(new GetWorkflowInstanceQuery(instanceId));
                workflow.Value!.CurrentStateCode.Should().Be("SALES_RESULT");
                workflow.Value.Status.Should().Be("Running");
                workflow.Value.Tasks.Single(task => task.Status == "Pending").AssignedUserId.Should().Be(currentUser.UserId);
            }
            finally
            {
                await transaction.RollbackAsync();
                db.ChangeTracker.Clear();
            }
        });
        (await db.Set<Order>().AnyAsync(order => order.Id == orderId)).Should().BeFalse();
    }

    private sealed class Factory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?> {
                ["ConnectionStrings:MySql"] = Environment.GetEnvironmentVariable("CRMSUITE_TEST_MYSQL"),
                ["Identity:SeedOnStartup"] = "false", ["Hangfire:Enabled"] = "false", ["HealthChecks:MySql:Enabled"] = "false",
                ["Http:UseHttpsRedirection"] = "false", ["Jwt:Key"] = "TEST_ONLY_KEY_32_BYTES_MINIMUM_0123456789" }));
            builder.ConfigureServices(services => {
                services.AddScoped<FoundationCurrentUser, TestUser>();
                services.RemoveAll<AppDbContext>();
                services.AddScoped(provider => new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
                    .UseMySql(Environment.GetEnvironmentVariable("CRMSUITE_TEST_MYSQL"), new MySqlServerVersion(new Version(8, 0, 36)),
                        mysql => mysql.EnableRetryOnFailure(3)).Options, provider.GetServices<IModuleModelConfigurator>()));
            });
        }
    }

    private sealed class TestUser : FoundationCurrentUser
    {
        public Guid? UserId { get; set; }
        public string? UserName => "superadmin";
        public string? IpAddress => "127.0.0.1";
        public bool IsAuthenticated => true;
    }
}
