using System.Runtime.CompilerServices;
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
        // Get all media keys via IMediaService, then resolve published views
        var rootMedia = _mediaService.GetRootMedia();
        using var ctx = _umbracoContextFactory.EnsureUmbracoContext();
        var cache = ctx.UmbracoContext.Media;

        if (cache is null)
        {
            yield break;
        }

        foreach (var root in rootMedia)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Enumerate root and descendants via paged API
            await foreach (var entry in EnumerateMediaTreeAsync(root.Key, cache, cancellationToken))
            {
                yield return entry;
            }
        }
    }

    private async IAsyncEnumerable<SemanticIndexEntry> EnumerateMediaTreeAsync(
        Guid mediaKey,
        Umbraco.Cms.Core.PublishedCache.IPublishedMediaCache cache,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var published = await cache.GetByIdAsync(mediaKey);
        if (published is not null)
        {
            var entry = CreateEntry(published);
            if (entry is not null)
            {
                yield return entry;
            }

            // Recurse children
            if (published.Children is not null)
            {
                foreach (var child in published.Children)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    await foreach (var childEntry in EnumerateMediaTreeAsync(child.Key, cache, cancellationToken))
                    {
                        yield return childEntry;
                    }
                }
            }
        }
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
