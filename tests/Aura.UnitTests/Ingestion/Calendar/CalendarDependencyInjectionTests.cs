using Aura.Application.Ports;
using Aura.Infrastructure.Adapters.Connectors.Calendar;
using Aura.Infrastructure.Adapters.GraphConnector;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aura.UnitTests.Ingestion.Calendar;

public class CalendarDependencyInjectionTests
{
    [Fact]
    public void AddCalendar_RegistersICalendarEventStore()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["GraphConnector:Enabled"] = "true"
            })
            .Build();

        services.AddCalendar(configuration);

        var provider = services.BuildServiceProvider();
        var store = provider.GetService<ICalendarEventStore>();

        Assert.NotNull(store);
        Assert.IsType<InMemoryCalendarEventStore>(store);
    }

    [Fact]
    public void AddCalendar_RegistersCalendarEventMapper()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["GraphConnector:Enabled"] = "true"
            })
            .Build();

        services.AddCalendar(configuration);

        var provider = services.BuildServiceProvider();
        var mapper = provider.GetService<CalendarEventMapper>();

        Assert.NotNull(mapper);
    }

    [Fact]
    public void AddCalendar_WhenGraphDisabled_DoesNotRegister()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["GraphConnector:Enabled"] = "false"
            })
            .Build();

        services.AddCalendar(configuration);

        var provider = services.BuildServiceProvider();
        var store = provider.GetService<ICalendarEventStore>();

        Assert.Null(store);
    }
}