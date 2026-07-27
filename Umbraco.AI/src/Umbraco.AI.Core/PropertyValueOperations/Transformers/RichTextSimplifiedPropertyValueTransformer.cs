using System.Text.Json.Nodes;
using CmsConstants = Umbraco.Cms.Core.Constants;

namespace Umbraco.AI.Core.PropertyValueOperations.Transformers;

/// <summary>
/// Simplified value transformer for the <c>Umbraco.RichText</c> editor.
/// </summary>
/// <remarks>
/// The rich-text write value is <c>{ markup, blocks }</c>, where <c>blocks</c> contains an
/// unconstrained per-property value node that a strict structured-output provider rejects. This
/// transformer offers the LLM a plain markup <b>string</b> instead, and wraps it back into the write
/// shape — preserving any existing inline blocks from the current value.
/// </remarks>
public sealed class RichTextSimplifiedPropertyValueTransformer : IAISimplifiedPropertyValueTransformer
{
    private const string MarkupPropertyName = "markup";
    private const string BlocksPropertyName = "blocks";

    // The blocks layout dictionary is keyed by the RTE editor alias (CMS RichTextBlockValue.PropertyEditorAlias).
    private const string BlocksLayoutKey = CmsConstants.PropertyEditors.Aliases.RichText;

    /// <inheritdoc />
    public string ForPropertyEditorSchemaAlias => CmsConstants.PropertyEditors.Aliases.RichText;

    /// <inheritdoc />
    public Task<JsonNode?> GetSimplifiedSchemaAsync(Guid dataTypeKey, CancellationToken cancellationToken = default)
        => Task.FromResult<JsonNode?>(new JsonObject
        {
            ["type"] = "string",
            ["description"] = "The rich-text body as an HTML markup string.",
        });

    /// <inheritdoc />
    public Task<JsonNode?> TransformToWriteValueAsync(
        JsonNode? simplifiedValue,
        JsonNode? currentValue,
        Guid dataTypeKey,
        CancellationToken cancellationToken = default)
    {
        // Defensive: a value already in write shape (object with a markup property) passes through.
        if (simplifiedValue is JsonObject writeShaped && writeShaped.ContainsKey(MarkupPropertyName))
        {
            return Task.FromResult<JsonNode?>(writeShaped.DeepClone());
        }

        var markup = simplifiedValue switch
        {
            JsonValue value when value.TryGetValue<string>(out var text) => text,
            null => string.Empty,
            _ => simplifiedValue.ToString(),
        };

        // Preserve existing inline blocks when the current value carries them; otherwise start empty.
        // Guard the type: a non-object current value (e.g. a legacy plain-string RTE value) must not throw.
        JsonNode blocks = currentValue is JsonObject currentObject
            && currentObject[BlocksPropertyName] is JsonObject currentBlocks
                ? currentBlocks.DeepClone()
                : BlockEnvelopeOps.Empty(BlocksLayoutKey);

        return Task.FromResult<JsonNode?>(new JsonObject
        {
            [MarkupPropertyName] = markup,
            [BlocksPropertyName] = blocks,
        });
    }
}
