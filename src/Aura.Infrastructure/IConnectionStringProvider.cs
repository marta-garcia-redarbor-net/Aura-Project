namespace Aura.Infrastructure;

/// <summary>
/// Provides connection strings for named stores.
/// Implementations resolve the active provider (SQLite or EF Core + Azure SQL)
/// based on environment configuration for each store.
/// </summary>
public interface IConnectionStringProvider
{
    /// <summary>
    /// Returns the connection string for the specified store.
    /// </summary>
    /// <param name="storeName">The store identifier (e.g., "FocusState", "WorkItems").</param>
    /// <returns>The connection string to use for the store.</returns>
    string GetConnectionString(string storeName);
}
