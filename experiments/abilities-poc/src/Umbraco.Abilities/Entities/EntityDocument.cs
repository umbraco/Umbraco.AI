using System.Text.Json.Nodes;

namespace Umbraco.Abilities.Entities;

/// <summary>
/// A working copy of an entity: its identity and its fields' values, as one JSON tree. A value can be anything its field
/// describes: a scalar, an object with fields of its own, or an array of items (<c>{ "key", "type", …fields }</c>). A
/// value that varies is stored per variant where the field is: <c>{ "en-US": "Summer", "da-DK": "Sommer" }</c>. The
/// structure says which fields vary, so nothing is guessed from the data.
/// </summary>
public sealed class EntityDocument
{
    public required string EntityType { get; init; }

    public required Guid Key { get; init; }

    /// <summary>The subtype, e.g. a document's content type alias. Null for entity types without subtypes.</summary>
    public string? Subtype { get; init; }

    /// <summary>The stored version this copy was loaded from; null when the entity is new.</summary>
    public long? LiveVersion { get; init; }

    /// <summary>The fields' values, by alias.</summary>
    public JsonObject Root { get; set; } = [];

    public EntityDocument Clone() => new()
    {
        EntityType = EntityType,
        Key = Key,
        Subtype = Subtype,
        LiveVersion = LiveVersion,
        Root = (JsonObject)Root.DeepClone(),
    };

    /// <summary>The form tools show.</summary>
    public JsonObject ToJson() => new()
    {
        ["entityType"] = EntityType,
        ["key"] = Key.ToString(),
        ["subtype"] = Subtype,
        ["version"] = LiveVersion,
        ["fields"] = Root.DeepClone(),
    };
}

/// <summary>A value in an entity, found by walking the tree: where it is, its field, the variant it is in, and the object holding it.</summary>
public sealed record EntityValue(string Path, FieldDefinition Field, VariantId Variant, JsonNode Value, JsonObject Owner);

/// <summary>
/// Reads and writes values in the tree through their field definitions, which say what each value varies by, so
/// callers never handle the per-variant form themselves.
/// </summary>
public static class EntityValues
{
    public const string KeyProperty = "key";
    public const string TypeProperty = "type";

    public static string Join(string path, string segment) => string.IsNullOrEmpty(path) ? segment : $"{path}/{segment}";

    /// <summary>A field's value in a variant, narrowed to what the field varies by.</summary>
    public static JsonNode? Get(JsonObject owner, FieldDefinition field, VariantId variant)
    {
        var stored = owner[field.Alias];
        return field.Varies ? (stored as JsonObject)?[variant.KeyFor(field.VariesBy)] : stored;
    }

    /// <summary>Sets a field's value in a variant, narrowed to what the field varies by. Null removes it.</summary>
    public static void Set(JsonObject owner, FieldDefinition field, VariantId variant, JsonNode? value)
    {
        if (!field.Varies)
        {
            Put(owner, field.Alias, value);
            return;
        }

        if (owner[field.Alias] is not JsonObject variants)
        {
            variants = [];
            owner[field.Alias] = variants;
        }

        Put(variants, variant.KeyFor(field.VariesBy), value);
        if (variants.Count == 0)
        {
            owner.Remove(field.Alias);
        }
    }

    /// <summary>A field's value in every variant that has one: once if invariant, else per variant.</summary>
    public static IEnumerable<(VariantId Variant, JsonNode Value)> Values(JsonObject owner, FieldDefinition field)
    {
        var stored = owner[field.Alias];
        if (stored is null)
        {
            yield break;
        }

        if (!field.Varies)
        {
            yield return (VariantId.Invariant, stored);
            yield break;
        }

        foreach (var (key, value) in stored as JsonObject ?? [])
        {
            if (value is not null)
            {
                yield return (VariantId.FromKey(key, field.VariesBy), value);
            }
        }
    }

