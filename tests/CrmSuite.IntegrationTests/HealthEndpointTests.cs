using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace CrmSuite.IntegrationTests;

public sealed class HealthEndpointTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public HealthEndpointTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Health_returns_healthy_without_database_dependency()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("status").GetString().Should().Be("Healthy");
        body.GetProperty("traceId").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Notification_hub_requires_authentication()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsync(
            "/hubs/notifications/negotiate?negotiateVersion=1",
            content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Foundation_health_returns_utc_and_jalali_time()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("success").GetBoolean().Should().BeTrue();
        body.GetProperty("data").GetProperty("status").GetString().Should().Be("Healthy");
        body.GetProperty("data").GetProperty("serverTimeUtc").GetString().Should().NotBeNullOrWhiteSpace();
        body.GetProperty("data").GetProperty("serverTimeJalali").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Foundation_settings_requires_authentication()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/foundation/settings");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("success").GetBoolean().Should().BeFalse();
        body.GetProperty("traceId").GetString().Should().NotBeNullOrWhiteSpace();
    }
}
