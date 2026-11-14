using System.Text.Json;
using System.Text.Json.Nodes;

using Umbraco.Cms.Core.Models;
using Umbraco.Extensions;
namespace Umbraco.Abilities.Entities;

/// <summary>What an edit works on: the document, its structure, the item types, and the variant being edited.</summary>
public sealed record EditTarget(EntityDocument Document, EntityStructure Structure, ItemTypeRegistry ItemTypes, VariantId Variant);

/// <summary>
/// Edits an entity document by path. Generalised from Umbraco.AI's block path engine: a path alternates
/// items field aliases and item keys, so <c>"blocks"</c> is an items field on the root, <c>"blocks/{key}"</c> is an
/// item and <c>"blocks/{key}/cards"</c> is an items field inside that item. <c>"{itemPath}/@settings"</c> and
/// <c>"{itemPath}/@layout"</c> are an item's settings element and its placement values. Fields are addressed
/// as an element path plus a field alias, and problems use the same paths (<c>"blocks/{key}/@settings/theme"</c>).
/// Values that vary are read and written for the edit's variant, narrowed to what each value varies by.
/// </summary>
public static class EntityEditor
{
    public sealed record ResolvedElement(EntityElement Element, ElementShape Shape, string Path, ItemTypeDefinition? ItemType, FieldDefinition? InField);

    public sealed record ResolvedItems(List<EntityItem> Items, FieldDefinition Field, string Path, VariantId Variant);

    public static string Join(string path, string segment) => string.IsNullOrEmpty(path) ? segment : $"{path}/{segment}";

    public static ResolvedElement ResolveElement(EditTarget target, string? path)
    {
        var segments = Split(path);
        var current = new ResolvedElement(target.Document.Root, target.Structure.Shape, string.Empty, null, null);
        var i = 0;
        while (i < segments.Length)
        {
            var segment = segments[i];
            if (segment.StartsWith('@'))
            {
                current = ResolveSubElement(current, segment, i == segments.Length - 1);
                i++;
                continue;
            }

            if (i + 1 >= segments.Length)
            {
                throw Invalid(path ?? string.Empty, "An element path ends with an item key, @settings or @layout. Use \"\" for the entity itself.");
            }

            var items = ResolveItemsOn(target, current, segment);
            if (!Guid.TryParse(segments[i + 1], out var key))
            {
                throw Invalid(Join(items.Path, segments[i + 1]), "Expected an item key (a GUID).");
            }

            var item = items.Items.FirstOrDefault(i => i.Key == key)
                ?? throw Invalid(Join(items.Path, key.ToString()), "No item with this key in the field.");

            var itemType = target.ItemTypes.Find(items.Field.ItemTypeGroup!, item.Type)
                ?? throw Invalid(Join(items.Path, key.ToString()), $"Item type '{item.Type}' is not registered.");

            current = new ResolvedElement(item, itemType.Shape, Join(items.Path, key.ToString()), itemType, items.Field);
            i += 2;
        }

        return current;
    }

    public static ResolvedItems ResolveItems(EditTarget target, string fieldPath)
    {
        var segments = Split(fieldPath);
        if (segments.Length == 0 || segments[^1].StartsWith('@'))
        {
            throw Invalid(fieldPath, "A field path ends with an items field's alias, e.g. \"rules\" or \"blocks/{key}/cards\".");
        }

        var owner = ResolveElement(target, string.Join('/', segments[..^1]));
        return ResolveItemsOn(target, owner, segments[^1]);
    }

    public static void SetField(EditTarget target, ResolvedElement element, string alias, JsonNode? value)
    {
        var path = Join(element.Path, alias);
        var field = element.Shape.Fields.FirstOrDefault(f => f.Alias == alias)
            ?? throw Invalid(path, $"No field '{alias}' here. Fields: {Names(element.Shape.Fields.Select(f => f.Alias))}.");

        if (field.IsItems)
        {
            throw Invalid(path, $"'{alias}' holds items. Use entity.addItem, removeItem or moveItem with fieldPath \"{path}\".");
        }

        var variant = RequireVariant(target, field.Variations, path);
        value = FieldChecker.Normalize(field, value);
        if (FieldChecker.Check(field, value, path) is { } problem)
        {
            throw new AbilityInputException(problem.ToString(), [problem with { Culture = variant.Culture, Segment = variant.Segment }]);
        }

        element.Element.Set(field, variant, value);
    }

