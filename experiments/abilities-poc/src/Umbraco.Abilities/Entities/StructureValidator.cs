using System.Text.Json.Nodes;

namespace Umbraco.Abilities.Entities;

/// <summary>
/// Checks a whole document against its structure: required fields, value types, slot counts, allowed item
/// types and references. Values that vary are checked for every variant the entity has, and problems say
/// which culture and segment they are in. Domain rules stay in <see cref="IEntityDefinition.ValidateAsync"/>.
/// </summary>
public static class StructureValidator
{
    private sealed record Context(
        EntityDocument Document,
        EntityStructure Structure,
        ItemTypeRegistry ItemTypes,
        Func<string, Guid, Task<bool>> EntityExists,
        IReadOnlyList<VariantId> Variants,
        List<Problem> Problems);

    /// <param name="entityExists">Whether an entity is live or created in the same change set.</param>
    public static async Task<IReadOnlyList<Problem>> ValidateAsync(
        EntityDocument document,
        EntityStructure structure,
        ItemTypeRegistry itemTypes,
        Func<string, Guid, Task<bool>> entityExists)
    {
        var problems = new List<Problem>();
        var variants = document.Variants();

        if (structure.Variations.VariesByCulture() && !variants.Any(v => v.Culture is not null))
        {
            problems.Add(new Problem(string.Empty, "The entity has no culture yet. Set a value, such as its name, in at least one culture."));
        }

        foreach (var unknown in variants.Where(v => v.Culture is not null && !structure.Cultures.Contains(v.Culture)))
        {
            problems.Add(new Problem(string.Empty, $"Unknown culture '{unknown.Culture}'.") { Culture = unknown.Culture, Segment = unknown.Segment });
        }

        var context = new Context(document, structure, itemTypes, entityExists, variants, problems);
        await ValidateElementAsync(context, document.Root, structure.Shape, string.Empty, variants);
        return problems;
    }

    /// <param name="variants">The variants the element exists in. Below a slot that varies, only that slot's culture.</param>
    private static async Task ValidateElementAsync(Context context, JsonObject element, ElementShape shape, string path, IReadOnlyList<VariantId> variants)
    {
        foreach (var field in shape.Fields)
        {
            foreach (var variant in Targets(field.Variations, variants))
            {
                var value = field.Variations == Variation.Invariant
                    ? Element.Fields(element)[field.Alias]
                    : Element.VariantPartOrNull(element, variant)?["fields"]?[field.Alias];
                await ValidateFieldAsync(context, field, value, EntityEditor.Join(path, field.Alias), variant);
            }
        }

        foreach (var slot in shape.Slots)
        {
            foreach (var variant in Targets(slot.Variations, variants))
            {
                var items = slot.Variations == Variation.Invariant
                    ? Element.Slot(element, slot.Alias)
                    : Element.VariantPartOrNull(element, variant)?["slots"]?[slot.Alias] as JsonArray ?? [];
                var below = slot.Variations == Variation.Invariant ? variants : variants.Where(v => v.Culture == variant.Culture).ToList();
                await ValidateSlotAsync(context, slot, items, EntityEditor.Join(path, slot.Alias), slot.Variations == Variation.Invariant ? null : variant, below);
            }
        }
    }

    private static async Task ValidateSlotAsync(Context context, SlotDefinition slot, JsonArray items, string slotPath, VariantId? scope, IReadOnlyList<VariantId> variants)
    {
        var located = new Problem(slotPath, string.Empty) { Culture = scope?.Culture, Segment = scope?.Segment };
        if (slot.Min is { } min && items.Count < min)
        {
            context.Problems.Add(located with { Message = $"Needs at least {min} item(s)." });
        }

        if (slot.Max is { } max && items.Count > max)
        {
            context.Problems.Add(located with { Message = $"Holds at most {max} item(s)." });
        }

        var allowed = context.ItemTypes.InGroup(slot.ItemTypeGroup).ToDictionary(t => t.Alias);
        foreach (var item in items.OfType<JsonObject>())
        {
            var itemPath = EntityEditor.Join(slotPath, Element.ItemKey(item).ToString());
            if (!allowed.TryGetValue(Element.ItemType(item), out var itemType))
            {
                context.Problems.Add(located with { Path = itemPath, Message = $"Item type '{Element.ItemType(item)}' is not allowed here." });
                continue;
            }

            // An item exists in the cultures it has values for, or in every culture when it has none yet.
            // This is the CMS's "expose": a block with only English values is not in the Danish page.
            var own = (item["variants"] as JsonObject ?? []).Select(v => VariantId.Parse(v.Key).Culture).OfType<string>().ToHashSet();
            var itemVariants = own.Count == 0 ? variants : variants.Where(v => v.Culture is null || own.Contains(v.Culture)).ToList();

            await ValidateElementAsync(context, item, itemType.Shape, itemPath, itemVariants);

            if (itemType.SettingsShape is { } settings)
            {
                await ValidateElementAsync(context, item["settings"] as JsonObject ?? Element.New(), settings, $"{itemPath}/@settings", itemVariants);
            }

            if (slot.ItemLayout is { } layout)
            {
                await ValidateElementAsync(context, item["layout"] as JsonObject ?? Element.New(), layout, $"{itemPath}/@layout", itemVariants);
            }
        }
    }

