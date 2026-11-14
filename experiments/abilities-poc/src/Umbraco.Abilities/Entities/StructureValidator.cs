using System.Text.Json.Nodes;

using Umbraco.Cms.Core.Models;
using Umbraco.Extensions;
namespace Umbraco.Abilities.Entities;

/// <summary>
/// Checks a whole document against its structure: required fields, value types, item counts, allowed item
/// types and references. Values that vary are checked for every variant the entity has, and problems say
/// which culture and segment they are in. Domain rules stay in <see cref="IEntityDefinition.ValidateAsync"/>.
/// </summary>
public static class StructureValidator
{
    private sealed record Context(
        EntityDocument Document,
        ItemTypeRegistry ItemTypes,
        Func<string, Guid, Task<bool>> EntityExists,
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

        var context = new Context(document, itemTypes, entityExists, problems);
        await ValidateElementAsync(context, document.Root, structure.Shape, string.Empty, variants);
        return problems;
    }

    /// <param name="variants">The variants the element exists in. Below an items field that varies, only that field's culture.</param>
    private static async Task ValidateElementAsync(Context context, EntityElement element, ElementShape shape, string path, IReadOnlyList<VariantId> variants)
    {
        foreach (var field in shape.Fields)
        {
            foreach (var variant in Targets(field.Variations, variants))
            {
                if (!field.IsItems)
                {
                    await ValidateFieldAsync(context, field, element.Get(field, variant), EntityEditor.Join(path, field.Alias), variant);
                    continue;
                }

                var items = element.VariantOrNull(variant.Narrow(field.Variations))?.Items.GetValueOrDefault(field.Alias) ?? [];
                var below = field.Variations == ContentVariation.Nothing ? variants : variants.Where(v => v.Culture == variant.Culture).ToList();
                await ValidateItemsAsync(context, field, items, EntityEditor.Join(path, field.Alias), field.Variations == ContentVariation.Nothing ? null : variant, below);
            }
        }
    }