    public static void SetFields(EditTarget target, ResolvedElement element, JsonObject? values)
    {
        if (values is null)
        {
            return;
        }

        var problems = new List<Problem>();
        foreach (var (alias, value) in values)
        {
            try
            {
                SetField(target, element, alias, value);
            }
            catch (AbilityInputException ex)
            {
                problems.AddRange(ex.Problems);
            }
        }

        if (problems.Count > 0)
        {
            throw new AbilityInputException("Some fields could not be set.", problems);
        }
    }

    public static ResolvedElement AddItem(EditTarget target, string fieldPath, string itemTypeAlias, Guid itemKey, int? position)
    {
        var items = ResolveItems(target, fieldPath);
        var allowed = target.ItemTypes.InGroup(items.Field.ItemTypeGroup!);
        var itemType = allowed.FirstOrDefault(t => t.Alias == itemTypeAlias)
            ?? throw Invalid(items.Path, $"Item type '{itemTypeAlias}' is not allowed in this field. Allowed: {Names(allowed.Select(t => t.Alias))}.");

        if (items.Field.Max is { } max && items.Items.Count >= max)
        {
            throw Invalid(items.Path, $"This field holds at most {max} item(s).");
        }

        var item = new EntityItem { Key = itemKey, Type = itemType.Alias };
        var index = position is { } p ? Math.Clamp(p, 0, items.Items.Count) : items.Items.Count;
        items.Items.Insert(index, item);
        return new ResolvedElement(item, itemType.Shape, Join(items.Path, itemKey.ToString()), itemType, items.Field);
    }

    public static void RemoveItem(EditTarget target, string fieldPath, Guid itemKey)
    {
        var items = ResolveItems(target, fieldPath);
        var item = items.Items.FirstOrDefault(i => i.Key == itemKey)
            ?? throw Invalid(Join(items.Path, itemKey.ToString()), "No item with this key in the field.");
        items.Items.Remove(item);
    }

    public static void MoveItem(EditTarget target, string fieldPath, Guid itemKey, int position)
    {
        var items = ResolveItems(target, fieldPath);
        var item = items.Items.FirstOrDefault(i => i.Key == itemKey)
            ?? throw Invalid(Join(items.Path, itemKey.ToString()), "No item with this key in the field.");
        items.Items.Remove(item);
        items.Items.Insert(Math.Clamp(position, 0, items.Items.Count), item);
    }

    /// <summary>
    /// The variant to use for a value that varies as given. Invariant values ignore the edit's variant; a value that
    /// varies by culture needs a known culture, or the entity's only culture when it has just one.
    /// </summary>
    public static VariantId RequireVariant(EditTarget target, ContentVariation varies, string path)
    {
        var variant = target.Variant.Narrow(varies);
        if (!varies.VariesByCulture())
        {
            return variant;
        }

        var cultures = target.Structure.Cultures;
        if (variant.Culture is null)
        {
            if (cultures.Count == 1)
            {
                return variant with { Culture = cultures[0] };
            }

            throw Invalid(path, $"This varies by culture. Give a culture: {Names(cultures)}.");
        }

        if (!cultures.Contains(variant.Culture))
        {
            throw Invalid(path, $"Unknown culture '{variant.Culture}'. Cultures: {Names(cultures)}.");
        }

        return variant;
    }

