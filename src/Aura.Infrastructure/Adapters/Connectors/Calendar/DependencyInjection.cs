using Aura.Application.Ports;
using Aura.Application.UseCases.Calendar;
using Aura.Infrastructure.Adapters.Calendar;
using Aura.Infrastructure.Adapters.Connectors.Graph;
using Aura.Infrastructure.Adapters.GraphConnector;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aura.Infrastructure.Adapters.Connectors.Calendar;

internal static class DependencyInjection
{
    internal static IServiceCollection AddCalendar(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var graphOptions = new GraphConnectorOptions();
        configuration.GetSection(GraphConnectorOptions.SectionName).Bind(graphOptions);

        if (!graphOptions.Enabled)
        {
            return services;
        }

        services.AddSingleton<CalendarEventMapper>();
        services.AddSingleton<InMemoryCalendarEventStore>();
        services.AddSingleton<ICalendarEventStore>(sp => sp.GetRequiredService<InMemoryCalendarEventStore>());
        services.AddScoped<IMessageSourceProvider<CalendarEventDto>, GraphCalendarEventProvider>();
        services.AddScoped<CalendarConnectorAdapter>();
        services.AddScoped<IConnectorAdapter>(sp => sp.GetRequiredService<CalendarConnectorAdapter>());

        // Meeting alert infrastructure
        services.AddSingleton<SqliteConnection>(sp =>
        {
            var connection = new SqliteConnection("Data Source=aura.db");
            connection.Open();
            SqliteMeetingAlertStore.InitializeSchema(connection);
            return connection;
        });
        services.AddSingleton<IMeetingAlertStore, SqliteMeetingAlertStore>();
        services.AddSingleton<IMeetingAlertDispatcher, LoggingMeetingAlertDispatcher>();
        services.AddScoped<CheckAndDispatchMeetingAlertsUseCase>();

        return services;
    }
}
