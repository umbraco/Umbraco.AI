using System.Text.Json.Nodes;

namespace Umbraco.AI.Core.PropertyValueOperations.Handlers;

/// <summary>
/// Property value handler for the <c>Umbraco.MultiUrlPicker</c> editor.
/// </summary>
/// <remarks>
/// MultiUrlPicker stores its value as a JSON array of items shaped like
/// <c>{ key, type, url, name, target?, queryString? }</c> where <c>type</c> is one of
/// <c>document</c>, <c>media</c>, <c>external</c>, <c>email</c>, etc. <see cref="SetItemPropertyValueAsync"/>
/// is supported for the editable scalar fields (<c>name</c>, <c>target</c>, <c>queryString</c>,
/// <c>url</c>) so the dispatcher can target nested edits via path.
/// </remarks>
public sealed class MultiUrlPickerPropertyValueHandler : IAIPropertyValueHandler
{
    private const string EditorSchemaAlias = "Umbraco.MultiUrlPicker";

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
                "MultiUrlPicker AddItem requires 'values' with at least 'type' and 'url' (or 'documentKey'/'mediaKey' for content links)."));
        }

        var type = args.Values["type"]?.GetValue<string?>();
        if (string.IsNullOrEmpty(type))
        {
            return AIValidationResult.Invalid(new AIPropertyValueOperationError(
                AIPropertyValueOperationError.Codes.SchemaMismatch,
                "MultiUrlPicker AddItem requires 'values.type' (e.g. 'external', 'document', 'media')."));
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
            ["type"] = args.Values?["type"]?.DeepClone(),
        };

        foreach (var optional in new[] { "url", "name", "target", "queryString", "documentKey", "mediaKey" })
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

        foreach (var entry in array)
        {
            if (TryGetGuid(entry, "key") != blockKey)
            {
                rebuilt.Add(entry?.DeepClone());
            }
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
            if (moved is null && TryGetGuid(entry, "key") == blockKey)
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
            if (entry is JsonObject obj && TryGetGuid(obj, "key") == blockKey)
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
            if (entry is JsonObject obj && TryGetGuid(obj, "key") == blockKey)
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

    private static Guid? TryGetGuid(JsonNode? node, string propertyName)
        => BlockEnvelopeOps.GetGuid(node, propertyName);
}
