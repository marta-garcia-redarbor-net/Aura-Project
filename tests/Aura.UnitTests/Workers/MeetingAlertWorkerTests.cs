using Aura.Application.Ports;
using Aura.Application.UseCases.Calendar;
using Aura.Domain.Calendar;
using Aura.Workers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using IAccount = Microsoft.Identity.Client.IAccount;
using IPublicClientApplication = Microsoft.Identity.Client.IPublicClientApplication;
using AccountId = Microsoft.Identity.Client.AccountId;
using NSubstitute;

namespace Aura.UnitTests.Workers;

public class MeetingAlertWorkerTests
{
    private readonly ICalendarEventStore _eventStore = Substitute.For<ICalendarEventStore>();
    private readonly IMeetingAlertStore _alertStore = Substitute.For<IMeetingAlertStore>();
    private readonly IMeetingAlertDispatcher _dispatcher = Substitute.For<IMeetingAlertDispatcher>();
    private readonly ILogger<CheckAndDispatchMeetingAlertsUseCase> _logger = Substitute.For<ILogger<CheckAndDispatchMeetingAlertsUseCase>>();
    private readonly IHostApplicationLifetime _lifetime = Substitute.For<IHostApplicationLifetime>();

    [Fact]
    public async Task ExecuteAsync_CreatesScopeAndRunsUseCase()
    {
        // Arrange
        var account = CreateAccount("oid-1");
        var msalApp = Substitute.For<IPublicClientApplication>();
#pragma warning disable CS0618
        msalApp.GetAccountsAsync().Returns(Task.FromResult((IEnumerable<IAccount>)[account]));
#pragma warning restore CS0618

        var services = new ServiceCollection();
        services.AddSingleton(_eventStore);
        services.AddSingleton(_alertStore);
        services.AddSingleton(_dispatcher);
        services.AddSingleton(_logger);
        services.AddSingleton(msalApp);
        services.AddScoped<CheckAndDispatchMeetingAlertsUseCase>();
        var provider = services.BuildServiceProvider();

        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        var workerLogger = Substitute.For<ILogger<MeetingAlertWorker>>();

        var worker = new MeetingAlertWorker(scopeFactory, _lifetime, workerLogger);

        // Use a CTS that cancels after a short delay to stop the loop
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

        // Act
        await worker.StartAsync(cts.Token);

        // Give the worker time to execute at least one poll cycle
        await Task.Delay(500);

        // Assert — the use case was invoked (event store called at least once)
        await _eventStore.Received(1).GetUpcomingAsync(
            Arg.Any<string>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_OperationCancelled_DoesNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(_eventStore);
        services.AddSingleton(_alertStore);
        services.AddSingleton(_dispatcher);
        services.AddSingleton(_logger);
        services.AddScoped<CheckAndDispatchMeetingAlertsUseCase>();
        var provider = services.BuildServiceProvider();

        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        var workerLogger = Substitute.For<ILogger<MeetingAlertWorker>>();

        var worker = new MeetingAlertWorker(scopeFactory, _lifetime, workerLogger);

        // Act — cancel immediately
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Should not throw
        await worker.StartAsync(cts.Token);

        // Give worker a moment to process cancellation
        await Task.Delay(100);
    }

    [Fact]
    public async Task ExecuteAsync_TokenCacheHasTwoUsers_ProcessesBothUserIds_NeverDefault()
    {
        // Arrange
        var account1 = CreateAccount("oid-user-1");
        var account2 = CreateAccount("oid-user-2");
        var msalApp = Substitute.For<IPublicClientApplication>();
#pragma warning disable CS0618
        msalApp.GetAccountsAsync().Returns(Task.FromResult((IEnumerable<IAccount>)[account1, account2]));
#pragma warning restore CS0618

        var services = new ServiceCollection();
        services.AddSingleton(_eventStore);
        services.AddSingleton(_alertStore);
        services.AddSingleton(_dispatcher);
        services.AddSingleton(_logger);
        services.AddSingleton(msalApp);
        services.AddScoped<CheckAndDispatchMeetingAlertsUseCase>();
        var provider = services.BuildServiceProvider();

        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        var workerLogger = Substitute.For<ILogger<MeetingAlertWorker>>();
        var worker = new MeetingAlertWorker(scopeFactory, _lifetime, workerLogger);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

        // Act
        await worker.StartAsync(cts.Token);
        await Task.Delay(500);

        // Assert
        await _eventStore.Received(1).GetUpcomingAsync(
            "oid-user-1",
            Arg.Any<DateTimeOffset>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>());

        await _eventStore.Received(1).GetUpcomingAsync(
            "oid-user-2",
            Arg.Any<DateTimeOffset>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>());

        await _eventStore.DidNotReceive().GetUpcomingAsync(
            "default",
            Arg.Any<DateTimeOffset>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_NoUsersInTokenCache_DoesNotProcessAlerts()
    {
        // Arrange
        var msalApp = Substitute.For<IPublicClientApplication>();
#pragma warning disable CS0618
        msalApp.GetAccountsAsync().Returns(Task.FromResult(Enumerable.Empty<IAccount>()));
#pragma warning restore CS0618

        var services = new ServiceCollection();
        services.AddSingleton(_eventStore);
        services.AddSingleton(_alertStore);
        services.AddSingleton(_dispatcher);
        services.AddSingleton(_logger);
        services.AddSingleton(msalApp);
        services.AddScoped<CheckAndDispatchMeetingAlertsUseCase>();
        var provider = services.BuildServiceProvider();

        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        var workerLogger = Substitute.For<ILogger<MeetingAlertWorker>>();
        var worker = new MeetingAlertWorker(scopeFactory, _lifetime, workerLogger);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

        // Act
        await worker.StartAsync(cts.Token);
        await Task.Delay(500);

        // Assert
        await _eventStore.DidNotReceive().GetUpcomingAsync(
            Arg.Any<string>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>());
    }

    private static IAccount CreateAccount(string oid)
    {
        var account = Substitute.For<IAccount>();
        account.HomeAccountId.Returns(new AccountId(oid, oid, null));
        return account;
    }
}
