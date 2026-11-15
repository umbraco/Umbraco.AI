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

using Umbraco.Extensions;
namespace Umbraco.Abilities.Entities.Models;

/// <summary>A property that is a field. <see cref="ValueType"/> is the property's type, or a <see cref="Varying{T}"/>'s value type.</summary>
internal sealed record FieldMember(PropertyInfo Property, FieldDefinition Field, bool Varying, Type ValueType, IReadOnlyList<ValidationAttribute> Rules);

internal sealed record ModelInfo(
    ElementShape Shape,
    IReadOnlyList<FieldMember> Fields,
    PropertyInfo? Key,
    PropertyInfo? Name,
    PropertyInfo? Version,
    IReadOnlyList<string> Dimensions,
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
        PropertyInfo? key = null, name = null, version = null;

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
            }

            var alias = AliasOf(property);
            var description = config.Description ?? string.Empty;
            var (varying, valueType) = Unwrap(property.PropertyType);
            var field = config.Role == PropertyRole.Items
                ? FieldDefinition.ItemsOf(alias, description, config.ItemTypeGroup!) with { Min = config.Min, Max = config.Max }
                : IsObject(valueType)
                    ? FieldDefinition.ObjectOf(alias, description, ShapeOf(valueType)) with { Required = config.Required ?? false }
                    : FieldFor(config, alias, description, valueType);
            field = field with { VariesBy = varying ? property.GetCustomAttribute<VariesByAttribute>()?.Dimensions ?? [VariationDimensions.Culture] : [] };

            if (property.GetCustomAttribute<ValueHandlerAttribute>() is { } handled)
            {
                field = field.Type == FieldType.Object
                    ? field with { Handler = (IValueHandler)Activator.CreateInstance(handled.HandlerType)! }
                    : throw new InvalidOperationException($"{type.Name}.{property.Name}: a value handler goes on an object property, whose fields it adds behaviour to.");
            }

            fields.Add(new FieldMember(property, field, varying, valueType, config.Rules));
            if (config.IsName)
            {
                name = property;
            }
        }

        var dimensions = fields.SelectMany(f => f.Field.VariesBy).Distinct().ToList();
        var entity = typeConfig?.Entity ?? (type.GetCustomAttribute<EntityAttribute>() is { } e ? (e.EntityType, e.Name, e.Description) : null);
        var itemType = typeConfig?.ItemType ?? (type.GetCustomAttribute<ItemTypeAttribute>() is { } i ? (i.Alias, i.Name, i.Description) : null);
        return new ModelInfo(new ElementShape(fields.Select(f => f.Field).ToList()), fields, key, name, version, dimensions, entity, itemType);
    }

    /// <summary>
    /// Whether a property's type is an object with fields of its own (a block's settings, an SEO group): a class the
    /// framework can create, so paths go into it. Anything else that isn't a scalar is a JSON value, set whole.
    /// </summary>
    private static bool IsObject(Type type)
        => type.IsClass
            && type != typeof(string)
            && type != typeof(EntityReference)
            && !typeof(IEnumerable).IsAssignableFrom(type)
            && !typeof(JsonNode).IsAssignableFrom(type)
            && type.GetConstructor(Type.EmptyTypes) is not null;

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
                : property.IsDefined(typeof(ItemsAttribute)) ? PropertyRole.Items
                : null,
            Description = property.GetCustomAttribute<DescriptionAttribute>()?.Description,
            Required = property.IsDefined(typeof(RequiredAttribute)) ? true : null,
            IsName = property.IsDefined(typeof(EntityNameAttribute)),
            Allowed = property.GetCustomAttribute<AllowedValuesAttribute>()?.Values.Select(v => v?.ToString() ?? string.Empty).ToList(),
            References = property.GetCustomAttribute<EntityKeyAttribute>()?.EntityTypes,
        };

        if (property.GetCustomAttribute<ItemsAttribute>() is { } items)
        {
            config.ItemTypeGroup = items.ItemTypeGroup;
            config.Min = items.Min >= 0 ? items.Min : null;
            config.Max = items.Max >= 0 ? items.Max : null;
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
            Description = fluent.Description ?? attributes.Description,
            Required = fluent.Required ?? attributes.Required,
            IsName = fluent.IsName || attributes.IsName,
            Allowed = fluent.Allowed ?? attributes.Allowed,
            References = fluent.References ?? attributes.References,
            ItemTypeGroup = fluent.ItemTypeGroup ?? attributes.ItemTypeGroup,
            Min = fluent.Min ?? attributes.Min,
            Max = fluent.Max ?? attributes.Max,
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

        var many = valueType != typeof(string) && ElementTypeOf(valueType) is not null;
        var single = many ? ElementTypeOf(valueType)! : valueType;
        single = Nullable.GetUnderlyingType(single) ?? single;

        if (single.IsEnum)
        {
            allowed ??= Enum.GetNames(single).Select(n => JsonNamingPolicy.CamelCase.ConvertName(n)).ToList();
        }

        FieldType? type = single switch
        {
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
        };
    }

    // ── Mapping ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>A model's values as the JSON object an entity document holds: items as keyed, typed objects, varying values per variant.</summary>
    public static JsonObject Write(object model)
    {
        var json = new JsonObject();
        foreach (var member in Info(model.GetType()).Fields)
        {
            var value = member.Property.GetValue(model);
            if (value is null)
            {
                continue;
            }

            if (!member.Varying)
            {
                json[member.Field.Alias] = WriteValue(member, value);
                continue;
            }

            var variants = new JsonObject();
            foreach (DictionaryEntry entry in (IDictionary)value)
            {
                if (entry.Value is not null)
                {
                    variants[(string)entry.Key] = WriteValue(member, entry.Value);
                }
            }

            if (variants.Count > 0)
            {
                json[member.Field.Alias] = variants;
            }
        }

        return json;
    }

    /// <summary>Reads a model of <paramref name="type"/> from an object in an entity document. Items are created as their registered item type classes.</summary>
    public static object Read(Type type, JsonObject json, ItemTypeRegistry itemTypes, string path)
    {
        var model = Activator.CreateInstance(type)!;
        foreach (var member in Info(type).Fields)
        {
            var fieldPath = EntityEditor.Join(path, member.Field.Alias);
            if (json[member.Field.Alias] is not { } node)
            {
                continue;
            }

            if (!member.Varying)
            {
                member.Property.SetValue(model, ReadValue(member, node, member.Property.PropertyType, itemTypes, fieldPath));
                continue;
            }

            var varying = (IDictionary)Activator.CreateInstance(member.Property.PropertyType)!;
            foreach (var (variant, value) in node.AsObject())
            {
                if (value is not null)
                {
                    varying[variant] = ReadValue(member, value, member.ValueType, itemTypes, fieldPath);
                }
            }

            member.Property.SetValue(model, varying);
        }

        return model;
    }

    private static JsonNode? WriteValue(FieldMember member, object value) => member.Field.Type switch
    {
        FieldType.Items => new JsonArray(((IEnumerable)value).Cast<object>().Select(item => (JsonNode?)WriteItem(item)).ToArray()),
        FieldType.Object => Write(value),
        _ => ToNode(value),
    };

    private static object? ReadValue(FieldMember member, JsonNode node, Type type, ItemTypeRegistry itemTypes, string path) => member.Field.Type switch
    {
        FieldType.Items => ReadItems(node as JsonArray ?? [], member.Field, type, itemTypes, path),
        FieldType.Object => Read(type, node.AsObject(), itemTypes, path),
        _ => FromNode(node, type),
    };

    private static JsonObject WriteItem(object model)
    {
        if (model is DynamicItem dynamic)
        {
            if (dynamic.Key == Guid.Empty)
            {
                dynamic.Key = Guid.NewGuid();
            }

            var json = EntityValues.NewItem(dynamic.Key, dynamic.Type);
            foreach (var (alias, value) in dynamic.Fields)
            {
                json[alias] = value?.DeepClone();
            }

            return json;
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

        var item = EntityValues.NewItem((Guid)keyProperty.GetValue(model)!, itemType.Alias);
        foreach (var (alias, value) in Write(model))
        {
            item[alias] = value?.DeepClone();
        }

        return item;
    }

    private static IList ReadItems(JsonArray items, FieldDefinition field, Type listType, ItemTypeRegistry itemTypes, string fieldPath)
    {
        var list = (IList)Activator.CreateInstance(listType)!;
        var dynamic = ElementTypeOf(listType) is { } elementType && typeof(DynamicItem).IsAssignableFrom(elementType);
        foreach (var item in items.OfType<JsonObject>())
        {
            var key = EntityValues.KeyOf(item);
            var itemPath = EntityEditor.Join(fieldPath, key.ToString());
            object model;
            if (dynamic)
            {
                var fields = item.Where(p => p.Key is not (EntityValues.KeyProperty or EntityValues.TypeProperty)).ToDictionary(p => p.Key, p => p.Value?.DeepClone());
                model = new DynamicItem { Key = key, Type = EntityValues.TypeOf(item), Fields = fields };
            }
            else
            {
                var itemClass = itemTypes.Find(field.ItemTypeGroup!, EntityValues.TypeOf(item))?.ClrType
                    ?? throw new AbilityInputException($"Item type '{EntityValues.TypeOf(item)}' has no model class.", [new Problem(itemPath, "No model class for this item type.")]);
                model = Read(itemClass, item, itemTypes, itemPath);
                Info(itemClass).Key?.SetValue(model, key);
            }

            if (model is EntityItemModel itemModel)
            {
                itemModel.Path = itemPath;
            }

            Paths.AddOrUpdate(model, itemPath);
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
        foreach (var member in Info(model.GetType()).Fields)
        {
            var fieldPath = EntityEditor.Join(path, member.Field.Alias);
            var value = member.Property.GetValue(model);
            if (!member.Varying)
            {
                var context = new ValidationContext(model) { MemberName = member.Property.Name, DisplayName = member.Field.Alias };
                foreach (var attribute in member.Rules)
                {
                    if (attribute.GetValidationResult(value, context) is { } result && result != ValidationResult.Success)
                    {
                        problems.Add(new Problem(fieldPath, result.ErrorMessage ?? "Invalid."));
                    }
                }
            }

            var values = member.Varying ? ((IDictionary?)value)?.Values.Cast<object?>() ?? [] : [value];
            foreach (var single in values.OfType<object>())
            {
                switch (member.Field.Type)
                {
                    case FieldType.Object:
                        ValidateObject(single, fieldPath, problems);
                        break;

                    // Dynamic items have no class to carry rules: their fields' rules are checked from the structure.
                    case FieldType.Items:
                        foreach (var item in ((IEnumerable)single).Cast<object>().Where(i => i is not DynamicItem))
                        {
                            ValidateObject(item, EntityEditor.Join(fieldPath, KeyOf(item).ToString()), problems);
                        }

                        break;
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
