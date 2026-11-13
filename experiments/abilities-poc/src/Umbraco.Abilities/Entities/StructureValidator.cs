using System.Text.Json.Nodes;

namespace Umbraco.Abilities.Entities;

/// <summary>
/// Checks a whole document against its structure: required fields, value types, slot counts,
/// allowed item types and references. Domain rules stay in <see cref="IEntityDefinition.ValidateAsync"/>.
/// </summary>
public static class StructureValidator
{
    /// <param name="entityExists">Whether an entity is live or created in the same draft set.</param>
    public static async Task<IReadOnlyList<Problem>> ValidateAsync(
        EntityDocument document,
        EntityStructure structure,
        ItemTypeRegistry itemTypes,
        Func<string, Guid, Task<bool>> entityExists)
    {
        var problems = new List<Problem>();
        await ValidateElementAsync(document, document.Root, structure.Shape, string.Empty, itemTypes, entityExists, problems);
        return problems;
    }

    private static async Task ValidateElementAsync(
        EntityDocument document,
        JsonObject element,
        ElementShape shape,
        string path,
        ItemTypeRegistry itemTypes,
        Func<string, Guid, Task<bool>> entityExists,
        List<Problem> problems)
    {
        var fields = Element.Fields(element);
        foreach (var field in shape.Fields)
        {
            var fieldPath = EntityEditor.Join(path, field.Alias);
            var value = fields[field.Alias];
            if (value is null)
            {
                if (field.Required)
                {
                    problems.Add(new Problem(fieldPath, "Required."));
                }

                continue;
            }

            if (FieldChecker.Check(field, value, fieldPath) is { } problem)
            {
                problems.Add(problem);
                continue;
            }

            if (field.Type == FieldType.EntityReference && field.ReferencesEntityType is { } entityType)
            {
                var key = Guid.Parse(value.GetValue<string>());
                if (!await entityExists(entityType, key))
                {
                    problems.Add(new Problem(fieldPath, $"No {entityType} with key {key}, live or in this draft set."));
                }
            }

            if (field.Type == FieldType.ItemReference && field.ReferencesItemInSlot is { } slotAlias)
            {
                var key = Guid.Parse(value.GetValue<string>());
                if (!Element.Slot(document.Root, slotAlias).Any(item => Element.ItemKey(item!) == key))
                {
                    problems.Add(new Problem(fieldPath, $"No item with key {key} in '{slotAlias}'."));
                }
            }
        }

        foreach (var slot in shape.Slots)
        {
            var slotPath = EntityEditor.Join(path, slot.Alias);
            var items = Element.Slot(element, slot.Alias);
            if (slot.Min is { } min && items.Count < min)
            {
                problems.Add(new Problem(slotPath, $"Needs at least {min} item(s)."));
            }

            if (slot.Max is { } max && items.Count > max)
            {
                problems.Add(new Problem(slotPath, $"Holds at most {max} item(s)."));
            }

            var allowed = itemTypes.InGroup(slot.ItemTypeGroup).ToDictionary(t => t.Alias);
            foreach (var item in items.OfType<JsonObject>())
            {
                var itemPath = EntityEditor.Join(slotPath, Element.ItemKey(item).ToString());
                if (!allowed.TryGetValue(Element.ItemType(item), out var itemType))
                {
                    problems.Add(new Problem(itemPath, $"Item type '{Element.ItemType(item)}' is not allowed here."));
                    continue;
                }

                await ValidateElementAsync(document, item, itemType.Shape, itemPath, itemTypes, entityExists, problems);
            }
        }
    }
}

/// <summary>Writes a shape as JSON Schema plus a slot list, for models and editors.</summary>
public static class SchemaWriter
{
    public static JsonObject Fields(ElementShape shape)
    {
        var properties = new JsonObject();
        foreach (var field in shape.Fields)
        {
            var schema = new JsonObject
            {
                ["type"] = field.Type switch
                {
                    FieldType.Number => "number",
                    FieldType.Integer => "integer",
                    FieldType.Boolean => "boolean",
                    _ => "string",
                },
                ["description"] = field.Description,
            };

            switch (field.Type)
            {
                case FieldType.Date:
                    schema["format"] = "date";
                    break;
                case FieldType.EntityReference:
                    schema["format"] = "uuid";
                    schema["x-references"] = field.ReferencesEntityType;
                    break;
                case FieldType.ItemReference:
                    schema["format"] = "uuid";
                    schema["x-references-item-in-slot"] = field.ReferencesItemInSlot;
                    break;
            }

            if (field.Allowed is { Count: > 0 } allowed)
            {
                schema["enum"] = new JsonArray(allowed.Select(a => (JsonNode?)a).ToArray());
            }

            properties[field.Alias] = schema;
        }

        return new JsonObject
        {
            ["type"] = "object",
            ["properties"] = properties,
            ["required"] = new JsonArray(shape.Fields.Where(f => f.Required).Select(f => (JsonNode?)f.Alias).ToArray()),
        };
    }

    public static JsonArray Slots(ElementShape shape, ItemTypeRegistry itemTypes)
        => new(shape.Slots.Select(slot => (JsonNode?)new JsonObject
        {
            ["alias"] = slot.Alias,
            ["description"] = slot.Description,
            ["min"] = slot.Min,
            ["max"] = slot.Max,
            ["allowedItemTypes"] = new JsonArray(itemTypes.InGroup(slot.ItemTypeGroup)
                .Select(t => (JsonNode?)new JsonObject { ["alias"] = t.Alias, ["name"] = t.Name, ["description"] = t.Description })
                .ToArray()),
        }).ToArray());
}
