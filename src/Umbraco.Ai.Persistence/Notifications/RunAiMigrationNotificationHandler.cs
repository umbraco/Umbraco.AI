using Microsoft.EntityFrameworkCore;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.Ai.Persistence.Notifications;

/// <summary>
/// Notification handler that runs pending EF Core migrations on application startup.
/// </summary>
public class RunAiMigrationNotificationHandler
    : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    private readonly UmbracoAiDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of <see cref="RunAiMigrationNotificationHandler"/>.
    /// </summary>
    public RunAiMigrationNotificationHandler(UmbracoAiDbContext dbContext)
        => _dbContext = dbContext;

    /// <inheritdoc />
    public async Task HandleAsync(
        UmbracoApplicationStartedNotification notification,
        CancellationToken cancellationToken)
    {
        IEnumerable<string> pending = await _dbContext.Database.GetPendingMigrationsAsync(cancellationToken);
        if (pending.Any())
        {
            await _dbContext.Database.MigrateAsync(cancellationToken);
        }
    }
}
