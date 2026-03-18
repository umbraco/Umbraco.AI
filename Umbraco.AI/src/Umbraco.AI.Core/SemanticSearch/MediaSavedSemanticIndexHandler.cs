using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.TaskQueue;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.AI.Core.SemanticSearch;

/// <summary>
/// Indexes media items for semantic search when media is saved.
/// Media does not have a publish workflow, so we index on save.
/// </summary>
internal sealed class MediaSavedSemanticIndexHandler
    : INotificationAsyncHandler<MediaSavedNotification>
{
    private readonly IBackgroundTaskQueue _taskQueue;
    private readonly AISemanticSearchOptions _options;

    public MediaSavedSemanticIndexHandler(
        IBackgroundTaskQueue taskQueue,
        IOptions<AISemanticSearchOptions> options)
    {
        _taskQueue = taskQueue;
        _options = options.Value;
    }

    public async Task HandleAsync(MediaSavedNotification notification, CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            return;
        }

        foreach (var media in notification.SavedEntities)
        {
            var key = media.Key;
            await _taskQueue.QueueAsync(
                new BackgroundWorkItem(
                    $"SemanticIndex:media:{key}",
                    null,
                    async (sp, ct) =>
                    {
                        var service = sp.GetRequiredService<IAISemanticSearchService>();
                        await service.IndexEntityAsync(key, "media", ct);
                    }),
                cancellationToken);
        }
    }
}
