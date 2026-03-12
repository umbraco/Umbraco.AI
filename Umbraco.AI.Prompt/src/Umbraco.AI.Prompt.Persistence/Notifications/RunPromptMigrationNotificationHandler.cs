using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Umbraco.AI.Prompt.Persistence.Configuration;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.AI.Prompt.Persistence.Notifications;

/// <summary>
/// Notification handler that runs database migrations on application startup.
/// </summary>
internal sealed class RunPromptMigrationNotificationHandler : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    // Injected to ensure the AddUmbracoDbContext options callback has been triggered,
    // which populates _migrationConfig with the connection string and provider name.
    private readonly IDbContextFactory<UmbracoAIPromptDbContext> _dbContextFactory;
    private readonly MigrationConnectionConfig _migrationConfig;
    private readonly ILogger<RunPromptMigrationNotificationHandler> _logger;

    public RunPromptMigrationNotificationHandler(
        IDbContextFactory<UmbracoAIPromptDbContext> dbContextFactory,
        MigrationConnectionConfig migrationConfig,
        ILogger<RunPromptMigrationNotificationHandler> logger)
    {
        _dbContextFactory = dbContextFactory;
        _migrationConfig = migrationConfig;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Running Umbraco.AI.Prompt database migrations...");

            // Create a clean DbContext using direct options rather than the DI factory.
            // In Development mode, Umbraco wraps SQLite connections with MiniProfiler's
            // ProfiledDbConnection, whose ConnectionString getter returns null and causes
            // NullReferenceException in EF Core's SqliteDatabaseCreator.Exists().
            // Creating the context directly bypasses the MiniProfiler connection wrapping.
            var optionsBuilder = new DbContextOptionsBuilder<UmbracoAIPromptDbContext>();
            UmbracoBuilderExtensions.ConfigureDatabaseProvider(
                optionsBuilder,
                _migrationConfig.ConnectionString,
                _migrationConfig.ProviderName);

            await using UmbracoAIPromptDbContext dbContext = new UmbracoAIPromptDbContext(optionsBuilder.Options);

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
