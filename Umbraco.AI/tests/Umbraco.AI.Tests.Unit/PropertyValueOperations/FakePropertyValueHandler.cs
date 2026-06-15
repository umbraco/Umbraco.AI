using System.Text.Json.Nodes;
using Umbraco.AI.Core.PropertyValueOperations;

namespace Umbraco.AI.Tests.Unit.PropertyValueOperations;

/// <summary>
/// Test handler that mimics a block-list-style envelope. Items are stored as JSON objects in the
/// <c>items</c> array, each with a <c>blockKey</c>, <c>contentTypeKey</c>, and <c>values</c>
/// dictionary keyed by property alias.
/// </summary>
internal sealed class FakePropertyValueHandler : IAIPropertyValueHandler
{
    public FakePropertyValueHandler(string editorSchemaAlias, AIValidationResult? addItemValidation = null)
    {
        ForPropertyEditorSchemaAlias = editorSchemaAlias;
        AddItemValidation = addItemValidation;
    }

    public string ForPropertyEditorSchemaAlias { get; }

    public AIValidationResult? AddItemValidation { get; }

    public Func<JsonNode?, Guid, AIPropertyValueOperationContext, JsonNode?>? OverrideRemoveItem { get; init; }

    public Task<AIAddItemHandlerResult> AddItemAsync(JsonNode? value, AIAddItemArgs args, AIPropertyValueOperationContext context, CancellationToken cancellationToken = default)
    {
        var envelope = value as JsonObject ?? new JsonObject { ["items"] = new JsonArray() };
        var items = envelope["items"] as JsonArray ?? new JsonArray();

        var newKey = Guid.NewGuid();
        var item = new JsonObject
        {
            ["blockKey"] = newKey,
            ["contentTypeKey"] = args.ElementType is null ? null : Guid.Parse(args.ElementType),
            ["values"] = args.Values?.DeepClone() ?? new JsonObject(),
        };

        if (args.Position is { } pos && pos >= 0 && pos <= items.Count)
        {
            // JsonArray doesn't support Insert directly; rebuild.
            var rebuilt = new JsonArray();
            for (var i = 0; i < items.Count; i++)
            {
                if (i == pos)
                {
                    rebuilt.Add(item);
                }

                rebuilt.Add(items[i]?.DeepClone());
            }

            if (pos == items.Count)
            {
                rebuilt.Add(item);
            }

            envelope["items"] = rebuilt;
        }
        else
        {
            items.Add(item);
            envelope["items"] = items;
        }

        return Task.FromResult(new AIAddItemHandlerResult(envelope, newKey));
    }

    public Task<JsonNode?> RemoveItemAsync(JsonNode? value, Guid blockKey, AIPropertyValueOperationContext context, CancellationToken cancellationToken = default)
    {
        if (OverrideRemoveItem is not null)
        {
            return Task.FromResult(OverrideRemoveItem(value, blockKey, context));
        }

        var envelope = value as JsonObject;
        if (envelope?["items"] is not JsonArray items)
        {
            return Task.FromResult<JsonNode?>(envelope);
        }

        var rebuilt = new JsonArray();
        foreach (var entry in items)
        {
            if (entry?["blockKey"]?.GetValue<Guid>() == blockKey)
            {
                continue;
            }

            rebuilt.Add(entry?.DeepClone());
        }

        envelope["items"] = rebuilt;
        return Task.FromResult<JsonNode?>(envelope);
    }

    public Task<JsonNode?> MoveItemAsync(JsonNode? value, Guid blockKey, int newPosition, AIPropertyValueOperationContext context, CancellationToken cancellationToken = default)
    {
        var envelope = value as JsonObject;
        if (envelope?["items"] is not JsonArray items)
        {
            return Task.FromResult<JsonNode?>(envelope);
        }

        var pulled = new List<JsonNode?>();
        JsonNode? moved = null;
        foreach (var entry in items)
        {
            if (entry?["blockKey"]?.GetValue<Guid>() == blockKey)
            {
                moved = entry.DeepClone();
                continue;
            }

            pulled.Add(entry?.DeepClone());
        }

        if (moved is null)
        {
            return Task.FromResult<JsonNode?>(envelope);
        }

        var clamped = Math.Clamp(newPosition, 0, pulled.Count);
        pulled.Insert(clamped, moved);

        var rebuilt = new JsonArray();
        foreach (var n in pulled)
        {
            rebuilt.Add(n);
        }

        envelope["items"] = rebuilt;
        return Task.FromResult<JsonNode?>(envelope);
    }

    public Task<JsonNode?> ClearAsync(JsonNode? value, AIPropertyValueOperationContext context, CancellationToken cancellationToken = default)
        => Task.FromResult<JsonNode?>(new JsonObject { ["items"] = new JsonArray() });

    public Guid? GetItemContentTypeKey(JsonNode? value, Guid blockKey, AIPropertyValueOperationContext context)
    {
        if (value is not JsonObject envelope || envelope["items"] is not JsonArray items)
        {
            return null;
        }

        foreach (var entry in items)
        {
            if (entry?["blockKey"]?.GetValue<Guid>() == blockKey)
            {
                return entry["contentTypeKey"]?.GetValue<Guid?>();
            }
        }

        return null;
    }

    public JsonNode? GetItemPropertyValue(JsonNode? value, Guid blockKey, string propertyAlias, AIVariantId? variantId, AIPropertyValueOperationContext context)
    {
        if (value is not JsonObject envelope || envelope["items"] is not JsonArray items)
        {
            return null;
        }

        foreach (var entry in items)
        {
            if (entry?["blockKey"]?.GetValue<Guid>() == blockKey)
            {
                return entry["values"]?[propertyAlias]?.DeepClone();
            }
        }

        return null;
    }

    public Task<JsonNode?> SetItemPropertyValueAsync(JsonNode? value, Guid blockKey, string propertyAlias, JsonNode? newPropertyValue, AIVariantId? variantId, AIPropertyValueOperationContext context, CancellationToken cancellationToken = default)
    {
        if (value is not JsonObject envelope || envelope["items"] is not JsonArray items)
        {
            return Task.FromResult<JsonNode?>(value);
        }

        foreach (var entry in items)
        {
            if (entry is JsonObject obj && obj["blockKey"]?.GetValue<Guid>() == blockKey)
            {
                if (obj["values"] is not JsonObject values)
                {
                    values = new JsonObject();
                    obj["values"] = values;
                }

                values[propertyAlias] = newPropertyValue?.DeepClone();
                break;
            }
        }

        return Task.FromResult<JsonNode?>(envelope);
    }

    public AIValidationResult ValidateAddItem(JsonNode? value, AIAddItemArgs args, AIPropertyValueOperationContext context)
        => AddItemValidation ?? AIValidationResult.Valid;
}