    private static ResolvedElement ResolveSubElement(ResolvedElement current, string segment, bool isLast)
    {
        var path = Join(current.Path, segment);
        if (current.ItemType is null || current.InField is null)
        {
            throw Invalid(path, "@settings and @layout follow an item key.");
        }

        if (!isLast)
        {
            throw Invalid(path, "Nothing can follow @settings or @layout.");
        }

        return segment switch
        {
            "@settings" when current.ItemType.SettingsShape is { } settings
                => new ResolvedElement(((EntityItem)current.Element).EnsureSettings(), settings, path, null, null),
            "@settings" => throw Invalid(path, $"Item type '{current.ItemType.Alias}' has no settings."),
            "@layout" when current.InField.ItemLayout is { } layout
                => new ResolvedElement(((EntityItem)current.Element).EnsureLayout(), layout, path, null, null),
            "@layout" => throw Invalid(path, $"Field '{current.InField.Alias}' has no layout values."),
            _ => throw Invalid(path, "Use @settings or @layout."),
        };
    }

    private static ResolvedItems ResolveItemsOn(EditTarget target, ResolvedElement owner, string alias)
    {
        var path = Join(owner.Path, alias);
        var field = owner.Shape.ItemFields.FirstOrDefault(f => f.Alias == alias)
            ?? throw Invalid(path, $"No items field '{alias}' here. Items fields: {Names(owner.Shape.ItemFields.Select(f => f.Alias))}.");

        var variant = RequireVariant(target, field.Variations, path);
        return new ResolvedItems(owner.Element.ItemsFor(field, variant), field, path, variant);
    }

    private static string[] Split(string? path) => (path ?? string.Empty).Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static string Names(IEnumerable<string> names)
    {
        var list = names.ToList();
        return list.Count == 0 ? "none" : string.Join(", ", list);
    }

    private static AbilityInputException Invalid(string path, string message) => new(message, [new Problem(path, message)]);
}

/// <summary>Checks one field value against its definition.</summary>
public static class FieldChecker
{
    /// <summary>A binding to an earlier step's output (<c>{{trigger.orderId}}</c>), where the field accepts one. Resolved when the workflow runs.</summary>
    public static bool IsBinding(FieldDefinition field, JsonNode? value)
        => field.AcceptsBindings && value is JsonValue scalar && scalar.TryGetValue<string>(out var text) && text.Contains("{{", StringComparison.Ordinal);

    /// <summary>The field's own rules (a range, a pattern), on a scalar value. Bindings are checked when they resolve.</summary>
    public static IEnumerable<string> CheckRules(FieldDefinition field, JsonNode? value)
    {
        if (field.Rules is not { Count: > 0 } rules || value is not JsonValue scalar || IsBinding(field, value))
        {
            yield break;
        }

        object? clr = scalar.GetValueKind() switch
        {
            JsonValueKind.Number => scalar.ToJsonString().Contains('.') ? double.Parse(scalar.ToJsonString(), System.Globalization.CultureInfo.InvariantCulture) : long.Parse(scalar.ToJsonString(), System.Globalization.CultureInfo.InvariantCulture),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => scalar.ToString(),
        };

        foreach (var rule in rules)
        {
            if (rule.GetValidationResult(clr, new System.ComponentModel.DataAnnotations.ValidationContext(clr ?? new object()) { DisplayName = field.Alias }) is { } result
                && result != System.ComponentModel.DataAnnotations.ValidationResult.Success)
            {
                yield return result.ErrorMessage ?? "Invalid.";
            }
        }
    }

    /// <summary>A reference given as a UDI (<c>umb://document/…</c>) becomes the field's own form: a key, or <c>{ entityType, key }</c>.</summary>
    public static JsonNode? Normalize(FieldDefinition field, JsonNode? value)
    {
        if (value is null || field.Type != FieldType.EntityReference)
        {
            return value;
        }

        if (field.Many)
        {
            return value is JsonArray array ? new JsonArray(array.Select(v => Normalize(field with { Many = false }, v?.DeepClone())).ToArray()) : value;
        }

        if (value is not JsonValue scalar || !scalar.TryGetValue<string>(out var text) || !EntityUdis.TryParse(text, out var udi))
        {
            return value;
        }

        if (field.ReferencesAnyOf is { Count: > 0 })
        {
            return new JsonObject { ["entityType"] = udi.EntityType, ["key"] = udi.Guid.ToString() };
        }

        // A UDI of another type stays as it is, so the check says which type was expected.
        return udi.EntityType == field.ReferencesEntityType ? JsonValue.Create(udi.Guid.ToString()) : value;
    }

