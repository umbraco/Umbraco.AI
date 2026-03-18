using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.TaskQueue;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.AI.Core.SemanticSearch;

/// <summary>
/// Indexes published content for semantic search when content is published.
/// </summary>
internal sealed class ContentPublishedSemanticIndexHandler
    : INotificationAsyncHandler<ContentPublishedNotification>
{
    private readonly IBackgroundTaskQueue _taskQueue;
    private readonly AISemanticSearchOptions _options;

    public ContentPublishedSemanticIndexHandler(
        IBackgroundTaskQueue taskQueue,
        IOptions<AISemanticSearchOptions> options)
    {
        _taskQueue = taskQueue;
        _options = options.Value;
    }

    public async Task HandleAsync(ContentPublishedNotification notification, CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            return;
        }

        foreach (var content in notification.PublishedEntities)
        {
            var key = content.Key;
            await _taskQueue.QueueAsync(
                new BackgroundWorkItem(
                    $"SemanticIndex:content:{key}",
                    null,
                    async (sp, ct) =>
                    {
                        var service = sp.GetRequiredService<IAISemanticSearchService>();
                        await service.IndexEntityAsync(key, "content", ct);
                    }),
                cancellationToken);
        }
    }
}
