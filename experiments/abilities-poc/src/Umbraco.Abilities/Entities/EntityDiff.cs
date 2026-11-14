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

    public static IReadOnlyCollection<ChangedPath> Compare(EntityElement before, EntityElement after)
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
    public static JsonNode? ValueAt(EntityElement root, ChangedPath change)
        => Locate(root, change, create: false) switch
        {
            null => null,
            { Order: true } at => new JsonArray(at.Slot!.Select(i => (JsonNode?)i.Key.ToString()).ToArray()),
            { ItemKey: { } key } at => at.Slot!.FirstOrDefault(i => i.Key == key) is { } item ? EntityJson.ToJson(item) : null,
            var at => at.Values?.Fields.GetValueOrDefault(at.Field!)?.DeepClone(),
        };

    /// <summary>Makes <paramref name="change"/>'s path in <paramref name="target"/> the same as in <paramref name="source"/>: keeping theirs.</summary>
    public static void CopyPath(EntityElement source, EntityElement target, ChangedPath change)
    {
        var from = Locate(source, change, create: false);
        var to = Locate(target, change, create: true);
        if (to is null)
        {
            return;
        }

        if (to.Order)
        {
            var order = (from?.Slot ?? []).Select(i => i.Key).ToList();
            var sorted = to.Slot!.OrderBy(i => order.IndexOf(i.Key) is var index and >= 0 ? index : int.MaxValue).ToList();
            to.Slot!.Clear();
            to.Slot.AddRange(sorted);
            return;
        }

        if (to.ItemKey is { } key)
        {
            to.Slot!.RemoveAll(i => i.Key == key);
            var position = from?.Slot?.FindIndex(i => i.Key == key) ?? -1;
            if (position >= 0)
            {
                to.Slot.Insert(Math.Min(position, to.Slot.Count), EntityJson.Clone(from!.Slot![position]));
            }

            return;
        }

        to.Values!.Fields[to.Field!] = from?.Values?.Fields.GetValueOrDefault(to.Field!)?.DeepClone();
    }

    private static bool Inside(string path, string container) => path.StartsWith(container + "/", StringComparison.Ordinal);

    private static void CompareElement(EntityElement? a, EntityElement? b, string path, HashSet<ChangedPath> changes)
    {
        CompareValues(a, b, path, string.Empty, changes);

        var variantKeys = Keys(a?.Variants).Union(Keys(b?.Variants));
        foreach (var key in variantKeys)
        {
            CompareValues(a?.Variants.GetValueOrDefault(key), b?.Variants.GetValueOrDefault(key), path, key, changes);
        }

        if (a is EntityItem || b is EntityItem)
        {
            var itemA = a as EntityItem;
            var itemB = b as EntityItem;
            if (itemA?.Settings is not null || itemB?.Settings is not null)
            {
                CompareElement(itemA?.Settings, itemB?.Settings, EntityEditor.Join(path, "@settings"), changes);
            }

            if (itemA?.Layout is not null || itemB?.Layout is not null)
            {
                CompareElement(itemA?.Layout, itemB?.Layout, EntityEditor.Join(path, "@layout"), changes);
            }
        }
    }

    private static void CompareValues(ElementValues? a, ElementValues? b, string path, string variantKey, HashSet<ChangedPath> changes)
    {
        foreach (var alias in Keys(a?.Fields).Union(Keys(b?.Fields)))
        {
            if (!JsonNode.DeepEquals(a?.Fields.GetValueOrDefault(alias), b?.Fields.GetValueOrDefault(alias)))
            {
                changes.Add(new ChangedPath(EntityEditor.Join(path, alias), variantKey, Structural: false));
            }
        }

        foreach (var alias in Keys(a?.Slots).Union(Keys(b?.Slots)))
        {
            var slotPath = EntityEditor.Join(path, alias);
            var itemsA = (a?.Slots.GetValueOrDefault(alias) ?? []).ToDictionary(i => i.Key);
            var itemsB = (b?.Slots.GetValueOrDefault(alias) ?? []).ToDictionary(i => i.Key);

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

            if (!itemsA.Keys.Where(itemsB.ContainsKey).SequenceEqual(itemsB.Keys.Where(itemsA.ContainsKey)))
            {
                changes.Add(new ChangedPath(EntityEditor.Join(slotPath, Order), variantKey, Structural: true));
            }
        }
    }

    private static IEnumerable<string> Keys<T>(Dictionary<string, T>? dictionary) => dictionary?.Keys ?? Enumerable.Empty<string>();

    private sealed record Location(ElementValues? Values, string? Field, List<EntityItem>? Slot, Guid? ItemKey, bool Order);

    /// <summary>Finds where a path points: the values and field, or the slot and item key, or a slot's order.</summary>
    private static Location? Locate(EntityElement root, ChangedPath change, bool create)
    {
        var segments = change.Path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        EntityElement? element = root;
        var i = 0;

        while (element is not null && i < segments.Length - 1)
        {
            var segment = segments[i];
            if (segment is "@settings" or "@layout" && element is EntityItem item)
            {
                element = segment == "@settings"
                    ? create ? item.EnsureSettings() : item.Settings
                    : create ? item.EnsureLayout() : item.Layout;
                i++;
                continue;
            }

            var slot = SlotOf(element, segment, change.Variant, create);
            if (i + 1 == segments.Length - 1)
            {
                var last = segments[^1];
                return slot is null ? null
                    : last == Order ? new Location(null, null, slot, null, Order: true)
                    : Guid.TryParse(last, out var key) ? new Location(null, null, slot, key, Order: false)
                    : null;
            }

            element = Guid.TryParse(segments[i + 1], out var itemKey) ? slot?.FirstOrDefault(n => n.Key == itemKey) : null;
            i += 2;
        }

        if (element is null)
        {
            return null;
        }

        var values = create ? element.Variant(change.Variant) : element.VariantOrNull(change.Variant);
        return new Location(values, segments[^1], null, null, Order: false);
    }

    /// <summary>A slot, invariant if the element has it so, else in the change's variant.</summary>
    private static List<EntityItem>? SlotOf(EntityElement element, string alias, VariantId variant, bool create)
    {
        if (element.Slots.TryGetValue(alias, out var invariant))
        {
            return invariant;
        }

        var values = create ? element.Variant(variant) : element.VariantOrNull(variant);
        return create ? values!.Slot(alias) : values?.Slots.GetValueOrDefault(alias);
    }
}
