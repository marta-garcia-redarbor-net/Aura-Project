using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Aura.Api;
using Aura.Application.Models;
using Aura.Application.Ports;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Aura.IntegrationTests.Dashboard;

/// <summary>
/// Integration tests for <c>GET /api/dashboard/initial</c>.
/// These verify the HTTP contract (auth, response shape, empty/populated) through
/// <see cref="WebApplicationFactory{TEntryPoint}"/> against Aura.Api. The UI host
/// (Aura.UI) consumes this endpoint over HTTP only — it never bypasses the API boundary.
/// </summary>
public class InitialDashboardEndpointTests : IClassFixture<WebApplicationFactory<ApiMarker>>
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly WebApplicationFactory<ApiMarker> _factory;

    public InitialDashboardEndpointTests(WebApplicationFactory<ApiMarker> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("EmbeddingProvider:Endpoint", "https://test.openai.azure.com");
            builder.UseSetting("EmbeddingProvider:DeploymentName", "test-model");
            builder.UseSetting("EmbeddingProvider:ApiKey", "fake-key");
            builder.UseSetting("MockJwt:Key",
                "aura-test-key-for-integration-tests-minimum-32-characters!");
        });
    }

    [Fact]
    public async Task GetInitialDashboard_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/dashboard/initial");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetInitialDashboard_WithTokenAndCards_Returns200PopulatedPayload()
    {
        var client = CreateAuthenticatedClient(new InitialDashboardDto(
            "Mock User",
            [new DashboardCardDto("Signed in as", "Mock User", "info")]));

        var response = await client.GetAsync("/api/dashboard/initial");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await ReadDashboardAsync(response);
        Assert.Equal("Mock User", payload.UserDisplayName);
        var card = Assert.Single(payload.Cards);
        Assert.Equal("Signed in as", card.Title);
        Assert.Equal("Mock User", card.Value);
        Assert.Equal("info", card.Status);
    }

    [Fact]
    public async Task GetInitialDashboard_WithTokenAndNoCards_Returns200EmptyPayload()
    {
        var client = CreateAuthenticatedClient(new InitialDashboardDto("Mock User", []));

        var response = await client.GetAsync("/api/dashboard/initial");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await ReadDashboardAsync(response);
        Assert.Equal("Mock User", payload.UserDisplayName);
        Assert.Empty(payload.Cards);
    }

    private HttpClient CreateAuthenticatedClient(InitialDashboardDto dashboard)
    {
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IInitialDashboardReader>(new StubInitialDashboardReader(dashboard));
            });
        });

        var client = factory.CreateClient();
        var token = GetMockTokenAsync(client).GetAwaiter().GetResult();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static async Task<string> GetMockTokenAsync(HttpClient client)
    {
        var loginResponse = await client.PostAsync("/api/auth/mock-login", null);
        var content = await loginResponse.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(content);
        return json.RootElement.GetProperty("token").GetString()!;
    }

    private static async Task<InitialDashboardDto> ReadDashboardAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<InitialDashboardDto>(content, SerializerOptions)!;
    }

    [Fact]
    public async Task GetInitialDashboard_WhenReaderThrows_Returns500Problem()
    {
        var client = CreateAuthenticatedClientWithReader(
            new ThrowingInitialDashboardReader(new InvalidOperationException("Reader exploded")));

        var response = await client.GetAsync("/api/dashboard/initial");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Dashboard request failed", content);
    }

    [Fact]
    public async Task GetInitialDashboard_WhenReaderReturnsCanceledTask_TestHostCurrentlySurfaces500()
    {
        var client = CreateAuthenticatedClientWithReader(
            new CancellingInitialDashboardReader());

        // This assertion documents the current WebApplicationFactory/TestServer behavior
        // for a cancelled reader task. It is not a claim that request cancellation is
        // semantically a server fault in production hosting.
        var response = await client.GetAsync("/api/dashboard/initial");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task GetInitialDashboard_WhenRequestIsCancelled_PropagatesRequestTokenToReader()
    {
        var reader = new RequestCancellationObservingDashboardReader();
        var client = CreateAuthenticatedClientWithReader(reader);
        using var requestCancellation = new CancellationTokenSource();

        var requestTask = client.GetAsync("/api/dashboard/initial", requestCancellation.Token);

        await reader.WaitForInvocationAsync();

        requestCancellation.Cancel();

        await reader.WaitForCancellationAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await requestTask);
        Assert.True(reader.ObservedCancellationRequested);
    }

    private HttpClient CreateAuthenticatedClientWithReader(IInitialDashboardReader reader)
    {
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton(reader);
            });
        });

        var client = factory.CreateClient();
        var token = GetMockTokenAsync(client).GetAwaiter().GetResult();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private sealed class StubInitialDashboardReader : IInitialDashboardReader
    {
        private readonly InitialDashboardDto _dashboard;

        public StubInitialDashboardReader(InitialDashboardDto dashboard)
        {
            _dashboard = dashboard;
        }

        public Task<InitialDashboardDto> GetAsync(CancellationToken cancellationToken)
            => Task.FromResult(_dashboard);
    }

    private sealed class ThrowingInitialDashboardReader : IInitialDashboardReader
    {
        private readonly Exception _exception;

        public ThrowingInitialDashboardReader(Exception exception)
        {
            _exception = exception;
        }

        public Task<InitialDashboardDto> GetAsync(CancellationToken cancellationToken)
            => Task.FromException<InitialDashboardDto>(_exception);
    }

    private sealed class CancellingInitialDashboardReader : IInitialDashboardReader
    {
        public Task<InitialDashboardDto> GetAsync(CancellationToken cancellationToken)
            => Task.FromCanceled<InitialDashboardDto>(new CancellationToken(canceled: true));
    }

    private sealed class RequestCancellationObservingDashboardReader : IInitialDashboardReader
    {
        private readonly TaskCompletionSource<bool> _invoked = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<bool> _cancelled = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool ObservedCancellationRequested { get; private set; }

        public Task<bool> WaitForInvocationAsync() => _invoked.Task;

        public Task<bool> WaitForCancellationAsync() => _cancelled.Task;

        public Task<InitialDashboardDto> GetAsync(CancellationToken cancellationToken)
        {
            _invoked.TrySetResult(true);

            cancellationToken.Register(() =>
            {
                ObservedCancellationRequested = true;
                _cancelled.TrySetResult(true);
            });

            var result = new TaskCompletionSource<InitialDashboardDto>(TaskCreationOptions.RunContinuationsAsynchronously);
            cancellationToken.Register(() => result.TrySetCanceled(cancellationToken));
            return result.Task;
        }
    }
}
