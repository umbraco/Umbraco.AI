using System.Runtime.CompilerServices;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;

namespace Umbraco.AI.Core.SemanticSearch;

/// <summary>
/// Semantic index source for published CMS content items.
/// </summary>
internal sealed class ContentSemanticIndexSource : ISemanticIndexSource
{
    private readonly IContentTextExtractor _textExtractor;
    private readonly IUmbracoContextFactory _umbracoContextFactory;
    private readonly IContentService _contentService;

    public ContentSemanticIndexSource(
        IContentTextExtractor textExtractor,
        IUmbracoContextFactory umbracoContextFactory,
        IContentService contentService)
    {
        _textExtractor = textExtractor;
        _umbracoContextFactory = umbracoContextFactory;
        _contentService = contentService;
    }

    /// <inheritdoc />
    public string EntityType => "content";

    /// <inheritdoc />
    public async Task<SemanticIndexEntry?> GetEntryAsync(Guid entityKey, CancellationToken cancellationToken = default)
    {
        using var ctx = _umbracoContextFactory.EnsureUmbracoContext();
        var content = await ctx.UmbracoContext.Content!.GetByIdAsync(entityKey);

        if (content is null)
        {
            return null;
        }

        return CreateEntry(content);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<SemanticIndexEntry> GetAllEntriesAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Get root content, then traverse tree using published cache
        var rootContent = _contentService.GetRootContent();
        using var ctx = _umbracoContextFactory.EnsureUmbracoContext();
        var cache = ctx.UmbracoContext.Content;

        if (cache is null)
        {
            yield break;
        }

        foreach (var root in rootContent)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await foreach (var entry in EnumerateContentTreeAsync(root.Key, cache, cancellationToken))
            {
                yield return entry;
            }
        }
    }

    private async IAsyncEnumerable<SemanticIndexEntry> EnumerateContentTreeAsync(
        Guid contentKey,
        Umbraco.Cms.Core.PublishedCache.IPublishedContentCache cache,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var published = await cache.GetByIdAsync(contentKey);
        if (published is null)
        {
            yield break;
        }

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

                await foreach (var childEntry in EnumerateContentTreeAsync(child.Key, cache, cancellationToken))
                {
                    yield return childEntry;
                }
            }
        }
    }

    private SemanticIndexEntry? CreateEntry(IPublishedContent content)
    {
        var text = _textExtractor.ExtractText(content);
        if (text is null)
        {
            return null;
        }

        return new SemanticIndexEntry(
            content.Key,
            EntityType,
            content.ContentType.Alias,
            content.Name,
            text,
            content.UpdateDate);
    }
}
