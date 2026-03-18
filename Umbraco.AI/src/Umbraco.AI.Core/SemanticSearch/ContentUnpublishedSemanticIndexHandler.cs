using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.TaskQueue;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.AI.Core.SemanticSearch;

/// <summary>
/// Removes content embeddings when content is unpublished.
/// </summary>
internal sealed class ContentUnpublishedSemanticIndexHandler
    : INotificationAsyncHandler<ContentUnpublishedNotification>
{
    private readonly IBackgroundTaskQueue _taskQueue;
    private readonly AISemanticSearchOptions _options;

    public ContentUnpublishedSemanticIndexHandler(
        IBackgroundTaskQueue taskQueue,
        IOptions<AISemanticSearchOptions> options)
    {
        _taskQueue = taskQueue;
        _options = options.Value;
    }

    public async Task HandleAsync(ContentUnpublishedNotification notification, CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            return;
        }

        foreach (var content in notification.UnpublishedEntities)
        {
            var key = content.Key;
            await _taskQueue.QueueAsync(
                new BackgroundWorkItem(
                    $"SemanticIndex:remove:{key}",
                    null,
                    async (sp, ct) =>
                    {
                        var service = sp.GetRequiredService<IAISemanticSearchService>();
                        await service.RemoveEntityAsync(key, ct);
                    }),
                cancellationToken);
        }
    }
}
