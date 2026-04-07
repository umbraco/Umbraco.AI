using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Umbraco.AI.Core.Configuration;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.AI.Prompt.Persistence.Notifications;

/// <summary>
/// Notification handler that runs database migrations on application startup.
/// </summary>
internal sealed class RunPromptMigrationNotificationHandler : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<RunPromptMigrationNotificationHandler> _logger;

    public RunPromptMigrationNotificationHandler(
        IConfiguration configuration,
        ILogger<RunPromptMigrationNotificationHandler> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Running Umbraco.AI.Prompt database migrations...");

            // Create a standalone DbContext rather than using IDbContextFactory. Umbraco's EFCoreScope
            // infrastructure shares NPoco connections (wrapped with MiniProfiler's ProfiledDbConnection)
            // onto pooled EF Core contexts via SetDbConnection(). These tainted contexts cause
            // NullReferenceException in SqliteDatabaseCreator.Exists() when the ProfiledDbConnection's
            // inner connection is disposed. Creating the context directly avoids the pooled factory.
            // See: https://github.com/umbraco/Umbraco-CMS/issues/22124
            var (connectionString, providerName) = AIConnectionStringResolver.Resolve(_configuration);

            var optionsBuilder = new DbContextOptionsBuilder<UmbracoAIPromptDbContext>();
            UmbracoAIPromptDbContext.ConfigureProvider(optionsBuilder, connectionString, providerName);

            // Downgrade PendingModelChangesWarning from exception to log so migrations
            // can still be applied during development when the model has unreleased changes.
            optionsBuilder.ConfigureWarnings(w =>
                w.Log(RelationalEventId.PendingModelChangesWarning));

            await using UmbracoAIPromptDbContext dbContext = new UmbracoAIPromptDbContext(optionsBuilder.Options);

            // Migrate history records from the shared __EFMigrationsHistory table to the
            // per-product table. This ensures previously applied migrations are recognized.
            await AIMigrationHistoryHelper.MigrateHistoryRecordsAsync(
                dbContext.Database.GetDbConnection(),
                UmbracoAIPromptDbContext.MigrationsHistoryTableName,
                UmbracoAIPromptDbContext.MigrationPrefix,
                _logger,
                cancellationToken);

            IEnumerable<string> pending = await dbContext.Database.GetPendingMigrationsAsync(cancellationToken);
            if (pending.Any())
            {
                await dbContext.Database.MigrateAsync(cancellationToken);
            }

            _logger.LogInformation("Umbraco.AI.Prompt database migrations completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run Umbraco.AI.Prompt database migrations.");
            throw;
        }
    }
}
