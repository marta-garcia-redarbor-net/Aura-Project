using System.Net.Http.Headers;
using System.Text.Json;

namespace Aura.UI.Services;

/// <summary>
/// Development-only handler that obtains a mock JWT from the API on first use
/// and attaches it to every outgoing request as a fallback when no browser token
/// is forwarded. Easy to remove — just delete this file and the registration in Program.cs.
///
/// The cached token is automatically refreshed when it nears expiration (within 1 minute)
/// to prevent "Session expired" errors caused by the 15-minute mock JWT lifetime.
/// </summary>
public sealed partial class DevAccessTokenHandler : DelegatingHandler
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<DevAccessTokenHandler> _logger;
    private string? _cachedToken;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private static readonly TimeSpan ExpirationBuffer = TimeSpan.FromMinutes(1);

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
        // Fast path: cached token is still fresh — return it
        if (_cachedToken is not null && !IsTokenExpiredOrNearExpiry(_cachedToken))
            return _cachedToken;

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            // Double-check after acquiring the lock
            if (_cachedToken is not null && !IsTokenExpiredOrNearExpiry(_cachedToken))
                return _cachedToken;

            if (_cachedToken is not null)
                Log.TokenExpiredRefreshing(_logger);

            _cachedToken = await FetchMockTokenAsync(cancellationToken);
            return _cachedToken;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private static bool IsTokenExpiredOrNearExpiry(string token)
    {
        try
        {
            var parts = token.Split('.');
            if (parts.Length < 2)
                return true;

            // Decode JWT payload (second segment)
            var payload = parts[1];
            var padded = (payload.Length % 4) switch
            {
                2 => payload + "==",
                3 => payload + "=",
                _ => payload
            };
            var jsonBytes = Convert.FromBase64String(padded);
            using var doc = JsonDocument.Parse(jsonBytes);

            if (doc.RootElement.TryGetProperty("exp", out var expProp) && expProp.TryGetInt64(out var expUnix))
            {
                var expTime = DateTimeOffset.FromUnixTimeSeconds(expUnix);
                return expTime <= DateTimeOffset.UtcNow + ExpirationBuffer;
            }

            return false; // no exp claim — assume valid
        }
        catch
        {
            return false; // can't parse — assume valid rather than breaking
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

        [LoggerMessage(EventId = 2003, Level = LogLevel.Information,
            Message = "Dev mock token expired or near expiry — refreshing")]
        public static partial void TokenExpiredRefreshing(ILogger logger);
    }
}
