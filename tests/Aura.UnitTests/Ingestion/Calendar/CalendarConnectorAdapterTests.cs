using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Domain.Calendar;
using Aura.Infrastructure.Adapters.Connectors.Calendar;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Aura.UnitTests.Ingestion.Calendar;

public class CalendarConnectorAdapterTests
{
    [Fact]
    public async Task ExecuteAsync_MapsAndSavesAllValidFixtures()
    {
        var store = Substitute.For<ICalendarEventStore>();
        var fixtures = new[]
        {
            new CalendarEventDto
            {
                ExternalId = "event-1",
                Subject = "Team standup",
                Start = new DateTimeOffset(2026, 6, 24, 10, 0, 0, TimeSpan.Zero),
                End = new DateTimeOffset(2026, 6, 24, 11, 0, 0, TimeSpan.Zero),
                IsOnlineMeeting = true,
                JoinUrl = "https://teams.microsoft.com/l/meetup-join/123",
                OrganizerName = "John Doe",
                OrganizerAddress = "john@example.com",
                LocationDisplayName = "Conference Room A",
                IsCancelled = false,
                OriginalTimeZone = "Eastern Standard Time"
            },
            new CalendarEventDto
            {
                ExternalId = "event-2",
                Subject = "Planning",
                Start = new DateTimeOffset(2026, 6, 24, 14, 0, 0, TimeSpan.Zero),
                End = new DateTimeOffset(2026, 6, 24, 15, 0, 0, TimeSpan.Zero),
                IsOnlineMeeting = false
            }
        };
        var adapter = new CalendarConnectorAdapter(
            NullLogger<CalendarConnectorAdapter>.Instance,
            store,
            new CalendarEventMapper(),
            () => fixtures);
        var request = CreateRequest();

        var result = await adapter.ExecuteAsync(request, CancellationToken.None);

        await store.Received(2).SaveAsync(Arg.Any<CalendarEvent>(), Arg.Any<CancellationToken>());
        Assert.Equal(2, result.ItemCount);
        Assert.Equal(ConnectorExecutionStatus.Success, result.Status);
        Assert.Equal(request.WindowEnd, result.MaxProcessedAt);
    }

    [Fact]
    public async Task ExecuteAsync_SkipsInvalidFixture_ContinuesBatch_WithPartialFailure()
    {
        var store = Substitute.For<ICalendarEventStore>();
        var fixtures = new[]
        {
            new CalendarEventDto
            {
                ExternalId = "event-1",
                Subject = "Valid",
                Start = new DateTimeOffset(2026, 6, 24, 10, 0, 0, TimeSpan.Zero),
                End = new DateTimeOffset(2026, 6, 24, 11, 0, 0, TimeSpan.Zero),
                IsOnlineMeeting = false
            },
            new CalendarEventDto
            {
                ExternalId = null,
                Subject = "Missing ID",
                Start = new DateTimeOffset(2026, 6, 24, 14, 0, 0, TimeSpan.Zero),
                End = new DateTimeOffset(2026, 6, 24, 15, 0, 0, TimeSpan.Zero),
                IsOnlineMeeting = false
            },
            new CalendarEventDto
            {
                ExternalId = "event-3",
                Subject = "Cancelled",
                Start = new DateTimeOffset(2026, 6, 24, 16, 0, 0, TimeSpan.Zero),
                End = new DateTimeOffset(2026, 6, 24, 17, 0, 0, TimeSpan.Zero),
                IsOnlineMeeting = false,
                IsCancelled = true
            }
        };
        var adapter = new CalendarConnectorAdapter(
            NullLogger<CalendarConnectorAdapter>.Instance,
            store,
            new CalendarEventMapper(),
            () => fixtures);
        var request = CreateRequest();

        var result = await adapter.ExecuteAsync(request, CancellationToken.None);

        await store.Received(1).SaveAsync(Arg.Any<CalendarEvent>(), Arg.Any<CancellationToken>());
        Assert.Equal(1, result.ItemCount);
        Assert.Equal(ConnectorExecutionStatus.PartialFailure, result.Status);
        Assert.False(string.IsNullOrWhiteSpace(result.FailureReason));
    }

