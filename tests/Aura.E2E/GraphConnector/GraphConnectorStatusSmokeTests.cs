using System.Net;
using Aura.UI;
using Aura.UI.Models;
using Aura.UI.Services;
using Aura.E2E.Shared;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Aura.E2E.GraphConnector;

public class GraphConnectorStatusSmokeTests : IClassFixture<WebApplicationFactory<UiMarker>>
{
    private readonly WebApplicationFactory<UiMarker> _factory;

    public GraphConnectorStatusSmokeTests(WebApplicationFactory<UiMarker> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("AuraApi:BaseUrl", "https://api.aura.test");
        });
    }

    [Theory]
    [InlineData("Disabled", "graph-connector-state-disabled")]
    [InlineData("MissingConfig", "graph-connector-state-missing")]
    [InlineData("PartialConfig", "graph-connector-state-partial")]
    [InlineData("ValidConfig", "graph-connector-state-valid")]
    public async Task GetRoot_RendersExpectedGraphConnectorState_WithoutEditControls(string state, string expectedTestId)
    {
        var client = CreateClient(new StubGraphConnectorApiClient(new GraphConnectorStatusResponse(state)));

        var response = await client.GetAsync("/dashboard");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains($"data-testid=\"{expectedTestId}\"", html);
        Assert.DoesNotContain("data-testid=\"graph-connector-edit\"", html);
        Assert.DoesNotContain("data-testid=\"graph-connector-save\"", html);
    }

    private HttpClient CreateClient(IGraphConnectorApiClient graphConnectorApiClient)
    {
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddAuthenticatedUiTestUser();
                services.AddStubFocusStateApiClient();
                services.RemoveAll<IGraphConnectorApiClient>();
                services.AddScoped(_ => graphConnectorApiClient);
                services.AddScoped<ISyncApiClient>(_ => new StubSyncClient());
            });
        });

        return factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    private sealed class StubGraphConnectorApiClient : IGraphConnectorApiClient
    {
        private readonly GraphConnectorStatusResponse _response;

        public StubGraphConnectorApiClient(GraphConnectorStatusResponse response)
        {
            _response = response;
        }

        public Task<GraphConnectorStatusResponse> GetStatusAsync(CancellationToken cancellationToken)
            => Task.FromResult(_response);
    }

    private sealed class StubSyncClient : ISyncApiClient
    {
        public Task<List<SourceSyncStateDto>> GetSyncStatusAsync(CancellationToken cancellationToken)
            => Task.FromResult(new List<SourceSyncStateDto>());

        public Task TriggerSyncAsync(CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
