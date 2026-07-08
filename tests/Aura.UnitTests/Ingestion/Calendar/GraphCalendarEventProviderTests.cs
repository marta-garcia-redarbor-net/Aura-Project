using System.Net;
using System.Text;
using System.Text.Json;
using Aura.Application.Models;
using Aura.Infrastructure.Adapters.Connectors.Calendar;
using Aura.Infrastructure.Adapters.Connectors.Graph;
using Aura.UnitTests.TestDoubles.Observability;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Graph;
using Microsoft.Graph.Models.ODataErrors;
using Microsoft.Kiota.Abstractions.Authentication;
using NSubstitute;

namespace Aura.UnitTests.Ingestion.Calendar;

public class GraphCalendarEventProviderTests
{
    [Fact]
    public async Task FetchAsync_ReturnsMappedCalendarEvents()
    {
        var responseJson = BuildCalendarViewResponse(
            new FakeEvent("event-1", "Team standup", "2026-06-24T10:00:00Z", "2026-06-24T11:00:00Z", true, "https://teams.microsoft.com/l/meetup-join/123", "John Doe", "john@example.com", "Conference Room A", false, "Eastern Standard Time"),
            new FakeEvent("event-2", "Planning", "2026-06-24T14:00:00Z", "2026-06-24T15:00:00Z", false, null, null, null, null, false, null)
        );

        var provider = CreateProvider(responseJson);
        var request = CreateRequest();

        var results = await provider.FetchAsync(request, CancellationToken.None);

        Assert.Equal(2, results.Count);

        Assert.Equal("event-1", results[0].ExternalId);
        Assert.Equal("Team standup", results[0].Subject);
        Assert.Equal(new DateTimeOffset(2026, 6, 24, 10, 0, 0, TimeSpan.Zero), results[0].Start);
        Assert.Equal(new DateTimeOffset(2026, 6, 24, 11, 0, 0, TimeSpan.Zero), results[0].End);
        Assert.True(results[0].IsOnlineMeeting);
        Assert.Equal("https://teams.microsoft.com/l/meetup-join/123", results[0].JoinUrl);
        Assert.Equal("John Doe", results[0].OrganizerName);
        Assert.Equal("john@example.com", results[0].OrganizerAddress);
        Assert.Equal("Conference Room A", results[0].LocationDisplayName);
        Assert.False(results[0].IsCancelled);
        Assert.Equal("Eastern Standard Time", results[0].OriginalTimeZone);
        Assert.Null(results[0].UserId);

        Assert.Equal("event-2", results[1].ExternalId);
        Assert.Equal("Planning", results[1].Subject);
        Assert.False(results[1].IsOnlineMeeting);
        Assert.Null(results[1].UserId);
    }

    [Fact]
    public async Task FetchAsync_EmptyResponse_ReturnsEmptyList()
    {
        var responseJson = """{"value":[]}""";
        var provider = CreateProvider(responseJson);

        var results = await provider.FetchAsync(CreateRequest(), CancellationToken.None);

        Assert.Empty(results);
    }

    [Fact]
    public async Task FetchAsync_MsalUiRequired_PropagatesException()
    {
        var factory = Substitute.For<IGraphClientFactory>();
        factory.CreateClientAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<GraphServiceClient>(x => throw new Microsoft.Identity.Client.MsalUiRequiredException("no_account", "No cached account."));

        var provider = new GraphCalendarEventProvider(factory, NullLogger<GraphCalendarEventProvider>.Instance);

        await Assert.ThrowsAsync<Microsoft.Identity.Client.MsalUiRequiredException>(
            () => provider.FetchAsync(CreateRequest(), CancellationToken.None));
    }

