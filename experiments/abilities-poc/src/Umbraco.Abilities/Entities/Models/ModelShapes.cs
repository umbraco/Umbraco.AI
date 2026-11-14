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

internal sealed record ItemsMember(PropertyInfo Property, FieldDefinition Field, bool Varying, Type ListType);

internal sealed record ModelInfo(
    ElementShape Shape,
    IReadOnlyList<FieldMember> Fields,
    IReadOnlyList<ItemsMember> ItemFields,
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
/// (fields, items fields, references, variations, schemas) from the properties and their attributes, the mapping between
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
        var itemFields = new List<ItemsMember>();
        var ordered = new List<FieldDefinition>();
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

            if (config.Role == PropertyRole.Items)
            {
                var items = FieldDefinition.ItemsOf(alias, description, config.ItemTypeGroup!) with
                {
                    Min = config.Min,
                    Max = config.Max,
                    Variations = variations,
                    ItemLayout = config.Layout is { } layoutType ? ShapeOf(layoutType) : null,
                };
                itemFields.Add(new ItemsMember(property, items, varying, valueType));
                ordered.Add(items);
                continue;
            }

            var field = FieldFor(config, alias, description, valueType) with { Variations = variations };
            if (property.GetCustomAttribute<ValueHandlerAttribute>() is { } handled)
            {
                field = field with
                {
                    Type = FieldType.Json,
                    Handler = (IValueHandler)Activator.CreateInstance(handled.HandlerType)!,
                    Schema = handled.HandlerType.GetProperty("Schema", BindingFlags.Public | BindingFlags.Static)?.GetValue(null) as JsonObject ?? field.Schema,
                };
            }

            fields.Add(new FieldMember(property, field, varying, valueType, config.Rules));
            ordered.Add(fields[^1].Field);
            if (config.IsName)
            {
                name = property;
            }
        }

        var all = ordered.Select(f => f.Variations);
        var entityVariations = all.Any(v => v.VariesByCulture()) ? ContentVariation.Culture : ContentVariation.Nothing;
        var entity = typeConfig?.Entity ?? (type.GetCustomAttribute<EntityAttribute>() is { } e ? (e.EntityType, e.Name, e.Description) : null);
        var itemType = typeConfig?.ItemType ?? (type.GetCustomAttribute<ItemTypeAttribute>() is { } i ? (i.Alias, i.Name, i.Description) : null);
        return new ModelInfo(
            new ElementShape(ordered),
            fields, itemFields, key, name, version, settings, layout, entityVariations, entity, itemType);
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
                : property.IsDefined(typeof(ItemsAttribute)) ? PropertyRole.Items
                : null,
            Description = property.GetCustomAttribute<DescriptionAttribute>()?.Description,
            Required = property.IsDefined(typeof(RequiredAttribute)) ? true : null,
            IsName = property.IsDefined(typeof(EntityNameAttribute)),
            Allowed = property.GetCustomAttribute<AllowedValuesAttribute>()?.Values.Select(v => v?.ToString() ?? string.Empty).ToList(),
            References = property.GetCustomAttribute<EntityKeyAttribute>()?.EntityTypes,
            ItemReferenceIn = property.GetCustomAttribute<ItemReferenceAttribute>()?.Field,
            Variations = property.GetCustomAttribute<VariationsAttribute>()?.Variations,
        };

        if (property.GetCustomAttribute<ItemsAttribute>() is { } items)
        {
            config.ItemTypeGroup = items.ItemTypeGroup;
            config.Min = items.Min >= 0 ? items.Min : null;
            config.Max = items.Max >= 0 ? items.Max : null;
            config.Layout = items.Layout;
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
            ItemReferenceIn = fluent.ItemReferenceIn ?? attributes.ItemReferenceIn,
            Variations = fluent.Variations ?? attributes.Variations,
            ItemTypeGroup = fluent.ItemTypeGroup ?? attributes.ItemTypeGroup,
            Min = fluent.Min ?? attributes.Min,
            Max = fluent.Max ?? attributes.Max,
            Layout = fluent.Layout ?? attributes.Layout,
        };
        merged.Rules.AddRange(attributes.Rules.Concat(fluent.Rules));
        return merged;
    }

    /// <summary>A field for a value of a CLR type, with the same mapping as a model class's properties.</summary>
    public static FieldDefinition FieldFor(string alias, string description, Type valueType, bool required, IReadOnlyList<string>? references = null, IReadOnlyList<string>? allowed = null)
        => FieldFor(new PropertyConfig { Required = required, References = references, Allowed = allowed }, alias, description, valueType);

    private static FieldDefinition FieldFor(PropertyConfig config, string alias, string description, Type valueType)
    {
        var required = config.Required ?? false;
        var references = config.References;
        var allowed = config.Allowed;
        var itemReference = config.ItemReferenceIn;

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
            ReferencesItemIn = itemReference,
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

        foreach (var member in info.ItemFields)
        {
            var value = member.Property.GetValue(model);
            if (member.Varying)
            {
                foreach (DictionaryEntry entry in (IDictionary?)value ?? new Hashtable())
                {
                    WriteItems((IEnumerable?)entry.Value, element.Variant(VariantId.Parse((string)entry.Key)).ItemsOf(member.Field.Alias));
                }
            }
            else
            {
                WriteItems((IEnumerable?)value, element.ItemsOf(member.Field.Alias));
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

        foreach (var member in info.ItemFields)
        {
            var fieldPath = EntityEditor.Join(path, member.Field.Alias);
            if (member.Varying)
            {
                var varying = (IDictionary)Activator.CreateInstance(member.Property.PropertyType)!;
                foreach (var (variant, items) in element.ItemValues(member.Field.Alias).Where(v => !v.Variant.IsInvariant))
                {
                    varying[variant.Key] = ReadItems(items, member, member.ListType, itemTypes, fieldPath);
                }

                member.Property.SetValue(model, varying);
            }
            else
            {
                member.Property.SetValue(model, ReadItems(element.Items.GetValueOrDefault(member.Field.Alias) ?? [], member, member.Property.PropertyType, itemTypes, fieldPath));
            }
        }

        return model;
    }

    private static void WriteItems(IEnumerable? models, List<EntityItem> items)
    {
        foreach (var model in models ?? Array.Empty<object>())
        {
            if (model is DynamicItem dynamic)
            {
                items.Add(WriteDynamic(dynamic));
                continue;
            }

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

    private static IList ReadItems(IEnumerable<EntityItem> items, ItemsMember member, Type listType, ItemTypeRegistry itemTypes, string fieldPath)
    {
        var list = (IList)Activator.CreateInstance(listType)!;
        var dynamic = ElementTypeOf(listType) is { } elementType && typeof(DynamicItem).IsAssignableFrom(elementType);
        foreach (var item in items)
        {
            var itemPath = EntityEditor.Join(fieldPath, item.Key.ToString());
            if (dynamic)
            {
                list.Add(ReadDynamic(item, itemPath));
                continue;
            }

            var itemClass = itemTypes.Find(member.Field.ItemTypeGroup, item.Type)?.ClrType
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

        foreach (var member in info.ItemFields)
        {
            var value = member.Property.GetValue(model);
            var lists = member.Varying ? ((IDictionary?)value)?.Values.Cast<IEnumerable>() ?? [] : [(IEnumerable?)value ?? Array.Empty<object>()];
            // Dynamic items have no class to carry rules: their fields' rules are checked from the structure.
            foreach (var item in lists.SelectMany(l => l.Cast<object>()).Where(i => i is not DynamicItem))
            {
                var itemPath = EntityEditor.Join(EntityEditor.Join(path, member.Field.Alias), KeyOf(item).ToString());
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

    // ── Dynamic items ───────────────────────────────────────────────────────────────────────────────

    private static EntityItem WriteDynamic(DynamicItem model)
    {
        if (model.Key == Guid.Empty)
        {
            model.Key = Guid.NewGuid();
        }

        var item = new EntityItem { Key = model.Key, Type = model.Type };
        foreach (var (alias, value) in model.Fields)
        {
            item.Fields[alias] = value?.DeepClone();
        }

        foreach (var (alias, children) in model.Items)
        {
            item.ItemsOf(alias).AddRange(children.Select(WriteDynamic));
        }

        return item;
    }

    private static DynamicItem ReadDynamic(EntityItem item, string path)
    {
        var model = new DynamicItem { Key = item.Key, Type = item.Type, Path = path };
        foreach (var (alias, value) in item.Fields)
        {
            model.Fields[alias] = value?.DeepClone();
        }

        foreach (var (alias, children) in item.Items)
        {
            var fieldPath = EntityEditor.Join(path, alias);
            model.Items[alias] = children.Select(c => ReadDynamic(c, EntityEditor.Join(fieldPath, c.Key.ToString()))).ToList();
        }

        Paths.AddOrUpdate(model, path);
        return model;
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
