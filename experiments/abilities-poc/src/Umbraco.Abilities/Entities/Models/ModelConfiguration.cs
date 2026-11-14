using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;

namespace Umbraco.Abilities.Entities.Models;

/// <summary>What a property is to the framework.</summary>
internal enum PropertyRole
{
    Field,
    Items,
    Key,
    Version,
    Settings,
    Layout,
    Ignored,
}

/// <summary>Everything said about one property, by attributes or fluently. Fluent configuration wins.</summary>
internal sealed class PropertyConfig
{
    public PropertyRole? Role { get; set; }

    public string? Description { get; set; }

    public bool? Required { get; set; }

    public bool IsName { get; set; }

    public IReadOnlyList<string>? Allowed { get; set; }

    public IReadOnlyList<string>? References { get; set; }

    public string? ItemTypeGroup { get; set; }

    public int? Min { get; set; }

    public int? Max { get; set; }

    public Type? Layout { get; set; }

    public List<ValidationAttribute> Rules { get; } = [];
}

/// <summary>Fluent configuration of one class, held until the class is first described.</summary>
internal sealed class TypeConfig
{
    public (string EntityType, string Name, string Description)? Entity { get; set; }

    public (string Alias, string Name, string Description)? ItemType { get; set; }

    public Dictionary<string, PropertyConfig> Properties { get; } = new(StringComparer.Ordinal);

    public PropertyConfig For(PropertyInfo property)
    {
        if (!Properties.TryGetValue(property.Name, out var config))
        {
            config = new PropertyConfig();
            Properties[property.Name] = config;
        }

        return config;
    }
}

/// <summary>
/// Fluent configuration, for classes a package can't or won't put attributes on: a Management API request model, a class
/// in another assembly. The framework treats it and the attributes alike. It covers what request models have needed so
/// far; add an option when a model needs it. Properties nobody configures are fields, named in camelCase.
/// </summary>
public static class ModelConfiguration
{
    private static readonly ConcurrentDictionary<Type, TypeConfig> Configs = new();

    /// <summary>Configures a class. Configure it before it is first described; configuring again replaces the configuration.</summary>
    public static void Configure<T>(Action<ModelBuilder<T>> configure)
        where T : class
    {
        var config = new TypeConfig();
        configure(new ModelBuilder<T>(config));
        Configs[typeof(T)] = config;
        ModelShapes.Forget(typeof(T));
    }

    internal static TypeConfig? Find(Type type) => Configs.GetValueOrDefault(type);
}

/// <summary>Configures a model class: its role as an entity or item type, and its properties.</summary>
public sealed class ModelBuilder<T>
    where T : class
{
    private readonly TypeConfig _config;

    internal ModelBuilder(TypeConfig config) => _config = config;

    /// <summary>The class is an entity type's model.</summary>
    public ModelBuilder<T> IsEntity(string entityType, string name, string description = "")
    {
        _config.Entity = (entityType, name, description);
        return this;
    }

    /// <summary>The class is an item type. Register it with <see cref="ItemTypeRegistry.Add{T}"/> under a group.</summary>
    public ModelBuilder<T> IsItemType(string alias, string name, string description = "")
    {
        _config.ItemType = (alias, name, description);
        return this;
    }

    /// <summary>The Guid property that is the entity's or item's key.</summary>
    public ModelBuilder<T> HasKey(Expression<Func<T, Guid>> property) => Role(property, PropertyRole.Key);

    /// <summary>The property holding the stored version, for rebasing.</summary>
    public ModelBuilder<T> HasVersion<TValue>(Expression<Func<T, TValue>> property) => Role(property, PropertyRole.Version);

    /// <summary>Leaves a property out of the structure.</summary>
    public ModelBuilder<T> Ignore<TValue>(Expression<Func<T, TValue>> property) => Role(property, PropertyRole.Ignored);

    /// <summary>A field.</summary>
    public FieldBuilder<T> Field<TValue>(Expression<Func<T, TValue>> property)
    {
        var config = _config.For(PropertyOf(property));
        config.Role = PropertyRole.Field;
        return new FieldBuilder<T>(this, config);
    }

    /// <summary>An items field: a list property holding items of the types registered for <paramref name="itemTypeGroup"/>.</summary>
    public ItemsBuilder<T> Items<TValue>(Expression<Func<T, TValue>> property, string itemTypeGroup)
    {
        var config = _config.For(PropertyOf(property));
        config.Role = PropertyRole.Items;
        config.ItemTypeGroup = itemTypeGroup;
        return new ItemsBuilder<T>(this, config);
    }

    private ModelBuilder<T> Role(LambdaExpression property, PropertyRole role)
    {
        _config.For(PropertyOf(property)).Role = role;
        return this;
    }

    internal static PropertyInfo PropertyOf(LambdaExpression expression)
        => (expression.Body is UnaryExpression { Operand: MemberExpression unary } ? unary.Member : (expression.Body as MemberExpression)?.Member) as PropertyInfo
            ?? throw new ArgumentException("Pass a property, e.g. x => x.Name.", nameof(expression));
}

/// <summary>Configures a field. Call <see cref="ModelBuilder{T}"/> methods from it to go on to the next property.</summary>
public sealed class FieldBuilder<T>
    where T : class
{
    private readonly ModelBuilder<T> _model;
    private readonly PropertyConfig _config;

    internal FieldBuilder(ModelBuilder<T> model, PropertyConfig config) => (_model, _config) = (model, config);

    public FieldBuilder<T> Description(string description) => Set(() => _config.Description = description);

    public FieldBuilder<T> Required() => Set(() => _config.Required = true);

    /// <summary>The field holds the entity's display name.</summary>
    public FieldBuilder<T> IsName() => Set(() => _config.IsName = true);

    /// <summary>A DataAnnotations rule, e.g. <c>new RangeAttribute(1, 100)</c>, reported on the field's path.</summary>
    public FieldBuilder<T> Rule(ValidationAttribute rule) => Set(() => _config.Rules.Add(rule));

    public FieldBuilder<T> Field<TValue>(Expression<Func<T, TValue>> property) => _model.Field(property);

    public ItemsBuilder<T> Items<TValue>(Expression<Func<T, TValue>> property, string itemTypeGroup) => _model.Items(property, itemTypeGroup);

    private FieldBuilder<T> Set(Action set)
    {
        set();
        return this;
    }
}

/// <summary>Configures an items field.</summary>
public sealed class ItemsBuilder<T>
    where T : class
{
    private readonly ModelBuilder<T> _model;
    private readonly PropertyConfig _config;

    internal ItemsBuilder(ModelBuilder<T> model, PropertyConfig config) => (_model, _config) = (model, config);

    public ItemsBuilder<T> Description(string description) => Set(() => _config.Description = description);

    public FieldBuilder<T> Field<TValue>(Expression<Func<T, TValue>> property) => _model.Field(property);

    public ItemsBuilder<T> Items<TValue>(Expression<Func<T, TValue>> property, string itemTypeGroup) => _model.Items(property, itemTypeGroup);

    private ItemsBuilder<T> Set(Action set)
    {
        set();
        return this;
    }
}
