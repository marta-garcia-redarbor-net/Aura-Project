using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Aura.Api;
using Aura.Application.Ports;
using FocusStateType = Aura.Domain.FocusState.FocusStateType;
using DomFocusState = Aura.Domain.FocusState.FocusState;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Aura.IntegrationTests.FocusState;

public class FocusStateEndpointTests : IClassFixture<WebApplicationFactory<ApiMarker>>
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly WebApplicationFactory<ApiMarker> _factory;

    public FocusStateEndpointTests(WebApplicationFactory<ApiMarker> factory)
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

    // ============================================================
    // Auth guard
    // ============================================================

    [Fact]
    public async Task GetFocusState_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/focus-state");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PutFocusState_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PutAsync(
            "/api/focus-state",
            new StringContent("{\"state\":\"DeepWork\"}", System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteFocusState_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.DeleteAsync("/api/focus-state");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ============================================================
    // GET
    // ============================================================

    [Fact]
    public async Task GetFocusState_WithNoOverride_ReturnsAutoComputedState()
    {
        var overrideStore = new StubOverrideStore();
        var client = CreateAuthenticatedClient(
            new StubResolver(FocusStateType.WindowOfOpportunity),
            overrideStore);

        var response = await client.GetAsync("/api/focus-state");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await ReadPayloadAsync(response);
        Assert.Equal("WindowOfOpportunity", body.State);
        Assert.False(body.IsOverridden);
        Assert.Equal("mock-user-001", body.UserId);
    }

    [Fact]
    public async Task GetFocusState_WithActiveOverride_ReturnsOverriddenStateWithFlag()
    {
        var overrideStore = new StubOverrideStore { StoredOverride = FocusStateType.DeepWork };
        var resolver = new StubResolver(FocusStateType.DeepWork);
        var client = CreateAuthenticatedClient(resolver, overrideStore);

        var response = await client.GetAsync("/api/focus-state");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await ReadPayloadAsync(response);
        Assert.Equal("DeepWork", body.State);
        Assert.True(body.IsOverridden);
        Assert.Equal("mock-user-001", body.UserId);
    }

    // ============================================================
    // PUT
    // ============================================================

    [Fact]
    public async Task PutFocusState_SetsOverride_Returns200()
    {
        var overrideStore = new StubOverrideStore();
        var resolver = new StubResolver(FocusStateType.WindowOfOpportunity);
        var client = CreateAuthenticatedClient(resolver, overrideStore);

        var response = await client.PutAsync(
            "/api/focus-state",
            new StringContent("{\"state\":\"DeepWork\"}", System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Assert.Equal(FocusStateType.DeepWork, overrideStore.LastSetState);
    }

    [Fact]
    public async Task PutFocusState_WithNullState_ClearsOverride()
    {
        var overrideStore = new StubOverrideStore { StoredOverride = FocusStateType.DeepWork };
        var resolver = new StubResolver(FocusStateType.WindowOfOpportunity);
        var client = CreateAuthenticatedClient(resolver, overrideStore);

        var response = await client.PutAsync(
            "/api/focus-state",
            new StringContent("{\"state\":null}", System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(overrideStore.WasCleared);
    }

    [Fact]
    public async Task PutFocusState_WithLiteralNullBody_ClearsOverride()
    {
        var overrideStore = new StubOverrideStore { StoredOverride = FocusStateType.DeepWork };
        var resolver = new StubResolver(FocusStateType.WindowOfOpportunity);
        var client = CreateAuthenticatedClient(resolver, overrideStore);

        var response = await client.PutAsync(
            "/api/focus-state",
            new StringContent("null", System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(overrideStore.WasCleared);
    }

    // ============================================================
    // DELETE
    // ============================================================

    [Fact]
    public async Task DeleteFocusState_ClearsOverride_Returns200()
    {
        var overrideStore = new StubOverrideStore();
        var resolver = new StubResolver(FocusStateType.DeepWork);
        var client = CreateAuthenticatedClient(resolver, overrideStore);

        var response = await client.DeleteAsync("/api/focus-state");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(overrideStore.WasCleared);
    }

    // ============================================================
    // Error handling
    // ============================================================

    [Fact]
    public async Task GetFocusState_WhenResolverThrows_Returns500()
    {
        var client = CreateAuthenticatedClient(
            new ThrowingResolver(new InvalidOperationException("Resolver failed")),
            new StubOverrideStore());

        var response = await client.GetAsync("/api/focus-state");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Focus state request failed", content);
    }

    // ============================================================
    // Helpers
    // ============================================================

    private HttpClient CreateAuthenticatedClient(IFocusStateResolver resolver, IFocusStateOverrideStore overrideStore)
    {
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton(resolver);
                services.AddSingleton(overrideStore);
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

    private static async Task<FocusStateResponseDto> ReadPayloadAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<FocusStateResponseDto>(content, SerializerOptions)!;
    }

    // ============================================================
    // Test DTO matching the API contract
    // ============================================================

    private sealed record FocusStateResponseDto
    {
        public string State { get; init; } = "";
        public bool IsOverridden { get; init; }
        public string UserId { get; init; } = "";
    }

    // ============================================================
    // Stubs
    // ============================================================

    private sealed class StubResolver : IFocusStateResolver
    {
        private readonly FocusStateType _state;
        private readonly Exception? _exception;

        public StubResolver(FocusStateType state) => _state = state;

        public StubResolver(Exception exception) => _exception = exception;

        public Task<DomFocusState> ResolveAsync(string userId, CancellationToken cancellationToken = default)
        {
            if (_exception is not null)
                return Task.FromException<DomFocusState>(_exception);

            return Task.FromResult(new DomFocusState(_state));
        }
    }

    private sealed class ThrowingResolver : IFocusStateResolver
    {
        private readonly Exception _exception;

        public ThrowingResolver(Exception exception) => _exception = exception;

        public Task<DomFocusState> ResolveAsync(string userId, CancellationToken cancellationToken = default)
            => Task.FromException<DomFocusState>(_exception);
    }

    private sealed class StubOverrideStore : IFocusStateOverrideStore
    {
        public FocusStateType? StoredOverride { get; set; }
        public FocusStateType? LastSetState { get; private set; }
        public bool WasCleared { get; private set; }

        public Task<FocusStateType?> GetAsync(string userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(StoredOverride);
        }

        public Task SetAsync(string userId, FocusStateType state)
        {
            LastSetState = state;
            StoredOverride = state;
            return Task.CompletedTask;
        }

        public Task ClearAsync(string userId)
        {
            WasCleared = true;
            StoredOverride = null;
            return Task.CompletedTask;
        }
    }
}
