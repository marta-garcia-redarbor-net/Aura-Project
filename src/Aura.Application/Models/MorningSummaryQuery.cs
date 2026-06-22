namespace Aura.Application.Models;

/// <summary>
/// Work-item reader query contract for Morning Summary composition.
/// </summary>
/// <param name="UserId">Target user identifier.</param>
/// <param name="FromUtc">Window lower bound (inclusive) in UTC.</param>
/// <param name="ToUtc">Window upper bound (inclusive) in UTC.</param>
public sealed record MorningSummaryQuery(string UserId, DateTimeOffset FromUtc, DateTimeOffset ToUtc);
