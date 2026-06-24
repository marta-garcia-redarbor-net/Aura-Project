using Aura.Domain.Calendar;

namespace Aura.Application.Ports;

public interface IMeetingAlertDispatcher
{
    Task DispatchAsync(MeetingAlert alert, CancellationToken ct);
}
