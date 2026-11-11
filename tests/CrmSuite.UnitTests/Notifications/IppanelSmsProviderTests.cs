using System.Net;
using System.Text;
using System.Text.Json;
using CrmSuite.Modules.Notifications.Application.Abstractions;
using CrmSuite.Modules.Notifications.Infrastructure.Sms;
using FluentAssertions;

namespace CrmSuite.UnitTests.Notifications;

public sealed class IppanelSmsProviderTests
{
    [Fact]
    public async Task Pattern_request_has_one_recipient_and_exact_payload()
    {
        var handler = new CaptureHandler(
            HttpStatusCode.OK,
            """{"message_outbox_ids":[12345]}""");
        var provider = CreateProvider(handler);

        var result = await provider.SendPatternAsync(
            "PATTERN-OTP",
            "+983000000001",
            "+989121234567",
            new Dictionary<string, string> { ["code"] = "12345" },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.MessageId.Should().Be("12345");
        handler.RequestUri.Should().Be("https://edge.ippanel.com/v1/api/send");
        handler.Authorization.Should().Be("test-api-key");

        using var document = JsonDocument.Parse(handler.Body!);
        var root = document.RootElement;
        root.GetProperty("sending_type").GetString().Should().Be("pattern");
        root.GetProperty("from_number").GetString().Should().Be("+983000000001");
        root.GetProperty("code").GetString().Should().Be("PATTERN-OTP");
        root.GetProperty("recipients").GetArrayLength().Should().Be(1);
        root.GetProperty("recipients")[0].GetString().Should().Be("+989121234567");
        root.GetProperty("params").GetProperty("code").GetString().Should().Be("12345");
    }

    [Fact]
    public async Task Uppercase_pattern_variable_is_discovered_and_retried()
    {
        var handler = new SequenceHandler(
            (HttpStatusCode.UnprocessableEntity,
                """{"meta":{"message":"تکمیل گزینه params.CODE الزامی است"}}"""),
            (HttpStatusCode.OK, """{"message_outbox_ids":[77]}"""));
        var provider = CreateProvider(handler);

        var result = await provider.SendPatternAsync(
            "PATTERN-OTP",
            "+983000000001",
            "+989121234567",
            new Dictionary<string, string> { ["code"] = "12345" },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.MessageId.Should().Be("77");
        handler.Bodies.Should().HaveCount(2);
        using var first = JsonDocument.Parse(handler.Bodies[0]);
        using var second = JsonDocument.Parse(handler.Bodies[1]);
        first.RootElement.GetProperty("params").TryGetProperty("code", out _).Should().BeTrue();
        second.RootElement.GetProperty("params").GetProperty("CODE").GetString()
            .Should().Be("12345");
        second.RootElement.GetProperty("params").TryGetProperty("code", out _).Should().BeFalse();
    }

    [Fact]
    public async Task Provider_rejects_empty_recipient_before_calling_http()
    {
        var handler = new CaptureHandler(HttpStatusCode.OK, """{"message_outbox_ids":[1]}""");
        var provider = CreateProvider(handler);

        var result = await provider.SendPatternAsync(
            "PATTERN-OTP",
            "+983000000001",
            " ",
            new Dictionary<string, string>(),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        handler.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task Provider_returns_failure_for_non_success_http_status()
    {
        var handler = new CaptureHandler(
            HttpStatusCode.Unauthorized,
            """{"message":"invalid api key"}""");
        var provider = CreateProvider(handler);

        var result = await provider.SendPatternAsync(
            "PATTERN-OTP",
            "+983000000001",
            "+989121234567",
            new Dictionary<string, string> { ["code"] = "12345" },
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("401");
        result.RawResponse.Should().Contain("invalid api key");
    }

    private static IppanelSmsProvider CreateProvider(HttpMessageHandler handler)
    {
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://edge.ippanel.com"),
        };
        return new IppanelSmsProvider(client, new FakeSettingsProvider());
    }

    private sealed class FakeSettingsProvider : IIppanelSettingsProvider
    {
        public Task<IppanelRuntimeSettings> GetAsync(CancellationToken cancellationToken) =>
            Task.FromResult(new IppanelRuntimeSettings(
                true,
                "test-api-key",
                "+983000000001",
                "PATTERN-OTP",
                "https://edge.ippanel.com/v1/api/send",
                "code",
                "Database",
                true));
    }

    private sealed class SequenceHandler : HttpMessageHandler
    {
        private readonly Queue<(HttpStatusCode Status, string Body)> _responses;

        public SequenceHandler(params (HttpStatusCode Status, string Body)[] responses)
        {
            _responses = new Queue<(HttpStatusCode Status, string Body)>(responses);
        }

        public List<string> Bodies { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Bodies.Add(request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken));
            var next = _responses.Dequeue();
            return new HttpResponseMessage(next.Status)
            {
                Content = new StringContent(next.Body, Encoding.UTF8, "application/json"),
            };
        }
    }

    private sealed class CaptureHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly string _response;

        public CaptureHandler(HttpStatusCode status, string response)
        {
            _status = status;
            _response = response;
        }

        public int CallCount { get; private set; }

        public Uri? RequestUri { get; private set; }

        public string? Authorization { get; private set; }

        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            RequestUri = request.RequestUri;
            Authorization = request.Headers.TryGetValues("Authorization", out var values)
                ? values.SingleOrDefault()
                : null;
            Body = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(_status)
            {
                Content = new StringContent(_response, Encoding.UTF8, "application/json"),
            };
        }
    }
}
