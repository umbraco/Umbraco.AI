using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Umbraco.AI.Core.Configuration;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.AI.Search.Db.Notifications;

/// <summary>
/// Runs pending EF Core migrations for AI Search at application startup.
/// </summary>
internal sealed class RunAISearchMigrationNotificationHandler
    : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<RunAISearchMigrationNotificationHandler> _logger;

    public RunAISearchMigrationNotificationHandler(
        IConfiguration configuration,
        ILogger<RunAISearchMigrationNotificationHandler> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task HandleAsync(
        UmbracoApplicationStartedNotification notification,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Running Umbraco.AI.Search database migrations...");

            var (connectionString, providerName) = AIConnectionStringResolver.Resolve(_configuration);

            var optionsBuilder = new DbContextOptionsBuilder<UmbracoAISearchDbContext>();
            UmbracoAISearchDbContext.ConfigureProvider(optionsBuilder, connectionString, providerName);

            optionsBuilder.ConfigureWarnings(w =>
                w.Log(RelationalEventId.PendingModelChangesWarning));

            await using var dbContext = new UmbracoAISearchDbContext(optionsBuilder.Options);

            // Migrate history records from the shared __EFMigrationsHistory table to the
            // per-product table. This ensures previously applied migrations are recognized.
            await AIMigrationHistoryHelper.MigrateHistoryRecordsAsync(
                dbContext.Database.GetDbConnection(),
                AIConnectionStringResolver.MigrationsHistoryTableName,
                _logger,
                cancellationToken);

            IEnumerable<string> pending = await dbContext.Database.GetPendingMigrationsAsync(cancellationToken);
            if (pending.Any())
            {
                await dbContext.Database.MigrateAsync(cancellationToken);
            }

            _logger.LogInformation("Umbraco.AI.Search database migrations completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run Umbraco.AI.Search database migrations.");
            throw;
        }
    }
}
