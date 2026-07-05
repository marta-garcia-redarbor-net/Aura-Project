using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Aura.Api;
using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Domain.FocusState;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Aura.IntegrationTests.Triage;

public class FocusStateAndTriageWebApplicationFactoryTests : IClassFixture<WebApplicationFactory<ApiMarker>>
{
    private readonly WebApplicationFactory<ApiMarker> _factory;

    public FocusStateAndTriageWebApplicationFactoryTests(WebApplicationFactory<ApiMarker> factory)
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
    public async Task AuthenticatedClient_UsingWebApplicationFactory_CanCallFocusStateAndTriageEndpoints()
    {
        var focusStateResolver = new StubFocusStateResolver(FocusStateType.DeepWork);
        var overrideStore = new StubOverrideStore(FocusStateType.DeepWork);
        var decisionStore = new StubDecisionStore(
        [
            new InterruptionDecisionRecord(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "Urgent PR review",
                "pr-review",
                "INTERRUPT",
                88,
                "Urgency exceeded threshold",
                DateTimeOffset.UtcNow,
                "WindowOfOpportunity")
        ]);

        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IFocusStateResolver>(focusStateResolver);
                services.AddSingleton<IFocusStateOverrideStore>(overrideStore);
                services.AddSingleton<IInterruptionDecisionStore>(decisionStore);
            });
        });

        var client = factory.CreateClient();
        var token = await GetMockTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var focusResponse = await client.GetAsync("/api/focus-state");
        Assert.Equal(HttpStatusCode.OK, focusResponse.StatusCode);
        var focusJson = JsonDocument.Parse(await focusResponse.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("DeepWork", focusJson.GetProperty("state").GetString());
        Assert.True(focusJson.GetProperty("isOverridden").GetBoolean());
        Assert.Equal("mock-user-001", focusJson.GetProperty("userId").GetString());

        var triageResponse = await client.GetAsync("/api/triage/decisions?page=1&pageSize=20");
        Assert.Equal(HttpStatusCode.OK, triageResponse.StatusCode);
        var triageJson = JsonDocument.Parse(await triageResponse.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(1, triageJson.GetProperty("totalCount").GetInt32());
        Assert.Equal(1, triageJson.GetProperty("items").GetArrayLength());
    }

    private static async Task<string> GetMockTokenAsync(HttpClient client)
    {
        var loginResponse = await client.PostAsync("/api/auth/mock-login", null);
        var content = await loginResponse.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(content);
        return json.RootElement.GetProperty("token").GetString()!;
    }

    private sealed class StubFocusStateResolver(FocusStateType state) : IFocusStateResolver
    {
        public Task<Domain.FocusState.FocusState> ResolveAsync(string userId, CancellationToken cancellationToken = default)
            => Task.FromResult(new Domain.FocusState.FocusState(state));
    }

    private sealed class StubOverrideStore(FocusStateType? state) : IFocusStateOverrideStore
    {
        public Task<FocusStateType?> GetAsync(string userId, CancellationToken cancellationToken = default)
            => Task.FromResult(state);

        public Task SetAsync(string userId, FocusStateType state)
            => Task.CompletedTask;

        public Task ClearAsync(string userId)
            => Task.CompletedTask;
    }

    private sealed class StubDecisionStore(IReadOnlyList<InterruptionDecisionRecord> records) : IInterruptionDecisionStore
    {
        public Task RecordAsync(InterruptionDecisionRecord record, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<PagedResult<InterruptionDecisionRecord>> QueryAsync(int page, int pageSize, CancellationToken cancellationToken = default)
            => Task.FromResult(new PagedResult<InterruptionDecisionRecord>
            {
                Items = records,
                TotalCount = records.Count,
                Page = page,
                PageSize = pageSize
            });
    }
}
