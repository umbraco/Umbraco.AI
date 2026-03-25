using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.AI.Search.EFCore.Notifications;

/// <summary>
/// Notification handler that runs pending EF Core migrations for AI Search on application startup.
/// </summary>
public class RunAISearchMigrationNotificationHandler
    : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    private readonly IOptions<ConnectionStrings> _connectionStrings;

    /// <summary>
    /// Initializes a new instance of <see cref="RunAISearchMigrationNotificationHandler"/>.
    /// </summary>
    public RunAISearchMigrationNotificationHandler(IOptions<ConnectionStrings> connectionStrings)
        => _connectionStrings = connectionStrings;

    /// <inheritdoc />
    public async Task HandleAsync(
        UmbracoApplicationStartedNotification notification,
        CancellationToken cancellationToken)
    {
        // Create a standalone DbContext to avoid NPoco/MiniProfiler connection issues.
        // See: https://github.com/umbraco/Umbraco-CMS/issues/22124
        var optionsBuilder = new DbContextOptionsBuilder<UmbracoAISearchDbContext>();
        UmbracoAISearchDbContext.ConfigureProvider(
            optionsBuilder,
            _connectionStrings.Value.ConnectionString,
            _connectionStrings.Value.ProviderName);

        await using UmbracoAISearchDbContext dbContext = new UmbracoAISearchDbContext(optionsBuilder.Options);

        IEnumerable<string> pending = await dbContext.Database.GetPendingMigrationsAsync(cancellationToken);
        if (pending.Any())
        {
            await dbContext.Database.MigrateAsync(cancellationToken);
        }
    }
}
