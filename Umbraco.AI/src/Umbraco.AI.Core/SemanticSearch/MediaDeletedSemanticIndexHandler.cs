using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.TaskQueue;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.AI.Core.SemanticSearch;

/// <summary>
/// Removes media embeddings when media is deleted.
/// </summary>
internal sealed class MediaDeletedSemanticIndexHandler
    : INotificationAsyncHandler<MediaDeletedNotification>
{
    private readonly IBackgroundTaskQueue _taskQueue;
    private readonly AISemanticSearchOptions _options;

    public MediaDeletedSemanticIndexHandler(
        IBackgroundTaskQueue taskQueue,
        IOptions<AISemanticSearchOptions> options)
    {
        _taskQueue = taskQueue;
        _options = options.Value;
    }

    public async Task HandleAsync(MediaDeletedNotification notification, CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            return;
        }

        foreach (var media in notification.DeletedEntities)
        {
            var key = media.Key;
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
