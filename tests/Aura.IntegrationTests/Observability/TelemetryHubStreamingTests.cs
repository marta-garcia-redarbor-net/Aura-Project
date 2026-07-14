using Aura.Api;
using Aura.Infrastructure.Observability;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace Aura.IntegrationTests.Observability;

/// <summary>
/// Integration tests for TelemetryHub SignalR streaming.
/// Verifies that clients can connect, authenticate, and receive telemetry data.
/// </summary>
public class TelemetryHubStreamingTests : IClassFixture<WebApplicationFactory<ApiMarker>>, IAsyncDisposable
{
    private readonly WebApplicationFactory<ApiMarker> _factory;
    private HubConnection? _connection;

    public TelemetryHubStreamingTests(WebApplicationFactory<ApiMarker> factory)
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
    public async Task Connect_WithoutToken_Fails()
    {
        var connection = BuildHubConnection(token: null);

        // The SignalR client throws HttpRequestException (401) when the server rejects
        // the connection due to missing authentication.
        await Assert.ThrowsAnyAsync<Exception>(() => connection.StartAsync());
    }

    [Fact]
    public async Task Connect_WithToken_ReceivesStreamedData()
    {
        // Seed some data into the buffers
        var logBuffer = _factory.Services.GetRequiredService<LogRecordBuffer>();
        logBuffer.Write(new LogRecordDto(
            Microsoft.Extensions.Logging.LogLevel.Information,
            DateTimeOffset.UtcNow,
            "test-corr-id",
            "Integration test log message",
            "TestSource"));

        var token = await GetAuthTokenAsync();
        _connection = BuildHubConnection(token);

        var logsReceived = new TaskCompletionSource<IReadOnlyList<LogRecordDto>>();
        _connection.On<IReadOnlyList<LogRecordDto>>("ReceiveLogs", logs =>
        {
            if (logs.Count > 0)
            {
                logsReceived.TrySetResult(logs);
            }
        });

        await _connection.StartAsync();

        // The TelemetryStreamService pushes every 1s, so wait up to 5s
        var received = await WaitForAsync(logsReceived.Task, timeoutSeconds: 5);

        Assert.NotNull(received);
        Assert.True(received.Count > 0, "Expected at least one log record from the stream");
    }

    [Fact]
    public async Task StreamLogs_ReturnsData()
    {
        var logBuffer = _factory.Services.GetRequiredService<LogRecordBuffer>();
        logBuffer.Write(new LogRecordDto(
            Microsoft.Extensions.Logging.LogLevel.Warning,
            DateTimeOffset.UtcNow,
            "stream-corr",
            "Stream test message",
            "StreamTest"));

        var token = await GetAuthTokenAsync();
        _connection = BuildHubConnection(token);
        await _connection.StartAsync();

        // Call the hub's StreamLogs method directly via channel
        var channel = await _connection.StreamAsChannelAsync<IReadOnlyList<LogRecordDto>>("StreamLogs");

        var hasData = await channel.WaitToReadAsync();
        Assert.True(hasData);

        var batch = await channel.ReadAsync();
        Assert.NotNull(batch);
        Assert.True(batch.Count > 0);
    }

    private HubConnection BuildHubConnection(string? token)
    {
        var builder = new HubConnectionBuilder()
            .WithUrl("http://localhost/hubs/telemetry", options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                if (token is not null)
                {
                    options.Headers.Add("Authorization", $"Bearer {token}");
                }
            });

        return builder.Build();
    }

    private async Task<string> GetAuthTokenAsync()
    {
        var client = _factory.CreateClient();
        var loginResponse = await client.PostAsync("/api/auth/mock-login", null);
        loginResponse.EnsureSuccessStatusCode();

        var content = await loginResponse.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(content);
        return json.RootElement.GetProperty("token").GetString()!;
    }

    private static async Task<T> WaitForAsync<T>(Task<T> task, int timeoutSeconds)
    {
        var completed = await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(timeoutSeconds)));
        if (completed != task)
        {
            throw new TimeoutException($"Timed out after {timeoutSeconds}s waiting for SignalR data");
        }
        return await task;
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }
    }
}
