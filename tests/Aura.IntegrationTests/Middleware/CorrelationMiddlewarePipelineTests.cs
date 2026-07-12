using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aura.Api;
using Aura.Application.Models;
using Aura.Application.Ports;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aura.IntegrationTests.Middleware;

public class CorrelationMiddlewarePipelineTests : IClassFixture<WebApplicationFactory<ApiMarker>>
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
    private readonly WebApplicationFactory<ApiMarker> _factory;

    public CorrelationMiddlewarePipelineTests(WebApplicationFactory<ApiMarker> factory)
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
    public async Task AllResponses_IncludeXCorrelationId()
    {
        var client = _factory.CreateClient();

        // Hit the root endpoint (no auth required)
        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("X-Correlation-Id"),
            "All responses should include X-Correlation-Id header");
    }

    [Fact]
    public async Task RequestWithHeader_ForwardsSameCorrelationId()
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add("X-Correlation-Id", "my-trace-42");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("X-Correlation-Id"));
        Assert.Equal("my-trace-42", response.Headers.GetValues("X-Correlation-Id").First());
    }

    [Fact]
    public async Task RequestWithoutHeader_GeneratesNewGuid()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("X-Correlation-Id"));
        var correlationId = response.Headers.GetValues("X-Correlation-Id").First();
        Assert.True(Guid.TryParse(correlationId, out _),
            $"The generated correlation ID '{correlationId}' should be a valid GUID");
    }

    [Fact]
    public async Task DashboardSystemStatus_LogsContainCorrelationScopeAndEntryExitPayload()
    {
        var logEntries = new List<TestLogEntry>();
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<ISystemStatusReader>(new StubSystemStatusReader());
                services.AddSingleton<ILoggerProvider>(_ => new CollectingLoggerProvider(logEntries));
            });
        }).CreateClient();

        var token = await GetMockTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/dashboard/system-status");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("X-Correlation-Id"));
        var correlationId = response.Headers.GetValues("X-Correlation-Id").Single();

        var started = logEntries
            .Where(entry => entry.EventId == 2001
                && string.Equals(entry.State.GetValueOrDefault("Path")?.ToString(), "/api/dashboard/system-status", StringComparison.Ordinal))
            .Single();

        var completed = logEntries
            .Where(entry => entry.EventId == 2002
                && string.Equals(entry.State.GetValueOrDefault("Path")?.ToString(), "/api/dashboard/system-status", StringComparison.Ordinal)
                && string.Equals(entry.State.GetValueOrDefault("StatusCode")?.ToString(), "200", StringComparison.Ordinal))
            .Single();

        Assert.Equal("GET", started.State["Method"]?.ToString());
        Assert.Equal("/api/dashboard/system-status", started.State["Path"]?.ToString());
        Assert.Equal(correlationId, started.Scope["CorrelationId"]?.ToString());

        Assert.Equal("GET", completed.State["Method"]?.ToString());
        Assert.Equal("/api/dashboard/system-status", completed.State["Path"]?.ToString());
        Assert.Equal("200", completed.State["StatusCode"]?.ToString());
        Assert.Equal(correlationId, completed.Scope["CorrelationId"]?.ToString());
        Assert.True(Convert.ToInt64(completed.State["ElapsedMilliseconds"]) >= 0);
    }

    [Fact]
    public async Task DashboardException_RecordsErrorEntryUsingRequestCorrelationId()
    {
        var store = new StubErrorStore();
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<ISystemStatusReader>(new ThrowingSystemStatusReader());
                services.AddSingleton<IErrorStore>(store);
            });
        }).CreateClient();

        var token = await GetMockTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/dashboard/system-status");
        request.Headers.Add("X-Correlation-Id", "corr-dashboard-err-1");

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        var recentErrorsResponse = await client.GetAsync("/api/dashboard/recent-errors");
        Assert.Equal(HttpStatusCode.OK, recentErrorsResponse.StatusCode);

        var content = await recentErrorsResponse.Content.ReadAsStringAsync();
        var errors = JsonSerializer.Deserialize<List<ErrorEntryDto>>(content, SerializerOptions) ?? [];

        var recorded = Assert.Single(errors);
        Assert.Equal("corr-dashboard-err-1", recorded.CorrelationId);
        Assert.Contains("GET /api/dashboard/system-status", recorded.Message, StringComparison.Ordinal);
    }

    private static async Task<string> GetMockTokenAsync(HttpClient client)
    {
        var loginResponse = await client.PostAsync("/api/auth/mock-login", null);
        loginResponse.EnsureSuccessStatusCode();
        var content = await loginResponse.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(content);
        return json.RootElement.GetProperty("token").GetString()!;
    }

    private sealed class StubSystemStatusReader : ISystemStatusReader
    {
        public Task<SystemStatusDto> GetStatusAsync(CancellationToken cancellationToken)
            => Task.FromResult(new SystemStatusDto(
                new SystemIndicatorDto(SystemIndicatorState.Ok, "API OK"),
                new SystemIndicatorDto(SystemIndicatorState.Ok, "Qdrant OK"),
                new SystemIndicatorDto(SystemIndicatorState.Ok, "MockAuth OK"),
                new SystemIndicatorDto(SystemIndicatorState.Ok, "DB OK"),
                new SystemIndicatorDto(SystemIndicatorState.Ok, "LLM OK")));
    }

    private sealed class ThrowingSystemStatusReader : ISystemStatusReader
    {
        public Task<SystemStatusDto> GetStatusAsync(CancellationToken cancellationToken)
            => throw new InvalidOperationException("forced dashboard failure");
    }

    private sealed class StubErrorStore : IErrorStore
    {
        private readonly List<ErrorEntry> _entries = [];

        public Task RecordAsync(ErrorEntry entry, CancellationToken ct = default)
        {
            _entries.Add(entry);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ErrorEntry>> GetRecentAsync(int count, CancellationToken ct = default)
        {
            var result = _entries.OrderByDescending(e => e.Timestamp).Take(count).ToList().AsReadOnly();
            return Task.FromResult<IReadOnlyList<ErrorEntry>>(result);
        }
    }

    private sealed class CollectingLoggerProvider(List<TestLogEntry> entries) : ILoggerProvider
    {
        private readonly AsyncLocal<Stack<IReadOnlyDictionary<string, object?>>> _scopeStack = new();

        public ILogger CreateLogger(string categoryName) => new CollectingLogger(entries, _scopeStack);
        public void Dispose() { }

        private sealed class CollectingLogger(
            List<TestLogEntry> entries,
            AsyncLocal<Stack<IReadOnlyDictionary<string, object?>>> scopeStack) : ILogger
        {
            public IDisposable BeginScope<TState>(TState state) where TState : notnull
            {
                var stack = scopeStack.Value ??= new Stack<IReadOnlyDictionary<string, object?>>();
                stack.Push(ToDictionary(state));
                return new PopScope(stack);
            }

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                var scope = MergeScope(scopeStack.Value);
                var stateValues = ToDictionary(state);

                lock (entries)
                {
                    entries.Add(new TestLogEntry(eventId.Id, logLevel, formatter(state, exception), stateValues, scope));
                }
            }

            private static IReadOnlyDictionary<string, object?> MergeScope(Stack<IReadOnlyDictionary<string, object?>>? stack)
            {
                if (stack is null || stack.Count == 0)
                {
                    return new Dictionary<string, object?>();
                }

                var merged = new Dictionary<string, object?>(StringComparer.Ordinal);
                foreach (var scope in stack.Reverse())
                {
                    foreach (var kv in scope)
                    {
                        merged[kv.Key] = kv.Value;
                    }
                }

                return merged;
            }

            private sealed class PopScope(Stack<IReadOnlyDictionary<string, object?>> stack) : IDisposable
            {
                public void Dispose()
                {
                    if (stack.Count > 0)
                    {
                        stack.Pop();
                    }
                }
            }
        }

        private static IReadOnlyDictionary<string, object?> ToDictionary<TState>(TState state)
        {
            if (state is IEnumerable<KeyValuePair<string, object?>> kvps)
            {
                var dict = new Dictionary<string, object?>(StringComparer.Ordinal);
                foreach (var kv in kvps)
                {
                    dict[kv.Key] = kv.Value;
                }

                return dict;
            }

            return new Dictionary<string, object?>
            {
                ["State"] = state
            };
        }
    }

    private sealed record TestLogEntry(
        int EventId,
        LogLevel Level,
        string Message,
        IReadOnlyDictionary<string, object?> State,
        IReadOnlyDictionary<string, object?> Scope);

    private sealed record ErrorEntryDto(string CorrelationId, DateTimeOffset Timestamp, string Message);
}
