using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Umbraco.Abilities.Samples.Commerce.Domain;

// A stand-in for Commerce's discount rule and reward providers, shaped like the real ones in Umbraco.Commerce.Core:
// a provider attribute whose label defaults to a localization key, a typed settings class, and setting definitions
// discovered from it (key, label, description, CLR type, editor UI alias, sort order, DataAnnotations rules). Providers
// are registered in a collection. Nothing here knows about abilities.

[AttributeUsage(AttributeTargets.Class)]
public sealed class DiscountRuleProviderAttribute(string alias) : Attribute
{
    public string Alias { get; } = alias;

    public string Label { get; set; } = $"#ucDiscountRuleProviders_{alias}Label";

    public string Description { get; set; } = $"#ucDiscountRuleProviders_{alias}Description";
}

[AttributeUsage(AttributeTargets.Class)]
public sealed class DiscountRewardProviderAttribute(string alias) : Attribute
{
    public string Alias { get; } = alias;

    public string Label { get; set; } = $"#ucDiscountRewardProviders_{alias}Label";

    public string Description { get; set; } = $"#ucDiscountRewardProviders_{alias}Description";
}

/// <summary>Like Commerce's <c>UmbracoCommerceSettingAttribute</c>.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class ProviderSettingAttribute : Attribute
{
    public string? Key { get; set; }

    public string? Label { get; set; }

    public string? Description { get; set; }

    public string? EditorUiAlias { get; set; }

    public int SortOrder { get; set; }
}

/// <summary>Like Commerce's <c>UmbracoCommerceSettingDefinition</c>.</summary>
public sealed record ProviderSettingDefinition(
    string Key,
    string? Label,
    string? Description,
    Type PropertyType,
    string? EditorUiAlias,
    int SortOrder,
    IReadOnlyList<ValidationAttribute> ValidationRules);

public abstract class DiscountProviderBase
{
    public abstract string Alias { get; }

    public abstract string Label { get; }

    public abstract string Description { get; }

    public abstract IReadOnlyList<ProviderSettingDefinition> SettingDefinitions { get; }

    /// <summary>A group provider holds child rules (Commerce's <c>groupDiscountRule</c>).</summary>
    public virtual bool HasChildren => false;
}

/// <summary>A provider with a typed settings class, from which its setting definitions are discovered.</summary>
public abstract class DiscountProviderBase<TSettings> : DiscountProviderBase
{
    private IReadOnlyList<ProviderSettingDefinition>? _settings;

    public override IReadOnlyList<ProviderSettingDefinition> SettingDefinitions => _settings ??= typeof(TSettings).GetProperties()
        .Select(p => (Property: p, Setting: p.GetCustomAttribute<ProviderSettingAttribute>()))
        .Where(p => p.Setting is not null)
        .Select(p => new ProviderSettingDefinition(
            p.Setting!.Key ?? char.ToLowerInvariant(p.Property.Name[0]) + p.Property.Name[1..],
            p.Setting.Label,
            p.Setting.Description,
            p.Property.PropertyType,
            p.Setting.EditorUiAlias,
            p.Setting.SortOrder,
            p.Property.GetCustomAttributes<ValidationAttribute>().ToList()))
        .OrderBy(s => s.SortOrder)
        .ToList();
}

public abstract class DiscountRuleProviderBase<TSettings> : DiscountProviderBase<TSettings>
{
    private DiscountRuleProviderAttribute Attribute => GetType().GetCustomAttribute<DiscountRuleProviderAttribute>()!;

    public override string Alias => Attribute.Alias;

    public override string Label => Attribute.Label;

    public override string Description => Attribute.Description;
}

public abstract class DiscountRewardProviderBase<TSettings> : DiscountProviderBase<TSettings>
{
    private DiscountRewardProviderAttribute Attribute => GetType().GetCustomAttribute<DiscountRewardProviderAttribute>()!;

    public override string Alias => Attribute.Alias;

    public override string Label => Attribute.Label;

    public override string Description => Attribute.Description;
}

/// <summary>Registered providers, as Commerce's collection builders hold them. Packages add theirs.</summary>
public sealed class DiscountProviderCollection : List<DiscountProviderBase>
{
    public DiscountProviderBase? Find(string alias) => this.FirstOrDefault(p => p.Alias == alias);
}

// ── Commerce's own providers ────────────────────────────────────────────────────────────────────────

public sealed class OrderTotalSettings
{
    [ProviderSetting(Label = "Minimum order total", Description = "In the store's currency.")]
    [Required]
    public decimal? MinTotal { get; set; }
}

/// <summary>Leaves its label as the localization key default, as many providers do.</summary>
[DiscountRuleProvider("orderTotal")]
public sealed class OrderTotalRuleProvider : DiscountRuleProviderBase<OrderTotalSettings>;

public sealed class CustomerGroupSettings
{
    [ProviderSetting(Label = "Customer group")]
    [Required]
    [AllowedValues("VIP", "Wholesale", "Retail")]
    public string? Group { get; set; }
}

[DiscountRuleProvider("customerGroup", Label = "Customer group", Description = "The customer belongs to a group.")]
public sealed class CustomerGroupRuleProvider : DiscountRuleProviderBase<CustomerGroupSettings>;

public sealed class ProductCollectionSettings
{
    [ProviderSetting(Key = "collection", Label = "Collection", EditorUiAlias = "Uc.PropertyEditorUi.CollectionPicker")]
    [Required]
    public string? CollectionId { get; set; }
}

[DiscountRuleProvider("productCollection", Label = "Product collection", Description = "The order contains a product from a collection.")]
public sealed class ProductCollectionRuleProvider : DiscountRuleProviderBase<ProductCollectionSettings>;

public sealed class GroupRuleSettings
{
    [ProviderSetting(Label = "Match", Description = "Whether all child rules or any of them must match.")]
    [Required]
    [AllowedValues("all", "any")]
    public string? Match { get; set; }
}

[DiscountRuleProvider("groupDiscountRule", Label = "Group", Description = "A group of rules, all or any of which must match.")]
public sealed class GroupRuleProvider : DiscountRuleProviderBase<GroupRuleSettings>
{
    public override bool HasChildren => true;
}

public sealed class PercentageOffSettings
{
    [ProviderSetting(Label = "Percentage")]
    [Required]
    [Range(1, 100)]
    public decimal? Percent { get; set; }
}

[DiscountRewardProvider("percentageOff", Label = "Percentage off", Description = "A percentage off the order.")]
public sealed class PercentageOffRewardProvider : DiscountRewardProviderBase<PercentageOffSettings>;

public sealed class NoSettings;

[DiscountRewardProvider("freeShipping", Label = "Free shipping", Description = "Shipping is free.")]
public sealed class FreeShippingRewardProvider : DiscountRewardProviderBase<NoSettings>;
