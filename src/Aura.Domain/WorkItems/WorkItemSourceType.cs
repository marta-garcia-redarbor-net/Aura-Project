namespace Aura.Domain.WorkItems;

/// <summary>
/// Canonical origin type for a <see cref="WorkItem"/>.
/// </summary>
public enum WorkItemSourceType
{
    TeamsMessage,
    SlackMessage,
    OutlookEmail,
    CalendarAppointment,
    PrReview,
    TodoTask,
    TeamsChat = 14
}
