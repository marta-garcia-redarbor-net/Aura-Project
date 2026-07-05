using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Concurrent;
using System.Text;
using Aura.Api;
using Aura.Application.Models;
using Aura.Application.Ports;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Aura.IntegrationTests.GraphConnector;

public class GraphConnectorStatusEndpointTests : IClassFixture<WebApplicationFactory<ApiMarker>>
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
    private readonly WebApplicationFactory<ApiMarker> _factory;
    private readonly ConcurrentQueue<string> _logEntries = new();

    public GraphConnectorStatusEndpointTests(WebApplicationFactory<ApiMarker> factory)
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
            builder.ConfigureLogging(logging =>
            {
                logging.AddProvider(new InMemoryLoggerProvider(_logEntries));
            });
        });
    }

    [Fact]
    public async Task GetGraphConnectorStatus_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/connectors/graph/status");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(GraphConnectorState.Disabled)]
    [InlineData(GraphConnectorState.MissingConfig)]
    [InlineData(GraphConnectorState.PartialConfig)]
    [InlineData(GraphConnectorState.ValidConfig)]
    public async Task GetGraphConnectorStatus_WithToken_Returns200WithState(GraphConnectorState expectedState)
    {
        var client = CreateAuthenticatedClient(new GraphConnectorStatusDto(expectedState));

        var response = await client.GetAsync("/api/connectors/graph/status");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await ReadStatusAsync(response);
        Assert.Equal(expectedState, payload.State);
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    public async Task WriteVerbs_AreRejectedWith405(string method)
    {
        var client = CreateAuthenticatedClient(new GraphConnectorStatusDto(GraphConnectorState.ValidConfig));
        var request = new HttpRequestMessage(new HttpMethod(method), "/api/connectors/graph/status");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [Fact]
    public async Task GetGraphConnectorStatus_WithRealConfigBinding_FromBaseSettings_ReturnsDisabled()
    {
        var client = CreateAuthenticatedClientWithConfiguration((builder, _) =>
        {
            builder.UseSetting("GraphConnector:Enabled", "false");
            builder.UseSetting("GraphConnector:TenantId", "tenant");
            builder.UseSetting("GraphConnector:ClientId", "client");
            builder.UseSetting("GraphConnector:ClientSecret", "secret");
        });

        var response = await client.GetAsync("/api/connectors/graph/status");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await ReadStatusAsync(response);
        Assert.Equal(GraphConnectorState.Disabled, payload.State);
    }

    [Fact]
    public async Task GetGraphConnectorStatus_SettingsBoundFromAppsettingsFile_ReturnsValidConfig()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"aura-graph-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);

        WebApplicationFactory<ApiMarker>? factory = null;

        try
        {
            var appSettingsPath = Path.Combine(tempDirectory, "appsettings.json");
            var appSettings = """
{
  "GraphConnector": {
    "Enabled": true,
    "TenantId": "tenant-from-appsettings",
    "ClientId": "client-from-appsettings",
    "ClientSecret": "secret-from-appsettings"
  }
}
""";

            File.WriteAllText(appSettingsPath, appSettings, Encoding.UTF8);

            factory = _factory.WithWebHostBuilder(builder =>
            {
                builder.UseContentRoot(tempDirectory);
                builder.UseSetting("ConnectionStrings:Aura", "Data Source=aura.db;Pooling=False");
                builder.ConfigureAppConfiguration((_, configBuilder) =>
                {
                    configBuilder.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
                });
            });
            var client = factory.CreateClient();
            var token = await GetMockTokenAsync(client);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync("/api/connectors/graph/status");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var payload = await ReadStatusAsync(response);
            Assert.Equal(GraphConnectorState.ValidConfig, payload.State);
        }
        finally
        {
            if (factory is not null)
            {
                await factory.DisposeAsync();
            }

            SqliteConnection.ClearAllPools();

            try
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
            catch (IOException)
            {
                // Temp directory cleanup is best-effort.
                // SQLite may hold a delayed file handle even after pool clear.
            }
        }
    }


    [Fact]
    public async Task GetGraphConnectorStatus_EnvironmentVariableShadowsAppsettingsConfig()
    {
        var envPrefix = $"AURA_GRAPH_TEST_{Guid.NewGuid():N}_";
        var enabledKey = $"{envPrefix}GraphConnector__Enabled";
        var tenantKey = $"{envPrefix}GraphConnector__TenantId";
        var clientKey = $"{envPrefix}GraphConnector__ClientId";
        var secretKey = $"{envPrefix}GraphConnector__ClientSecret";

        Environment.SetEnvironmentVariable(enabledKey, "true");
        Environment.SetEnvironmentVariable(tenantKey, "tenant-from-environment");
        Environment.SetEnvironmentVariable(clientKey, "client-from-environment");
        Environment.SetEnvironmentVariable(secretKey, "secret-from-environment");

        try
        {
            var client = CreateAuthenticatedClientWithConfiguration((builder, _) =>
            {
                builder.ConfigureAppConfiguration((_, configBuilder) =>
                {
                    var appsettingsConfig = new Dictionary<string, string?>
                    {
                        ["GraphConnector:Enabled"] = "false",
                        ["GraphConnector:TenantId"] = "",
                        ["GraphConnector:ClientId"] = "",
                        ["GraphConnector:ClientSecret"] = ""
                    };

                    configBuilder.AddInMemoryCollection(appsettingsConfig);
                    configBuilder.AddEnvironmentVariables(envPrefix);
                });
            });

            var response = await client.GetAsync("/api/connectors/graph/status");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var payload = await ReadStatusAsync(response);
            Assert.Equal(GraphConnectorState.ValidConfig, payload.State);
        }
        finally
        {
            Environment.SetEnvironmentVariable(enabledKey, null);
            Environment.SetEnvironmentVariable(tenantKey, null);
            Environment.SetEnvironmentVariable(clientKey, null);
            Environment.SetEnvironmentVariable(secretKey, null);
        }
    }

    [Fact]
    public async Task GetGraphConnectorStatus_EmitsStatusEvaluationAndEndpointLogs()
    {
        var client = CreateAuthenticatedClientWithConfiguration((builder, _) =>
        {
            builder.UseSetting("GraphConnector:Enabled", "true");
            builder.UseSetting("GraphConnector:TenantId", "tenant-a");
            builder.UseSetting("GraphConnector:ClientId", "client-a");
            builder.UseSetting("GraphConnector:ClientSecret", "secret-a");
        });

        var response = await client.GetAsync("/api/connectors/graph/status");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(_logEntries, message => message.Contains("Graph connector status evaluated as", StringComparison.Ordinal));
        Assert.Contains(_logEntries, message => message.Contains("Graph connector status endpoint returned", StringComparison.Ordinal));
    }

    private HttpClient CreateAuthenticatedClient(GraphConnectorStatusDto status)
    {
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IGraphConnectorStatusReader>(new StubGraphConnectorStatusReader(status));
            });
        });

        var client = factory.CreateClient();
        var token = GetMockTokenAsync(client).GetAwaiter().GetResult();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private HttpClient CreateAuthenticatedClientWithConfiguration(Action<IWebHostBuilder, WebApplicationFactoryClientOptions> configure)
    {
        var options = new WebApplicationFactoryClientOptions();
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            configure(builder, options);
        });

        var client = factory.CreateClient(options);
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

    private static async Task<GraphConnectorStatusDto> ReadStatusAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<GraphConnectorStatusDto>(content, SerializerOptions)!;
    }

    private sealed class StubGraphConnectorStatusReader : IGraphConnectorStatusReader
    {
        private readonly GraphConnectorStatusDto _status;

        public StubGraphConnectorStatusReader(GraphConnectorStatusDto status)
        {
            _status = status;
        }

        public Task<GraphConnectorStatusDto> GetStatusAsync(CancellationToken cancellationToken)
            => Task.FromResult(_status);
    }

    private sealed class InMemoryLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentQueue<string> _entries;

        public InMemoryLoggerProvider(ConcurrentQueue<string> entries)
        {
            _entries = entries;
        }

        public ILogger CreateLogger(string categoryName) => new InMemoryLogger(_entries);

        public void Dispose()
        {
        }
    }

    private sealed class InMemoryLogger : ILogger
    {
        private readonly ConcurrentQueue<string> _entries;

        public InMemoryLogger(ConcurrentQueue<string> entries)
        {
            _entries = entries;
        }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            _entries.Enqueue(formatter(state, exception));
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }
}
