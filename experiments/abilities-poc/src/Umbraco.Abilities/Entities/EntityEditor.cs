using System.Text.Json;
using System.Text.Json.Nodes;

namespace Umbraco.Abilities.Entities;

/// <summary>
/// Edits an entity document by path. Generalised from Umbraco.AI's block path engine: a path alternates
/// slot aliases and item keys, so <c>"cards"</c> is a slot on the root, <c>"blocks/{key}"</c> is an item and
/// <c>"blocks/{key}/cards"</c> is a slot inside that item. Fields are addressed as an element path plus a
/// field alias, and problems are reported with the same paths (<c>"rewards/{key}/percent"</c>).
/// </summary>
public static class EntityEditor
{
    public sealed record ResolvedElement(JsonObject Element, ElementShape Shape, string Path);

    public sealed record ResolvedSlot(JsonArray Items, SlotDefinition Slot, string Path);

    public static string Join(string path, string segment) => string.IsNullOrEmpty(path) ? segment : $"{path}/{segment}";

    public static ResolvedElement ResolveElement(EntityDocument document, EntityStructure structure, ItemTypeRegistry itemTypes, string? path)
    {
        var segments = Split(path);
        if (segments.Length % 2 != 0)
        {
            throw Invalid(path ?? string.Empty, "An element path alternates slot aliases and item keys, so it has an even number of segments. Use \"\" for the entity itself.");
        }

        var current = new ResolvedElement(document.Root, structure.Shape, string.Empty);
        for (var i = 0; i < segments.Length; i += 2)
        {
            var slot = ResolveSlotOn(current, segments[i]);
            if (!Guid.TryParse(segments[i + 1], out var key))
            {
                throw Invalid(Join(slot.Path, segments[i + 1]), "Expected an item key (a GUID).");
            }

            var item = slot.Items.FirstOrDefault(n => Element.ItemKey(n!) == key) as JsonObject
                ?? throw Invalid(Join(slot.Path, key.ToString()), "No item with this key in the slot.");

            var itemType = itemTypes.Find(Element.ItemType(item))
                ?? throw Invalid(Join(slot.Path, key.ToString()), $"Item type '{Element.ItemType(item)}' is not registered.");

            current = new ResolvedElement(item, itemType.Shape, Join(slot.Path, key.ToString()));
        }

        return current;
    }

    public static ResolvedSlot ResolveSlot(EntityDocument document, EntityStructure structure, ItemTypeRegistry itemTypes, string slotPath)
    {
        var segments = Split(slotPath);
        if (segments.Length % 2 != 1)
        {
            throw Invalid(slotPath, "A slot path ends with a slot alias, e.g. \"rules\" or \"blocks/{key}/cards\".");
        }

        var owner = ResolveElement(document, structure, itemTypes, string.Join('/', segments[..^1]));
        return ResolveSlotOn(owner, segments[^1]);
    }

    public static void SetField(ResolvedElement element, string alias, JsonNode? value)
    {
        var field = element.Shape.Fields.FirstOrDefault(f => f.Alias == alias)
            ?? throw Invalid(Join(element.Path, alias), $"No field '{alias}' here. Fields: {Names(element.Shape.Fields.Select(f => f.Alias))}.");

        var problem = FieldChecker.Check(field, value, Join(element.Path, alias));
        if (problem is not null)
        {
            throw new AbilityInputException(problem.Message, [problem]);
        }

        Element.Fields(element.Element)[alias] = value?.DeepClone();
    }

    public static void SetFields(ResolvedElement element, JsonObject? values)
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
                SetField(element, alias, value);
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

    public static ResolvedElement AddItem(
        EntityDocument document,
        EntityStructure structure,
        ItemTypeRegistry itemTypes,
        string slotPath,
        string itemTypeAlias,
        Guid itemKey,
        int? position)
    {
        var slot = ResolveSlot(document, structure, itemTypes, slotPath);
        var allowed = itemTypes.InGroup(slot.Slot.ItemTypeGroup);
        var itemType = allowed.FirstOrDefault(t => t.Alias == itemTypeAlias)
            ?? throw Invalid(slot.Path, $"Item type '{itemTypeAlias}' is not allowed in this slot. Allowed: {Names(allowed.Select(t => t.Alias))}.");

        if (slot.Slot.Max is { } max && slot.Items.Count >= max)
        {
            throw Invalid(slot.Path, $"This slot holds at most {max} item(s).");
        }

        var item = Element.NewItem(itemKey, itemType.Alias);
        var index = position is { } p ? Math.Clamp(p, 0, slot.Items.Count) : slot.Items.Count;
        slot.Items.Insert(index, item);
        return new ResolvedElement(item, itemType.Shape, Join(slot.Path, itemKey.ToString()));
    }

    public static void RemoveItem(EntityDocument document, EntityStructure structure, ItemTypeRegistry itemTypes, string slotPath, Guid itemKey)
    {
        var slot = ResolveSlot(document, structure, itemTypes, slotPath);
        var item = slot.Items.FirstOrDefault(n => Element.ItemKey(n!) == itemKey)
            ?? throw Invalid(Join(slot.Path, itemKey.ToString()), "No item with this key in the slot.");
        slot.Items.Remove(item);
    }

    public static void MoveItem(EntityDocument document, EntityStructure structure, ItemTypeRegistry itemTypes, string slotPath, Guid itemKey, int position)
    {
        var slot = ResolveSlot(document, structure, itemTypes, slotPath);
        var item = slot.Items.FirstOrDefault(n => Element.ItemKey(n!) == itemKey)
            ?? throw Invalid(Join(slot.Path, itemKey.ToString()), "No item with this key in the slot.");
        slot.Items.Remove(item);
        slot.Items.Insert(Math.Clamp(position, 0, slot.Items.Count), item);
    }

    private static ResolvedSlot ResolveSlotOn(ResolvedElement owner, string alias)
    {
        var slot = owner.Shape.Slots.FirstOrDefault(s => s.Alias == alias)
            ?? throw Invalid(Join(owner.Path, alias), $"No slot '{alias}' here. Slots: {Names(owner.Shape.Slots.Select(s => s.Alias))}.");
        return new ResolvedSlot(Element.Slot(owner.Element, alias), slot, Join(owner.Path, alias));
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
    public static Problem? Check(FieldDefinition field, JsonNode? value, string path)
    {
        if (value is null)
        {
            return null;
        }

        if (value is not JsonValue scalar)
        {
            return new Problem(path, $"Expected a {Describe(field.Type)}, not an object or array.");
        }

        var kind = scalar.GetValueKind();
        string? message = field.Type switch
        {
            FieldType.String when kind != JsonValueKind.String => "Expected a string.",
            FieldType.Number when kind != JsonValueKind.Number => "Expected a number.",
            FieldType.Integer when kind != JsonValueKind.Number || !scalar.TryGetValue<long>(out _) => "Expected a whole number.",
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

    public static string Describe(FieldType type) => type switch
    {
        FieldType.EntityReference or FieldType.ItemReference => "key",
        _ => type.ToString().ToLowerInvariant(),
    };
}
