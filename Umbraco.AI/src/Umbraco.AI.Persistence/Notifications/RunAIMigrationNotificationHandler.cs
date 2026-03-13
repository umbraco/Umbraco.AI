using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

using AIBuilderExtensions = Umbraco.AI.Extensions.UmbracoBuilderExtensions;

namespace Umbraco.AI.Persistence.Notifications;

/// <summary>
/// Notification handler that runs pending EF Core migrations on application startup.
/// </summary>
public class RunAIMigrationNotificationHandler
    : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    private readonly IOptions<ConnectionStrings> _connectionStrings;

    /// <summary>
    /// Initializes a new instance of <see cref="RunAIMigrationNotificationHandler"/>.
    /// </summary>
    public RunAIMigrationNotificationHandler(IOptions<ConnectionStrings> connectionStrings)
        => _connectionStrings = connectionStrings;

    /// <inheritdoc />
    public async Task HandleAsync(
        UmbracoApplicationStartedNotification notification,
        CancellationToken cancellationToken)
    {
        // Create a standalone DbContext rather than using IDbContextFactory. Umbraco's EFCoreScope
        // infrastructure shares NPoco connections (wrapped with MiniProfiler's ProfiledDbConnection)
        // onto pooled EF Core contexts via SetDbConnection(). These tainted contexts cause
        // NullReferenceException in SqliteDatabaseCreator.Exists() when the ProfiledDbConnection's
        // inner connection is disposed. Creating the context directly avoids the pooled factory.
        // See: https://github.com/umbraco/Umbraco-CMS/issues/22124
        var optionsBuilder = new DbContextOptionsBuilder<UmbracoAIDbContext>();
        AIBuilderExtensions.ConfigureDatabaseProvider(
            optionsBuilder,
            _connectionStrings.Value.ConnectionString,
            _connectionStrings.Value.ProviderName);

        await using UmbracoAIDbContext dbContext = new UmbracoAIDbContext(optionsBuilder.Options);

        IEnumerable<string> pending = await dbContext.Database.GetPendingMigrationsAsync(cancellationToken);
        if (pending.Any())
        {
            await dbContext.Database.MigrateAsync(cancellationToken);
        }
    }
}
