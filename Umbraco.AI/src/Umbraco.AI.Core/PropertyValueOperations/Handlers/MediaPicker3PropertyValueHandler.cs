using System.Text.Json.Nodes;

namespace Umbraco.AI.Core.PropertyValueOperations.Handlers;

/// <summary>
/// Property value handler for the <c>Umbraco.MediaPicker3</c> editor.
/// </summary>
/// <remarks>
/// MediaPicker3 stores its value as a JSON array of items shaped like
/// <c>{ key, mediaKey, mediaTypeAlias?, focalPoint?, crops? }</c>. The picker has no concept of
/// item content types or nested properties, so descent operations are not supported and
/// <see cref="GetItemContentTypeKey"/> always returns <c>null</c>.
/// </remarks>
public sealed class MediaPicker3PropertyValueHandler : IAIPropertyValueHandler
{
    private const string EditorSchemaAlias = "Umbraco.MediaPicker3";

    /// <inheritdoc />
    public string ForPropertyEditorSchemaAlias => EditorSchemaAlias;

    /// <inheritdoc />
    public AIValidationResult ValidateAddItem(JsonNode? value, AIAddItemArgs args, AIPropertyValueOperationContext context)
    {
        if (args.Values is null)
        {
            return AIValidationResult.Invalid(new AIPropertyValueOperationError(
                AIPropertyValueOperationError.Codes.SchemaMismatch,
                "MediaPicker3 AddItem requires 'values' with at least 'mediaKey'."));
        }

        if (!args.Values.ContainsKey("mediaKey") || args.Values["mediaKey"] is null)
        {
            return AIValidationResult.Invalid(new AIPropertyValueOperationError(
                AIPropertyValueOperationError.Codes.SchemaMismatch,
                "MediaPicker3 AddItem requires 'values.mediaKey' (the media item's GUID)."));
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
            ["mediaKey"] = args.Values?["mediaKey"]?.DeepClone(),
        };

        if (args.Values?["mediaTypeAlias"] is JsonNode mediaTypeAlias)
        {
            item["mediaTypeAlias"] = mediaTypeAlias.DeepClone();
        }

        if (args.Values?["focalPoint"] is JsonNode focalPoint)
        {
            item["focalPoint"] = focalPoint.DeepClone();
        }

        if (args.Values?["crops"] is JsonNode crops)
        {
            item["crops"] = crops.DeepClone();
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
        => null;

    /// <inheritdoc />
    public Task<JsonNode?> SetItemPropertyValueAsync(
        JsonNode? value,
        Guid blockKey,
        string propertyAlias,
        JsonNode? newPropertyValue,
        AIVariantId? variantId,
        AIPropertyValueOperationContext context,
        CancellationToken cancellationToken = default)
        => throw new InvalidOperationException("MediaPicker3 items do not have nested properties.");

    private static JsonArray AsArray(JsonNode? value)
        => value is JsonArray arr ? arr : new JsonArray();

    private static Guid? TryGetGuid(JsonNode? node, string propertyName)
    {
        if (node is not JsonObject obj || obj[propertyName] is not JsonValue jv)
        {
            return null;
        }

        if (jv.TryGetValue<Guid>(out var guid))
        {
            return guid;
        }

        if (jv.TryGetValue<string>(out var s) && Guid.TryParse(s, out guid))
        {
            return guid;
        }

        return null;
    }
}
