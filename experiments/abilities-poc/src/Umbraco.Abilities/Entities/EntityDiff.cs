using System.Text.Json.Nodes;

namespace Umbraco.Abilities.Entities;

/// <summary>
/// A difference between two versions of an entity, at a path in the entity edit abilities' form. A value change has
/// the variant it is in; <see cref="Structural"/> marks an item added or removed (<c>"blocks/{key}"</c>) or a list
/// reordered (<c>"blocks/@order"</c>).
/// </summary>
public sealed record ChangedPath(string Path, string VariantKey, bool Structural)
{
    public VariantId Variant => VariantId.Parse(VariantKey);
}

/// <summary>
/// Compares two versions of an entity, and copies a path from one to another. This is what lets a change set rebase onto
/// a newer live version: what someone else changed is the difference between the version the set started from and the
/// live one, what the set changed is the difference between that start and the set's result, and a conflict is a path
/// both changed differently. It walks the structure with the values, so items are matched by key and objects by field.
/// </summary>
public static class EntityDiff
{
    public const string Order = "@order";

    public static IReadOnlyCollection<ChangedPath> Compare(JsonObject before, JsonObject after, ElementShape shape, ItemTypeRegistry itemTypes)
    {
        var changes = new HashSet<ChangedPath>();
        CompareObject(before, after, shape, itemTypes, string.Empty, VariantId.Invariant, changes);
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

    /// <summary>The value at a path, for showing a conflict: a value, an item, a list's item keys in order, or null when absent.</summary>
    public static JsonNode? ValueAt(JsonObject root, ElementShape shape, ItemTypeRegistry itemTypes, ChangedPath change)
        => Find(root, shape, itemTypes, change, create: false) switch
        {
            null => null,
            { Order: true } at => new JsonArray(((at.Value as JsonArray) ?? []).OfType<JsonObject>().Select(i => (JsonNode?)EntityValues.KeyOf(i).ToString()).ToArray()),
            { ItemKey: { } key } at => EntityValues.FindItem(at.Value as JsonArray, key)?.DeepClone(),
            var at => at.Value?.DeepClone(),
        };

    /// <summary>Makes <paramref name="change"/>'s path in <paramref name="target"/> the same as in <paramref name="source"/>: keeping theirs.</summary>
    public static void CopyPath(JsonObject source, JsonObject target, ElementShape shape, ItemTypeRegistry itemTypes, ChangedPath change)
    {
        var from = Find(source, shape, itemTypes, change, create: false);
        if (Find(target, shape, itemTypes, change, create: true) is not { } to)
        {
            return;
        }

        var fromItems = from?.Value as JsonArray;
        if (change.Path.EndsWith("/" + Order, StringComparison.Ordinal))
        {
            var order = (fromItems ?? []).OfType<JsonObject>().Select(EntityValues.KeyOf).ToList();
            var items = EntityValues.Items(to.Owner, to.Field, to.Variant, create: true)!;
            var sorted = items.OfType<JsonObject>().OrderBy(i => order.IndexOf(EntityValues.KeyOf(i)) is var index and >= 0 ? index : int.MaxValue).ToList();
            items.Clear();
            sorted.ForEach(i => items.Add(i));
            return;
        }

        if (to.ItemKey is { } key || (change.Structural && Guid.TryParse(change.Path.Split('/')[^1], out key)))
        {
            var items = EntityValues.Items(to.Owner, to.Field, to.Variant, create: true)!;
            if (EntityValues.FindItem(items, key) is { } existing)
            {
                items.Remove(existing);
            }

            var theirs = fromItems?.OfType<JsonObject>().ToList() ?? [];
            var position = theirs.FindIndex(i => EntityValues.KeyOf(i) == key);
            if (position >= 0)
            {
                items.Insert(Math.Min(position, items.Count), theirs[position].DeepClone());
            }

            return;
        }

        EntityValues.Set(to.Owner, to.Field, to.Variant, from?.Value?.DeepClone());
    }

    private static bool Inside(string path, string container) => path.StartsWith(container + "/", StringComparison.Ordinal);

    private static void CompareObject(JsonObject? a, JsonObject? b, ElementShape shape, ItemTypeRegistry itemTypes, string path, VariantId context, HashSet<ChangedPath> changes)
    {
        foreach (var field in shape.Fields)
        {
            var fieldPath = EntityValues.Join(path, field.Alias);
            var variants = Variants(a, field).Union(Variants(b, field));
            foreach (var own in variants)
            {
                var variant = own.Over(context);
                var valueA = a is null ? null : EntityValues.Get(a, field, own);
                var valueB = b is null ? null : EntityValues.Get(b, field, own);
                switch (field.Type)
                {
                    case FieldType.Items:
                        CompareItems(valueA as JsonArray, valueB as JsonArray, field, itemTypes, fieldPath, variant, changes);
                        break;
                    case FieldType.Object when field.Shape is { } objectShape && (valueA is JsonObject || valueB is JsonObject):
                        CompareObject(valueA as JsonObject, valueB as JsonObject, objectShape, itemTypes, fieldPath, variant, changes);
                        break;
                    default:
                        if (!JsonNode.DeepEquals(valueA, valueB))
                        {
                            changes.Add(new ChangedPath(fieldPath, variant.Key, Structural: false));
                        }

                        break;
                }
            }
        }
    }

    private static void CompareItems(JsonArray? a, JsonArray? b, FieldDefinition field, ItemTypeRegistry itemTypes, string fieldPath, VariantId variant, HashSet<ChangedPath> changes)
    {
        var itemsA = (a ?? []).OfType<JsonObject>().ToDictionary(EntityValues.KeyOf);
        var itemsB = (b ?? []).OfType<JsonObject>().ToDictionary(EntityValues.KeyOf);
        foreach (var key in itemsA.Keys.Union(itemsB.Keys))
        {
            var itemPath = EntityValues.Join(fieldPath, key.ToString());
            if (itemsA.TryGetValue(key, out var itemA) && itemsB.TryGetValue(key, out var itemB)
                && EntityValues.TypeOf(itemA) == EntityValues.TypeOf(itemB)
                && itemTypes.Find(field.ItemTypeGroup!, EntityValues.TypeOf(itemA)) is { } itemType)
            {
                CompareObject(itemA, itemB, itemType.Shape, itemTypes, itemPath, variant, changes);
            }
            else if (!JsonNode.DeepEquals(itemsA.GetValueOrDefault(key), itemsB.GetValueOrDefault(key)))
            {
                changes.Add(new ChangedPath(itemPath, variant.Key, Structural: true));
            }
        }

        if (!itemsA.Keys.Where(itemsB.ContainsKey).SequenceEqual(itemsB.Keys.Where(itemsA.ContainsKey)))
        {
            changes.Add(new ChangedPath(EntityValues.Join(fieldPath, Order), variant.Key, Structural: true));
        }
    }

    private static IEnumerable<VariantId> Variants(JsonObject? owner, FieldDefinition field)
        => owner is null ? [] : EntityValues.Values(owner, field).Select(v => v.Variant);

    // Where a change is, in a document: its own variant for each varying value on the way.
    private static PathTarget? Find(JsonObject root, ElementShape shape, ItemTypeRegistry itemTypes, ChangedPath change, bool create)
    {
        var path = change.Structural && !change.Path.EndsWith("/" + Order, StringComparison.Ordinal) && create
            ? change.Path[..change.Path.LastIndexOf('/')] // the items field, to put their item back into
            : change.Path;
        try
        {
            return EntityValues.Find(root, shape, itemTypes, path, (field, _) => change.Variant.Narrow(field.VariesBy), create);
        }
        catch (AbilityInputException)
        {
            return null; // Not there in this version.
        }
    }
}
