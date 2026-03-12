using Microsoft.EntityFrameworkCore;
using Umbraco.AI.Extensions;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.AI.Persistence.Notifications;

/// <summary>
/// Notification handler that runs pending EF Core migrations on application startup.
/// </summary>
public class RunAIMigrationNotificationHandler
    : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    // Injected to ensure the AddUmbracoDbContext options callback has been triggered,
    // which populates _migrationConfig with the connection string and provider name.
    private readonly IDbContextFactory<UmbracoAIDbContext> _dbContextFactory;
    private readonly MigrationConnectionConfig _migrationConfig;

    /// <summary>
    /// Initializes a new instance of <see cref="RunAIMigrationNotificationHandler"/>.
    /// </summary>
    public RunAIMigrationNotificationHandler(
        IDbContextFactory<UmbracoAIDbContext> dbContextFactory,
        MigrationConnectionConfig migrationConfig)
    {
        _dbContextFactory = dbContextFactory;
        _migrationConfig = migrationConfig;
    }

    /// <inheritdoc />
    public async Task HandleAsync(
        UmbracoApplicationStartedNotification notification,
        CancellationToken cancellationToken)
    {
        // Create a clean DbContext using direct options rather than the DI factory.
        // In Development mode, Umbraco wraps SQLite connections with MiniProfiler's
        // ProfiledDbConnection, whose ConnectionString getter returns null and causes
        // NullReferenceException in EF Core's SqliteDatabaseCreator.Exists().
        // Creating the context directly bypasses the MiniProfiler connection wrapping.
        var optionsBuilder = new DbContextOptionsBuilder<UmbracoAIDbContext>();
        UmbracoBuilderExtensions.ConfigureDatabaseProvider(
            optionsBuilder,
            _migrationConfig.ConnectionString,
            _migrationConfig.ProviderName);

        await using UmbracoAIDbContext dbContext = new UmbracoAIDbContext(optionsBuilder.Options);

        IEnumerable<string> pending = await dbContext.Database.GetPendingMigrationsAsync(cancellationToken);
        if (pending.Any())
        {
            await dbContext.Database.MigrateAsync(cancellationToken);
        }
    }
}
