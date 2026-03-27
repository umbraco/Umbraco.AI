using Microsoft.Extensions.Logging;
using Umbraco.AI.Search.Core.Search;
using Umbraco.AI.Search.Core.VectorStore;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.Changes;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Core.Notifications;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;
using Umbraco.Extensions;

namespace Umbraco.AI.Search.Core.Notifications;

/// <summary>
/// Handles media indexing for the AI vector store via cache refresher and rebuild notifications.
/// </summary>
/// <remarks>
/// <para>
/// This is a workaround for <c>IPublishedContentChangeStrategy</c> not handling media changes.
/// See: https://github.com/umbraco/Umbraco.Cms.Search/issues/108
/// </para>
/// <para>
/// When the issue is resolved, this handler should be removed and media indexing should
/// be handled by the CMS Search framework via <c>RegisterContentIndex</c>.
/// </para>
/// <para>
/// Uses <see cref="MediaCacheRefresherNotification"/> for individual media saves (fires on
/// all servers in load-balanced environments) and <see cref="IndexRebuildCompletedNotification"/>
/// to re-index all media after an index rebuild.
/// </para>
/// </remarks>
internal sealed class MediaIndexingNotificationHandler :
    INotificationHandler<MediaCacheRefresherNotification>,
    INotificationAsyncHandler<IndexRebuildCompletedNotification>
{
    private const int PageSize = 500;

    private readonly AIVectorIndexer _indexer;
    private readonly IMediaService _mediaService;
    private readonly IContentIndexingDataCollectionService _dataCollectionService;
    private readonly IAIVectorStore _vectorStore;
    private readonly ILogger<MediaIndexingNotificationHandler> _logger;

    public MediaIndexingNotificationHandler(
        AIVectorIndexer indexer,
        IMediaService mediaService,
        IContentIndexingDataCollectionService dataCollectionService,
        IAIVectorStore vectorStore,
        ILogger<MediaIndexingNotificationHandler> logger)
    {
        _indexer = indexer;
        _mediaService = mediaService;
        _dataCollectionService = dataCollectionService;
        _vectorStore = vectorStore;
        _logger = logger;
    }

    /// <summary>
    /// Handles individual media save/delete via cache refresher.
    /// </summary>
    public void Handle(MediaCacheRefresherNotification notification)
    {
        if (notification.MessageObject is not MediaCacheRefresher.JsonPayload[] payloads)
        {
            return;
        }

        foreach (MediaCacheRefresher.JsonPayload payload in payloads)
        {
            if (payload.Key is null)
            {
                continue;
            }

            if (payload.ChangeTypes.HasType(TreeChangeTypes.Remove))
            {
                HandleRemoveAsync(payload.Key.Value).GetAwaiter().GetResult();
            }
            else if (payload.ChangeTypes.HasTypesAny(TreeChangeTypes.RefreshNode | TreeChangeTypes.RefreshBranch))
            {
                IndexMediaAsync(payload.Key.Value).GetAwaiter().GetResult();
            }
        }
    }

    /// <summary>
    /// Re-indexes all media after an index rebuild completes for our index.
    /// </summary>
    public async Task HandleAsync(IndexRebuildCompletedNotification notification, CancellationToken cancellationToken)
    {
        if (notification.IndexAlias != AISearchConstants.IndexAliases.Search)
        {
            return;
        }

        _logger.LogInformation("Rebuilding media vectors for index {IndexAlias}", notification.IndexAlias);

        var page = 0;
        long totalIndexed = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            IEnumerable<IMedia> mediaItems = _mediaService.GetPagedDescendants(-1, page, PageSize, out var totalRecords);

            foreach (IMedia media in mediaItems)
            {
                await IndexMediaAsync(media, cancellationToken);
                totalIndexed++;
            }

            if ((page + 1) * PageSize >= totalRecords)
            {
                break;
            }

            page++;
        }

        _logger.LogInformation("Rebuilt media vectors: {Count} items indexed for {IndexAlias}", totalIndexed, notification.IndexAlias);
    }

    private async Task IndexMediaAsync(Guid mediaKey)
    {
        IMedia? media = _mediaService.GetById(mediaKey);
        if (media is null)
        {
            return;
        }

        await IndexMediaAsync(media, CancellationToken.None);
    }

    private async Task IndexMediaAsync(IMedia media, CancellationToken cancellationToken)
    {
        try
        {
            IEnumerable<IndexField>? fields = await _dataCollectionService.CollectAsync(media, published: false, cancellationToken);
            if (fields is null)
            {
                return;
            }

            await _indexer.AddOrUpdateAsync(
                AISearchConstants.IndexAliases.Search,
                media.Key,
                UmbracoObjectTypes.Media,
                [new Variation(null, null)],
                fields,
                protection: null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to index media {MediaKey} in AI search", media.Key);
        }
    }

    private async Task HandleRemoveAsync(Guid mediaKey)
    {
        await _vectorStore.DeleteDocumentAsync(
            AISearchConstants.IndexAliases.Search,
            mediaKey.ToString("D"));
    }
}
