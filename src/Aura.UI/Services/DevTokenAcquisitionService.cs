using Microsoft.Extensions.Logging;

namespace Aura.UI.Services;

/// <summary>
/// Development-only implementation of <see cref="ITokenAcquisitionService"/> that returns a mock JWT token.
/// Used as a fallback when MSAL configuration is absent from Program.cs.
/// Easy to remove — just delete this file and the registration in Program.cs.
/// </summary>
public sealed partial class DevTokenAcquisitionService : ITokenAcquisitionService
{
    private readonly ILogger<DevTokenAcquisitionService> _logger;
    private string? _cachedToken;

    public DevTokenAcquisitionService(ILogger<DevTokenAcquisitionService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<string> AcquireTokenAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedToken is not null)
        {
            return Task.FromResult(_cachedToken);
        }

        // Generate a mock JWT token for development use
        _cachedToken = GenerateMockJwt();

        Log.DevTokenAcquired(_logger);

        return Task.FromResult(_cachedToken);
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
        public static partial void DevTokenAcquired(ILogger logger);
    }
}