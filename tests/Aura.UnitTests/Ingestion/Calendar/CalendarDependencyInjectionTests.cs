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
                ["GraphConnector:Enabled"] = "true",
                ["GraphConnector:TenantId"] = "11111111-1111-1111-1111-111111111111",
                ["GraphConnector:ClientId"] = "22222222-2222-2222-2222-222222222222"
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
                ["GraphConnector:Enabled"] = "true",
                ["GraphConnector:TenantId"] = "11111111-1111-1111-1111-111111111111",
                ["GraphConnector:ClientId"] = "22222222-2222-2222-2222-222222222222"
            })
            .Build();

        services.AddCalendar(configuration);

        var provider = services.BuildServiceProvider();
        var mapper = provider.GetService<CalendarEventMapper>();

        Assert.NotNull(mapper);
    }

    [Fact]
    public void AddCalendar_WhenGraphDisabled_DoesNotRegisterGraphSpecificServices()
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

        // ICalendarEventStore IS registered (used for meeting alerts, independent of Graph)
        var store = provider.GetService<ICalendarEventStore>();
        Assert.NotNull(store);

        // But Graph-specific services should NOT be registered
        var mapper = provider.GetService<CalendarEventMapper>();
        Assert.Null(mapper);
    }
}
