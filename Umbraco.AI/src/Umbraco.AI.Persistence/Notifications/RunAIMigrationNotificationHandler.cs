using Microsoft.EntityFrameworkCore;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.AI.Persistence.Notifications;

/// <summary>
/// Notification handler that runs pending EF Core migrations on application startup.
/// </summary>
public class RunAiMigrationNotificationHandler
    : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    private readonly IDbContextFactory<UmbracoAIDbContext> _dbContextFactory;

    /// <summary>
    /// Initializes a new instance of <see cref="RunAiMigrationNotificationHandler"/>.
    /// </summary>
    public RunAiMigrationNotificationHandler(IDbContextFactory<UmbracoAIDbContext> dbContextFactory)
        => _dbContextFactory = dbContextFactory;

    /// <inheritdoc />
    public async Task HandleAsync(
        UmbracoApplicationStartedNotification notification,
        CancellationToken cancellationToken)
    {
        await using UmbracoAIDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        IEnumerable<string> pending = await dbContext.Database.GetPendingMigrationsAsync(cancellationToken);
        if (pending.Any())
        {
            await dbContext.Database.MigrateAsync(cancellationToken);
        }
    }
}
