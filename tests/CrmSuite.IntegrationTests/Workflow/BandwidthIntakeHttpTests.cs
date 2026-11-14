using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using CrmSuite.BuildingBlocks.Application.Security;
using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Catalog.Domain.Entities;
using CrmSuite.Modules.Catalog.Domain.Enums;
using CrmSuite.Modules.Customers.Domain.Entities;
using CrmSuite.Modules.Customers.Domain.Enums;
using CrmSuite.Modules.Forms.Domain.Entities;
using CrmSuite.Modules.Forms.Infrastructure.Persistence;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Identity.Domain.Entities;
using CrmSuite.Modules.Identity.Infrastructure.Security;
using CrmSuite.Modules.Workflow.Application.Dtos;
using CrmSuite.Modules.Workflow.Application.Sales;
using CrmSuite.Modules.Workflow.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using CrmSuite.Modules.Identity.Application.Abstractions;

namespace CrmSuite.IntegrationTests.Workflow;

public sealed class BandwidthIntakeHttpTests
{
    [Fact]
    public async Task Sales_department_member_can_submit_without_a_sales_role()
    {
        await using var factory = new Factory(); await factory.SeedAsync();
        using var salesMember = factory.Client(factory.SalesDepartmentMemberId, Roles.Support);
        var created = await Read<ServiceRequestResultDto>(await salesMember.PostAsJsonAsync("/api/sales/service-requests", new {
            customerId = factory.CustomerId, serviceId = factory.ServiceId, attributesJson = RequestData("cable") }));
        (await Get(salesMember, created.WorkflowInstanceId!.Value)).CurrentStateCode.Should().Be("FEASIBILITY_TRIAGE");
    }
    [Theory]
    [InlineData("wireless", "yes", "yes")]
    [InlineData("fiber", "no", "no")]
    [InlineData("cable", "yes", "yes")]
    public async Task Customer_request_requires_sales_approval_and_expert_result_requires_manager_review(string substrate, string outcome, string visit)
    {
        await using var factory = new Factory();
        await factory.SeedAsync();
        using var customer = factory.Client(factory.CustomerUserId, Roles.Customer);
        using var sales = factory.Client(factory.SalesId, Roles.SalesExpert);
        using var manager = factory.Client(factory.ManagerId, Roles.TechnicalManager);
        using var expert = factory.Client(factory.ExpertId, Roles.TechnicalExpert);
        var created = await Read<ServiceRequestResultDto>(await customer.PostAsJsonAsync("/api/portal/sales/requests", new {
            serviceId = factory.ServiceId, customerId = Guid.NewGuid(), attributesJson = RequestData(substrate) }));
        var id = created.WorkflowInstanceId!.Value;
        var workflow = await Get(sales, id);
        workflow.CurrentStateCode.Should().Be("SALES_REVIEW");
        var customerAttempt = await customer.PostAsJsonAsync($"/api/portal/workflow/instances/{id}/action", new {
            transitionId = workflow.AvailableTransitions[0].Id, actionDataJson = "{}" });
        customerAttempt.IsSuccessStatusCode.Should().BeFalse();
        await Move(sales, id, "FEASIBILITY_TRIAGE", "{}");
        await Move(manager, id, "FEASIBILITY_REPORT", JsonSerializer.Serialize(new { needs_visit = visit, expert_user_id = factory.ExpertId.ToString() }));
        workflow = await Get(expert, id);
        workflow.Tasks.Last(task => task.Status == "Pending").AssignedUserId.Should().Be(factory.ExpertId);
        workflow.AvailableTransitions.Should().ContainSingle();
        await Move(expert, id, "FEASIBILITY_REVIEW", ResultData(outcome));
        workflow = await Get(manager, id);
        workflow.Tasks.Last(task => task.Status == "Pending").AssignedUserId.Should().BeNull("the selected expert must not receive the manager review");
        await Move(manager, id, "FEASIBILITY_REPORT", ResultData(outcome));
        workflow = await Get(expert, id);
        workflow.Tasks.Last(task => task.Status == "Pending").AssignedUserId.Should().Be(factory.ExpertId);
        await Move(expert, id, "FEASIBILITY_REVIEW", ResultData(outcome));
        await Move(manager, id, "SALES_RESULT", ResultData(outcome));
        workflow = await Get(sales, id);
        workflow.Status.Should().Be("Running");
        workflow.CurrentStateCode.Should().Be("SALES_RESULT");
        workflow.Tasks.Last(task => task.Status == "Pending").AssignedUserId.Should().Be(factory.SalesId);
        workflow.Tasks.Last(task => task.Status == "Pending").AssigneeName.Should().Be("فروش آزمایشی");
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.Set<CrmSuite.Modules.Orders.Domain.Entities.Order>().SingleAsync(item => item.Id == created.OrderId)).CustomerId.Should().Be(factory.CustomerId);
        var forms = await db.Set<FormSubmission>().Where(item => item.EntityId == created.OrderId).ToListAsync();
        forms.Should().HaveCount(3);
        forms.Single(item => item.SubmittedDataJson.Contains("is_feasible")).SubmittedDataJson.Should().Contain(outcome);
        (await db.Set<FormSubmissionHistory>().CountAsync()).Should().BeGreaterThan(3);
    }

    [Fact]
    public async Task Sales_request_starts_at_technical_manager_and_self_report_returns_to_its_sales_author()
    {
        await using var factory = new Factory(); await factory.SeedAsync();
        using var sales = factory.Client(factory.SalesId, Roles.SalesExpert);
        using var manager = factory.Client(factory.ManagerId, Roles.TechnicalManager);
        var created = await Read<ServiceRequestResultDto>(await sales.PostAsJsonAsync("/api/sales/service-requests", new {
            customerId = factory.CustomerId, serviceId = factory.ServiceId, attributesJson = RequestData("fiber") }));
        var id = created.WorkflowInstanceId!.Value;
        (await Get(manager, id)).CurrentStateCode.Should().Be("FEASIBILITY_TRIAGE");
        await Move(manager, id, "FEASIBILITY_REPORT", """{"needs_visit":"no"}""");
        var invalid = await manager.PostAsJsonAsync($"/api/workflow/instances/{id}/action", new {
            transitionId = (await Get(manager, id)).AvailableTransitions.Single().Id, actionDataJson = "{}" });
        invalid.IsSuccessStatusCode.Should().BeFalse("a result form is mandatory even for the manager");
        await Move(manager, id, "SALES_RESULT", ResultData("no"));
        var workflow = await Get(sales, id);
        workflow.Tasks.Last(task => task.Status == "Pending").AssignedUserId.Should().Be(factory.SalesId);
        using var admin = factory.Client(factory.AdminId, Roles.SuperAdmin);
        var reopen = await admin.PostAsync($"/api/workflow/definitions/{workflow.DefinitionId}/reopen", null);
        reopen.IsSuccessStatusCode.Should().BeFalse("definitions with any request must remain locked");
    }

    [Fact]
    public async Task Unused_workflow_can_be_reopened_and_invalid_wireless_request_is_not_saved()
    {
        await using var factory = new Factory(); await factory.SeedAsync();
        using var sales = factory.Client(factory.SalesId, Roles.SalesExpert);
        var response = await sales.PostAsJsonAsync("/api/sales/service-requests", new {
            customerId = factory.CustomerId, serviceId = factory.ServiceId,
            attributesJson = """{"substrate":"wireless","bandwidth_mb":20}""" });
        response.IsSuccessStatusCode.Should().BeFalse();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.Set<CrmSuite.Modules.Orders.Domain.Entities.Order>().CountAsync()).Should().Be(0);
        var definition = await db.Set<CrmSuite.Modules.Workflow.Domain.Entities.WorkflowDefinition>().SingleAsync(item => item.Code == BandwidthIntake.Code);
        using var admin = factory.Client(factory.AdminId, Roles.SuperAdmin);
        var reopened = await admin.PostAsync($"/api/workflow/definitions/{definition.Id}/reopen", null);
        reopened.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static string RequestData(string substrate) => JsonSerializer.Serialize(new {
        cooperation_type = "invoice", billing_period_months = "3", bandwidth_mb = 20, extra_ip_count = 0, substrate,
        building_height = 4, geo_coordinates = "35.7,51.4", tower_height = 0, has_cable = "no" });
    private static string ResultData(string outcome) => JsonSerializer.Serialize(new {
        is_feasible = outcome, required_equipment = "رادیو", needs_tower = "yes", tower_height = 6, pipe_meters = 2,
        cable_meters = 25, description = "نتیجه تست", attachment = "/uploads/receipts/test.png" });
    private static async Task<T> Read<T>(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, text);
        using var json = JsonDocument.Parse(text);
        return json.RootElement.GetProperty("data").Deserialize<T>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }
    private static async Task<WorkflowInstanceDto> Get(HttpClient client, Guid id) =>
        await Read<WorkflowInstanceDto>(await client.GetAsync($"/api/workflow/instances/{id}"));
    private static async Task Move(HttpClient client, Guid id, string target, string payload)
    {
        var workflow = await Get(client, id);
        using var graphResponse = await client.GetAsync($"/api/workflow/definitions/{workflow.DefinitionId}");
        var graph = await Read<WorkflowDefinitionDto>(graphResponse);
        var targetId = graph.States.Single(state => state.Code == target).Id;
        var transition = workflow.AvailableTransitions.Single(item => item.ToStateId == targetId);
        await Read<WorkflowInstanceDto>(await client.PostAsJsonAsync($"/api/workflow/instances/{id}/action", new {
            transitionId = transition.Id, actionDataJson = payload }));
    }

    private sealed class Factory : WebApplicationFactory<Program>
    {
        private readonly string _database = $"intake-{Guid.NewGuid():N}";
        public Guid SalesId { get; private set; }
        public Guid ManagerId { get; private set; }
        public Guid ExpertId { get; private set; }
        public Guid CustomerUserId { get; private set; }
        public Guid CustomerId { get; private set; }
        public Guid ServiceId { get; private set; }
        public Guid AdminId { get; private set; }
        public Guid SalesDepartmentMemberId { get; private set; }
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?> {
                ["Identity:SeedOnStartup"] = "false", ["Hangfire:Enabled"] = "false", ["HealthChecks:MySql:Enabled"] = "false",
                ["Http:UseHttpsRedirection"] = "false", ["Jwt:Key"] = "TEST_ONLY_KEY_32_BYTES_MINIMUM_0123456789",
                ["ConnectionStrings:MySql"] = "Server=127.0.0.1;Database=unused;User=root;" }));
            builder.ConfigureServices(services => {
                services.RemoveAll<AppDbContext>();
                services.AddScoped(provider => new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(_database).Options,
                    provider.GetServices<IModuleModelConfigurator>()));
                var number = 0;
                var sequences = new Mock<ISequenceService>();
                sequences.Setup(item => item.GetNextValueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(() => $"TEST-{Interlocked.Increment(ref number)}");
                services.AddSingleton(sequences.Object);
                services.AddAuthentication(options => { options.DefaultAuthenticateScheme = "IntakeTest"; options.DefaultChallengeScheme = "IntakeTest"; })
                    .AddScheme<AuthenticationSchemeOptions, TestAuthentication>("IntakeTest", _ => { });
            });
        }
        public HttpClient Client(Guid userId, string role)
        {
            var client = CreateClient(); client.DefaultRequestHeaders.Add("X-Test-User", userId.ToString());
            client.DefaultRequestHeaders.Add("X-Test-Role", role); return client;
        }
        public async Task SeedAsync()
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>(); await db.Database.EnsureCreatedAsync();
            await scope.ServiceProvider.GetRequiredService<IdentitySeeder>().SeedAsync();
            await scope.ServiceProvider.GetRequiredService<FormsSeedService>().SeedAsync();
            var salesDepartment = new Department("فروش"); var technical = new Department("فنی");
            db.AddRange(salesDepartment, technical); await db.SaveChangesAsync();
            async Task<Guid> User(string name, string fullName, string role, Guid? department)
            {
                var user = new User(name, "test-hash", fullName, "09121111111", null);
                user.ReplaceRoles([await db.Set<Role>().Where(item => item.Name == role).Select(item => item.Id).SingleAsync()], null, DateTime.UtcNow);
                if (department.HasValue) user.ReplaceDepartments([department.Value], null, DateTime.UtcNow);
                db.Add(user); await db.SaveChangesAsync(); return user.Id;
            }
            SalesId = await User("sales-test", "فروش آزمایشی", Roles.SalesExpert, salesDepartment.Id);
            SalesDepartmentMemberId = await User("sales-member-test", "عضو دپارتمان فروش", Roles.Support, salesDepartment.Id);
            ManagerId = await User("manager-test", "مدیر فنی آزمایشی", Roles.TechnicalManager, technical.Id);
            ExpertId = await User("expert-test", "کارشناس آزمایشی", Roles.TechnicalExpert, technical.Id);
            CustomerUserId = await User("customer-test", "مشتری آزمایشی", Roles.Customer, null);
            AdminId = await db.Set<User>().Where(item => item.Username == "superadmin").Select(item => item.Id).SingleAsync();
            var customer = new Customer(CustomerType.Natural, "مشتری", "آزمایشی", null, null, "0012345678", "09121111111",
                null, null, null, SalesId, null, false, null, CustomerUserId);
            db.Add(customer); CustomerId = customer.Id;
            var category = new ServiceCategory("اینترنت"); db.Add(category);
            var service = new Service("BW-INTAKE-TEST", "پهنای باند اختصاصی", category.Id, ServiceType.BandwidthDedicated, ServiceUnit.BandwidthMbps, null, true);
            service.Prices.Add(new ServicePrice(service.Id, 1000, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow));
            db.Add(service); ServiceId = service.Id; await db.SaveChangesAsync();
            await scope.ServiceProvider.GetRequiredService<WorkflowSeedService>().SeedAsync();
        }
    }

    private sealed class TestAuthentication(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue("X-Test-User", out var id)) return AuthenticateResult.NoResult();
            var role = Request.Headers["X-Test-Role"].ToString();
            var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, id.ToString()), new(ClaimTypes.Role, role) };
            var permissions = await Context.RequestServices.GetRequiredService<IPermissionProvider>().GetPermissionsAsync(Guid.Parse(id.ToString()), Context.RequestAborted);
            claims.AddRange(permissions.Select(permission => new Claim(CustomClaimTypes.Permission, permission)));
            return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name)), Scheme.Name));
        }
    }
}
