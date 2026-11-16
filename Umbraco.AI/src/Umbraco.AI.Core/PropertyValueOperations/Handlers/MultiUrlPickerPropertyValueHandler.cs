using System.Text.Json.Nodes;

namespace Umbraco.AI.Core.PropertyValueOperations.Handlers;

/// <summary>
/// Property value handler for the <c>Umbraco.MultiUrlPicker</c> editor.
/// </summary>
/// <remarks>
/// MultiUrlPicker's editor value is a JSON array of links shaped like the CMS <c>LinkDisplay</c>:
/// <c>{ type, unique?, url?, name?, target?, queryString?, culture? }</c>, where <c>type</c> is
/// <c>document</c>, <c>media</c> or <c>external</c>. The CMS keeps a document or media link's target
/// only from <c>unique</c> (the linked item's key), and the URL only for external links.
/// <para>
/// Links have no key of their own, so an item is matched by the <c>key</c> this handler adds to a
/// new link (until the value is saved) or by its <c>unique</c>. <see cref="SetItemPropertyValueAsync"/>
/// is supported for the editable scalar fields (<c>name</c>, <c>target</c>, <c>queryString</c>,
/// <c>url</c>) so the dispatcher can target nested edits via path.
/// </para>
/// </remarks>
public sealed class MultiUrlPickerPropertyValueHandler : IAIPropertyValueHandler
{
    private const string EditorSchemaAlias = "Umbraco.MultiUrlPicker";

    private const string TargetPropertyName = "unique";

    /// <summary>
    /// Names a model may use for a document or media link's target. <c>unique</c> is what the CMS
    /// reads; <c>documentKey</c>/<c>mediaKey</c> were documented earlier and are still accepted.
    /// </summary>
    private static readonly string[] TargetAliases = [TargetPropertyName, "documentKey", "mediaKey"];

    private static readonly string[] CopiedProperties = ["url", "name", "target", "queryString", "culture"];

