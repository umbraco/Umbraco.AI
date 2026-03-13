using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.AI.Prompt.Persistence.Configuration;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.AI.Prompt.Persistence.Notifications;

/// <summary>
/// Notification handler that runs database migrations on application startup.
/// </summary>
internal sealed class RunPromptMigrationNotificationHandler : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    private readonly IOptions<ConnectionStrings> _connectionStrings;
    private readonly ILogger<RunPromptMigrationNotificationHandler> _logger;

    public RunPromptMigrationNotificationHandler(
        IOptions<ConnectionStrings> connectionStrings,
        ILogger<RunPromptMigrationNotificationHandler> logger)
    {
        _connectionStrings = connectionStrings;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Running Umbraco.AI.Prompt database migrations...");

            // Create a standalone DbContext rather than using IDbContextFactory. Umbraco's EFCoreScope
            // infrastructure shares NPoco connections (wrapped with MiniProfiler's ProfiledDbConnection)
            // onto pooled EF Core contexts via SetDbConnection(). These tainted contexts cause
            // NullReferenceException in SqliteDatabaseCreator.Exists() when the ProfiledDbConnection's
            // inner connection is disposed. Creating the context directly avoids the pooled factory.
            // See: https://github.com/umbraco/Umbraco-CMS/issues/22124
            var optionsBuilder = new DbContextOptionsBuilder<UmbracoAIPromptDbContext>();
            UmbracoBuilderExtensions.ConfigureDatabaseProvider(
                optionsBuilder,
                ResolveConnectionString(),
                _connectionStrings.Value.ProviderName);

            await using UmbracoAIPromptDbContext dbContext = new UmbracoAIPromptDbContext(optionsBuilder.Options);

            IEnumerable<string> pending = await dbContext.Database.GetPendingMigrationsAsync(cancellationToken);
            if (pending.Any())
            {
                await dbContext.Database.MigrateAsync(cancellationToken);
            }

            _logger.LogInformation("Umbraco.AI.Prompt database migrations completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run Umbraco.AI.Prompt database migrations.");
            throw;
        }
    }

    private string? ResolveConnectionString()
    {
        var connectionString = _connectionStrings.Value.ConnectionString;

        string? dataDirectory = AppDomain.CurrentDomain
            .GetData(Constants.System.DataDirectoryName)?.ToString();

        if (!string.IsNullOrEmpty(dataDirectory))
        {
            connectionString = connectionString?.Replace(
                Constants.System.DataDirectoryPlaceholder, dataDirectory);
        }

        return connectionString;
    }
}
