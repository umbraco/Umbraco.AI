using System.Collections;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization;

using Umbraco.Cms.Core.Models;
using Umbraco.Extensions;
namespace Umbraco.Abilities.Entities.Models;

internal sealed record FieldMember(PropertyInfo Property, FieldDefinition Field, bool Varying, Type ValueType, IReadOnlyList<ValidationAttribute> Rules);

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
    ContentVariation Variations,
    (string EntityType, string Name, string Description)? Entity,
    (string Alias, string Name, string Description)? ItemType);

/// <summary>
/// Everything the framework works out from a model class, so package developers don't write it: the structure
/// (fields, slots, references, variations, schemas) from the properties and their attributes, the mapping between
/// the class and an entity document, and DataAnnotations validation reported on entity paths.
/// </summary>
public static class ModelShapes
{
    private static readonly ConcurrentDictionary<Type, ModelInfo> Cache = new();

    // Where each item read from a document sits, for problem paths on items that don't derive from EntityItemModel.
    private static readonly ConditionalWeakTable<object, string> Paths = [];

    /// <summary>The structure of a model class.</summary>
    public static ElementShape ShapeOf(Type type) => Info(type).Shape;

    internal static ModelInfo Info(Type type) => Cache.GetOrAdd(type, Build);

    internal static void Forget(Type type) => Cache.TryRemove(type, out _);

    /// <summary>Where an item read from a document sits, e.g. <c>"rewards/{key}"</c>; empty for an item not read from one.</summary>
    public static string PathOf(object item) => Paths.TryGetValue(item, out var path) ? path : string.Empty;

    /// <summary>The key of an item model, by its configured key property.</summary>
    public static Guid KeyOf(object item)
        => Info(item.GetType()).Key?.GetValue(item) is Guid key ? key : throw new InvalidOperationException($"{item.GetType().Name} has no key property.");

    // ── Describing ───────────────────────────────────────────────────────────────────────────────────

    private static ModelInfo Build(Type type)
    {
        var typeConfig = ModelConfiguration.Find(type);
        var fields = new List<FieldMember>();
        var slots = new List<SlotMember>();
        PropertyInfo? key = null, name = null, version = null, settings = null, layout = null;

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!property.CanRead || !property.CanWrite)
            {
                continue;
            }

            var config = Merge(FromAttributes(property), typeConfig?.Properties.GetValueOrDefault(property.Name));
            switch (config.Role)
            {
                case PropertyRole.Ignored:
                    continue;
                case PropertyRole.Key:
                    key = property;
                    continue;
                case PropertyRole.Version:
                    version = property;
                    continue;
                case PropertyRole.Settings:
                    settings = property;
                    continue;
                case PropertyRole.Layout:
                    layout = property;
                    continue;
            }

            var alias = config.Alias ?? AliasOf(property);
            var description = config.Description ?? string.Empty;
            var (varying, valueType) = Unwrap(property.PropertyType);
            var variations = varying ? config.Variations ?? ContentVariation.Culture : ContentVariation.Nothing;

            if (config.Role == PropertyRole.Slot)
            {
                slots.Add(new SlotMember(property, new SlotDefinition(alias, description, config.SlotGroup!)
                {
                    Min = config.Min,
                    Max = config.Max,
                    Variations = variations,
                    ItemLayout = config.Layout is { } layoutType ? ShapeOf(layoutType) : null,
                }, varying, valueType));
                continue;
            }

