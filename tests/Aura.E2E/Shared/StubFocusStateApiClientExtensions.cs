using Aura.UI.Models;
using Aura.UI.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Aura.E2E.Shared;

/// <summary>
/// Registers a deterministic <see cref="IFocusStateApiClient"/> stub so HTTP-only
/// smoke tests can render the shared layout (which includes the focus-state badge)
/// without reaching a real Aura.Api host.
/// </summary>
internal static class StubFocusStateApiClientServiceCollectionExtensions
{
    public static IServiceCollection AddStubFocusStateApiClient(
        this IServiceCollection services,
        string state = "WindowOfOpportunity")
    {
        services.RemoveAll<IFocusStateApiClient>();
        services.AddScoped<IFocusStateApiClient>(_ => new StubFocusStateApiClient(
            new FocusStateResponse(State: state, IsOverridden: false, UserId: "test-user-001")));

        services.RemoveAll<IFocusStateRefreshScheduler>();
        services.AddSingleton<IFocusStateRefreshScheduler, NoopFocusStateRefreshScheduler>();

        return services;
    }

    private sealed class StubFocusStateApiClient(FocusStateResponse response) : IFocusStateApiClient
    {
        public Task<FocusStateResponse> GetCurrentAsync(CancellationToken cancellationToken)
            => Task.FromResult(response);

        public Task SetOverrideAsync(string state, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task ClearOverrideAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class NoopFocusStateRefreshScheduler : IFocusStateRefreshScheduler
    {
        public IDisposable StartRecurring(TimeSpan interval, Func<Task> callback)
            => EmptyDisposable.Instance;

        private sealed class EmptyDisposable : IDisposable
        {
            public static readonly EmptyDisposable Instance = new();
            public void Dispose() { }
        }
    }
}
