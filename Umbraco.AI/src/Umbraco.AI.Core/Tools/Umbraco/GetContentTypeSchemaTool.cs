using System.ComponentModel;
using System.Text.Json.Nodes;

using Umbraco.AI.Core.Tools.Scopes;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.OperationStatus;

namespace Umbraco.AI.Core.Tools.Umbraco;

/// <summary>
/// Arguments for the GetContentTypeSchema tool.
/// </summary>
/// <param name="ContentTypeAliasOrKey">
/// The content type alias (e.g., 'blogPost', 'article') OR its GUID key
/// (e.g., 'e667d852-1e35-41d1-b0eb-46db0ec2342c').
/// </param>
public record GetContentTypeSchemaArgs(
    [property: Description("The content type alias (e.g., 'blogPost', 'article') OR the content type GUID key. Use the ContentType value from search_umbraco / get_umbraco_content results, or the 'Content type' GUID from the Entity Context.")]
    string ContentTypeAliasOrKey);

/// <summary>
/// Tool that retrieves the schema of a content type by its alias,
/// including property definitions, editor types, and JSON Schema for each property's
/// expected input value (when the property editor supports schema generation).
/// </summary>
[AITool("get_content_type_schema", "Get Content Type Schema", ScopeId = ContentReadScope.ScopeId)]
public class GetContentTypeSchemaTool : AIToolBase<GetContentTypeSchemaArgs>
{
    private readonly IPublishedContentTypeCache _publishedContentTypeCache;
    private readonly IPropertyEditorSchemaService _propertyEditorSchemaService;
    private readonly IIdKeyMap _idKeyMap;

    /// <summary>
    /// Initializes a new instance of <see cref="GetContentTypeSchemaTool"/>.
    /// </summary>
    public GetContentTypeSchemaTool(
        IPublishedContentTypeCache publishedContentTypeCache,
        IPropertyEditorSchemaService propertyEditorSchemaService,
        IIdKeyMap idKeyMap)
    {
        _publishedContentTypeCache = publishedContentTypeCache;
        _propertyEditorSchemaService = propertyEditorSchemaService;
        _idKeyMap = idKeyMap;
    }

    /// <inheritdoc />
    public override string Description =>
        "Retrieves the content type schema by its alias. " +
        "Returns each property's alias, editor type, value type, data type key, and (when available) " +
        "a JSON Schema describing the exact value shape the property accepts on write — including " +
        "configuration-driven schemas for block list and block grid properties. " +
        "REQUIRED before calling set_value for any non-string property (media picker, block list, block grid, " +
        "multi-node tree picker, multi-url picker, image cropper, slider, color picker, rich text, etc.). " +
        "The Entity Context system prompt only shows formatted values — it does NOT reveal the input shape, " +
        "so do not guess; always inspect ValueSchema first and produce a value that matches it. " +
        "If a property has no ValueSchema, call get_property_value_schema with its DataTypeKey for a focused lookup. " +
        "Pass the ContentType alias from search_umbraco or get_umbraco_content results " +
        "(or the contentType GUID from the Entity Context).";

    /// <inheritdoc />
    protected override async Task<object> ExecuteAsync(GetContentTypeSchemaArgs args, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(args.ContentTypeAliasOrKey))
        {
            return new GetContentTypeSchemaResult(false, null, "Content type alias or key cannot be empty.");
        }

        // The Get overloads on IPublishedContentTypeCache throw when the alias/key is
        // not registered for the requested item type, so each lookup must be wrapped
        // in try/catch and we fall through Content -> Element -> Media to find a hit.
        IPublishedContentType? contentType = null;
        if (Guid.TryParse(args.ContentTypeAliasOrKey, out var contentTypeKey))
        {
            contentType = TryGetByKey(contentTypeKey);
        }
        else
        {
            contentType = TryGetByAlias(args.ContentTypeAliasOrKey);
        }

        if (contentType is null)
        {
            return new GetContentTypeSchemaResult(
                false, null, $"Content type '{args.ContentTypeAliasOrKey}' was not found.");
        }

        var properties = await Task.WhenAll(
            contentType.PropertyTypes.Select(pt => BuildPropertySchemaAsync(pt, cancellationToken)));

        var compositions = contentType.CompositionAliases?.ToList() ?? [];

        var schema = new ContentTypeSchemaItem(
            contentType.Alias,
            contentType.IsElement,
            compositions,
            properties);

        return new GetContentTypeSchemaResult(true, schema, null);
    }

    private IPublishedContentType? TryGetByAlias(string alias)
    {
        foreach (var itemType in new[] { PublishedItemType.Content, PublishedItemType.Element, PublishedItemType.Media, PublishedItemType.Member })
        {
            try
            {
                var ct = _publishedContentTypeCache.Get(itemType, alias);
                if (ct is not null)
                {
                    return ct;
                }
            }
            catch
            {
                // The cache throws when the alias isn't registered for the item type;
                // swallow and try the next bucket.
            }
        }

        return null;
    }

    private IPublishedContentType? TryGetByKey(Guid key)
    {
        foreach (var itemType in new[] { PublishedItemType.Content, PublishedItemType.Element, PublishedItemType.Media, PublishedItemType.Member })
        {
            try
            {
                var ct = _publishedContentTypeCache.Get(itemType, key);
                if (ct is not null)
                {
                    return ct;
                }
            }
            catch
            {
                // Same swallow-and-try-next behaviour as the alias path.
            }
        }

        return null;
    }

    private async Task<ContentTypePropertySchema> BuildPropertySchemaAsync(
        IPublishedPropertyType pt,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var keyAttempt = _idKeyMap.GetKeyForId(pt.DataType.Id, UmbracoObjectTypes.DataType);
        Guid? dataTypeKey = keyAttempt.Success ? keyAttempt.Result : null;

        JsonObject? valueSchema = null;
        if (dataTypeKey is { } key)
        {
            Attempt<PropertyValueSchema, PropertyEditorSchemaOperationStatus> attempt =
                await _propertyEditorSchemaService.GetSchemaAsync(key);
            if (attempt.Success)
            {
                valueSchema = BlockSchemaEnricher.Enrich(
                    attempt.Result?.JsonSchema,
                    _publishedContentTypeCache,
                    _propertyEditorSchemaService);
            }
        }

        return new ContentTypePropertySchema(
            pt.Alias,
            pt.DataType.EditorAlias,
            pt.ModelClrType?.Name ?? "unknown",
            dataTypeKey,
            valueSchema);
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
/// <param name="DataTypeKey">
/// The data type's GUID key. Pass to get_property_value_schema for a focused schema lookup
/// (useful for nested element-type properties when a richer schema is needed).
/// </param>
/// <param name="ValueSchema">
/// JSON Schema (draft 2020-12) describing the value shape this property accepts on write.
/// <c>null</c> when the property editor does not implement <c>IValueSchemaProvider</c>.
/// </param>
public record ContentTypePropertySchema(
    string Alias,
    string EditorAlias,
    string ValueType,
    Guid? DataTypeKey,
    JsonObject? ValueSchema);