            fields.Add(new FieldMember(property, FieldFor(config, alias, description, valueType) with { Variations = variations }, varying, valueType, config.Rules));
            if (config.IsName)
            {
                name = property;
            }
        }

        var all = fields.Select(f => f.Field.Variations).Concat(slots.Select(s => s.Slot.Variations));
        var entityVariations = all.Any(v => v.VariesByCulture()) ? ContentVariation.Culture : ContentVariation.Nothing;
        var entity = typeConfig?.Entity ?? (type.GetCustomAttribute<EntityAttribute>() is { } e ? (e.EntityType, e.Name, e.Description) : null);
        var itemType = typeConfig?.ItemType ?? (type.GetCustomAttribute<ItemTypeAttribute>() is { } i ? (i.Alias, i.Name, i.Description) : null);
        return new ModelInfo(
            new ElementShape(fields.Select(f => f.Field).ToList(), slots.Select(s => s.Slot).ToList()),
            fields, slots, key, name, version, settings, layout, entityVariations, entity, itemType);
    }

    /// <summary>What a property's attributes say, as configuration.</summary>
    private static PropertyConfig FromAttributes(PropertyInfo property)
    {
        var config = new PropertyConfig
        {
            Role = property.IsDefined(typeof(JsonIgnoreAttribute)) ? PropertyRole.Ignored
                : property.DeclaringType == typeof(EntityItemModel) && property.Name == nameof(EntityItemModel.Key) ? PropertyRole.Key
                : property.DeclaringType == typeof(EntityItemModel) ? PropertyRole.Ignored
                : property.IsDefined(typeof(KeyAttribute)) ? PropertyRole.Key
                : property.IsDefined(typeof(EntityVersionAttribute)) ? PropertyRole.Version
                : property.IsDefined(typeof(ItemSettingsAttribute)) ? PropertyRole.Settings
                : property.IsDefined(typeof(ItemLayoutAttribute)) ? PropertyRole.Layout
                : property.IsDefined(typeof(SlotAttribute)) ? PropertyRole.Slot
                : null,
            Description = property.GetCustomAttribute<DescriptionAttribute>()?.Description,
            Required = property.IsDefined(typeof(RequiredAttribute)) ? true : null,
            IsName = property.IsDefined(typeof(EntityNameAttribute)),
            Allowed = property.GetCustomAttribute<AllowedValuesAttribute>()?.Values.Select(v => v?.ToString() ?? string.Empty).ToList(),
            References = property.GetCustomAttribute<EntityKeyAttribute>()?.EntityTypes,
            ItemReferenceSlot = property.GetCustomAttribute<ItemReferenceAttribute>()?.Slot,
            Variations = property.GetCustomAttribute<VariationsAttribute>()?.Variations,
        };

        if (property.GetCustomAttribute<SlotAttribute>() is { } slot)
        {
            config.SlotGroup = slot.ItemTypeGroup;
            config.Min = slot.Min >= 0 ? slot.Min : null;
            config.Max = slot.Max >= 0 ? slot.Max : null;
            config.Layout = slot.Layout;
        }

        // [Required] and [AllowedValues] are part of the structure, which the generic validator checks per variant.
        config.Rules.AddRange(property.GetCustomAttributes<ValidationAttribute>().Where(a => a is not RequiredAttribute and not AllowedValuesAttribute));
        return config;
    }

    /// <summary>Attribute configuration with fluent configuration laid over it.</summary>
    private static PropertyConfig Merge(PropertyConfig attributes, PropertyConfig? fluent)
    {
        if (fluent is null)
        {
            return attributes;
        }

        var merged = new PropertyConfig
        {
            Role = fluent.Role ?? attributes.Role,
            Alias = fluent.Alias ?? attributes.Alias,
            Description = fluent.Description ?? attributes.Description,
            Required = fluent.Required ?? attributes.Required,
            IsName = fluent.IsName || attributes.IsName,
            Allowed = fluent.Allowed ?? attributes.Allowed,
            References = fluent.References ?? attributes.References,
            ItemReferenceSlot = fluent.ItemReferenceSlot ?? attributes.ItemReferenceSlot,
            Variations = fluent.Variations ?? attributes.Variations,
            SlotGroup = fluent.SlotGroup ?? attributes.SlotGroup,
            Min = fluent.Min ?? attributes.Min,
            Max = fluent.Max ?? attributes.Max,
            Layout = fluent.Layout ?? attributes.Layout,
        };
        merged.Rules.AddRange(attributes.Rules.Concat(fluent.Rules));
        return merged;
    }

    private static FieldDefinition FieldFor(PropertyConfig config, string alias, string description, Type valueType)
    {
        var required = config.Required ?? false;
        var references = config.References;
        var allowed = config.Allowed;
        var itemReference = config.ItemReferenceSlot;

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
        foreach (var model in models ?? Array.Empty<object>())
        {
            var info = Info(model.GetType());
            var itemType = info.ItemType
                ?? throw new InvalidOperationException($"{model.GetType().Name} is not an item type: add [ItemType] or configure it with IsItemType.");
            var keyProperty = info.Key
                ?? throw new InvalidOperationException($"{model.GetType().Name} has no key: derive from EntityItemModel, add [Key] or configure HasKey.");
            if ((Guid)keyProperty.GetValue(model)! == Guid.Empty)
            {
                keyProperty.SetValue(model, Guid.NewGuid());
            }

            var item = new EntityItem { Key = (Guid)keyProperty.GetValue(model)!, Type = itemType.Alias };
            Write(model, item);

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

            var model = Read(itemClass, item, itemTypes, itemPath);
            var info = Info(itemClass);
            info.Key?.SetValue(model, item.Key);
            if (model is EntityItemModel itemModel)
            {
                itemModel.Path = itemPath;
            }

            Paths.AddOrUpdate(model, itemPath);

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
            foreach (var attribute in field.Rules)
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
            foreach (var item in lists.SelectMany(l => l.Cast<object>()))
            {
                var itemPath = EntityEditor.Join(EntityEditor.Join(path, slot.Slot.Alias), KeyOf(item).ToString());
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
