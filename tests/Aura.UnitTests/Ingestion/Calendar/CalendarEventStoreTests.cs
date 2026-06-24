using System.Reflection;
using Aura.Application.Ports;

namespace Aura.UnitTests.Ingestion.Calendar;

public class CalendarEventStoreTests
{
    [Fact]
    public void ICalendarEventStore_HasExpectedMethods()
    {
        var interfaceType = typeof(ICalendarEventStore);
        Assert.NotNull(interfaceType);

        var methods = interfaceType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
        Assert.Contains(methods, m => m.Name == "SaveAsync" && m.ReturnType == typeof(Task));
        Assert.Contains(methods, m => m.Name == "SaveBatchAsync" && m.ReturnType == typeof(Task));
        Assert.Contains(methods, m => m.Name == "GetUpcomingAsync" && m.ReturnType == typeof(Task<IReadOnlyList<Aura.Domain.Calendar.CalendarEvent>>));
    }
}