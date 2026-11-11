using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Umbraco.AI.Core.Configuration;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.AI.Persistence.Notifications;

/// <summary>
/// Notification handler that runs pending EF Core migrations on application startup.
/// </summary>
/// <remarks>
/// Runs on <see cref="PostRuntimePremigrationsUpgradeNotification"/>, before any
/// <see cref="UmbracoApplicationStartingNotification"/> handler, so startup work in other packages
/// (such as an Umbraco Deploy import on start) finds the tables in place.
/// </remarks>
public class RunAIMigrationNotificationHandler
    : INotificationAsyncHandler<PostRuntimePremigrationsUpgradeNotification>,
      INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    /// <summary>
    /// v17 migration ID to the v18 ID of the same migration. <c>UmbracoAI_AddCachedInputTokens</c>
    /// was generated separately on each line (v17 in 17.3.0, v18 in 18.3.0). Both providers' pairs
    /// are listed; the other provider's pair never matches, so it does nothing.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> V17MigrationIdRenames = new Dictionary<string, string>
    {
        // SQL Server
        ["20260731095757_UmbracoAI_AddCachedInputTokens"] = "20260731093410_UmbracoAI_AddCachedInputTokens",
        // SQLite
        ["20260731095759_UmbracoAI_AddCachedInputTokens"] = "20260731093420_UmbracoAI_AddCachedInputTokens",
    };

    private readonly IConfiguration _configuration;
    private readonly ILogger<RunAIMigrationNotificationHandler> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="RunAIMigrationNotificationHandler"/>.
    /// </summary>
    public RunAIMigrationNotificationHandler(
        IConfiguration configuration,
        ILogger<RunAIMigrationNotificationHandler> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task HandleAsync(
        PostRuntimePremigrationsUpgradeNotification notification,
        CancellationToken cancellationToken)
        => RunMigrationsAsync(cancellationToken);

    /// <inheritdoc />
    [Obsolete("Use HandleAsync(PostRuntimePremigrationsUpgradeNotification, CancellationToken) instead. Will be removed in v20.")]
    public Task HandleAsync(
        UmbracoApplicationStartedNotification notification,
        CancellationToken cancellationToken)
        => RunMigrationsAsync(cancellationToken);

    private async Task RunMigrationsAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Running Umbraco.AI database migrations...");

            // Create a standalone DbContext rather than using IDbContextFactory. Umbraco's EFCoreScope
            // infrastructure shares NPoco connections (wrapped with MiniProfiler's ProfiledDbConnection)
            // onto pooled EF Core contexts via SetDbConnection(). These tainted contexts cause
            // NullReferenceException in SqliteDatabaseCreator.Exists() when the ProfiledDbConnection's
            // inner connection is disposed. Creating the context directly avoids the pooled factory.
            // See: https://github.com/umbraco/Umbraco-CMS/issues/22124
            var (connectionString, providerName) = AIConnectionStringResolver.Resolve(_configuration);

            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogDebug("No database connection string available — skipping Umbraco.AI migrations (Umbraco may still be installing).");
                return;
            }

            var optionsBuilder = new DbContextOptionsBuilder<UmbracoAIDbContext>();
            UmbracoAIDbContext.ConfigureProvider(optionsBuilder, connectionString, providerName);

            // Downgrade PendingModelChangesWarning from exception to log so migrations
            // can still be applied during development when the model has unreleased changes.
            optionsBuilder.ConfigureWarnings(w =>
                w.Log(RelationalEventId.PendingModelChangesWarning));

            await using UmbracoAIDbContext dbContext = new UmbracoAIDbContext(optionsBuilder.Options);

            // Migrate history records from the shared __EFMigrationsHistory table to the
            // per-product table. This ensures previously applied migrations are recognized.
            await AIMigrationHistoryHelper.MigrateHistoryRecordsAsync(
                dbContext.Database.GetDbConnection(),
                AIConnectionStringResolver.MigrationsHistoryTableName,
                _logger,
                cancellationToken);

            // Migrations that shipped with a different ID on the v17 line. Without this a site
            // upgrading from 17.x re-runs them and fails on schema that already exists.
            await AIMigrationHistoryHelper.RenameMigrationIdsAsync(
                dbContext.Database.GetDbConnection(),
                AIConnectionStringResolver.MigrationsHistoryTableName,
                V17MigrationIdRenames,
                _logger,
                cancellationToken);

            IEnumerable<string> pending = await dbContext.Database.GetPendingMigrationsAsync(cancellationToken);
            if (pending.Any())
            {
                await dbContext.Database.MigrateAsync(cancellationToken);
            }

            _logger.LogInformation("Umbraco.AI database migrations completed successfully.");
        }
        catch (Exception ex)
        {
            // Log and keep serving: this runs during CMS boot, so rethrowing would stop the whole
            // site from starting when only Umbraco.AI is affected.
            _logger.LogError(ex, "Failed to run Umbraco.AI database migrations.");
        }
    }
}