    public static Problem? Check(FieldDefinition field, JsonNode? value, string path)
    {
        if (value is null || IsBinding(field, value))
        {
            return null;
        }

        if (field.Many)
        {
            if (value is not JsonArray array)
            {
                return new Problem(path, $"Expected an array of {Describe(field)}s.");
            }

            var single = field with { Many = false };
            return array.Select(v => Check(single, v, path)).FirstOrDefault(p => p is not null);
        }

        if (field.Type == FieldType.Json)
        {
            return null; // Checked against the editor's own schema by the entity definition.
        }

        if (field.Type == FieldType.EntityReference && field.ReferencesAnyOf is { Count: > 0 } types)
        {
            return value is JsonObject reference
                && reference["entityType"]?.GetValueKind() == JsonValueKind.String
                && types.Contains(reference["entityType"]!.GetValue<string>())
                && Guid.TryParse(reference["key"]?.ToString(), out _)
                    ? null
                    : new Problem(path, $"Expected {{ \"entityType\": one of {string.Join(", ", types)}, \"key\": a GUID }}.");
        }

        if (field.Type == FieldType.EntityReference && value is JsonValue udiValue && udiValue.TryGetValue<string>(out var text) && EntityUdis.TryParse(text, out var udi))
        {
            return new Problem(path, $"Expected a {field.ReferencesEntityType}, not a {udi.EntityType}.");
        }

        if (value is not JsonValue scalar)
        {
            return new Problem(path, $"Expected a {Describe(field)}, not an object or array.");
        }

        var kind = scalar.GetValueKind();
        string? message = field.Type switch
        {
            FieldType.String when kind != JsonValueKind.String => "Expected a string.",
            FieldType.Number when kind != JsonValueKind.Number => "Expected a number.",
            // Read from the JSON text: a value holding a CLR int, rather than parsed JSON, has no TryGetValue<long>.
            FieldType.Integer when kind != JsonValueKind.Number || !long.TryParse(scalar.ToJsonString(), out _) => "Expected a whole number.",
            FieldType.Boolean when kind is not (JsonValueKind.True or JsonValueKind.False) => "Expected true or false.",
            FieldType.Date when kind != JsonValueKind.String || !DateOnly.TryParseExact(scalar.GetValue<string>(), "yyyy-MM-dd", out _) => "Expected a date as yyyy-MM-dd.",
            FieldType.EntityReference or FieldType.ItemReference when kind != JsonValueKind.String || !Guid.TryParse(scalar.GetValue<string>(), out _) => "Expected a key (a GUID).",
            _ => null,
        };

        if (message is null && field.Allowed is { Count: > 0 } allowed && kind == JsonValueKind.String && !allowed.Contains(scalar.GetValue<string>()))
        {
            message = $"Must be one of: {string.Join(", ", allowed)}.";
        }

        return message is null ? null : new Problem(path, message);
    }

    /// <summary>The entities a valid reference value points at.</summary>
    public static IEnumerable<(string EntityType, Guid Key)> EntityReferences(FieldDefinition field, JsonNode value)
    {
        var values = field.Many && value is JsonArray array ? array.Where(v => v is not null).Select(v => v!) : [value];
        foreach (var single in values)
        {
            if (single is JsonObject reference)
            {
                yield return (reference["entityType"]!.GetValue<string>(), Guid.Parse(reference["key"]!.ToString()));
            }
            else if (field.ReferencesEntityType is { } type)
            {
                yield return (type, Guid.Parse(single.GetValue<string>()));
            }
        }
    }

    public static string Describe(FieldDefinition field) => field.Type switch
    {
        FieldType.EntityReference or FieldType.ItemReference => "key",
        _ => field.Type.ToString().ToLowerInvariant(),
    };
}
