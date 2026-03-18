using System.Runtime.CompilerServices;
using Umbraco.Cms.Core.Models;
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
        using var ctx = _umbracoContextFactory.EnsureUmbracoContext();
        var cache = ctx.UmbracoContext.Content;

        if (cache is null)
        {
            yield break;
        }

        const int parentId = -1;
        const int pageSize = 100;
        var pageIndex = 0L;
        IContent[] page;

        do
        {
            cancellationToken.ThrowIfCancellationRequested();
            page = _contentService.GetPagedDescendants(parentId, pageIndex, pageSize, out _).ToArray();

            foreach (var content in page)
            {
                var published = await cache.GetByIdAsync(content.Key);
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
