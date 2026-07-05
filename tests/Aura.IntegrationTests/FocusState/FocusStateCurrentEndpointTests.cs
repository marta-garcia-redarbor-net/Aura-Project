using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Aura.Api;
using Aura.Application.Ports;
using FocusStateDomain = Aura.Domain.FocusState.FocusState;
using FocusStateType = Aura.Domain.FocusState.FocusStateType;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Aura.IntegrationTests.FocusState;

public sealed class FocusStateCurrentEndpointTests : IClassFixture<WebApplicationFactory<ApiMarker>>
{
    private readonly WebApplicationFactory<ApiMarker> _factory;

    public FocusStateCurrentEndpointTests(WebApplicationFactory<ApiMarker> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("EmbeddingProvider:Endpoint", "https://test.openai.azure.com");
            builder.UseSetting("EmbeddingProvider:DeploymentName", "test-model");
            builder.UseSetting("EmbeddingProvider:ApiKey", "fake-key");
            builder.UseSetting("UseEntraId", "false");
            builder.UseSetting("MockJwt:Key",
                "aura-test-key-for-integration-tests-minimum-32-characters!");
        });
    }

    [Fact]
    public async Task GetCurrent_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/focus-state");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetCurrent_WithTokenAndDeepWorkState_Returns200WithCurrentState()
    {
        var client = CreateAuthenticatedClient(new StubFocusStateResolver(FocusStateType.DeepWork));

        var response = await client.GetAsync("/api/focus-state");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        Assert.Equal("DeepWork", doc.RootElement.GetProperty("state").GetString());
        Assert.True(doc.RootElement.TryGetProperty("isOverridden", out _));
        Assert.True(doc.RootElement.TryGetProperty("userId", out _));
    }

    [Fact]
    public async Task GetCurrent_WithToken_PassesAuthenticatedOidToResolver()
    {
        var resolver = new CapturingFocusStateResolver();
        var client = CreateAuthenticatedClient(resolver);

        var response = await client.GetAsync("/api/focus-state");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("mock-user-001", resolver.LastUserId);
    }

    private HttpClient CreateAuthenticatedClient(IFocusStateResolver resolver)
    {
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton(resolver);
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

    private sealed class StubFocusStateResolver(FocusStateType stateType) : IFocusStateResolver
    {
        public Task<FocusStateDomain> ResolveAsync(string userId, CancellationToken cancellationToken = default)
        {
            var state = new FocusStateDomain();
            if (stateType == FocusStateType.Away)
            {
                state.GoToAway();
            }
            else if (stateType == FocusStateType.DeepWork)
            {
                state.GoToAway();
                state.TryEnterDeepWork();
            }
            else if (stateType == FocusStateType.Recovery)
            {
                state.GoToAway();
                state.GoToRecovery();
            }

            return Task.FromResult(state);
        }
    }

    private sealed class CapturingFocusStateResolver : IFocusStateResolver
    {
        public string? LastUserId { get; private set; }

        public Task<FocusStateDomain> ResolveAsync(string userId, CancellationToken cancellationToken = default)
        {
            LastUserId = userId;
            return Task.FromResult(new FocusStateDomain());
        }
    }
}
