using Aura.Application.Ports;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aura.Infrastructure.Adapters.MorningSummaryScheduling;

internal static class DependencyInjection
{
    internal static IServiceCollection AddMorningSummarySchedulingAdapters(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<MorningSummaryOptions>(configuration.GetSection(MorningSummaryOptions.SectionName));
        services.AddScoped<IMorningSummarySettingsProvider, AppSettingsMorningSummarySettingsProvider>();

        services.AddSingleton<IMorningSummaryEmissionStore>(sp =>
        {
            var connectionString = configuration.GetConnectionString("Aura")
                                   ?? "Data Source=aura.db";

            var connection = new SqliteConnection(connectionString);
            connection.Open();
            SqliteMorningSummaryEmissionStore.InitializeSchema(connection);
            return new SqliteMorningSummaryEmissionStore(connection);
        });

        return services;
    }
}