    [Fact]
    public async Task FetchAsync_MsalUiRequired_EmitsWarningLogWithOidAndConnector_AndTokenExpiredMetric()
    {
        var factory = Substitute.For<IGraphClientFactory>();
        factory.CreateClientAsync("oid-cal-expired", Arg.Any<CancellationToken>())
            .Returns<GraphServiceClient>(_ => throw new Microsoft.Identity.Client.MsalUiRequiredException("interaction_required", "No cached account."));

        var logger = new ScopeAwareTestLogger<GraphCalendarEventProvider>();
        var provider = new GraphCalendarEventProvider(factory, logger);
        var request = new ConnectorExecutionRequest(
            new CheckpointIdentity("calendar", "calendar", "acme", userOid: "oid-cal-expired"),
            DateTimeOffset.UtcNow.AddHours(-1), DateTimeOffset.UtcNow);

        using var meter = new MeterCapture("Aura.Infrastructure.GraphConnector", "graph.token.expired");

        await Assert.ThrowsAsync<Microsoft.Identity.Client.MsalUiRequiredException>(() => provider.FetchAsync(request, CancellationToken.None));

        var warning = logger.Entries.Single(e => e.EventId.Id == 3403);
        Assert.Equal(Microsoft.Extensions.Logging.LogLevel.Warning, warning.Level);
        Assert.Equal("oid-cal-expired", warning.State["Oid"]?.ToString());
        Assert.Equal("calendar", warning.State["Connector"]?.ToString());

        var metric = Assert.Single(meter.Snapshot().Where(m =>
            m.Instrument == "graph.token.expired"
            && m.GetTag("connector") == "calendar"
            && m.GetTag("oid") == "oid-cal-expired"));
        Assert.Equal(1L, metric.Value);
        Assert.Equal("calendar", metric.GetTag("connector"));
        Assert.Equal("oid-cal-expired", metric.GetTag("oid"));
    }

    [Fact]
    public async Task FetchAsync_CalendarEventWithNullFields_MapsCorrectly()
    {
        var responseJson = BuildCalendarViewResponse(
            new FakeEvent("event-3", "All-day", "2026-06-24T00:00:00Z", "2026-06-25T00:00:00Z", false, null, null, null, null, false, null)
        );
        var provider = CreateProvider(responseJson);

        var results = await provider.FetchAsync(CreateRequest(), CancellationToken.None);

        Assert.Single(results);
        Assert.Equal("event-3", results[0].ExternalId);
        Assert.Equal("All-day", results[0].Subject);
        Assert.False(results[0].IsOnlineMeeting);
        Assert.Null(results[0].JoinUrl);
        Assert.Null(results[0].OrganizerName);
        Assert.Null(results[0].OrganizerAddress);
        Assert.Null(results[0].LocationDisplayName);
        Assert.Null(results[0].OriginalTimeZone);
    }

    [Fact]
    public async Task FetchAsync_PassesOidToFactory()
    {
        // Arrange
        var factory = Substitute.For<IGraphClientFactory>();
        var responseJson = """{"value":[]}""";
        var handler = new FakeGraphHttpHandler(responseJson);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://graph.microsoft.com/v1.0/") };
        var adapter = new Microsoft.Kiota.Http.HttpClientLibrary.HttpClientRequestAdapter(
            new AnonymousAuthenticationProvider(), httpClient: httpClient);
        var graphClient = new GraphServiceClient(adapter);

        factory.CreateClientAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(graphClient));

        var provider = new GraphCalendarEventProvider(factory, NullLogger<GraphCalendarEventProvider>.Instance);
        var request = new ConnectorExecutionRequest(
            new CheckpointIdentity("calendar", "calendar", "acme", userOid: "oid-cal-99"),
            DateTimeOffset.UtcNow.AddHours(-1), DateTimeOffset.UtcNow);

        // Act
        await provider.FetchAsync(request, CancellationToken.None);

