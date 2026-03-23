using System.ComponentModel;

using Umbraco.AI.Core.Tools.Scopes;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Web;

namespace Umbraco.AI.Core.Tools.Umbraco;

/// <summary>
/// Arguments for the GetContentTypeSchema tool.
/// </summary>
/// <param name="ContentKey">Optional key of a content item whose content type schema to retrieve.</param>
/// <param name="ContentTypeAlias">Optional content type alias to look up directly (e.g., 'blogPost', 'article').</param>
public record GetContentTypeSchemaArgs(
    [property: Description("The unique key (GUID) of a content item. The schema of its content type will be returned. Use IDs from search_umbraco or get_umbraco_content results. Provide either this or ContentTypeAlias.")]
    Guid? ContentKey = null,

    [property: Description("The content type alias to look up directly (e.g., 'blogPost', 'article'). Use this when you already know the content type alias from a previous tool call. Provide either this or ContentKey.")]
    string? ContentTypeAlias = null);

/// <summary>
/// Tool that retrieves the schema of a content type,
/// including property definitions and their editor types.
/// Accepts either a content item key or a content type alias.
/// </summary>
[AITool("get_content_type_schema", "Get Content Type Schema", ScopeId = ContentReadScope.ScopeId)]
public class GetContentTypeSchemaTool : AIToolBase<GetContentTypeSchemaArgs>
{
    private readonly IUmbracoContextAccessor _umbracoContextAccessor;
    private readonly IPublishedContentTypeCache _publishedContentTypeCache;

    /// <summary>
    /// Initializes a new instance of <see cref="GetContentTypeSchemaTool"/>.
    /// </summary>
    /// <param name="umbracoContextAccessor">The Umbraco context accessor.</param>
    /// <param name="publishedContentTypeCache">The published content type cache for alias-based lookups.</param>
    public GetContentTypeSchemaTool(
        IUmbracoContextAccessor umbracoContextAccessor,
        IPublishedContentTypeCache publishedContentTypeCache)
    {
        _umbracoContextAccessor = umbracoContextAccessor;
        _publishedContentTypeCache = publishedContentTypeCache;
    }

    /// <inheritdoc />
    public override string Description =>
        "Retrieves the content type schema by content item key or content type alias. " +
        "Returns the property definitions including alias, editor type, and value type. " +
        "Use this to understand what properties a document type has and what editors they use. " +
        "Useful for knowing what fields to fill in or what content structure to expect. " +
        "Pass either a content item key or a content type alias directly.";

    /// <inheritdoc />
    protected override Task<object> ExecuteAsync(GetContentTypeSchemaArgs args, CancellationToken cancellationToken = default)
    {
        if ((args.ContentKey is null || args.ContentKey == Guid.Empty) && string.IsNullOrWhiteSpace(args.ContentTypeAlias))
        {
            return Task.FromResult<object>(new GetContentTypeSchemaResult(
                false, null, "Either ContentKey or ContentTypeAlias must be provided."));
        }

        // If alias is provided, look up directly via the content type cache
        if (!string.IsNullOrWhiteSpace(args.ContentTypeAlias))
        {
            return Task.FromResult<object>(ResolveByAlias(args.ContentTypeAlias));
        }

        // Otherwise resolve via content item
        if (!_umbracoContextAccessor.TryGetUmbracoContext(out var umbracoContext))
        {
            return Task.FromResult<object>(new GetContentTypeSchemaResult(
                false, null, "Umbraco context is not available."));
        }

        // Try content cache first, then media cache
        var content = umbracoContext.Content?.GetById(args.ContentKey!.Value)
            ?? umbracoContext.Media?.GetById(args.ContentKey!.Value);

        if (content is null)
        {
            return Task.FromResult<object>(new GetContentTypeSchemaResult(
                false, null, $"Content with key '{args.ContentKey}' was not found or is not published."));
        }

        return Task.FromResult<object>(BuildSchemaResult(content.ContentType));
    }

    private GetContentTypeSchemaResult ResolveByAlias(string alias)
    {
        // Try content types first, then media types
        var contentType = _publishedContentTypeCache.Get(PublishedItemType.Content, alias)
            ?? _publishedContentTypeCache.Get(PublishedItemType.Element, alias)
            ?? _publishedContentTypeCache.Get(PublishedItemType.Media, alias);

        if (contentType is null)
        {
            return new GetContentTypeSchemaResult(
                false, null, $"Content type with alias '{alias}' was not found.");
        }

        return BuildSchemaResult(contentType);
    }

    private static GetContentTypeSchemaResult BuildSchemaResult(IPublishedContentType contentType)
    {
        var properties = contentType.PropertyTypes
            .Select(pt => new ContentTypePropertySchema(
                pt.Alias,
                pt.DataType.EditorAlias,
                pt.ModelClrType?.Name ?? "unknown"))
            .ToList();

        var compositions = contentType.CompositionAliases?.ToList() ?? [];

        var schema = new ContentTypeSchemaItem(
            contentType.Alias,
            contentType.IsElement,
            compositions,
            properties);

        return new GetContentTypeSchemaResult(true, schema, null);
    }
}

/// <summary>
/// Result of the get content type schema tool.
/// </summary>
/// <param name="Success">Whether the content type was found.</param>
/// <param name="Schema">The content type schema, if found.</param>
/// <param name="Message">Optional message (typically for errors).</param>
public record GetContentTypeSchemaResult(
    bool Success,
    ContentTypeSchemaItem? Schema,
    string? Message);

/// <summary>
/// Schema information for a content type.
/// </summary>
/// <param name="Alias">The content type alias.</param>
/// <param name="IsElement">Whether this is an element type (used in block editors).</param>
/// <param name="Compositions">Aliases of composed content types.</param>
/// <param name="Properties">The property definitions.</param>
public record ContentTypeSchemaItem(
    string Alias,
    bool IsElement,
    IReadOnlyList<string> Compositions,
    IReadOnlyList<ContentTypePropertySchema> Properties);

/// <summary>
/// Schema information for a single property on a content type.
/// </summary>
/// <param name="Alias">The property alias.</param>
/// <param name="EditorAlias">The property editor alias (e.g., "Umbraco.TextBox", "Umbraco.RichText").</param>
/// <param name="ValueType">The CLR value type name.</param>
public record ContentTypePropertySchema(
    string Alias,
    string EditorAlias,
    string ValueType);
