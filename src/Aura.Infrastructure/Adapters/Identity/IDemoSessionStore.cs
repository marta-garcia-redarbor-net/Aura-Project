namespace Aura.Infrastructure.Adapters.Identity;

/// <summary>
/// Stores active demo/mock JWT sessions for server-side token validation.
/// </summary>
public interface IDemoSessionStore
{
    void Activate(string sessionId, string userId, DateTimeOffset expiresAtUtc);

    bool IsActive(string sessionId, string userId, DateTimeOffset nowUtc);
}
