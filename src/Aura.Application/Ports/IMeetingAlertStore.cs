using Aura.Domain.Calendar;

namespace Aura.Application.Ports;

public interface IMeetingAlertStore
{
    Task<MeetingAlert?> GetUnsentAlertAsync(string eventId, MeetingAlertTrigger trigger, DateTimeOffset date, CancellationToken ct);
    Task MarkSentAsync(MeetingAlert alert, CancellationToken ct);
    Task<IReadOnlyList<MeetingAlert>> GetUpcomingAlertsAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken ct);
}
