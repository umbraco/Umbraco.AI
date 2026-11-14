using System.Text.Json.Nodes;

namespace Umbraco.Abilities.Entities;

/// <summary>
/// A difference between two versions of an entity, at a path in the entity edit abilities' form. A field change has
/// the variant it is in; <see cref="Structural"/> marks an item added or removed (<c>"blocks/{key}"</c>) or a slot
/// reordered (<c>"blocks/@order"</c>).
/// </summary>
public sealed record ChangedPath(string Path, string VariantKey, bool Structural)
{
    public VariantId Variant => VariantId.Parse(VariantKey);
}

/// <summary>
/// Compares two versions of any entity document, and copies a path from one to another. This is what lets a change set
/// rebase onto a newer live version: what someone else changed is the difference between the version the set started
/// from and the live one, what the set changed is the difference between that start and the set's result, and a
/// conflict is a path both changed differently.
/// </summary>
public static class EntityDiff
{
    public const string Order = "@order";

    public static IReadOnlyCollection<ChangedPath> Compare(JsonObject before, JsonObject after)
    {
        var changes = new HashSet<ChangedPath>();
        CompareElement(before, after, string.Empty, changes);
        return changes;
    }

    /// <summary>Whether two changes touch the same thing: the same value in the same variant, or one inside an item the other added or removed.</summary>
    public static bool Overlaps(ChangedPath a, ChangedPath b)
    {
        if (a.Path == b.Path)
        {
            return a.VariantKey == b.VariantKey || a.Structural || b.Structural;
        }

        return (a.Structural && Inside(b.Path, a.Path)) || (b.Structural && Inside(a.Path, b.Path));
    }

    /// <summary>The value at a path, for showing a conflict: a field's value, an item, a slot's item keys in order, or null when absent.</summary>
    public static JsonNode? ValueAt(JsonObject root, ChangedPath change)
    {
        var target = Locate(root, change);
        return target switch
        {
            null => null,
            { Order: true } => new JsonArray(target.Slot!.Select(i => (JsonNode?)Element.ItemKey(i!).ToString()).ToArray()),
            { ItemKey: { } key } => target.Slot!.FirstOrDefault(i => Element.ItemKey(i!) == key)?.DeepClone(),
            _ => target.Fields?[target.Field!]?.DeepClone(),
        };
    }

    /// <summary>Makes <paramref name="path"/> in <paramref name="target"/> the same as in <paramref name="source"/>: keeping theirs.</summary>
    public static void CopyPath(JsonObject source, JsonObject target, ChangedPath change)
    {
        var from = Locate(source, change);
        var to = Locate(target, change, create: true);
        if (to is null)
        {
            return;
        }

        if (change.Path.EndsWith($"/{Order}", StringComparison.Ordinal) || change.Path == Order)
        {
            var order = (from?.Slot ?? []).Select(i => Element.ItemKey(i!)).ToList();
            var items = to.Slot!.OfType<JsonObject>().ToList();
            var sorted = items.OrderBy(i => order.IndexOf(Element.ItemKey(i)) is var index and >= 0 ? index : int.MaxValue).ToList();
            to.Slot!.Clear();
            sorted.ForEach(to.Slot.Add);
            return;
        }

        if (to.ItemKey is { } key)
        {
            var existing = to.Slot!.FirstOrDefault(i => Element.ItemKey(i!) == key);
            var theirs = from?.Slot?.Select((item, index) => (item, index)).FirstOrDefault(x => Element.ItemKey(x.item!) == key);
            if (existing is not null)
            {
                to.Slot.Remove(existing);
            }

            if (theirs is { item: { } item, index: var position })
            {
                to.Slot.Insert(Math.Min(position, to.Slot.Count), item.DeepClone());
            }

            return;
        }

        to.Fields![to.Field!] = from?.Fields?[to.Field!]?.DeepClone();
    }

    private static bool Inside(string path, string container) => path.StartsWith(container + "/", StringComparison.Ordinal);

    private static void CompareElement(JsonObject? a, JsonObject? b, string path, HashSet<ChangedPath> changes)
    {
        CompareFields(a?["fields"] as JsonObject, b?["fields"] as JsonObject, path, string.Empty, changes);
        CompareSlots(a?["slots"] as JsonObject, b?["slots"] as JsonObject, path, string.Empty, changes);

        var variantKeys = Keys(a?["variants"] as JsonObject).Union(Keys(b?["variants"] as JsonObject));
        foreach (var key in variantKeys)
        {
            var va = a?["variants"]?[key] as JsonObject;
            var vb = b?["variants"]?[key] as JsonObject;
            CompareFields(va?["fields"] as JsonObject, vb?["fields"] as JsonObject, path, key, changes);
            CompareSlots(va?["slots"] as JsonObject, vb?["slots"] as JsonObject, path, key, changes);
        }

        foreach (var sub in new[] { "settings", "layout" })
        {
            if (a?[sub] is not null || b?[sub] is not null)
            {
                CompareElement(a?[sub] as JsonObject, b?[sub] as JsonObject, EntityEditor.Join(path, $"@{sub}"), changes);
            }
        }
    }