        // Assert
        await factory.Received(1).CreateClientAsync("oid-cal-99", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FetchAsync_WhenIdentityHasUserOid_MapsUserIdOnDtos()
    {
        var responseJson = BuildCalendarViewResponse(
            new FakeEvent("event-oid", "Owned meeting", "2026-06-24T10:00:00Z", "2026-06-24T11:00:00Z", false, null, null, null, null, false, null));

        var provider = CreateProvider(responseJson);
        var request = new ConnectorExecutionRequest(
            new CheckpointIdentity("calendar", "calendar", "acme", userOid: "oid-777"),
            DateTimeOffset.UtcNow.AddHours(-1),
            DateTimeOffset.UtcNow);

        var results = await provider.FetchAsync(request, CancellationToken.None);

        Assert.Single(results);
        Assert.Equal("oid-777", results[0].UserId);
    }

    [Fact]
    public async Task FetchAsync_GraphHttp4xx_ReturnsFailureWithStatusCode()
    {
        var factory = Substitute.For<IGraphClientFactory>();
        factory.CreateClientAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<GraphServiceClient>(x => throw new ODataError
            {
                ResponseStatusCode = 403
            });

        var provider = new GraphCalendarEventProvider(factory, NullLogger<GraphCalendarEventProvider>.Instance);

        var ex = await Assert.ThrowsAsync<ODataError>(
            () => provider.FetchAsync(CreateRequest(), CancellationToken.None));

        Assert.Equal(403, ex.ResponseStatusCode);
    }

    [Fact]
    public async Task FetchAsync_GraphHttp4xx_EmitsWarningLogAndHttpErrorMetricWithConnectorAndEndpoint()
    {
        var factory = Substitute.For<IGraphClientFactory>();
        factory.CreateClientAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<GraphServiceClient>(_ => throw new ODataError { ResponseStatusCode = 403 });

        var logger = new ScopeAwareTestLogger<GraphCalendarEventProvider>();
        var provider = new GraphCalendarEventProvider(factory, logger);

        using var meter = new MeterCapture("Aura.Infrastructure.GraphConnector", "graph.http.error");

        var ex = await Assert.ThrowsAsync<ODataError>(() => provider.FetchAsync(CreateRequest(), CancellationToken.None));
        Assert.Equal(403, ex.ResponseStatusCode);

        var warning = logger.Entries.Single(e => e.EventId.Id == 3404);
        Assert.Equal(Microsoft.Extensions.Logging.LogLevel.Warning, warning.Level);
        Assert.Equal("403", warning.State["StatusCode"]?.ToString());
        Assert.Equal("me/calendarView", warning.State["Endpoint"]?.ToString());
        Assert.Equal("calendar", warning.State["Connector"]?.ToString());

        var metric = Assert.Single(meter.Snapshot().Where(m =>
            m.Instrument == "graph.http.error"
            && m.GetTag("connector") == "calendar"
            && m.GetTag("status_code") == "403"
            && m.GetTag("endpoint") == "me/calendarView"));
        Assert.Equal(1L, metric.Value);
        Assert.Equal("calendar", metric.GetTag("connector"));
        Assert.Equal("403", metric.GetTag("status_code"));
        Assert.Equal("me/calendarView", metric.GetTag("endpoint"));
    }

    [Fact]
    public async Task FetchAsync_GraphHttp5xx_ReturnsFailureWithStatusCode()
    {
        var factory = Substitute.For<IGraphClientFactory>();
        factory.CreateClientAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<GraphServiceClient>(x => throw new ODataError
            {
                ResponseStatusCode = 503
            });

        var provider = new GraphCalendarEventProvider(factory, NullLogger<GraphCalendarEventProvider>.Instance);

        var ex = await Assert.ThrowsAsync<ODataError>(
            () => provider.FetchAsync(CreateRequest(), CancellationToken.None));

        Assert.Equal(503, ex.ResponseStatusCode);
    }

    [Fact]
    public async Task FetchAsync_GraphHttp5xx_EmitsErrorLogAndHttpErrorMetricWithConnectorAndEndpoint()
    {
        var factory = Substitute.For<IGraphClientFactory>();
        factory.CreateClientAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<GraphServiceClient>(_ => throw new ODataError { ResponseStatusCode = 503 });

        var logger = new ScopeAwareTestLogger<GraphCalendarEventProvider>();
        var provider = new GraphCalendarEventProvider(factory, logger);

        using var meter = new MeterCapture("Aura.Infrastructure.GraphConnector", "graph.http.error");

        var ex = await Assert.ThrowsAsync<ODataError>(() => provider.FetchAsync(CreateRequest(), CancellationToken.None));
        Assert.Equal(503, ex.ResponseStatusCode);

        var error = logger.Entries.Single(e => e.EventId.Id == 3405);
        Assert.Equal(Microsoft.Extensions.Logging.LogLevel.Error, error.Level);
        Assert.Equal("503", error.State["StatusCode"]?.ToString());
        Assert.Equal("me/calendarView", error.State["Endpoint"]?.ToString());
        Assert.Equal("calendar", error.State["Connector"]?.ToString());

        var metric = Assert.Single(meter.Snapshot().Where(m =>
            m.Instrument == "graph.http.error"
            && m.GetTag("connector") == "calendar"
            && m.GetTag("status_code") == "503"
            && m.GetTag("endpoint") == "me/calendarView"));
        Assert.Equal(1L, metric.Value);
        Assert.Equal("calendar", metric.GetTag("connector"));
        Assert.Equal("503", metric.GetTag("status_code"));
        Assert.Equal("me/calendarView", metric.GetTag("endpoint"));
    }

    private static GraphCalendarEventProvider CreateProvider(string responseJson)
    {
        var handler = new FakeGraphHttpHandler(responseJson);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://graph.microsoft.com/v1.0/") };
        var adapter = new Microsoft.Kiota.Http.HttpClientLibrary.HttpClientRequestAdapter(
            new AnonymousAuthenticationProvider(), httpClient: httpClient);
        var graphClient = new GraphServiceClient(adapter);

        var factory = Substitute.For<IGraphClientFactory>();
        factory.CreateClientAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(graphClient));

        return new GraphCalendarEventProvider(factory, NullLogger<GraphCalendarEventProvider>.Instance);
    }

