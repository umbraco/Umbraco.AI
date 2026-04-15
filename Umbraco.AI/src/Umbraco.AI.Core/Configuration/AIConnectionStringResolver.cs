using Microsoft.Extensions.Configuration;
using Umbraco.Extensions;

namespace Umbraco.AI.Core.Configuration;

/// <summary>
/// Resolves the database connection string for Umbraco AI packages.
/// </summary>
/// <remarks>
/// Checks for a dedicated <c>umbracoAIDbDSN</c> connection string first,
/// falling back to the standard Umbraco CMS connection string (<c>umbracoDbDSN</c>).
/// This allows AI data to be stored in a separate database when desired.
/// </remarks>
public static class AIConnectionStringResolver
{
    /// <summary>
    /// The connection string name for the dedicated AI database.
    /// </summary>
    public const string ConnectionName = "umbracoAIDbDSN";

    /// <summary>
    /// The shared EF Core migrations history table for all Umbraco AI packages.
    /// </summary>
    public const string MigrationsHistoryTableName = "__UmbracoAIMigrationsHistory";

    /// <summary>
    /// Resolves the AI database connection string and provider name.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>
    /// A tuple of (connectionString, providerName). Returns the dedicated AI connection string
    /// if configured, otherwise falls back to the Umbraco CMS connection string.
    /// Both values may be <c>null</c> if no connection string is configured (e.g., during install).
    /// </returns>
    public static (string? ConnectionString, string? ProviderName) Resolve(IConfiguration configuration)
    {
        // Try AI-specific connection string first
        var connectionString = configuration.GetUmbracoConnectionString(ConnectionName, out var providerName);

        if (!string.IsNullOrEmpty(connectionString))
        {
            providerName = NormalizeProviderName(providerName);
            return (connectionString, providerName);
        }

        // Fall back to the standard Umbraco CMS connection string
        connectionString = configuration.GetUmbracoConnectionString(out providerName);

        if (!string.IsNullOrEmpty(connectionString))
        {
            providerName = NormalizeProviderName(providerName);
        }

        return (connectionString, providerName);
    }

    /// <summary>
    /// Normalizes the database provider name, handling legacy provider names
    /// (e.g., <c>System.Data.SqlClient</c> on Umbraco Cloud) and defaulting
    /// to SQL Server when not specified.
    /// </summary>
    private static string NormalizeProviderName(string? providerName)
    {
        // Migrate legacy System.Data.SqlClient to Microsoft.Data.SqlClient
        // (Umbraco Cloud and Azure-configured connection strings use the legacy provider name)
        if (string.Equals(providerName, "System.Data.SqlClient", StringComparison.OrdinalIgnoreCase))
        {
            return Umbraco.Cms.Core.Constants.ProviderNames.SQLServer;
        }

        // Default to SQL Server if not specified (matching Umbraco convention)
        return providerName ?? Umbraco.Cms.Core.Constants.ProviderNames.SQLServer;
    }
}
