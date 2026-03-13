using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.AI.Agent.Persistence.Configuration;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.AI.Agent.Persistence.Notifications;

/// <summary>
/// Notification handler that runs database migrations on application startup.
/// </summary>
internal sealed class RunAgentMigrationNotificationHandler : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    private readonly IOptions<ConnectionStrings> _connectionStrings;
    private readonly ILogger<RunAgentMigrationNotificationHandler> _logger;

    public RunAgentMigrationNotificationHandler(
        IOptions<ConnectionStrings> connectionStrings,
        ILogger<RunAgentMigrationNotificationHandler> logger)
    {
        _connectionStrings = connectionStrings;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Running Umbraco.AI.Agent database migrations...");

            // Create a standalone DbContext rather than using IDbContextFactory. Umbraco's EFCoreScope
            // infrastructure shares NPoco connections (wrapped with MiniProfiler's ProfiledDbConnection)
            // onto pooled EF Core contexts via SetDbConnection(). These tainted contexts cause
            // NullReferenceException in SqliteDatabaseCreator.Exists() when the ProfiledDbConnection's
            // inner connection is disposed. Creating the context directly avoids the pooled factory.
            // See: https://github.com/umbraco/Umbraco-CMS/issues/22124
            var optionsBuilder = new DbContextOptionsBuilder<UmbracoAIAgentDbContext>();
            UmbracoBuilderExtensions.ConfigureDatabaseProvider(
                optionsBuilder,
                _connectionStrings.Value.ConnectionString,
                _connectionStrings.Value.ProviderName);

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
