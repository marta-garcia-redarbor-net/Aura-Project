namespace Aura.Application.Models;

/// <summary>
/// Composition request contract for Morning Summary.
/// </summary>
/// <param name="UserId">Target user identifier.</param>
/// <param name="Window">Resolved summary window.</param>
public sealed record MorningSummaryRequest(string UserId, MorningSummaryWindow Window);