    private static async Task ValidateItemsAsync(Context context, FieldDefinition field, List<EntityItem> items, string fieldPath, VariantId? scope, IReadOnlyList<VariantId> variants)
    {
        var located = new Problem(fieldPath, string.Empty) { Culture = scope?.Culture, Segment = scope?.Segment };
        if (field.Min is { } min && items.Count < min)
        {
            context.Problems.Add(located with { ErrorMessages = [$"Needs at least {min} item(s)."] });
        }

        if (field.Max is { } max && items.Count > max)
        {
            context.Problems.Add(located with { ErrorMessages = [$"Holds at most {max} item(s)."] });
        }

        var allowed = context.ItemTypes.InGroup(field.ItemTypeGroup!).ToDictionary(t => t.Alias);
        foreach (var item in items)
        {
            var itemPath = EntityEditor.Join(fieldPath, item.Key.ToString());
            if (!allowed.TryGetValue(item.Type, out var itemType))
            {
                context.Problems.Add(located with { Path = itemPath, ErrorMessages = [$"Item type '{item.Type}' is not allowed here."] });
                continue;
            }

            // An item exists in the cultures it has values for, or in every culture when it has none yet.
            // This is the CMS's "expose": a block with only English values is not in the Danish page.
            var own = item.VariantIds.Select(v => v.Culture).OfType<string>().ToHashSet();
            var itemVariants = own.Count == 0 ? variants : variants.Where(v => v.Culture is null || own.Contains(v.Culture)).ToList();

            await ValidateElementAsync(context, item, itemType.Shape, itemPath, itemVariants);

            if (itemType.SettingsShape is { } settings)
            {
                await ValidateElementAsync(context, item.Settings ?? new EntityElement(), settings, $"{itemPath}/@settings", itemVariants);
            }

            if (field.ItemLayout is { } layout)
            {
                await ValidateElementAsync(context, item.Layout ?? new EntityElement(), layout, $"{itemPath}/@layout", itemVariants);
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

        // A value with a handler is checked by its handler, which knows its inner model.
        if (field.Handler is { } handler)
        {
            context.Problems.AddRange(handler.Validate(value).Select(p => p with
            {
                Path = EntityEditor.Join(path, p.Path),
                Culture = p.Culture ?? variant.Culture,
                Segment = p.Segment ?? variant.Segment,
            }));
            return;
        }

        if (FieldChecker.Check(field, value, path) is { } problem)
        {
            context.Problems.Add(At(problem.ToString()));
            return;
        }

        foreach (var message in FieldChecker.CheckRules(field, value))
        {
            context.Problems.Add(At(message));
        }

        if (field.Type == FieldType.EntityReference && !FieldChecker.IsBinding(field, value))
        {
            foreach (var (entityType, key) in FieldChecker.EntityReferences(field, value))
            {
                if (!await context.EntityExists(entityType, key))
                {
                    context.Problems.Add(At($"No {entityType} with key {key}, live or in this change set."));
                }
            }
        }

        if (field.Type == FieldType.ItemReference && field.ReferencesItemIn is { } itemsAlias)
        {
            var key = Guid.Parse(value.GetValue<string>());
            if (!(context.Document.Root.Items.GetValueOrDefault(itemsAlias) ?? []).Any(item => item.Key == key))
            {
                context.Problems.Add(At($"No item with key {key} in '{itemsAlias}'."));
            }
        }
    }

    /// <summary>The variants a value is checked in: once if invariant, else each variant the element exists in, narrowed to what it varies by.</summary>
    private static IEnumerable<VariantId> Targets(ContentVariation varies, IReadOnlyList<VariantId> variants)
    {
        if (varies == ContentVariation.Nothing)
        {
            return [VariantId.Invariant];
        }

        return variants
            .Select(v => v.Narrow(varies))
            .Where(v => !varies.VariesByCulture() || v.Culture is not null)
            .Distinct();
    }
}

/// <summary>Writes a shape as JSON Schema, for models and editors. Items fields list their item types.</summary>
public static class SchemaWriter
{
    /// <summary>
    /// The JSON Schema of a shape's fields. An items field lists the item types it holds, offering only those
    /// <paramref name="userKey"/> may use, when given; an item type's own fields are fetched in a second step.
    /// </summary>
    public static JsonObject Fields(ElementShape shape, ItemTypeRegistry? itemTypes = null, Guid? userKey = null)
    {
        var properties = new JsonObject();
        foreach (var field in shape.Fields)
        {
            if (field.IsItems)
            {
                properties[field.Alias] = Items(field, itemTypes, userKey);
                continue;
            }

            var schema = Single(field);
            if (field.Many)
            {
                schema = new JsonObject { ["type"] = "array", ["items"] = schema };
            }

            schema["description"] = field.Description;
            if (field.Handler is { } handler)
            {
                schema["x-edit"] = handler.Usage;
            }

            if (field.AcceptsBindings)
            {
                schema["x-bindings"] = true;
            }

            if (field.Variations != ContentVariation.Nothing)
            {
                schema["x-variations"] = field.Variations.ToString();
            }

            properties[field.Alias] = schema;
        }

        return new JsonObject
        {
            ["type"] = "object",
            ["properties"] = properties,
            ["required"] = new JsonArray(shape.ValueFields.Where(f => f.Required).Select(f => (JsonNode?)f.Alias).ToArray()),
        };
    }

    private static JsonObject Items(FieldDefinition field, ItemTypeRegistry? itemTypes, Guid? userKey)
    {
        var schema = new JsonObject
        {
            ["type"] = "array",
            ["description"] = field.Description,
            ["x-edit"] = "Items: edit with entity.addItem, removeItem and moveItem, using this field's path.",
            ["x-item-types"] = new JsonArray((itemTypes?.InGroup(field.ItemTypeGroup!) ?? [])
                .Where(t => userKey is null || t.IsAvailableTo(userKey.Value))
                .Select(t => (JsonNode?)new JsonObject { ["alias"] = t.Alias, ["group"] = t.Group, ["name"] = t.Name, ["description"] = t.Description })
                .ToArray()),
        };

        if (field.Min is { } min)
        {
            schema["minItems"] = min;
        }

        if (field.Max is { } max)
        {
            schema["maxItems"] = max;
        }

        if (field.Variations != ContentVariation.Nothing)
        {
            schema["x-variations"] = field.Variations.ToString();
        }

        if (field.ItemLayout is { } layout)
        {
            schema["x-item-layout"] = Fields(layout);
        }

        return schema;
    }

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
                schema["x-references-item-in"] = field.ReferencesItemIn;
                break;
        }

        if (field.Allowed is { Count: > 0 } allowed)
        {
            schema["enum"] = new JsonArray(allowed.Select(a => (JsonNode?)a).ToArray());
        }

        return schema;
    }
}
