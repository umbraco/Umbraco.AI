using System.Runtime.CompilerServices;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;

namespace Umbraco.AI.Core.SemanticSearch;

/// <summary>
/// Semantic index source for CMS media items.
/// </summary>
internal sealed class MediaSemanticIndexSource : ISemanticIndexSource
{
    private readonly IContentTextExtractor _textExtractor;
    private readonly IUmbracoContextFactory _umbracoContextFactory;
    private readonly IMediaService _mediaService;

    public MediaSemanticIndexSource(
        IContentTextExtractor textExtractor,
        IUmbracoContextFactory umbracoContextFactory,
        IMediaService mediaService)
    {
        _textExtractor = textExtractor;
        _umbracoContextFactory = umbracoContextFactory;
        _mediaService = mediaService;
    }

    /// <inheritdoc />
    public string EntityType => "media";

    /// <inheritdoc />
    public async Task<SemanticIndexEntry?> GetEntryAsync(Guid entityKey, CancellationToken cancellationToken = default)
    {
        using var ctx = _umbracoContextFactory.EnsureUmbracoContext();
        var media = await ctx.UmbracoContext.Media!.GetByIdAsync(entityKey);

        if (media is null)
        {
            return null;
        }

        return CreateEntry(media);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<SemanticIndexEntry> GetAllEntriesAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var ctx = _umbracoContextFactory.EnsureUmbracoContext();
        var cache = ctx.UmbracoContext.Media!;

        const int parentId = -1;
        const int pageSize = 100;
        var pageIndex = 0L;
        IMedia[] page;

        do
        {
            cancellationToken.ThrowIfCancellationRequested();
            page = _mediaService.GetPagedDescendants(parentId, pageIndex, pageSize, out _).ToArray();

            foreach (var media in page)
            {
                var published = await cache.GetByIdAsync(media.Key);
                if (published is null)
                {
                    continue;
                }

                var entry = CreateEntry(published);
                if (entry is not null)
                {
                    yield return entry;
                }
            }

            pageIndex++;
        }
        while (page.Length == pageSize);
    }

    private SemanticIndexEntry? CreateEntry(IPublishedContent media)
    {
        var text = _textExtractor.ExtractText(media);
        if (text is null)
        {
            return null;
        }

        return new SemanticIndexEntry(
            media.Key,
            EntityType,
            media.ContentType.Alias,
            media.Name,
            text,
            media.UpdateDate);
    }
}
