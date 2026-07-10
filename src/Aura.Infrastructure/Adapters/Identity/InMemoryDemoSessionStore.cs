using System.Collections.Concurrent;

namespace Aura.Infrastructure.Adapters.Identity;

/// <summary>
/// In-memory store for active demo sessions, used to validate MockJwt session claims server-side.
/// </summary>
internal sealed class InMemoryDemoSessionStore : IDemoSessionStore
{
    private readonly ConcurrentDictionary<string, SessionEntry> _sessions = new(StringComparer.Ordinal);

    public void Activate(string sessionId, string userId, DateTimeOffset expiresAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        _sessions[sessionId] = new SessionEntry(userId, expiresAtUtc);
    }

    public bool IsActive(string sessionId, string userId, DateTimeOffset nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        if (!_sessions.TryGetValue(sessionId, out var entry))
        {
            return false;
        }

        if (entry.ExpiresAtUtc <= nowUtc)
        {
            _sessions.TryRemove(sessionId, out _);
            return false;
        }

        if (!string.Equals(entry.UserId, userId, StringComparison.Ordinal))
        {
            return false;
        }

        return true;
    }

    private sealed record SessionEntry(string UserId, DateTimeOffset ExpiresAtUtc);
}
