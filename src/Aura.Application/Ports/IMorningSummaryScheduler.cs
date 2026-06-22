using Aura.Application.Models;

namespace Aura.Application.Ports;

/// <summary>
/// Resolves and evaluates Morning Summary windows.
/// </summary>
public interface IMorningSummaryScheduler
{
    /// <summary>
    /// Resolves a summary window from schedule context.
    /// </summary>
    /// <param name="context">Schedule context with user, timezone, target local time, and date.</param>
    /// <returns>Resolved summary window contract.</returns>
    MorningSummaryWindow ResolveWindow(MorningSummaryScheduleContext context);

    /// <summary>
    /// Evaluates whether a summary window is due at an evaluation instant.
    /// </summary>
    /// <param name="window">Resolved summary window to evaluate.</param>
    /// <param name="evaluationInstant">UTC instant used to evaluate due state.</param>
    /// <returns><see langword="true"/> when the window is due; otherwise <see langword="false"/>.</returns>
    bool IsWindowDue(MorningSummaryWindow window, DateTimeOffset evaluationInstant);
}