    [Fact]
    public async Task ExecuteAsync_WithoutFixtureProvider_UsesDefaultFixturePath()
    {
        var store = Substitute.For<ICalendarEventStore>();
        var adapter = new CalendarConnectorAdapter(
            NullLogger<CalendarConnectorAdapter>.Instance,
            store,
            new CalendarEventMapper());
        var request = CreateRequest();

        var result = await adapter.ExecuteAsync(request, CancellationToken.None);

        // Default fixtures: 1 valid, 1 missing ExternalId (skipped), 1 cancelled (skipped)
        await store.Received(1).SaveAsync(Arg.Any<CalendarEvent>(), Arg.Any<CancellationToken>());
        Assert.Equal(1, result.ItemCount);
        Assert.Equal(ConnectorExecutionStatus.PartialFailure, result.Status);
        Assert.False(string.IsNullOrWhiteSpace(result.FailureReason));
    }

    [Fact]
    public async Task ExecuteAsync_WithSourceProvider_UsesProviderInsteadOfFixtures()
    {
        var store = Substitute.For<ICalendarEventStore>();
        var provider = Substitute.For<IMessageSourceProvider<CalendarEventDto>>();
        var providerPayloads = new[]
        {
            new CalendarEventDto
            {
                ExternalId = "graph-event-1",
                Subject = "From Graph provider",
                Start = new DateTimeOffset(2026, 6, 24, 10, 0, 0, TimeSpan.Zero),
                End = new DateTimeOffset(2026, 6, 24, 11, 0, 0, TimeSpan.Zero),
                IsOnlineMeeting = true,
                JoinUrl = "https://teams.microsoft.com/l/meetup-join/456",
                OrganizerName = "Alice",
                OrganizerAddress = "alice@example.com",
                LocationDisplayName = "Room B",
                IsCancelled = false,
                OriginalTimeZone = "UTC"
            }
        };
        var request = CreateRequest();
        provider.FetchAsync(request, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<CalendarEventDto>>(providerPayloads));

        var adapter = new CalendarConnectorAdapter(
            NullLogger<CalendarConnectorAdapter>.Instance,
            store,
            new CalendarEventMapper(),
            fixtureProvider: () => throw new InvalidOperationException("Fixture must not be called when provider exists"),
            sourceProvider: provider);

        var result = await adapter.ExecuteAsync(request, CancellationToken.None);

        await provider.Received(1).FetchAsync(request, Arg.Any<CancellationToken>());
        await store.Received(1).SaveAsync(Arg.Any<CalendarEvent>(), Arg.Any<CancellationToken>());
        Assert.Equal(1, result.ItemCount);
        Assert.Equal(ConnectorExecutionStatus.Success, result.Status);
    }

    [Fact]
    public async Task ExecuteAsync_WithSourceProvider_SavesMappedCalendarEvent()
    {
        var store = Substitute.For<ICalendarEventStore>();
        var capturedEvents = new List<CalendarEvent>();
        store.When(s => s.SaveAsync(Arg.Any<CalendarEvent>(), Arg.Any<CancellationToken>()))
            .Do(ci => capturedEvents.Add(ci.Arg<CalendarEvent>()));

        var provider = Substitute.For<IMessageSourceProvider<CalendarEventDto>>();
        var providerPayloads = new[]
        {
            new CalendarEventDto
            {
                ExternalId = "graph-event-2",
                Subject = "PR Review needed",
                Start = new DateTimeOffset(2026, 6, 24, 14, 0, 0, TimeSpan.Zero),
                End = new DateTimeOffset(2026, 6, 24, 15, 0, 0, TimeSpan.Zero),
                IsOnlineMeeting = false,
                OrganizerName = "Bob",
                OrganizerAddress = "bob@example.com",
                LocationDisplayName = "Room C",
                IsCancelled = false,
                OriginalTimeZone = "UTC"
            }
        };
        var request = CreateRequest();
        provider.FetchAsync(request, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<CalendarEventDto>>(providerPayloads));

        var adapter = new CalendarConnectorAdapter(
            NullLogger<CalendarConnectorAdapter>.Instance,
            store,
            new CalendarEventMapper(),
            sourceProvider: provider);

        await adapter.ExecuteAsync(request, CancellationToken.None);

        Assert.Single(capturedEvents);
        var evt = capturedEvents[0];
        Assert.Equal("graph-event-2", evt.Id);
        Assert.Equal("PR Review needed", evt.Title);
        Assert.Equal(new DateTimeOffset(2026, 6, 24, 14, 0, 0, TimeSpan.Zero), evt.StartUtc);
        Assert.Equal(new DateTimeOffset(2026, 6, 24, 15, 0, 0, TimeSpan.Zero), evt.EndUtc);
        Assert.False(evt.IsOnlineMeeting);
        Assert.Equal("Bob", evt.Organizer);
        Assert.Equal("Room C", evt.Location);
        Assert.Equal("UTC", evt.OriginalTimeZone);
        Assert.Null(evt.UserId);
    }

