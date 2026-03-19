using System.ComponentModel;

using Umbraco.AI.Core.Tools.Scopes;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Web;

namespace Umbraco.AI.Core.Tools.Umbraco;

/// <summary>
/// Arguments for the GetContentTypeSchema tool.
/// </summary>
/// <param name="ContentKey">The key of a content item whose content type schema to retrieve.</param>
public record GetContentTypeSchemaArgs(
    [property: Description("The unique key (GUID) of a content item. The schema of its content type will be returned. Use IDs from search_umbraco or get_umbraco_content results.")]
    Guid ContentKey);

/// <summary>
/// Tool that retrieves the schema of a content type from a content item,
/// including property definitions and their editor types.
/// </summary>
[AITool("get_content_type_schema", "Get Content Type Schema", ScopeId = ContentReadScope.ScopeId)]
public class GetContentTypeSchemaTool : AIToolBase<GetContentTypeSchemaArgs>
{
    private readonly IUmbracoContextAccessor _umbracoContextAccessor;

    /// <summary>
    /// Initializes a new instance of <see cref="GetContentTypeSchemaTool"/>.
    /// </summary>
    /// <param name="umbracoContextAccessor">The Umbraco context accessor.</param>
    public GetContentTypeSchemaTool(IUmbracoContextAccessor umbracoContextAccessor)
    {
        _umbracoContextAccessor = umbracoContextAccessor;
    }

    /// <inheritdoc />
    public override string Description =>
        "Retrieves the content type schema for a given content item. " +
        "Returns the property definitions including alias, editor type, and value type. " +
        "Use this to understand what properties a document type has and what editors they use. " +
        "Useful for knowing what fields to fill in or what content structure to expect. " +
        "Pass the key of any content item of the desired type.";

    /// <inheritdoc />
    protected override Task<object> ExecuteAsync(GetContentTypeSchemaArgs args, CancellationToken cancellationToken = default)
    {
        if (args.ContentKey == Guid.Empty)
        {
            return Task.FromResult<object>(new GetContentTypeSchemaResult(
                false, null, "Content key cannot be empty."));
        }

        if (!_umbracoContextAccessor.TryGetUmbracoContext(out var umbracoContext))
        {
            return Task.FromResult<object>(new GetContentTypeSchemaResult(
                false, null, "Umbraco context is not available."));
        }

        // Try content cache first, then media cache
        var content = umbracoContext.Content?.GetById(args.ContentKey)
            ?? umbracoContext.Media?.GetById(args.ContentKey);

        if (content is null)
        {
            return Task.FromResult<object>(new GetContentTypeSchemaResult(
                false, null, $"Content with key '{args.ContentKey}' was not found or is not published."));
        }

        var contentType = content.ContentType;

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

        return Task.FromResult<object>(new GetContentTypeSchemaResult(true, schema, null));
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