    private static void CompareFields(JsonObject? a, JsonObject? b, string path, string variantKey, HashSet<ChangedPath> changes)
    {
        foreach (var alias in Keys(a).Union(Keys(b)))
        {
            if (!JsonNode.DeepEquals(a?[alias], b?[alias]))
            {
                changes.Add(new ChangedPath(EntityEditor.Join(path, alias), variantKey, Structural: false));
            }
        }
    }

    private static void CompareSlots(JsonObject? a, JsonObject? b, string path, string variantKey, HashSet<ChangedPath> changes)
    {
        foreach (var alias in Keys(a).Union(Keys(b)))
        {
            var slotPath = EntityEditor.Join(path, alias);
            var itemsA = (a?[alias] as JsonArray ?? []).OfType<JsonObject>().ToDictionary(Element.ItemKey);
            var itemsB = (b?[alias] as JsonArray ?? []).OfType<JsonObject>().ToDictionary(Element.ItemKey);

            foreach (var key in itemsA.Keys.Union(itemsB.Keys))
            {
                var itemPath = EntityEditor.Join(slotPath, key.ToString());
                if (itemsA.TryGetValue(key, out var itemA) && itemsB.TryGetValue(key, out var itemB))
                {
                    CompareElement(itemA, itemB, itemPath, changes);
                }
                else
                {
                    changes.Add(new ChangedPath(itemPath, variantKey, Structural: true));
                }
            }

            var commonA = itemsA.Keys.Where(itemsB.ContainsKey).ToList();
            var commonB = itemsB.Keys.Where(itemsA.ContainsKey).ToList();
            if (!commonA.SequenceEqual(commonB))
            {
                changes.Add(new ChangedPath(EntityEditor.Join(slotPath, Order), variantKey, Structural: true));
            }
        }
    }

    private static IEnumerable<string> Keys(JsonObject? node) => node?.Select(kv => kv.Key) ?? [];

    private sealed record Location(JsonObject? Fields, string? Field, JsonArray? Slot, Guid? ItemKey, bool Order);

    /// <summary>Finds where a path points, without the structure: the fields and field, or the slot and item key.</summary>
    private static Location? Locate(JsonObject root, ChangedPath change, bool create = false)
    {
        var segments = change.Path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        JsonObject? element = root;
        var i = 0;

        // Walk down through items and @settings / @layout until only the last part is left.
        while (element is not null && i < segments.Length - 1)
        {
            var segment = segments[i];
            if (segment is "@settings" or "@layout")
            {
                element = create ? Element.SubElement(element, segment[1..]) : element[segment[1..]] as JsonObject;
                i++;
                continue;
            }

            var slot = SlotOf(element, segment, change.VariantKey, create);
            if (i + 1 == segments.Length - 1)
            {
                // "slot/{key}" or "slot/@order": the last part is about the slot itself.
                var last = segments[^1];
                return slot is null ? null
                    : last == Order ? new Location(null, null, slot, null, Order: true)
                    : Guid.TryParse(last, out var key) ? new Location(null, null, slot, key, Order: false)
                    : null;
            }

            element = Guid.TryParse(segments[i + 1], out var itemKey)
                ? slot?.FirstOrDefault(n => Element.ItemKey(n!) == itemKey) as JsonObject
                : null;
            i += 2;
        }

        if (element is null)
        {
            return null;
        }

        var fields = change.VariantKey.Length == 0
            ? element["fields"] as JsonObject ?? (create ? Element.Fields(element) : null)
            : create ? Element.Fields(Element.VariantPart(element, change.Variant)) : Element.VariantPartOrNull(element, change.Variant)?["fields"] as JsonObject;
        return new Location(fields, segments[^1], null, null, Order: false);
    }

    private static JsonArray? SlotOf(JsonObject element, string alias, string variantKey, bool create)
    {
        if (element["slots"]?[alias] is JsonArray invariant)
        {
            return invariant;
        }

        if (variantKey.Length > 0)
        {
            var part = create ? Element.VariantPart(element, VariantId.Parse(variantKey)) : Element.VariantPartOrNull(element, VariantId.Parse(variantKey));
            return part is null ? null : create ? Element.Slot(part, alias) : part["slots"]?[alias] as JsonArray;
        }

        return create ? Element.Slot(element, alias) : null;
    }
}
