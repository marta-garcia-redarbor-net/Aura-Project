using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;

namespace Aura.UnitTests.TestDoubles.Observability;

internal sealed class ScopeAwareTestLogger<T> : ILogger<T>
{
    private readonly AsyncLocal<Stack<IReadOnlyDictionary<string, object?>>> _scopeStack = new();
    private readonly ConcurrentQueue<LogEntry> _entries = new();

    public IReadOnlyCollection<LogEntry> Entries => new ReadOnlyCollection<LogEntry>(_entries.ToList());

    public IDisposable BeginScope<TState>(TState state) where TState : notnull
    {
        var dictionary = ToDictionary(state);
        var stack = _scopeStack.Value ??= new Stack<IReadOnlyDictionary<string, object?>>();
        stack.Push(dictionary);
        return new PopScope(stack);
    }

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        var structuredState = ToDictionary(state);
        var scopeState = MergeScopeState();

        _entries.Enqueue(new LogEntry(
            logLevel,
            eventId,
            formatter(state, exception),
            exception,
            structuredState,
            scopeState));
    }

    public bool TryGetCurrentScopeValue(string key, out object? value)
    {
        value = null;
        var merged = MergeScopeState();
        if (!merged.TryGetValue(key, out var found))
        {
            return false;
        }

        value = found;
        return true;
    }

    private IReadOnlyDictionary<string, object?> MergeScopeState()
    {
        var stack = _scopeStack.Value;
        if (stack is null || stack.Count == 0)
        {
            return new Dictionary<string, object?>();
        }

        var merged = new Dictionary<string, object?>(StringComparer.Ordinal);

        foreach (var scope in stack.Reverse())
        {
            foreach (var kvp in scope)
            {
                merged[kvp.Key] = kvp.Value;
            }
        }

        return merged;
    }

    private static IReadOnlyDictionary<string, object?> ToDictionary<TState>(TState state)
    {
        if (state is IEnumerable<KeyValuePair<string, object?>> kvps)
        {
            var dict = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (var kvp in kvps)
            {
                dict[kvp.Key] = kvp.Value;
            }

            return dict;
        }

        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["State"] = state
        };
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

    internal sealed record LogEntry(
        LogLevel Level,
        EventId EventId,
        string Message,
        Exception? Exception,
        IReadOnlyDictionary<string, object?> State,
        IReadOnlyDictionary<string, object?> Scope);
}
