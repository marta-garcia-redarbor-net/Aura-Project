using System.Net.Http.Headers;
using System.Text.Json;

namespace Aura.UI.Services;

/// <summary>
/// Development-only handler that obtains a mock JWT from the API on first use
/// and attaches it to every outgoing request as a fallback when no browser token
/// is forwarded. Easy to remove — just delete this file and the registration in Program.cs.
/// </summary>
public sealed partial class DevAccessTokenHandler : DelegatingHandler
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<DevAccessTokenHandler> _logger;
    private string? _cachedToken;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public DevAccessTokenHandler(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<DevAccessTokenHandler> logger)
    {
        _ = httpClientFactory; // reserved for future use
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("DevAccessTokenHandler.SendAsync called for {Uri}", request.RequestUri);
        
        // Don't overwrite if a real token was already set (e.g. by ForwardedAccessTokenHandler)
        if (request.Headers.Authorization is null)
        {
            _logger.LogInformation("DevAccessTokenHandler: No Authorization header, acquiring mock token for {Uri}", request.RequestUri);
            var token = await GetOrAcquireTokenAsync(cancellationToken);
            if (token is not null)
            {
                _logger.LogInformation("DevAccessTokenHandler: Setting mock token for {Uri}", request.RequestUri);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            else
            {
                _logger.LogWarning("DevAccessTokenHandler: Failed to acquire mock token for {Uri}", request.RequestUri);
            }
        }
        else
        {
            _logger.LogInformation("DevAccessTokenHandler: Authorization header already present for {Uri}", request.RequestUri);
        }

        return await base.SendAsync(request, cancellationToken);
    }

    internal async Task<string?> GetOrAcquireTokenAsync(CancellationToken cancellationToken)
    {
        if (_cachedToken is not null)
            return _cachedToken;

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            if (_cachedToken is not null)
                return _cachedToken;

            _cachedToken = await FetchMockTokenAsync(cancellationToken);
            return _cachedToken;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task<string?> FetchMockTokenAsync(CancellationToken cancellationToken)
    {
        try
        {
            var baseUrl = _configuration["AuraApi:BaseUrl"] ?? "http://localhost:5180";
            using var client = new HttpClient { BaseAddress = new Uri(baseUrl) };

            using var response = await client.PostAsync("/api/auth/mock-login", null, cancellationToken);
            response.EnsureSuccessStatusCode();

            using var doc = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);

            var token = doc.RootElement.GetProperty("token").GetString();

            Log.MockTokenAcquired(_logger, baseUrl);
            return token;
        }
        catch (Exception ex)
        {
            Log.MockTokenFailed(_logger, ex);
            return null;
        }
    }

    private static partial class Log
    {
        [LoggerMessage(EventId = 2001, Level = LogLevel.Information,
            Message = "Dev mock token acquired from {BaseUrl}/api/auth/mock-login")]
        public static partial void MockTokenAcquired(ILogger logger, string baseUrl);

        [LoggerMessage(EventId = 2002, Level = LogLevel.Warning,
            Message = "Failed to acquire dev mock token — API calls will be unauthenticated")]
        public static partial void MockTokenFailed(ILogger logger, Exception exception);
    }
}
