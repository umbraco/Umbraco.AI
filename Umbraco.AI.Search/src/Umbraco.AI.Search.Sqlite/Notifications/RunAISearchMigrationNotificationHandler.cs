using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.AI.Search.Sqlite.Notifications;

/// <summary>
/// Notification handler that runs pending EF Core migrations for AI Search on SQLite at application startup.
/// </summary>
public class RunAISearchSqliteMigrationNotificationHandler
    : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    private readonly IOptions<ConnectionStrings> _connectionStrings;

    public RunAISearchSqliteMigrationNotificationHandler(IOptions<ConnectionStrings> connectionStrings)
        => _connectionStrings = connectionStrings;

    /// <inheritdoc />
    public async Task HandleAsync(
        UmbracoApplicationStartedNotification notification,
        CancellationToken cancellationToken)
    {
        var providerName = _connectionStrings.Value.ProviderName;

        if (providerName != Umbraco.Cms.Core.Constants.ProviderNames.SQLLite
            && providerName != "Microsoft.Data.SQLite")
        {
            return;
        }

        var optionsBuilder = new DbContextOptionsBuilder<UmbracoAISearchDbContext>();
        optionsBuilder.UseSqlite(
            _connectionStrings.Value.ConnectionString,
            x => x.MigrationsAssembly(typeof(RunAISearchSqliteMigrationNotificationHandler).Assembly.FullName));

        await using var dbContext = new UmbracoAISearchDbContext(optionsBuilder.Options);

        IEnumerable<string> pending = await dbContext.Database.GetPendingMigrationsAsync(cancellationToken);
        if (pending.Any())
        {
            await dbContext.Database.MigrateAsync(cancellationToken);
        }
    }
}