    private static async Task ValidateFieldAsync(Context context, FieldDefinition field, JsonNode? value, string path, VariantId variant)
    {
        Problem At(string message) => new(path, message) { Culture = variant.Culture, Segment = variant.Segment };

        if (value is null)
        {
            if (field.Required)
            {
                context.Problems.Add(At("Required."));
            }

            return;
        }

        if (FieldChecker.Check(field, value, path) is { } problem)
        {
            context.Problems.Add(At(problem.Message));
            return;
        }

        if (field.Type == FieldType.EntityReference)
        {
            foreach (var (entityType, key) in FieldChecker.EntityReferences(field, value))
            {
                if (!await context.EntityExists(entityType, key))
                {
                    context.Problems.Add(At($"No {entityType} with key {key}, live or in this change set."));
                }
            }
        }

        if (field.Type == FieldType.ItemReference && field.ReferencesItemInSlot is { } slotAlias)
        {
            var key = Guid.Parse(value.GetValue<string>());
            if (!Element.Slot(context.Document.Root, slotAlias).Any(item => Element.ItemKey(item!) == key))
            {
                context.Problems.Add(At($"No item with key {key} in '{slotAlias}'."));
            }
        }
    }

    /// <summary>The variants a value is checked in: once if invariant, else each variant the element exists in, narrowed to what it varies by.</summary>
    private static IEnumerable<VariantId> Targets(Variation varies, IReadOnlyList<VariantId> variants)
    {
        if (varies == Variation.Invariant)
        {
            return [VariantId.Invariant];
        }

        return variants
            .Select(v => v.Narrow(varies))
            .Where(v => !varies.VariesByCulture() || v.Culture is not null)
            .Distinct();
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
            var schema = Single(field);
            if (field.Many)
            {
                schema = new JsonObject { ["type"] = "array", ["items"] = schema };
            }

            schema["description"] = field.Description;
            if (field.Variations != Variation.Invariant)
            {
                schema["x-variations"] = field.Variations.ToString();
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
            ["variations"] = slot.Variations == Variation.Invariant ? null : slot.Variations.ToString(),
            ["min"] = slot.Min,
            ["max"] = slot.Max,
            ["itemLayout"] = slot.ItemLayout is { } layout ? Fields(layout) : null,
            ["allowedItemTypes"] = new JsonArray(itemTypes.InGroup(slot.ItemTypeGroup)
                .Select(t => (JsonNode?)new JsonObject { ["alias"] = t.Alias, ["group"] = t.Group, ["name"] = t.Name, ["description"] = t.Description })
                .ToArray()),
        }).ToArray());

    private static JsonObject Single(FieldDefinition field)
    {
        if (field.Type == FieldType.Json)
        {
            return (JsonObject?)field.Schema?.DeepClone() ?? new JsonObject();
        }

        if (field.Type == FieldType.EntityReference && field.ReferencesAnyOf is { Count: > 0 } types)
        {
            return new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject
                {
                    ["entityType"] = new JsonObject { ["type"] = "string", ["enum"] = new JsonArray(types.Select(t => (JsonNode?)t).ToArray()) },
                    ["key"] = new JsonObject { ["type"] = "string", ["format"] = "uuid" },
                },
                ["required"] = new JsonArray("entityType", "key"),
            };
        }

        var schema = new JsonObject
        {
            ["type"] = field.Type switch
            {
                FieldType.Number => "number",
                FieldType.Integer => "integer",
                FieldType.Boolean => "boolean",
                _ => "string",
            },
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

        return schema;
    }
}