    private static readonly HashSet<string> EditableProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        "name", "target", "queryString", "url",
    };

    /// <inheritdoc />
    public string ForPropertyEditorSchemaAlias => EditorSchemaAlias;

    /// <inheritdoc />
    public AIValidationResult ValidateAddItem(JsonNode? value, AIAddItemArgs args, AIPropertyValueOperationContext context)
    {
        if (args.Values is null)
        {
            return AIValidationResult.Invalid(new AIPropertyValueOperationError(
                AIPropertyValueOperationError.Codes.SchemaMismatch,
                "MultiUrlPicker AddItem requires 'values' with 'type', plus 'url' for an external link or 'unique' (the linked item's key) for a document or media link."));
        }

        var type = args.Values["type"]?.GetValue<string?>();
        if (string.IsNullOrEmpty(type))
        {
            return AIValidationResult.Invalid(new AIPropertyValueOperationError(
                AIPropertyValueOperationError.Codes.SchemaMismatch,
                "MultiUrlPicker AddItem requires 'values.type' (e.g. 'external', 'document', 'media')."));
        }

        if (IsContentLink(type) && GetTarget(args.Values) is null)
        {
            return AIValidationResult.Invalid(new AIPropertyValueOperationError(
                AIPropertyValueOperationError.Codes.SchemaMismatch,
                $"MultiUrlPicker AddItem with type '{type}' requires 'values.unique': the key of the linked {type}."));
        }

        return AIValidationResult.Valid;
    }

    /// <inheritdoc />
    public Task<AIAddItemHandlerResult> AddItemAsync(
        JsonNode? value,
        AIAddItemArgs args,
        AIPropertyValueOperationContext context,
        CancellationToken cancellationToken = default)
    {
        var array = AsArray(value);
        var key = Guid.NewGuid();

        var item = new JsonObject
        {
            ["key"] = key,
            // The CMS builds the link's UDI from the raw type and reads it back case-sensitively.
            ["type"] = args.Values?["type"]?.GetValue<string?>()?.ToLowerInvariant(),
        };

        if (GetTarget(args.Values) is { } target)
        {
            item[TargetPropertyName] = target;
        }

        foreach (var optional in CopiedProperties)
        {
            if (args.Values?[optional] is JsonNode node)
            {
                item[optional] = node.DeepClone();
            }
        }

        if (args.Position is { } pos && pos >= 0 && pos < array.Count)
        {
            var rebuilt = new JsonArray();
            for (var i = 0; i < array.Count; i++)
            {
                if (i == pos)
                {
                    rebuilt.Add(item);
                }

                rebuilt.Add(array[i]?.DeepClone());
            }

            return Task.FromResult(new AIAddItemHandlerResult(rebuilt, key));
        }

        var clone = (JsonArray)array.DeepClone();
        clone.Add(item);
        return Task.FromResult(new AIAddItemHandlerResult(clone, key));
    }

    /// <inheritdoc />
    public Task<JsonNode?> RemoveItemAsync(
        JsonNode? value,
        Guid blockKey,
        AIPropertyValueOperationContext context,
        CancellationToken cancellationToken = default)
    {
        var array = AsArray(value);
        var rebuilt = new JsonArray();
        var removed = false;

        // Several links can point at the same item (same `unique`), so only the first match goes,
        // matching how Move/Get/Set pick the first match.
        foreach (var entry in array)
        {
            if (!removed && IsItem(entry, blockKey))
            {
                removed = true;
                continue;
            }

            rebuilt.Add(entry?.DeepClone());
        }

        return Task.FromResult<JsonNode?>(rebuilt);
    }

    /// <inheritdoc />
    public Task<JsonNode?> MoveItemAsync(
        JsonNode? value,
        Guid blockKey,
        int newPosition,
        AIPropertyValueOperationContext context,
        CancellationToken cancellationToken = default)
    {
        var array = AsArray(value);
        JsonNode? moved = null;
        var rest = new List<JsonNode?>();

        foreach (var entry in array)
        {
            if (moved is null && IsItem(entry, blockKey))
            {
                moved = entry?.DeepClone();
                continue;
            }

            rest.Add(entry?.DeepClone());
        }

        if (moved is null)
        {
            return Task.FromResult<JsonNode?>(value);
        }

        var clamped = Math.Clamp(newPosition, 0, rest.Count);
        rest.Insert(clamped, moved);

        var rebuilt = new JsonArray();
        foreach (var node in rest)
        {
            rebuilt.Add(node);
        }

        return Task.FromResult<JsonNode?>(rebuilt);
    }

    /// <inheritdoc />
    public Task<JsonNode?> ClearAsync(
        JsonNode? value,
        AIPropertyValueOperationContext context,
        CancellationToken cancellationToken = default)
        => Task.FromResult<JsonNode?>(new JsonArray());

    /// <inheritdoc />
    public Guid? GetItemContentTypeKey(JsonNode? value, Guid blockKey, AIPropertyValueOperationContext context)
        => null;

    /// <inheritdoc />
    public JsonNode? GetItemPropertyValue(
        JsonNode? value,
        Guid blockKey,
        string propertyAlias,
        AIVariantId? variantId,
        AIPropertyValueOperationContext context)
    {
        if (value is not JsonArray array)
        {
            return null;
        }

        foreach (var entry in array)
        {
            if (entry is JsonObject obj && IsItem(obj, blockKey))
            {
                return obj[propertyAlias]?.DeepClone();
            }
        }

        return null;
    }

    /// <inheritdoc />
    public Task<JsonNode?> SetItemPropertyValueAsync(
        JsonNode? value,
        Guid blockKey,
        string propertyAlias,
        JsonNode? newPropertyValue,
        AIVariantId? variantId,
        AIPropertyValueOperationContext context,
        CancellationToken cancellationToken = default)
    {
        if (!EditableProperties.Contains(propertyAlias))
        {
            throw new InvalidOperationException(
                $"MultiUrlPicker does not allow setting '{propertyAlias}'. Editable fields: {string.Join(", ", EditableProperties)}.");
        }

        var array = AsArray(value);
        var rebuilt = new JsonArray();

        foreach (var entry in array)
        {
            if (entry is JsonObject obj && IsItem(obj, blockKey))
            {
                var clone = (JsonObject)obj.DeepClone();
                clone[propertyAlias] = newPropertyValue?.DeepClone();
                rebuilt.Add(clone);
            }
            else
            {
                rebuilt.Add(entry?.DeepClone());
            }
        }

        return Task.FromResult<JsonNode?>(rebuilt);
    }

    private static JsonArray AsArray(JsonNode? value)
        => value is JsonArray arr ? arr : new JsonArray();

    private static bool IsContentLink(string type)
        => type.Equals("document", StringComparison.OrdinalIgnoreCase)
            || type.Equals("media", StringComparison.OrdinalIgnoreCase);

    private static Guid? GetTarget(JsonNode? values)
        => TargetAliases.Select(alias => BlockEnvelopeOps.GetGuid(values, alias)).FirstOrDefault(guid => guid is not null);

    private static bool IsItem(JsonNode? entry, Guid blockKey)
        => BlockEnvelopeOps.GetGuid(entry, "key") == blockKey
            || BlockEnvelopeOps.GetGuid(entry, TargetPropertyName) == blockKey;
}
