namespace Aura.Application.Models;

public abstract record NormalizedSignal(string Key, string Explanation);

public sealed record BooleanSignal(string Key, bool Value, string Explanation)
    : NormalizedSignal(Key, Explanation);

public sealed record LevelSignal(string Key, SignalLevel Value, string Explanation)
    : NormalizedSignal(Key, Explanation);

public enum SignalLevel
{
    None,
    Low,
    Medium,
    High,
    Critical
}
