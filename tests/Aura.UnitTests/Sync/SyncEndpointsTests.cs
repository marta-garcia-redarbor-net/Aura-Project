using System.Reflection;
using Aura.Api.Endpoints;
using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Application.UseCases.IngestionSync;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Aura.UnitTests.Sync;

public class SyncEndpointsTests
{
    private static readonly MethodInfo PostSyncNowMethod = typeof(SyncEndpoints)
        .GetMethod("PostSyncNowAsync", BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new InvalidOperationException("SyncEndpoints.PostSyncNowAsync method not found");

    [Fact]
    public async Task PostSyncNowAsync_WhenCurrentUserOidMissing_ReturnsUnauthorized_AndSkipsConnectorExecution()
    {
        var adapter = new CapturingAdapter();
        var syncStore = Substitute.For<ISyncStateStore>();
        var useCase = new TriggerSyncUseCase(
            [adapter],
            syncStore,
            NullLogger<TriggerSyncUseCase>.Instance);

        var currentUserService = Substitute.For<ICurrentUserService>();
        currentUserService.GetCurrentUser().Returns(new AuraUser
        {
            UserId = "demo-user",
            DisplayName = "Demo User",
            Email = "demo@aura.local",
            Oid = null,
            TenantId = "tenant-demo"
        });

        var loggerFactory = NullLoggerFactory.Instance;

        var task = (Task<IResult>)PostSyncNowMethod.Invoke(
            null,
            [useCase, currentUserService, loggerFactory, CancellationToken.None])!;

        var result = await task;
        var services = new ServiceCollection();
        services.AddSingleton<Microsoft.Extensions.Logging.ILoggerFactory>(NullLoggerFactory.Instance);
        var httpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
        await result.ExecuteAsync(httpContext);

        Assert.Equal(StatusCodes.Status401Unauthorized, httpContext.Response.StatusCode);
        Assert.Equal(0, adapter.ExecuteCount);
    }

    [Fact]
    public async Task PostSyncNowAsync_WhenCurrentUserOidPresent_PropagatesOidToUseCaseExecution()
    {
        var adapter = new CapturingAdapter();
        var syncStore = Substitute.For<ISyncStateStore>();
        var useCase = new TriggerSyncUseCase(
            [adapter],
            syncStore,
            NullLogger<TriggerSyncUseCase>.Instance);

        var currentUserService = Substitute.For<ICurrentUserService>();
        currentUserService.GetCurrentUser().Returns(new AuraUser
        {
            UserId = "real-user",
            DisplayName = "Real User",
            Email = "real@aura.local",
            Oid = "oid-real-1",
            TenantId = "tenant-real"
        });

        var loggerFactory = NullLoggerFactory.Instance;

        var task = (Task<IResult>)PostSyncNowMethod.Invoke(
            null,
            [useCase, currentUserService, loggerFactory, CancellationToken.None])!;

        var result = await task;
        var services = new ServiceCollection();
        services.AddSingleton<Microsoft.Extensions.Logging.ILoggerFactory>(NullLoggerFactory.Instance);
        var httpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
        await result.ExecuteAsync(httpContext);

        Assert.Equal(StatusCodes.Status200OK, httpContext.Response.StatusCode);
        Assert.Equal("oid-real-1", adapter.CapturedIdentity?.UserOid);
    }

    private sealed class CapturingAdapter : IConnectorAdapter
    {
        public string ConnectorName => "outlook";

        public int ExecuteCount { get; private set; }

        public CheckpointIdentity? CapturedIdentity { get; private set; }

        public Task<ConnectorExecutionResult> ExecuteAsync(ConnectorExecutionRequest request, CancellationToken ct)
        {
            ExecuteCount++;
            CapturedIdentity = request.Identity;
            return Task.FromResult(new ConnectorExecutionResult(request.Identity, 1, ConnectorExecutionStatus.Success));
        }
    }
}
