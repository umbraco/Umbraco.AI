using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.AI.Prompt.Persistence.Notifications;

/// <summary>
/// Notification handler that runs database migrations on application startup.
/// </summary>
internal sealed class RunPromptMigrationNotificationHandler : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    private readonly IDbContextFactory<UmbracoAIPromptDbContext> _dbContextFactory;
    private readonly ILogger<RunPromptMigrationNotificationHandler> _logger;

    public RunPromptMigrationNotificationHandler(
        IDbContextFactory<UmbracoAIPromptDbContext> dbContextFactory,
        ILogger<RunPromptMigrationNotificationHandler> logger)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Running Umbraco.Ai.Prompt database migrations...");

            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            await dbContext.Database.MigrateAsync(cancellationToken);

            _logger.LogInformation("Umbraco.Ai.Prompt database migrations completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run Umbraco.Ai.Prompt database migrations.");
            throw;
        }
    }
}
