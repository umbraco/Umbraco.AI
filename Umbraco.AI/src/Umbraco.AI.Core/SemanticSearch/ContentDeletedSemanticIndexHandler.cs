using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.TaskQueue;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.AI.Core.SemanticSearch;

/// <summary>
/// Removes content embeddings when content is deleted.
/// </summary>
internal sealed class ContentDeletedSemanticIndexHandler
    : INotificationAsyncHandler<ContentDeletedNotification>
{
    private readonly IBackgroundTaskQueue _taskQueue;
    private readonly AISemanticSearchOptions _options;

    public ContentDeletedSemanticIndexHandler(
        IBackgroundTaskQueue taskQueue,
        IOptions<AISemanticSearchOptions> options)
    {
        _taskQueue = taskQueue;
        _options = options.Value;
    }

    public async Task HandleAsync(ContentDeletedNotification notification, CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            return;
        }

        foreach (var content in notification.DeletedEntities)
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