    [Fact]
    public async Task ExecuteAsync_WithSourceProvider_PreservesCalendarEventDtoUserId()
    {
        var store = Substitute.For<ICalendarEventStore>();
        CalendarEvent? captured = null;
        store.When(s => s.SaveAsync(Arg.Any<CalendarEvent>(), Arg.Any<CancellationToken>()))
            .Do(ci => captured = ci.Arg<CalendarEvent>());

        var provider = Substitute.For<IMessageSourceProvider<CalendarEventDto>>();
        provider.FetchAsync(Arg.Any<ConnectorExecutionRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<CalendarEventDto>>(
            [
                new CalendarEventDto
                {
                    ExternalId = "graph-event-user",
                    Subject = "Owned event",
                    Start = new DateTimeOffset(2026, 6, 24, 10, 0, 0, TimeSpan.Zero),
                    End = new DateTimeOffset(2026, 6, 24, 11, 0, 0, TimeSpan.Zero),
                    IsCancelled = false,
                    UserId = "oid-42"
                }
            ]));

        var adapter = new CalendarConnectorAdapter(
            NullLogger<CalendarConnectorAdapter>.Instance,
            store,
            new CalendarEventMapper(),
            sourceProvider: provider);

        var request = new ConnectorExecutionRequest(
            new CheckpointIdentity("calendar", "calendar", "acme", userOid: "oid-request"),
            new DateTimeOffset(2026, 06, 24, 00, 00, 00, TimeSpan.Zero),
            new DateTimeOffset(2026, 06, 24, 23, 59, 59, TimeSpan.Zero));

        await adapter.ExecuteAsync(request, CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal("oid-42", captured!.UserId);
    }

    [Fact]
    public async Task ExecuteAsync_ProviderThrows_ReturnsFailure()
    {
        var store = Substitute.For<ICalendarEventStore>();
        var provider = Substitute.For<IMessageSourceProvider<CalendarEventDto>>();
        var request = CreateRequest();
        provider.FetchAsync(request, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IReadOnlyList<CalendarEventDto>>(new InvalidOperationException("Graph API error")));

        var adapter = new CalendarConnectorAdapter(
            NullLogger<CalendarConnectorAdapter>.Instance,
            store,
            new CalendarEventMapper(),
            sourceProvider: provider);

        var result = await adapter.ExecuteAsync(request, CancellationToken.None);

        Assert.Equal(ConnectorExecutionStatus.Failure, result.Status);
        Assert.False(string.IsNullOrWhiteSpace(result.FailureReason));
    }

    private static ConnectorExecutionRequest CreateRequest() =>
        new(
            new CheckpointIdentity("calendar", "calendar", "acme"),
            new DateTimeOffset(2026, 06, 24, 00, 00, 00, TimeSpan.Zero),
            new DateTimeOffset(2026, 06, 24, 23, 59, 59, TimeSpan.Zero));
}
