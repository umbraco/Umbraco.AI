using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Umbraco.AI.Agent.Persistence.Configuration;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.AI.Agent.Persistence.Notifications;

/// <summary>
/// Notification handler that runs database migrations on application startup.
/// </summary>
internal sealed class RunAgentMigrationNotificationHandler : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    // Injected to ensure the AddUmbracoDbContext options callback has been triggered,
    // which populates _migrationConfig with the connection string and provider name.
    private readonly IDbContextFactory<UmbracoAIAgentDbContext> _dbContextFactory;
    private readonly MigrationConnectionConfig _migrationConfig;
    private readonly ILogger<RunAgentMigrationNotificationHandler> _logger;

    public RunAgentMigrationNotificationHandler(
        IDbContextFactory<UmbracoAIAgentDbContext> dbContextFactory,
        MigrationConnectionConfig migrationConfig,
        ILogger<RunAgentMigrationNotificationHandler> logger)
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
            _logger.LogInformation("Running Umbraco.AI.Agent database migrations...");

            // Create a clean DbContext using direct options rather than the DI factory.
            // In Development mode, Umbraco wraps SQLite connections with MiniProfiler's
            // ProfiledDbConnection, whose ConnectionString getter returns null and causes
            // NullReferenceException in EF Core's SqliteDatabaseCreator.Exists().
            // Creating the context directly bypasses the MiniProfiler connection wrapping.
            var optionsBuilder = new DbContextOptionsBuilder<UmbracoAIAgentDbContext>();
            UmbracoBuilderExtensions.ConfigureDatabaseProvider(
                optionsBuilder,
                _migrationConfig.ConnectionString,
                _migrationConfig.ProviderName);

            await using UmbracoAIAgentDbContext dbContext = new UmbracoAIAgentDbContext(optionsBuilder.Options);

            IEnumerable<string> pending = await dbContext.Database.GetPendingMigrationsAsync(cancellationToken);
            if (pending.Any())
            {
                await dbContext.Database.MigrateAsync(cancellationToken);
            }

            _logger.LogInformation("Umbraco.AI.Agent database migrations completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run Umbraco.AI.Agent database migrations.");
            throw;
        }
    }
}
