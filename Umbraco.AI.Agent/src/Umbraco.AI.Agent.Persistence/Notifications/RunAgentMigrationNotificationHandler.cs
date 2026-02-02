using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.AI.Agent.Persistence.Notifications;

/// <summary>
/// Notification handler that runs database migrations on application startup.
/// </summary>
internal sealed class RunAgentMigrationNotificationHandler : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    private readonly IDbContextFactory<UmbracoAIAgentDbContext> _dbContextFactory;
    private readonly ILogger<RunAgentMigrationNotificationHandler> _logger;

    public RunAgentMigrationNotificationHandler(
        IDbContextFactory<UmbracoAIAgentDbContext> dbContextFactory,
        ILogger<RunAgentMigrationNotificationHandler> logger)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Running Umbraco.Ai.Agent database migrations...");

            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            await dbContext.Database.MigrateAsync(cancellationToken);

            _logger.LogInformation("Umbraco.Ai.Agent database migrations completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run Umbraco.Ai.Agent database migrations.");
            throw;
        }
    }
}
