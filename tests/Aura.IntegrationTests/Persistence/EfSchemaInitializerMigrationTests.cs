using Aura.Infrastructure;
using Aura.Infrastructure.Adapters.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Aura.IntegrationTests.Persistence;

public class EfSchemaInitializerMigrationTests
{
    [Fact]
    public async Task StartAsync_CleanDb_AppliesBaselineAndTraceMigrations()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aura-ef-init-clean-{Guid.NewGuid():N}.db");

        try
        {
            var services = new ServiceCollection();
            services.AddDbContext<AuraDbContext>(options => options.UseSqlite($"Data Source={dbPath}"));
            services.AddScoped<EfSchemaInitializer>();

            await using var provider = services.BuildServiceProvider();
            await using var scope = provider.CreateAsyncScope();
            var initializer = scope.ServiceProvider.GetRequiredService<EfSchemaInitializer>();

            await initializer.StartAsync(CancellationToken.None);

            var db = scope.ServiceProvider.GetRequiredService<AuraDbContext>();

            var migrations = await db.Database.GetAppliedMigrationsAsync();
            Assert.Contains("20260710110000_InitialCreateBaseline", migrations);
            Assert.Contains("20260710120000_AddInterruptionDecisionTraceColumns", migrations);

            var pendingMigrations = await db.Database.GetPendingMigrationsAsync();
            Assert.Empty(pendingMigrations);

            Assert.True(await TableExistsAsync(db, "WorkItems"));
            Assert.True(await TableExistsAsync(db, "InterruptionDecisions"));
            Assert.True(await ColumnExistsAsync(db, "InterruptionDecisions", "RetrievedSemanticContext"));
            Assert.True(await ColumnExistsAsync(db, "InterruptionDecisions", "LlmRationale"));
            Assert.True(await ColumnExistsAsync(db, "InterruptionDecisions", "GuardrailOutcome"));
        }
        finally
        {
            SqliteConnection.ClearAllPools();

            if (File.Exists(dbPath))
            {
                TryDeleteWithRetries(dbPath);
            }

            TryDeleteWithRetries($"{dbPath}-wal");
            TryDeleteWithRetries($"{dbPath}-shm");
        }
    }

    [Fact]
    public async Task StartAsync_LegacyDbWithoutEfHistory_BackfillsAndKeepsSchemaUsable()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aura-ef-init-legacy-{Guid.NewGuid():N}.db");

        try
        {
            await CreateLegacySchemaWithoutEfHistoryAsync(dbPath);

            var services = new ServiceCollection();
            services.AddDbContext<AuraDbContext>(options => options.UseSqlite($"Data Source={dbPath}"));
            services.AddScoped<EfSchemaInitializer>();

            await using var provider = services.BuildServiceProvider();
            await using var scope = provider.CreateAsyncScope();
            var initializer = scope.ServiceProvider.GetRequiredService<EfSchemaInitializer>();

            await initializer.StartAsync(CancellationToken.None);

            var db = scope.ServiceProvider.GetRequiredService<AuraDbContext>();

            var migrations = await db.Database.GetAppliedMigrationsAsync();
            Assert.Contains("20260710110000_InitialCreateBaseline", migrations);
            Assert.Contains("20260710120000_AddInterruptionDecisionTraceColumns", migrations);

            var pendingMigrations = await db.Database.GetPendingMigrationsAsync();
            Assert.Empty(pendingMigrations);

            Assert.True(await TableExistsAsync(db, "WorkItems"));
            Assert.True(await ColumnExistsAsync(db, "InterruptionDecisions", "RetrievedSemanticContext"));
            Assert.True(await ColumnExistsAsync(db, "InterruptionDecisions", "LlmRationale"));
            Assert.True(await ColumnExistsAsync(db, "InterruptionDecisions", "GuardrailOutcome"));
        }
        finally
        {
            SqliteConnection.ClearAllPools();

            if (File.Exists(dbPath))
            {
                TryDeleteWithRetries(dbPath);
            }

            TryDeleteWithRetries($"{dbPath}-wal");
            TryDeleteWithRetries($"{dbPath}-shm");
        }
    }

    private static async Task CreateLegacySchemaWithoutEfHistoryAsync(string dbPath)
    {
        var options = new DbContextOptionsBuilder<AuraDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;

        await using var db = new AuraDbContext(options);
        await db.Database.EnsureCreatedAsync();

        await db.Database.ExecuteSqlRawAsync("DROP TABLE IF EXISTS __EFMigrationsHistory;");
    }

    private static async Task<bool> TableExistsAsync(AuraDbContext db, string tableName)
    {
        var connection = db.Database.GetDbConnection();
        var shouldClose = connection.State != System.Data.ConnectionState.Open;
        if (shouldClose)
        {
            await connection.OpenAsync();
        }

        try
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT 1 FROM sqlite_master WHERE type='table' AND name=$name LIMIT 1;";
            var p = cmd.CreateParameter();
            p.ParameterName = "$name";
            p.Value = tableName;
            cmd.Parameters.Add(p);
            var result = await cmd.ExecuteScalarAsync();
            return result is not null;
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static async Task<bool> ColumnExistsAsync(AuraDbContext db, string tableName, string columnName)
    {
        var connection = db.Database.GetDbConnection();
        var shouldClose = connection.State != System.Data.ConnectionState.Open;
        if (shouldClose)
        {
            await connection.OpenAsync();
        }

        try
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = $"PRAGMA table_info(\"{tableName}\");";
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static void TryDeleteWithRetries(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        for (var attempt = 1; attempt <= 5; attempt++)
        {
            try
            {
                File.Delete(path);
                return;
            }
            catch (IOException) when (attempt < 5)
            {
                Thread.Sleep(100 * attempt);
            }
        }

        throw new IOException($"Failed to delete SQLite artifact after retries: {path}");
    }
}
