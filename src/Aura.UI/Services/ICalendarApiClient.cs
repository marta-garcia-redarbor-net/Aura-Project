using Aura.UI.Models;

namespace Aura.UI.Services;

public interface ICalendarApiClient
{
    Task<IReadOnlyList<UpcomingMeetingResponse>> GetUpcomingMeetingsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<UpcomingMeetingResponse>> GetTodayCalendarAsync(CancellationToken cancellationToken);
}
