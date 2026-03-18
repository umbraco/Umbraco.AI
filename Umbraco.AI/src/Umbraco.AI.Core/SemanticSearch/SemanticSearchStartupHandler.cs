using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.TaskQueue;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.AI.Core.SemanticSearch;

/// <summary>
/// Checks if the semantic search index is empty on startup and triggers a full reindex.
/// </summary>
internal sealed class SemanticSearchStartupHandler
    : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    private readonly IBackgroundTaskQueue _taskQueue;
    private readonly AISemanticSearchOptions _options;

    public SemanticSearchStartupHandler(
        IBackgroundTaskQueue taskQueue,
        IOptions<AISemanticSearchOptions> options)
    {
        _taskQueue = taskQueue;
        _options = options.Value;
    }

    public async Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            return;
        }

        await _taskQueue.QueueAsync(
            new BackgroundWorkItem(
                "SemanticIndex:startup-check",
                null,
                async (sp, ct) =>
                {
                    var service = sp.GetRequiredService<IAISemanticSearchService>();
                    var status = await service.GetIndexStatusAsync(ct);

                    if (status.TotalIndexed == 0)
                    {
                        await service.ReindexAllAsync(ct);
                    }
                }),
            cancellationToken);
    }
}
