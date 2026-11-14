using System.Collections;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization;

namespace Umbraco.Abilities.Entities.Models;

internal sealed record FieldMember(PropertyInfo Property, FieldDefinition Field, bool Varying, Type ValueType);

internal sealed record SlotMember(PropertyInfo Property, SlotDefinition Slot, bool Varying, Type ListType);

internal sealed record ModelInfo(
    ElementShape Shape,
    IReadOnlyList<FieldMember> Fields,
    IReadOnlyList<SlotMember> Slots,
    PropertyInfo? Key,
    PropertyInfo? Name,
    PropertyInfo? Version,
    PropertyInfo? Settings,
    PropertyInfo? Layout,
    Variation Variations);

/// <summary>
/// Everything the framework works out from a model class, so package developers don't write it: the structure
/// (fields, slots, references, variations, schemas) from the properties and their attributes, the mapping between
/// the class and an entity document, and DataAnnotations validation reported on entity paths.
/// </summary>
public static class ModelShapes
{
    private static readonly ConcurrentDictionary<Type, ModelInfo> Cache = new();

    /// <summary>The structure of a model class.</summary>
    public static ElementShape ShapeOf(Type type) => Info(type).Shape;

    internal static ModelInfo Info(Type type) => Cache.GetOrAdd(type, Build);

    // ── Describing ───────────────────────────────────────────────────────────────────────────────────

    private static ModelInfo Build(Type type)
    {
        var fields = new List<FieldMember>();
        var slots = new List<SlotMember>();
        PropertyInfo? key = null, name = null, version = null, settings = null, layout = null;

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!property.CanRead || !property.CanWrite || property.DeclaringType == typeof(EntityItemModel) || property.IsDefined(typeof(JsonIgnoreAttribute)))
            {
                continue;
            }

            if (property.IsDefined(typeof(KeyAttribute)))
            {
                key = property;
                continue;
            }

            if (property.IsDefined(typeof(EntityVersionAttribute)))
            {
                version = property;
                continue;
            }

            if (property.IsDefined(typeof(ItemSettingsAttribute)))
            {
                settings = property;
                continue;
            }

            if (property.IsDefined(typeof(ItemLayoutAttribute)))
            {
                layout = property;
                continue;
            }

            var alias = AliasOf(property);
            var description = property.GetCustomAttribute<DescriptionAttribute>()?.Description ?? string.Empty;
            var (varying, valueType) = Unwrap(property.PropertyType);
            var variations = varying ? property.GetCustomAttribute<VariationsAttribute>()?.Variations ?? Variation.Culture : Variation.Invariant;

            if (property.GetCustomAttribute<SlotAttribute>() is { } slotAttribute)
            {
                slots.Add(new SlotMember(property, new SlotDefinition(alias, description, slotAttribute.ItemTypeGroup)
                {
                    Min = slotAttribute.Min >= 0 ? slotAttribute.Min : null,
                    Max = slotAttribute.Max >= 0 ? slotAttribute.Max : null,
                    Variations = variations,
                    ItemLayout = slotAttribute.Layout is { } layoutType ? ShapeOf(layoutType) : null,
                }, varying, valueType));
                continue;
            }

