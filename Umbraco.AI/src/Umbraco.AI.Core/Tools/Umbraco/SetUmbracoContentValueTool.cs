using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;

using Umbraco.AI.Core.PropertyValueOperations;
using Umbraco.AI.Core.Tools.Scopes;
using Umbraco.Cms.Core.Services;

namespace Umbraco.AI.Core.Tools.Umbraco;

/// <summary>
/// Arguments for the SetUmbracoContentValue tool.
/// </summary>
public record SetUmbracoContentValueArgs(
    [property: Description("The unique key (GUID) of the content item to update.")]
    Guid Key,

    [property: Description("Path identifying the property to set, potentially nested inside a block. An array alternating property-alias segments and block-key segments, e.g. [{\"alias\":\"heroImage\"}] for a root property, or [{\"alias\":\"contentBlocks\"},{\"blockKey\":\"<guid>\"},{\"alias\":\"innerText\"}] for a property inside a block. Must start and end with an alias segment.")]
    IReadOnlyList<UmbracoPropertyPathSegmentArg> Path,

    [property: Description("The value to set, in the shape get_content_type_schema/get_property_value_schema describes for this property.")]
    JsonElement Value,

    [property: Description("Optional culture code (e.g., 'en-US') when the content item varies by culture.")]
    string? Culture = null,

    [property: Description("Optional segment identifier when the content item is segmented.")]
    string? Segment = null);

/// <summary>
/// Tool that sets a content property's value directly (a full replacement), including properties
/// nested inside blocks. Unlike update_umbraco_content's PropertyValues, this works for structured
/// properties too — Block List, Block Grid, and rich text — by reusing the same value engine the
/// backoffice UI's own block-editing tools use.
/// </summary>
[AITool("set_umbraco_content_value", "Set Umbraco Content Value", ScopeId = ContentWriteScope.ScopeId, IsDestructive = true)]
public class SetUmbracoContentValueTool(
    IContentEditingService contentEditingService,
    IAIPropertyValueDispatcher dispatcher,
    IUmbracoWriteAuthorizer authorizer)
    : AIToolBase<SetUmbracoContentValueArgs>
{
    /// <inheritdoc />
    public override string Description =>
        "Sets a content property's value directly, including properties nested inside blocks (identify " +
        "the nesting via Path). Works for Block List, Block Grid, and rich text, unlike " +
        "update_umbraco_content's simple PropertyValues. Call get_content_type_schema first to see the " +
        "expected value shape. Persists immediately as a draft — call publish_umbraco_content afterward " +
        "to make the change live.";

    /// <inheritdoc />
    protected override async Task<object> ExecuteAsync(SetUmbracoContentValueArgs args, CancellationToken cancellationToken = default)
    {
        var dispatchArgs = new JsonObject { ["value"] = JsonNode.Parse(args.Value.GetRawText()) };

        var outcome = await ContentPropertyValueOperationHelper.ExecuteAsync(
            authorizer,
            contentEditingService,
            dispatcher,
            args.Key,
            args.Path,
            AIPropertyOperation.SetValue,
            dispatchArgs,
            args.Culture,
            args.Segment,
            cancellationToken);

        return new SetUmbracoContentValueResult(outcome.Success, outcome.Message);
    }

    /// <inheritdoc />
    protected override string? DescribeInvocation(SetUmbracoContentValueArgs args)
    {
        var propertyAlias = args.Path.LastOrDefault()?.Alias;
        return propertyAlias is null ? null : $"Set '{propertyAlias}' to {FormatValuePreview(args.Value)}.";
    }

    private static string FormatValuePreview(JsonElement value)
    {
        var raw = value.ValueKind == JsonValueKind.String ? $"'{value.GetString()}'" : value.GetRawText();
        return raw.Length > 80 ? string.Concat(raw.AsSpan(0, 77), "...") : raw;
    }
}

/// <summary>
/// Result of the set Umbraco content value tool.
/// </summary>
public record SetUmbracoContentValueResult(
    bool Success,
    string? Message);