    /// <summary>An items field's array in a variant, created empty when <paramref name="create"/> and missing.</summary>
    public static JsonArray? Items(JsonObject owner, FieldDefinition field, VariantId variant, bool create)
    {
        if (Get(owner, field, variant) is JsonArray items || !create)
        {
            return Get(owner, field, variant) as JsonArray;
        }

        items = [];
        Set(owner, field, variant, items);
        return items;
    }

    public static Guid KeyOf(JsonObject item) => Guid.Parse(item[KeyProperty]!.ToString());

    public static string TypeOf(JsonObject item) => item[TypeProperty]!.GetValue<string>();

    public static JsonObject NewItem(Guid key, string type) => new() { [KeyProperty] = key.ToString(), [TypeProperty] = type };

    public static JsonObject? FindItem(JsonArray? items, Guid key) => items?.OfType<JsonObject>().FirstOrDefault(i => KeyOf(i) == key);

    /// <summary>A field's value whether it is invariant or varies, read without its definition: the value, else the first variant's.</summary>
    public static JsonNode? ReadAny(JsonObject owner, string alias)
        => owner[alias] is JsonObject variants && variants.Select(v => v.Value).FirstOrDefault(v => v is JsonValue) is { } first ? first : owner[alias];

    /// <summary>
    /// Every value in the tree, with its path and the variant it is in. A value inside an item or object takes the
    /// variant of the value holding it, with what it varies by itself on top (a heading that varies inside an invariant
    /// block list).
    /// </summary>
    public static IEnumerable<EntityValue> Walk(JsonObject root, ElementShape shape, ItemTypeRegistry itemTypes)
        => Walk(root, shape, itemTypes, string.Empty, VariantId.Invariant);

    private static IEnumerable<EntityValue> Walk(JsonObject owner, ElementShape shape, ItemTypeRegistry itemTypes, string path, VariantId context)
    {
        foreach (var field in shape.Fields)
        {
            var fieldPath = Join(path, field.Alias);
            foreach (var (own, value) in Values(owner, field))
            {
                var variant = own.Over(context);
                yield return new EntityValue(fieldPath, field, variant, value, owner);

                IEnumerable<EntityValue> inner = field.Type switch
                {
                    FieldType.Items => (value as JsonArray ?? []).OfType<JsonObject>().SelectMany(item =>
                        itemTypes.Find(field.ItemTypeGroup!, TypeOf(item)) is { } itemType
                            ? Walk(item, itemType.Shape, itemTypes, Join(fieldPath, KeyOf(item).ToString()), variant)
                            : []),
                    FieldType.Object when field.Shape is { } objectShape && value is JsonObject obj => Walk(obj, objectShape, itemTypes, fieldPath, variant),
                    _ => [],
                };

                foreach (var found in inner)
                {
                    yield return found;
                }
            }
        }
    }

    /// <summary>The variants an entity has values in: every one that appears anywhere in it.</summary>
    public static IReadOnlyList<VariantId> VariantsIn(JsonObject root, ElementShape shape, ItemTypeRegistry itemTypes)
        => Walk(root, shape, itemTypes).Select(v => v.Variant).Where(v => !v.IsInvariant).Distinct().ToList();

    /// <summary>The variants values directly on this object are in, not counting objects and items inside it.</summary>
    public static IReadOnlySet<VariantId> OwnVariants(JsonObject owner, ElementShape shape)
        => shape.Fields.SelectMany(f => Values(owner, f)).Select(v => v.Variant).Where(v => !v.IsInvariant).ToHashSet();

    /// <summary>
    /// Finds what a path points at: a field (with the object holding it), an item in an items field
    /// (<c>blocks/{key}</c>), or an items field's order (<c>blocks/@order</c>). Steps go through items by key and through
    /// objects by field alias. <paramref name="variantFor"/> picks the variant for each varying value on the way.
    /// <paramref name="create"/> creates missing objects and item arrays, for writing. Throws
    /// <see cref="AbilityInputException"/> with a problem on the path when it doesn't resolve.
    /// </summary>
    public static PathTarget Find(JsonObject root, ElementShape shape, ItemTypeRegistry itemTypes, string path, Func<FieldDefinition, string, VariantId> variantFor, bool create)
    {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length == 0)
        {
            throw Invalid(path, "A path starts with a field alias.");
        }