            fields.Add(new FieldMember(property, FieldFor(property, alias, description, valueType) with { Variations = variations }, varying, valueType));
            if (property.IsDefined(typeof(EntityNameAttribute)))
            {
                name = property;
            }
        }

        var all = fields.Select(f => f.Field.Variations).Concat(slots.Select(s => s.Slot.Variations));
        var entityVariations = all.Any(v => v.VariesByCulture()) ? Variation.Culture : Variation.Invariant;
        return new ModelInfo(
            new ElementShape(fields.Select(f => f.Field).ToList(), slots.Select(s => s.Slot).ToList()),
            fields, slots, key, name, version, settings, layout, entityVariations);
    }

    private static FieldDefinition FieldFor(PropertyInfo property, string alias, string description, Type valueType)
    {
        var required = property.IsDefined(typeof(RequiredAttribute));
        var references = property.GetCustomAttribute<EntityKeyAttribute>()?.EntityTypes;
        var allowed = property.GetCustomAttribute<AllowedValuesAttribute>()?.Values.Select(v => v?.ToString() ?? string.Empty).ToList();
        var itemReference = property.GetCustomAttribute<ItemReferenceAttribute>()?.Slot;

        var many = valueType != typeof(string) && ElementTypeOf(valueType) is not null;
        var single = many ? ElementTypeOf(valueType)! : valueType;
        single = Nullable.GetUnderlyingType(single) ?? single;

        if (single.IsEnum)
        {
            allowed ??= Enum.GetNames(single).Select(n => JsonNamingPolicy.CamelCase.ConvertName(n)).ToList();
        }

        FieldType? type = single switch
        {
            _ when itemReference is not null => FieldType.ItemReference,
            _ when single == typeof(EntityReference) && references is { Count: > 0 } => FieldType.EntityReference,
            _ when single == typeof(Guid) && references is { Count: > 0 } => FieldType.EntityReference,
            _ when single == typeof(string) || single.IsEnum => FieldType.String,
            _ when single == typeof(bool) => FieldType.Boolean,
            _ when single == typeof(int) || single == typeof(long) || single == typeof(short) => FieldType.Integer,
            _ when single == typeof(decimal) || single == typeof(double) || single == typeof(float) => FieldType.Number,
            _ when single == typeof(DateOnly) => FieldType.Date,
            _ => null,
        };

        if (type is null)
        {
            // Anything else is a structured value: described by its own JSON Schema, generated from the type.
            return new FieldDefinition(alias, description, FieldType.Json, required)
            {
                Schema = JsonSchemaExporter.GetJsonSchemaAsNode(AbilityJson.Options, valueType) as JsonObject,
            };
        }

        return new FieldDefinition(alias, description, type.Value, required)
        {
            Many = many,
            Allowed = allowed,
            ReferencesEntityType = single == typeof(Guid) ? references?.FirstOrDefault() : null,
            ReferencesAnyOf = single == typeof(EntityReference) ? references : null,
            ReferencesItemInSlot = itemReference,
        };
    }

    // ── Mapping ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Writes a model's values into an element.</summary>
    public static void Write(object model, EntityElement element)
    {
        var info = Info(model.GetType());
        foreach (var field in info.Fields)
        {
            var value = field.Property.GetValue(model);
            if (field.Varying)
            {
                foreach (DictionaryEntry entry in (IDictionary?)value ?? new Hashtable())
                {
                    if (entry.Value is not null)
                    {
                        element.Variant(VariantId.Parse((string)entry.Key)).Fields[field.Field.Alias] = ToNode(entry.Value);
                    }
                }
            }
            else if (value is not null)
            {
                element.Fields[field.Field.Alias] = ToNode(value);
            }
        }

        foreach (var slot in info.Slots)
        {
            var value = slot.Property.GetValue(model);
            if (slot.Varying)
            {
                foreach (DictionaryEntry entry in (IDictionary?)value ?? new Hashtable())
                {
                    WriteItems((IEnumerable?)entry.Value, element.Variant(VariantId.Parse((string)entry.Key)).Slot(slot.Slot.Alias));
                }
            }
            else
            {
                WriteItems((IEnumerable?)value, element.Slot(slot.Slot.Alias));
            }
        }
    }

    /// <summary>Reads a model of <paramref name="type"/> from an element. Items are created as their registered item type classes.</summary>
    public static object Read(Type type, EntityElement element, ItemTypeRegistry itemTypes, string path)
    {
        var info = Info(type);
        var model = Activator.CreateInstance(type)!;

        foreach (var field in info.Fields)
        {
            if (field.Varying)
            {
                var varying = (IDictionary)Activator.CreateInstance(field.Property.PropertyType)!;
                foreach (var (variant, node) in element.Values(field.Field.Alias).Where(v => !v.Variant.IsInvariant))
                {
                    varying[variant.Key] = FromNode(node, field.ValueType);
                }

                field.Property.SetValue(model, varying);
            }
            else if (element.Fields.GetValueOrDefault(field.Field.Alias) is { } node)
            {
                field.Property.SetValue(model, FromNode(node, field.Property.PropertyType));
            }
        }

        foreach (var slot in info.Slots)
        {
            var slotPath = EntityEditor.Join(path, slot.Slot.Alias);
            if (slot.Varying)
            {
                var varying = (IDictionary)Activator.CreateInstance(slot.Property.PropertyType)!;
                foreach (var (variant, items) in element.SlotItems(slot.Slot.Alias).Where(v => !v.Variant.IsInvariant))
                {
                    varying[variant.Key] = ReadItems(items, slot, slot.ListType, itemTypes, slotPath);
                }

                slot.Property.SetValue(model, varying);
            }
            else
            {
                slot.Property.SetValue(model, ReadItems(element.Slots.GetValueOrDefault(slot.Slot.Alias) ?? [], slot, slot.Property.PropertyType, itemTypes, slotPath));
            }
        }

        return model;
    }

    private static void WriteItems(IEnumerable? models, List<EntityItem> items)
    {
        foreach (EntityItemModel model in models ?? Array.Empty<EntityItemModel>())
        {
            var itemType = model.GetType().GetCustomAttribute<ItemTypeAttribute>()
                ?? throw new InvalidOperationException($"{model.GetType().Name} has no [ItemType] attribute.");
            if (model.Key == Guid.Empty)
            {
                model.Key = Guid.NewGuid();
            }

            var item = new EntityItem { Key = model.Key, Type = itemType.Alias };
            Write(model, item);

            var info = Info(model.GetType());
            if (info.Settings?.GetValue(model) is { } settings)
            {
                Write(settings, item.EnsureSettings());
            }

            if (info.Layout?.GetValue(model) is { } layout)
            {
                Write(layout, item.EnsureLayout());
            }

            items.Add(item);
        }
    }

    private static IList ReadItems(IEnumerable<EntityItem> items, SlotMember slot, Type listType, ItemTypeRegistry itemTypes, string slotPath)
    {
        var list = (IList)Activator.CreateInstance(listType)!;
        foreach (var item in items)
        {
            var itemPath = EntityEditor.Join(slotPath, item.Key.ToString());
            var itemClass = itemTypes.Find(slot.Slot.ItemTypeGroup, item.Type)?.ClrType
                ?? throw new AbilityInputException($"Item type '{item.Type}' has no model class.", [new Problem(itemPath, "No model class for this item type.")]);

            var model = (EntityItemModel)Read(itemClass, item, itemTypes, itemPath);
            model.Key = item.Key;
            model.Path = itemPath;

            var info = Info(itemClass);
            if (info.Settings is { } settingsProperty && item.Settings is { } settings)
            {
                settingsProperty.SetValue(model, Read(settingsProperty.PropertyType, settings, itemTypes, $"{itemPath}/@settings"));
            }

            if (info.Layout is { } layoutProperty && item.Layout is { } layout)
            {
                layoutProperty.SetValue(model, Read(layoutProperty.PropertyType, layout, itemTypes, $"{itemPath}/@layout"));
            }

            list.Add(model);
        }

        return list;
    }

    // ── Validating ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Runs the DataAnnotations attributes on a model and everything in it ([Range], [StringLength], [RegularExpression],
    /// custom ones), reporting each on its entity path, e.g. <c>"rewards/{key}/percent"</c>.
    /// </summary>
    public static IReadOnlyList<Problem> Validate(object model, string path = "")
    {
        var problems = new List<Problem>();
        ValidateObject(model, path, problems);
        return problems;
    }

    private static void ValidateObject(object model, string path, List<Problem> problems)
    {
        var info = Info(model.GetType());
        foreach (var field in info.Fields.Where(f => !f.Varying))
        {
            var value = field.Property.GetValue(model);
            var context = new ValidationContext(model) { MemberName = field.Property.Name, DisplayName = field.Field.Alias };

            // [Required] and [AllowedValues] are part of the structure, which the generic validator checks per variant.
            foreach (var attribute in field.Property.GetCustomAttributes<ValidationAttribute>()
                .Where(a => a is not RequiredAttribute and not AllowedValuesAttribute))
            {
                if (attribute.GetValidationResult(value, context) is { } result && result != ValidationResult.Success)
                {
                    problems.Add(new Problem(EntityEditor.Join(path, field.Field.Alias), result.ErrorMessage ?? "Invalid."));
                }
            }
        }

        foreach (var slot in info.Slots)
        {
            var value = slot.Property.GetValue(model);
            var lists = slot.Varying ? ((IDictionary?)value)?.Values.Cast<IEnumerable>() ?? [] : [(IEnumerable?)value ?? Array.Empty<object>()];
            foreach (EntityItemModel item in lists.SelectMany(l => l.Cast<EntityItemModel>()))
            {
                var itemPath = EntityEditor.Join(EntityEditor.Join(path, slot.Slot.Alias), item.Key.ToString());
                ValidateObject(item, itemPath, problems);

                var itemInfo = Info(item.GetType());
                if (itemInfo.Settings?.GetValue(item) is { } settings)
                {
                    ValidateObject(settings, $"{itemPath}/@settings", problems);
                }

                if (itemInfo.Layout?.GetValue(item) is { } layout)
                {
                    ValidateObject(layout, $"{itemPath}/@layout", problems);
                }
            }
        }
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────────────────────────

    internal static string AliasOf(MemberInfo member)
        => member.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? JsonNamingPolicy.CamelCase.ConvertName(member.Name);

    private static (bool Varying, Type ValueType) Unwrap(Type type)
        => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Varying<>) ? (true, type.GetGenericArguments()[0]) : (false, type);

    private static Type? ElementTypeOf(Type type)
        => type.IsArray ? type.GetElementType()
            : type.IsGenericType && typeof(IEnumerable).IsAssignableFrom(type) && type.GetGenericArguments().Length == 1 ? type.GetGenericArguments()[0]
            : null;

    private static JsonNode? ToNode(object value) => JsonSerializer.SerializeToNode(value, value.GetType(), AbilityJson.Options);

    private static object? FromNode(JsonNode node, Type type) => node.Deserialize(type, AbilityJson.Options);
}
