using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;

namespace Aura.UI.Services;

/// <summary>
/// Development-only implementation of <see cref="ITokenAcquisitionService"/> that reads the
/// real JWT from the authenticated user's claims (acquired via the /login/dev flow).
/// Uses <see cref="AuthenticationStateProvider"/> (Blazor circuit-safe) instead of
/// <see cref="Microsoft.AspNetCore.Http.IHttpContextAccessor"/>, because in Blazor Server
/// interactive mode the HttpContext is null during component lifecycle.
/// Falls back to a mock JWT if no claim is found (e.g. during prerendering).
/// </summary>
public sealed partial class DevTokenAcquisitionService : ITokenAcquisitionService
{
    private readonly ILogger<DevTokenAcquisitionService> _logger;
    private readonly AuthenticationStateProvider _authStateProvider;
    private string? _cachedToken;
    private bool _triedClaim;

    public DevTokenAcquisitionService(
        ILogger<DevTokenAcquisitionService> logger,
        AuthenticationStateProvider authStateProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _authStateProvider = authStateProvider;
    }

    public async Task<string> AcquireTokenAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedToken is not null)
        {
            return _cachedToken;
        }

        // Try to read the real JWT from the "token" claim on the authenticated user.
        // AuthenticationStateProvider works in Blazor Server interactive mode (SignalR circuit),
        // unlike IHttpContextAccessor which is null after the initial HTTP request.
        if (!_triedClaim)
        {
            _triedClaim = true;
            var state = await _authStateProvider.GetAuthenticationStateAsync();
            var tokenClaim = state.User.FindFirst("token")?.Value;

            if (!string.IsNullOrEmpty(tokenClaim))
            {
                _cachedToken = tokenClaim;
                Log.DevTokenFromClaim(_logger);
                return _cachedToken;
            }
        }

        // Fallback mock JWT for prerendering or unauthenticated state.
        _cachedToken = GenerateMockJwt();
        Log.DevTokenFallback(_logger);

        return _cachedToken;
    }

    private static string GenerateMockJwt()
    {
        // Simple mock JWT for development - NOT a real token
        var header = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("{\"alg\":\"HS256\",\"typ\":\"JWT\"}"));
        var payload = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("{\"sub\":\"dev-user\",\"name\":\"Development User\",\"iat\":1234567890}"));
        var signature = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("dev-signature"));

        return $"{header}.{payload}.{signature}";
    }

    private static partial class Log
    {
        [LoggerMessage(EventId = 3001, Level = LogLevel.Warning,
            Message = "Using dev mock token for SignalR authentication — remove DevTokenAcquisitionService when real auth is wired up")]
        public static partial void DevTokenFallback(ILogger logger);

        [LoggerMessage(EventId = 3002, Level = LogLevel.Debug,
            Message = "Using real JWT from user claims for SignalR authentication")]
        public static partial void DevTokenFromClaim(ILogger logger);
    }
}