using System.Text.Json;
using System.Text.Json.Nodes;

namespace Umbraco.Abilities.Entities;

/// <summary>What an edit works on: the document, its structure, the item types, and the variant being edited.</summary>
public sealed record EditTarget(EntityDocument Document, EntityStructure Structure, ItemTypeRegistry ItemTypes, VariantId Variant);

/// <summary>
/// Edits an entity by path. A path goes down through values: in an object a step is a field alias, in an items field a
/// step is an item's key. So <c>"blocks/{key}/heading"</c> is a field of an item, <c>"blocks/{key}/settings/theme"</c> a
/// field of an object in it, and <c>"blocks/{key}/cards"</c> an items field inside it. Problems use the same paths.
/// Values that vary are read and written for the edit's variant, narrowed to what each value varies by. Generalised from
/// Umbraco.AI's block path engine.
/// </summary>
public static class EntityEditor
{
    public static string Join(string path, string segment) => EntityValues.Join(path, segment);

    /// <summary>What a path points at, for editing: the variant of each varying value on the way is the edit's.</summary>
    public static PathTarget Find(EditTarget target, string path, bool create = true)
        => EntityValues.Find(target.Document.Root, target.Structure.Shape, target.ItemTypes, path, (field, at) => RequireVariant(target, field.VariesBy, at), create);

    /// <summary>Sets the value at a path. Returns the path.</summary>
    public static string SetValue(EditTarget target, string path, JsonNode? value)
    {
        var at = Find(target, path);
        if (at.ItemKey is not null || at.Order)
        {
            throw Invalid(path, "A path to set ends with a field alias.");
        }

        SetField(at.Owner, at.Field, at.Path, at.Variant, value);
        return at.Path;
    }

    /// <summary>Sets fields of an object (the entity, or a new item) from a JSON object of values by alias.</summary>
    public static void SetFields(EditTarget target, JsonObject owner, ElementShape shape, string path, JsonObject? values)
    {
        var problems = new List<Problem>();
        foreach (var (alias, value) in values ?? [])
        {
            var fieldPath = Join(path, alias);
            try
            {
                var field = shape.Find(alias)
                    ?? throw Invalid(fieldPath, $"No field '{alias}' here. Fields: {EntityValues.Names(shape.Fields.Select(f => f.Alias))}.");
                SetField(owner, field, fieldPath, RequireVariant(target, field.VariesBy, fieldPath), value);
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

    /// <summary>Adds an item to the items field at <paramref name="fieldPath"/>, with field values. Returns the item's path.</summary>
    public static string AddItem(EditTarget target, string fieldPath, string itemTypeAlias, Guid key, JsonObject? fields, int? position)
    {
        var at = RequireItemsField(Find(target, fieldPath), fieldPath);
        var allowed = target.ItemTypes.InGroup(at.Field.ItemTypeGroup!);
        var itemType = allowed.FirstOrDefault(t => t.Alias == itemTypeAlias)
            ?? throw Invalid(at.Path, $"Item type '{itemTypeAlias}' is not allowed in this field. Allowed: {EntityValues.Names(allowed.Select(t => t.Alias))}.");

        var items = EntityValues.Items(at.Owner, at.Field, at.Variant, create: true)!;
        if (at.Field.Max is { } max && items.Count >= max)
        {
            throw Invalid(at.Path, $"This field holds at most {max} item(s).");
        }

        if (EntityValues.FindItem(items, key) is not null)
        {
            throw Invalid(Join(at.Path, key.ToString()), "An item with this key is already in the field.");
        }

        var item = EntityValues.NewItem(key, itemType.Alias);
        items.Insert(position is { } p ? Math.Clamp(p, 0, items.Count) : items.Count, item);
        var itemPath = Join(at.Path, key.ToString());
        SetFields(target, item, itemType.Shape, itemPath, fields);
        Notify(at, (handler, value, inside) => handler.ItemAdded(value, inside, key));
        return itemPath;
    }

    public static void RemoveItem(EditTarget target, string fieldPath, Guid key)
    {
        var at = RequireItemsField(Find(target, fieldPath, create: false), fieldPath);
        var items = (JsonArray?)at.Value;
        items!.Remove(EntityValues.FindItem(items, key) ?? throw Invalid(Join(at.Path, key.ToString()), "No item with this key in the field."));
        Notify(at, (handler, value, inside) => handler.ItemRemoved(value, inside, key));
    }

    public static void MoveItem(EditTarget target, string fieldPath, Guid key, int position)
    {
        var at = RequireItemsField(Find(target, fieldPath, create: false), fieldPath);
        var items = (JsonArray?)at.Value;
        var item = EntityValues.FindItem(items, key) ?? throw Invalid(Join(at.Path, key.ToString()), "No item with this key in the field.");
        items!.Remove(item);
        items.Insert(Math.Clamp(position, 0, items.Count), item);
        Notify(at, (handler, value, inside) => handler.ItemMoved(value, inside, key));
    }

    /// <summary>
    /// The variant to use for a value that varies by <paramref name="variesBy"/>: the edit's variant, narrowed to those
    /// dimensions. A dimension with listed values needs one of them, or the only one when there is just one; an open
    /// dimension (the CMS's segments) may be left out for its default.
    /// </summary>
    public static VariantId RequireVariant(EditTarget target, IReadOnlyList<string> variesBy, string path)
    {
        var variant = target.Variant.Narrow(variesBy);
        foreach (var name in variesBy)
        {
            if (target.Structure.Dimension(name)?.Values is not { } values)
            {
                continue;
            }

            if (variant[name] is not { } value)
            {
                variant = values.Count == 1
                    ? variant.With(name, values[0])
                    : throw Invalid(path, $"This varies by {name}. Give a {name} in the variant: {EntityValues.Names(values)}.");
            }
            else if (!values.Contains(value))
            {
                throw Invalid(path, $"Unknown {name} '{value}'. Values: {EntityValues.Names(values)}.");
            }
        }

        return variant;
    }

    private static void SetField(JsonObject owner, FieldDefinition field, string path, VariantId variant, JsonNode? value)
    {
        if (field.IsItems)
        {
            throw Invalid(path, $"'{field.Alias}' holds items. Use entity.addItem, removeItem or moveItem with fieldPath \"{path}\".");
        }

        value = FieldChecker.Normalize(field, value);
        if (FieldChecker.Check(field, value, path) is { } problem)
        {
            throw new AbilityInputException(problem.ToString(), [problem.In(variant)]);
        }

        EntityValues.Set(owner, field, variant, value);
    }

    private static PathTarget RequireItemsField(PathTarget at, string path)
        => at.Field.IsItems && at.ItemKey is null && !at.Order
            ? at
            : throw Invalid(path, "A field path ends with an items field's alias, e.g. \"rules\" or \"blocks/{key}/cards\".");

    // Tells the handler of the value the items are in, if any, so it can keep the rest of its value in step.
    private static void Notify(PathTarget at, Action<IValueHandler, JsonObject, string> change)
    {
        if (at.Handled is { } handled && handled.Value is JsonObject value)
        {
            change(handled.Field.Handler!, value, at.PathInside(handled));
        }
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

        if (field.Type == FieldType.Object)
        {
            return value is JsonObject ? null : new Problem(path, "Expected an object.");
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
