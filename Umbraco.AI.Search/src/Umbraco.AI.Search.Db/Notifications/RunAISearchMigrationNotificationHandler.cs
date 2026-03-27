using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.AI.Search.Db.Notifications;

/// <summary>
/// Runs pending EF Core migrations for AI Search at application startup.
/// </summary>
internal sealed class RunAISearchMigrationNotificationHandler
    : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    private readonly IOptions<ConnectionStrings> _connectionStrings;
    private readonly ILogger<RunAISearchMigrationNotificationHandler> _logger;

    public RunAISearchMigrationNotificationHandler(
        IOptions<ConnectionStrings> connectionStrings,
        ILogger<RunAISearchMigrationNotificationHandler> logger)
    {
        _connectionStrings = connectionStrings;
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

            var optionsBuilder = new DbContextOptionsBuilder<UmbracoAISearchDbContext>();
            UmbracoAISearchDbContext.ConfigureProvider(
                optionsBuilder,
                _connectionStrings.Value.ConnectionString,
                _connectionStrings.Value.ProviderName);

            optionsBuilder.ConfigureWarnings(w =>
                w.Log(RelationalEventId.PendingModelChangesWarning));

            await using var dbContext = new UmbracoAISearchDbContext(optionsBuilder.Options);

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