    private static ConnectorExecutionRequest CreateRequest()
        => new(new CheckpointIdentity("calendar", "calendar", "acme"),
               DateTimeOffset.UtcNow.AddHours(-1), DateTimeOffset.UtcNow);

    private static string BuildCalendarViewResponse(params FakeEvent[] events)
    {
        var items = events.Select(e => new Dictionary<string, object?>
        {
            ["id"] = e.Id,
            ["subject"] = e.Subject,
            ["start"] = new Dictionary<string, object?>
            {
                ["dateTime"] = e.Start,
                ["timeZone"] = "UTC"
            },
            ["end"] = new Dictionary<string, object?>
            {
                ["dateTime"] = e.End,
                ["timeZone"] = "UTC"
            },
            ["isOnlineMeeting"] = e.IsOnlineMeeting,
            ["onlineMeeting"] = e.JoinUrl is not null ? new Dictionary<string, object?>
            {
                ["joinUrl"] = e.JoinUrl
            } : null,
            ["organizer"] = e.OrganizerName is not null ? new Dictionary<string, object?>
            {
                ["emailAddress"] = new Dictionary<string, object?>
                {
                    ["name"] = e.OrganizerName,
                    ["address"] = e.OrganizerAddress
                }
            } : null,
            ["location"] = e.LocationDisplayName is not null ? new Dictionary<string, object?>
            {
                ["displayName"] = e.LocationDisplayName
            } : null,
            ["isCancelled"] = e.IsCancelled,
            ["originalStartTimeZone"] = e.OriginalTimeZone
        });

        return JsonSerializer.Serialize(new { value = items });
    }

    private sealed record FakeEvent(
        string Id,
        string Subject,
        string Start,
        string End,
        bool IsOnlineMeeting,
        string? JoinUrl,
        string? OrganizerName,
        string? OrganizerAddress,
        string? LocationDisplayName,
        bool IsCancelled,
        string? OriginalTimeZone);
}

internal sealed class FakeGraphHttpHandler : HttpMessageHandler
{
    private readonly string _responseBody;
    private readonly HttpStatusCode _statusCode;

    public FakeGraphHttpHandler(string responseBody, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _responseBody = responseBody;
        _statusCode = statusCode;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_responseBody, Encoding.UTF8, "application/json")
        };
        return Task.FromResult(response);
    }
}
