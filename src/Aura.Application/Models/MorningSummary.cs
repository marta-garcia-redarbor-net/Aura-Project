namespace Aura.Application.Models;

/// <summary>
/// Morning Summary payload contract.
/// </summary>
/// <param name="UserId">Target user identifier.</param>
/// <param name="Window">Resolved summary window.</param>
/// <param name="GeneratedAtUtc">UTC generation instant.</param>
/// <param name="Entries">Ordered ranked entries for the window.</param>
public sealed record MorningSummary(
    string UserId,
    MorningSummaryWindow Window,
    DateTimeOffset GeneratedAtUtc,
    IReadOnlyList<RankedWorkItem> Entries);
