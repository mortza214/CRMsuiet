using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CrmSuite.Api;
using CrmSuite.Api.Authorization;
using CrmSuite.Api.Middleware;
using CrmSuite.Api.Realtime;
using CrmSuite.Api.Services;
using CrmSuite.BuildingBlocks.Application;
using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.BuildingBlocks.Application.Common;
using CrmSuite.BuildingBlocks.Application.Security;
using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Api.Maintenance;
using CrmSuite.Modules.Billing.Infrastructure;
using CrmSuite.Modules.Billing.Infrastructure.Jobs;
using CrmSuite.Modules.Catalog.Infrastructure;
using CrmSuite.Modules.Customers.Infrastructure;
using CrmSuite.Modules.Foundation.Api;
using CrmSuite.Modules.Forms.Infrastructure;
using CrmSuite.Modules.Forms.Infrastructure.Persistence;
using CrmSuite.Modules.Foundation.Infrastructure;
using CrmSuite.Modules.Notifications.Infrastructure;
using CrmSuite.Modules.Orders.Infrastructure;
using CrmSuite.Modules.Payments.Infrastructure;
using CrmSuite.Modules.Reports.Infrastructure;
using CrmSuite.Modules.Settings.Infrastructure;
using CrmSuite.Modules.Workflow.Infrastructure;
using CrmSuite.Modules.Workflow.Infrastructure.Persistence;
using CrmSuite.Modules.Tickets.Infrastructure;
using CrmSuite.Modules.Wallet.Infrastructure;
using CrmSuite.Modules.Wallet.Infrastructure.Jobs;
using CrmSuite.Modules.Identity.Api.Authorization;
using CrmSuite.Modules.Identity.Infrastructure;
using CrmSuite.Modules.Identity.Infrastructure.Security;
using FoundationCurrentUserService = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.MySql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseWindowsService(options => options.ServiceName = "CrmSuite.Api");
    builder.Host.UseSerilog((context, services, loggerConfiguration) => loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    var configuration = builder.Configuration;
    var connectionString = configuration.GetConnectionString("MySql")
        ?? throw new InvalidOperationException("ConnectionStrings:MySql is required.");
    var serverVersion = new MySqlServerVersion(
        Version.Parse(configuration["Database:ServerVersion"] ?? "8.0.36"));

    // Persistence -------------------------------------------------------------------------
    builder.Services.AddDbContext<AppDbContext>(options =>
    {
        options.UseMySql(connectionString, serverVersion, mysql =>
        {
            mysql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
            mysql.MigrationsAssembly(typeof(AppDbContext).Assembly.GetName().Name);
        });

        if (configuration.GetValue("Database:EnableDetailedErrors", false))
        {
            options.EnableDetailedErrors();
        }

        if (configuration.GetValue("Database:EnableSensitiveDataLogging", false))
        {
            options.EnableSensitiveDataLogging();
        }
    });

    builder.Services.AddMemoryCache();
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<CurrentUserService>();
    builder.Services.AddScoped<ICurrentUserService>(provider =>
        provider.GetRequiredService<CurrentUserService>());
    builder.Services.AddScoped<FoundationCurrentUserService>(provider =>
        provider.GetRequiredService<CurrentUserService>());
    builder.Services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
    builder.Services.AddScoped<TestDataResetService>();

    // CQRS and module application services ------------------------------------------------
    builder.Services.AddApplication(ModuleAssemblies.LoadApplicationAssemblies());
    builder.Services.AddFoundationInfrastructure();
    builder.Services.AddFoundationApi();
    builder.Services.AddIdentityInfrastructure(configuration);
    builder.Services.AddCustomersInfrastructure(configuration);
    builder.Services.AddCatalogInfrastructure();
    builder.Services.AddOrdersInfrastructure();
    builder.Services.AddBillingInfrastructure();
    builder.Services.AddPaymentsInfrastructure(configuration);
    builder.Services.AddWalletInfrastructure();
    builder.Services.AddTicketsInfrastructure();
    builder.Services.AddFormsInfrastructure();
    builder.Services.AddNotificationsInfrastructure();
    builder.Services.AddSettingsInfrastructure();
    builder.Services.AddReportsInfrastructure();
    builder.Services.AddWorkflowInfrastructure();

    // Each module owns an empty EF model configurator in this foundation slice.
    foreach (var assembly in ModuleAssemblies.LoadInfrastructureAssemblies())
    {
        foreach (var type in assembly.GetTypes().Where(type =>
                     type is { IsAbstract: false, IsInterface: false }
                     && typeof(IModuleModelConfigurator).IsAssignableFrom(type)))
        {
            builder.Services.AddScoped(typeof(IModuleModelConfigurator), type);
        }
    }

    // HTTP / JSON -------------------------------------------------------------------------
    builder.Services
        .AddControllers()
        .ConfigureApplicationPartManager(manager =>
        {
            var existingAssemblies = manager.ApplicationParts
                .OfType<AssemblyPart>()
                .Select(part => part.Assembly)
                .ToHashSet();

            foreach (var assembly in ModuleAssemblies.LoadApiAssemblies())
            {
                if (existingAssemblies.Add(assembly))
                {
                    manager.ApplicationParts.Add(new AssemblyPart(assembly));
                }
            }
        })
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

    builder.Services.Configure<ApiBehaviorOptions>(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(entry => entry.Value?.Errors.Count > 0)
                .SelectMany(entry => entry.Value!.Errors.Select(error =>
                    string.IsNullOrWhiteSpace(entry.Key)
                        ? error.ErrorMessage
                        : $"{entry.Key}: {error.ErrorMessage}"))
                .ToArray();

            return new BadRequestObjectResult(
                ApiResponse<object?>.Fail(
                    "داده‌های ورودی معتبر نیستند.",
                    errors,
                    context.HttpContext.TraceIdentifier));
        };
    });

    // Authentication / authorization -------------------------------------------------------
    var jwtSection = configuration.GetSection("Jwt");
    var jwtKey = jwtSection["Key"];

    if (string.IsNullOrWhiteSpace(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32)
    {
        throw new InvalidOperationException("Jwt:Key must contain at least 32 bytes.");
    }

    if (builder.Environment.IsProduction()
        && jwtKey.StartsWith("CHANGE_ME", StringComparison.Ordinal))
    {
        throw new InvalidOperationException(
            "Jwt:Key still uses the development placeholder. Configure it with user-secrets or an environment variable.");
    }

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = configuration.GetValue("Jwt:RequireHttpsMetadata", true);
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtSection["Issuer"],
                ValidateAudience = true,
                ValidAudience = jwtSection["Audience"],
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30),
                NameClaimType = System.Security.Claims.ClaimTypes.Name,
                RoleClaimType = System.Security.Claims.ClaimTypes.Role,
            };

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];

                    if (!string.IsNullOrEmpty(accessToken)
                        && context.HttpContext.Request.Path.StartsWithSegments("/hubs/notifications"))
                    {
                        context.Token = accessToken;
                    }

                    return Task.CompletedTask;
                },
                OnChallenge = async context =>
                {
                    context.HandleResponse();

                    if (context.Response.HasStarted)
                    {
                        return;
                    }

                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json; charset=utf-8";

                    await context.Response.WriteAsJsonAsync(
                        ApiResponse<object?>.Fail(
                            "احراز هویت لازم است.",
                            ["توکن معتبر نیست یا ارسال نشده است."],
                            context.HttpContext.TraceIdentifier),
                        context.HttpContext.RequestAborted);
                },
                OnForbidden = async context =>
                {
                    if (context.Response.HasStarted)
                    {
                        return;
                    }

                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    context.Response.ContentType = "application/json; charset=utf-8";

                    await context.Response.WriteAsJsonAsync(
                        ApiResponse<object?>.Fail(
                            "شما اجازه انجام این عملیات را ندارید.",
                            ["دسترسی لازم برای این منبع وجود ندارد."],
                            context.HttpContext.TraceIdentifier),
                        context.HttpContext.RequestAborted);
                },
            };
        });

    builder.Services.AddAuthorization();
    builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
    builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

    // Background jobs ---------------------------------------------------------------------
    if (configuration.GetValue("Hangfire:Enabled", false))
    {
        var storageOptions = new MySqlStorageOptions
        {
            TablesPrefix = configuration["Hangfire:TablesPrefix"] ?? "hangfire",
            PrepareSchemaIfNecessary = configuration.GetValue("Hangfire:PrepareSchema", true),
        };

        // Hangfire.MySqlStorage parses the connection string itself and trips over a trailing ';'.
        var hangfireConnectionString = connectionString.TrimEnd(';');

        builder.Services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseStorage(new MySqlStorage(hangfireConnectionString, storageOptions)));

        builder.Services.AddHangfireServer(options =>
            options.WorkerCount = configuration.GetValue("Hangfire:WorkerCount", 4));
    }

    // Real-time notifications -------------------------------------------------------------
    builder.Services.AddSignalR(options =>
    {
        options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    });

    // CORS --------------------------------------------------------------------------------
    var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    builder.Services.AddCors(options => options.AddPolicy("Default", policy =>
    {
        policy.WithOrigins(allowedOrigins.Length > 0 ? allowedOrigins : ["http://localhost:5173"])
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    }));

    // Health checks -----------------------------------------------------------------------
    var healthChecks = builder.Services.AddHealthChecks();

    if (configuration.GetValue("HealthChecks:MySql:Enabled", true))
    {
        healthChecks.AddMySql(
            connectionString,
            name: "mysql",
            failureStatus: HealthStatus.Unhealthy,
            tags: ["ready"],
            timeout: TimeSpan.FromSeconds(3));
    }

    // OpenAPI -----------------------------------------------------------------------------
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "CrmSuite API",
            Version = "v1",
            Description = "سامانه مدیریت مشتری، فروش سرویس، تیکت، کیف پول و پورسانت",
        });

        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "توکن JWT را بدون کلمه Bearer وارد کنید.",
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            [new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer",
                },
            }] = [],
        });
    });

    var app = builder.Build();

    if (configuration.GetValue("Identity:SeedOnStartup", true))
    {
        try
        {
            await using var scope = app.Services.CreateAsyncScope();
            var seeder = scope.ServiceProvider.GetRequiredService<IdentitySeeder>();
            await seeder.SeedAsync();
        }
        catch (Exception exception) when (app.Environment.IsDevelopment())
        {
            app.Logger.LogWarning(
                exception,
                "Identity seed skipped. Make sure MySQL is running and the Identity migration is applied.");
        }
    }

    if (configuration.GetValue("Identity:SeedOnStartup", true))
    {
        try
        {
            await using var scope = app.Services.CreateAsyncScope();
            var formSeeder = scope.ServiceProvider.GetRequiredService<FormsSeedService>();
            await formSeeder.SeedAsync();
            var workflowSeeder = scope.ServiceProvider.GetRequiredService<WorkflowSeedService>();
            await workflowSeeder.SeedAsync();
        }
        catch (Exception exception) when (app.Environment.IsDevelopment())
        {
#pragma warning disable CA1848
            app.Logger.LogWarning(
                exception,
                "Bandwidth workflow seed skipped. Make sure MySQL is running and migrations are applied.");
#pragma warning restore CA1848
        }
    }

    // Pipeline ----------------------------------------------------------------------------
    app.UseSerilogRequestLogging();
    app.UseMiddleware<ExceptionHandlingMiddleware>();

    if (configuration.GetValue("Http:UseHttpsRedirection", false))
    {
        app.UseHttpsRedirection();
    }

    app.UseCors("Default");
    app.UseAuthentication();
    app.UseAuthorization();

    var swaggerEnabled = configuration.GetValue("Swagger:Enabled", app.Environment.IsDevelopment());

    if (swaggerEnabled)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.DocumentTitle = "CrmSuite API";
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "CrmSuite API v1");
            options.RoutePrefix = "swagger";
        });

        app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
    }

    if (configuration.GetValue("Hangfire:Enabled", false))
    {
        var recurringJobs = app.Services.GetRequiredService<IRecurringJobManager>();
        recurringJobs.RemoveIfExists("wallet-monthly-commissions");

        recurringJobs.RemoveIfExists("bandwidth-monthly-invoices");
        recurringJobs.AddOrUpdate<CommissionMonthlyPayoutJob>(
            "commission-monthly-payouts",
            job => job.ExecuteAsync(CancellationToken.None),
            "0 2 1 * *");

        recurringJobs.AddOrUpdate<GenerateRecurringInvoiceJob>(
            "bandwidth-recurring-invoices",
            job => job.ExecuteAsync(CancellationToken.None),
            "0 0 1 * *");

        app.UseHangfireDashboard(
            configuration["Hangfire:DashboardPath"] ?? "/hangfire",
            new DashboardOptions
            {
                DashboardTitle = "CrmSuite Jobs",
                Authorization = [new HangfireDashboardAuthorizationFilter()],
            });
    }

    app.UseStaticFiles();
    app.MapControllers();
    app.MapHub<NotificationHub>("/hubs/notifications").RequireAuthorization();
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json; charset=utf-8";
            await context.Response.WriteAsJsonAsync(new
            {
                status = report.Status.ToString(),
                checks = report.Entries.Select(entry => new
                {
                    name = entry.Key,
                    status = entry.Value.Status.ToString(),
                    duration = entry.Value.Duration.TotalMilliseconds,
                }),
                traceId = context.TraceIdentifier,
            });
        },
    });

    await app.RunAsync();
}
catch (HostAbortedException)
{
    // EF Core design-time tooling intentionally aborts the host after building services.
    throw;
}
catch (Exception exception)
{
    Log.Fatal(exception, "CrmSuite API terminated unexpectedly.");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}

public partial class Program
{
}