        var owner = root;
        var prefix = string.Empty;
        PathTarget? handled = null;
        for (var i = 0; ;)
        {
            var fieldPath = Join(prefix, segments[i]);
            var field = shape.Find(segments[i])
                ?? throw Invalid(fieldPath, $"No field '{segments[i]}' here. Fields: {Names(shape.Fields.Select(f => f.Alias))}.");
            var variant = variantFor(field, fieldPath);
            var target = new PathTarget(owner, field, fieldPath, variant, null, false, handled);

            if (i == segments.Length - 1)
            {
                return target;
            }

            if (field.Handler is not null)
            {
                handled = target;
            }

            var next = segments[i + 1];
            switch (field.Type)
            {
                case FieldType.Items:
                    var items = Items(owner, field, variant, create);
                    if (next == EntityDiff.Order && i + 1 == segments.Length - 1)
                    {
                        return target with { Order = true };
                    }

                    if (!Guid.TryParse(next, out var key))
                    {
                        throw Invalid(Join(fieldPath, next), "Expected an item key (a GUID).");
                    }

                    var item = FindItem(items, key);
                    if (i + 1 == segments.Length - 1)
                    {
                        return item is null ? throw Invalid(Join(fieldPath, next), "No item with this key in the field.") : target with { ItemKey = key };
                    }

                    owner = item ?? throw Invalid(Join(fieldPath, next), "No item with this key in the field.");
                    shape = itemTypes.Find(field.ItemTypeGroup!, TypeOf(owner))?.Shape
                        ?? throw Invalid(Join(fieldPath, next), $"Item type '{TypeOf(owner)}' is not registered.");
                    prefix = Join(fieldPath, next);
                    i += 2;
                    break;

                case FieldType.Object when field.Shape is { } objectShape:
                    if (Get(owner, field, variant) is not JsonObject obj)
                    {
                        if (!create)
                        {
                            throw Invalid(fieldPath, "This has no value yet.");
                        }

                        obj = [];
                        Set(owner, field, variant, obj);
                    }

                    owner = obj;
                    shape = objectShape;
                    prefix = fieldPath;
                    i++;
                    break;

                default:
                    throw Invalid(Join(fieldPath, next), $"'{field.Alias}' holds a single value: set it whole at '{fieldPath}'.");
            }
        }
    }

    internal static string Names(IEnumerable<string> names)
    {
        var list = names.ToList();
        return list.Count == 0 ? "none" : string.Join(", ", list);
    }

    private static void Put(JsonObject owner, string name, JsonNode? value)
    {
        if (value is null)
        {
            owner.Remove(name);
        }
        else
        {
            owner[name] = value.Parent is null ? value : value.DeepClone();
        }
    }

    private static AbilityInputException Invalid(string path, string message) => new(message, [new Problem(path, message)]);
}

/// <summary>
/// What a path points at: <see cref="Field"/> on <see cref="Owner"/> in <see cref="Variant"/>; with <see cref="ItemKey"/>,
/// one of its items; with <see cref="Order"/>, the order of its items. <see cref="Handled"/> is the nearest field on the
/// way whose value has a handler, if any.
/// </summary>
public sealed record PathTarget(JsonObject Owner, FieldDefinition Field, string Path, VariantId Variant, Guid? ItemKey, bool Order, PathTarget? Handled)
{
    public JsonNode? Value => EntityValues.Get(Owner, Field, Variant);

    /// <summary>The path inside the handled value, e.g. <c>blocks</c> for <c>intro/blocks</c>.</summary>
    public string PathInside(PathTarget handled) => Path.Length > handled.Path.Length ? Path[(handled.Path.Length + 1)..] : string.Empty;
}
