using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Infrastructure.Adapters.Options;
using Microsoft.Extensions.Options;

namespace Aura.Infrastructure.Adapters.Services.Rules;

/// <summary>
/// Rule that triggers when a WorkItem has deadline metadata and the deadline is within the configured window.
/// Priority: 40.
/// </summary>
public sealed class DeadlineUrgencyRule : IInterruptionRule
{
    private readonly InterruptionOptions _options;

    public DeadlineUrgencyRule(IOptions<InterruptionOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
    }

    public int Priority => 40;

    public Task<RuleResult> EvaluateAsync(EvaluationContext context, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (context.TryLevelSignal(WorkItemSignalKeys.TimeCriticalitySignal, out var timeCriticality)
            && timeCriticality is SignalLevel.High or SignalLevel.Critical)
        {
            return Task.FromResult(new RuleResult(
                nameof(DeadlineUrgencyRule),
                true,
                timeCriticality == SignalLevel.Critical ? 10.0 : 9.0,
                0.9,
                $"Typed time-criticality signal is {timeCriticality}."));
        }

        var deadline = FindDeadlineFromMetadata(context.Item.Metadata);

        if (deadline is null)
        {
            return Task.FromResult(new RuleResult(
                nameof(DeadlineUrgencyRule), false, 0, 0.9,
                "No deadline metadata found"));
        }

        var windowEnd = DateTimeOffset.UtcNow.AddHours(_options.DeadlineWindowHours);
        var matched = deadline.Value <= windowEnd && deadline.Value > DateTimeOffset.UtcNow;

        return Task.FromResult(new RuleResult(
            nameof(DeadlineUrgencyRule),
            matched,
            matched ? 9.0 : 3.0,
            0.85,
            matched
                ? $"Deadline {deadline.Value:O} is within {_options.DeadlineWindowHours}h window"
                : $"Deadline {deadline.Value:O} is outside {_options.DeadlineWindowHours}h window"));
    }

    private static DateTimeOffset? FindDeadlineFromMetadata(IReadOnlyDictionary<string, string> metadata)
    {
        foreach (var key in metadata.Keys)
        {
            if (key.StartsWith("outlook.deadline.", StringComparison.OrdinalIgnoreCase))
            {
                if (DateTimeOffset.TryParse(metadata[key],
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.AssumeUniversal,
                    out var deadline))
                {
                    return deadline;
                }
            }
        }

        return null;
    }
}
