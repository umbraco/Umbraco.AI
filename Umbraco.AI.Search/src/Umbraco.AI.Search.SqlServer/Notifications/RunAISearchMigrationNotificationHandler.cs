using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.AI.Search.SqlServer.Notifications;

/// <summary>
/// Runs pending EF Core migrations for AI Search on SQL Server at application startup.
/// </summary>
public class RunAISearchSqlServerMigrationNotificationHandler
    : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    private readonly IOptions<ConnectionStrings> _connectionStrings;

    public RunAISearchSqlServerMigrationNotificationHandler(IOptions<ConnectionStrings> connectionStrings)
        => _connectionStrings = connectionStrings;

    /// <inheritdoc />
    public async Task HandleAsync(
        UmbracoApplicationStartedNotification notification,
        CancellationToken cancellationToken)
    {
        var optionsBuilder = new DbContextOptionsBuilder<UmbracoAISearchDbContext>();
        optionsBuilder.UseSqlServer(
            _connectionStrings.Value.ConnectionString,
            x => x.MigrationsAssembly(typeof(RunAISearchSqlServerMigrationNotificationHandler).Assembly.FullName));

        await using var dbContext = new UmbracoAISearchDbContext(optionsBuilder.Options);

        IEnumerable<string> pending = await dbContext.Database.GetPendingMigrationsAsync(cancellationToken);
        if (pending.Any())
        {
            await dbContext.Database.MigrateAsync(cancellationToken);
        }
    }
}
