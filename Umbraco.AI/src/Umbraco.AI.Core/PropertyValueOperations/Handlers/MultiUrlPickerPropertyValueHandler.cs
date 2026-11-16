using System.Text.Json.Nodes;

using CmsConstants = Umbraco.Cms.Core.Constants;

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
public class MultiUrlPickerPropertyValueHandler : IAIPropertyValueHandler
{
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
    public string ForPropertyEditorSchemaAlias => EditorAlias;

    /// <summary>
    /// The editor alias this handler serves; also names the editor in error messages.
    /// </summary>
    private protected virtual string EditorAlias => CmsConstants.PropertyEditors.Aliases.MultiUrlPicker;

    /// <summary>
    /// The most links the editor can hold, or <c>null</c> when it has no fixed limit.
    /// </summary>
    private protected virtual int? MaxItems => null;

    /// <inheritdoc />
    public AIValidationResult ValidateAddItem(JsonNode? value, AIAddItemArgs args, AIPropertyValueOperationContext context)
    {
        if (MaxItems is int maxItems && AsArray(value).Count >= maxItems)
        {
            return AIValidationResult.Invalid(new AIPropertyValueOperationError(
                AIPropertyValueOperationError.Codes.SchemaMismatch,
                $"{EditorAlias} holds at most {maxItems} link. Remove the existing one or use set_value to replace it."));
        }

        if (args.Values is null)
        {
            return AIValidationResult.Invalid(new AIPropertyValueOperationError(
                AIPropertyValueOperationError.Codes.SchemaMismatch,
                $"{EditorAlias} AddItem requires 'values' with 'type', plus 'url' for an external link or 'unique' (the linked item's key) for a document or media link."));
        }

        var type = args.Values["type"]?.GetValue<string?>();
        if (string.IsNullOrEmpty(type))
        {
            return AIValidationResult.Invalid(new AIPropertyValueOperationError(
                AIPropertyValueOperationError.Codes.SchemaMismatch,
                $"{EditorAlias} AddItem requires 'values.type' (e.g. 'external', 'document', 'media')."));
        }

        if (IsContentLink(type) && GetTarget(args.Values) is null)
        {
            return AIValidationResult.Invalid(new AIPropertyValueOperationError(
                AIPropertyValueOperationError.Codes.SchemaMismatch,
                $"{EditorAlias} AddItem with type '{type}' requires 'values.unique': the key of the linked {type}."));
        }

        return AIValidationResult.Valid;
    }

    /// <inheritdoc />
    /// <remarks>
    /// The CMS keeps a document or media link's target only from <c>unique</c> and the URL only for an
    /// external link, so a link outside those rules is saved without a target and no error. It is
    /// rejected here with the shape to use instead. <c>type</c> must be lowercase, as in the CMS value
    /// schema, because the CMS builds the link's UDI from the type as sent. An external link may be
    /// only an anchor (<c>queryString</c>), as the backoffice link picker allows.
    /// </remarks>
    public AIValidationResult ValidateSetValue(JsonNode? value, JsonNode? newValue, AIPropertyValueOperationContext context)
    {
        if (newValue is null)
        {
            return AIValidationResult.Valid;
        }

        if (newValue is not JsonArray links)
        {
            return InvalidLinks("The value must be an array of links.");
        }

        if (MaxItems is int maxItems && links.Count > maxItems)
        {
            return InvalidLinks($"{EditorAlias} holds at most {maxItems} link.");
        }

        for (var i = 0; i < links.Count; i++)
        {
            if (links[i] is not JsonObject link)
            {
                return InvalidLinks($"Link {i} must be an object.");
            }

            var type = link["type"] is JsonValue typeValue && typeValue.TryGetValue<string>(out var raw) ? raw : null;
            if (type is not ("document" or "media" or "external"))
            {
                return InvalidLinks($"Link {i} has type '{type}', which isn't supported.");
            }

            if (IsContentLink(type) && BlockEnvelopeOps.GetGuid(link, TargetPropertyName) is null)
            {
                return InvalidLinks($"Link {i} is a {type} link without 'unique' set to the key of the linked {type}.");
            }

            if (type == "external" && !HasText(link, "url") && !HasText(link, "queryString"))
            {
                return InvalidLinks($"Link {i} is an external link without a 'url' or 'queryString'.");
            }
        }

        return AIValidationResult.Valid;
    }

    private AIValidationResult InvalidLinks(string problem)
        => AIValidationResult.Invalid(new AIPropertyValueOperationError(
            AIPropertyValueOperationError.Codes.SchemaMismatch,
            $"{problem} Each {EditorAlias} link needs 'type': 'document' or 'media' with 'unique' set to the linked item's key, " +
            "or 'external' with a 'url' (or only a 'queryString' anchor). The link is not saved otherwise, so nothing was changed."));

    private static bool HasText(JsonObject link, string propertyName)
        => link[propertyName] is JsonValue value && value.TryGetValue<string>(out var text) && !string.IsNullOrWhiteSpace(text);

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
                $"{EditorAlias} does not allow setting '{propertyAlias}'. Editable fields: {string.Join(", ", EditableProperties)}.");
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
