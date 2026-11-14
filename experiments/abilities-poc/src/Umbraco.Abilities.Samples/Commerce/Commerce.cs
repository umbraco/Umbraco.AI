using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

using Umbraco.Abilities.Entities;
using Umbraco.Abilities.Entities.Models;
using Umbraco.Abilities.Samples.Commerce.Domain;

namespace Umbraco.Abilities.Samples.Commerce;

// ── The editing contracts: plain C# classes, separate from Commerce's domain model (Domain.cs). Everything the
// AI and change sets need is derived from them; the definition maps them to and from the domain. ─────────────

[Entity("umbraco-commerce-collection", "Collection", Description = "A product collection.")]
public sealed class Collection
{
    [Key]
    public Guid Key { get; set; }

    [EntityVersion]
    public long? Version { get; set; }

    [EntityName, Required, Description("Collection name.")]
    public string? Name { get; set; }
}

[Entity("umbraco-commerce-discount", "Discount", Description = "A discount applied when all its rules match.")]
public sealed class DiscountEditModel
{
    [Key]
    public Guid Key { get; set; }

    [EntityVersion]
    public long? Version { get; set; }

    [EntityName, Required, Description("Name shown to editors.")]
    public string? Name { get; set; }

    [Description("Optional coupon code. Unique.")]
    public string? Code { get; set; }

    [Description("First day the discount applies.")]
    public DateOnly? StartDate { get; set; }

    [Description("Last day the discount applies.")]
    public DateOnly? EndDate { get; set; }

    [EntityKey("document"), Description("Optional page that promotes the discount.")]
    public Guid? LandingPage { get; set; }

    [Description("Whether the discount is live. Use commerce.activateDiscount to switch it on.")]
    public bool? Active { get; set; }

    [Slot(DiscountDefinition.RuleGroup, Min = 1), Description("All rules must match for the discount to apply.")]
    public List<DiscountRule> Rules { get; set; } = [];

    [Slot(DiscountDefinition.RewardGroup, Min = 1, Max = 3), Description("What the customer gets.")]
    public List<DiscountReward> Rewards { get; set; } = [];
}

/// <summary>A discount rule. Other packages add rule types by subclassing this and registering the class.</summary>
public abstract class DiscountRule : EntityItemModel;

[ItemType("orderTotal", "Order total", Description = "The order total is at least an amount.")]
public sealed class OrderTotalRule : DiscountRule
{
    [Required, Description("Minimum order total, in the store's currency.")]
    public decimal? MinTotal { get; set; }
}

[ItemType("customerGroup", "Customer group", Description = "The customer belongs to a group.")]
public sealed class CustomerGroupRule : DiscountRule
{
    [Required, AllowedValues("VIP", "Wholesale", "Retail"), Description("Customer group.")]
    public string? Group { get; set; }
}

[ItemType("productCollection", "Product collection", Description = "The order contains a product from a collection.")]
public sealed class ProductCollectionRule : DiscountRule
{
    [Required, EntityKey("umbraco-commerce-collection"), Description("The collection.")]
    public Guid? Collection { get; set; }
}

public abstract class DiscountReward : EntityItemModel;

[ItemType("percentageOff", "Percentage off", Description = "A percentage off the order.")]
public sealed class PercentageOffReward : DiscountReward
{
    [Required, Range(1, 100), Description("Percentage, 1 to 100.")]
    public decimal? Percent { get; set; }
}

[ItemType("freeShipping", "Free shipping", Description = "Shipping is free.")]
public sealed class FreeShippingReward : DiscountReward;

// ── The definitions: load, save, and the rules attributes can't express. ─────────────────────────────────

public sealed class CollectionDefinition(InMemoryDatabase database, ItemTypeRegistry itemTypes)
    : InMemoryModelDefinition<Collection>(database, itemTypes, "/umbraco/section/commerce/collection")
{
    public override bool CanCreate => false;

    public Guid Seed(string name)
    {
        var collection = new Collection { Key = Guid.NewGuid(), Name = name };
        SaveModelAsync(collection).GetAwaiter().GetResult();
        return collection.Key;
    }
}

/// <summary>
/// Maps Commerce's own discount model to and from its editing contract. This mapping is the part a package writes by
/// hand; the structure, schemas, paths, validation and change sets come from <see cref="DiscountEditModel"/>.
/// </summary>
public sealed class DiscountDefinition(DiscountService discounts, InMemoryDatabase unitOfWork, ItemTypeRegistry itemTypes)
    : EntityDefinitionBase<DiscountEditModel>(itemTypes, unitOfWork)
{
    public const string RuleGroup = "commerce.discount.rule";
    public const string RewardGroup = "commerce.discount.reward";

    public static void RegisterItemTypes(ItemTypeRegistry itemTypes) => itemTypes
        .Add<OrderTotalRule>(RuleGroup)
        .Add<CustomerGroupRule>(RuleGroup)
        .Add<ProductCollectionRule>(RuleGroup)
        .Add<PercentageOffReward>(RewardGroup)
        .Add<FreeShippingReward>(RewardGroup);

    public override Task<DiscountEditModel?> LoadModelAsync(Guid key, CancellationToken cancellationToken = default)
        => Task.FromResult(discounts.GetDiscount(key) is { } discount ? ToEditModel(discount) : null);

    public override Task SaveModelAsync(DiscountEditModel model, CancellationToken cancellationToken = default)
    {
        var discount = discounts.GetDiscount(model.Key)?.AsWritable() ?? discounts.CreateDiscount(model.Key, model.Name!);
        discount
            .SetName(model.Name!)
            .SetCode(model.Code)
            .SetDates(ToUtc(model.StartDate), ToUtc(model.EndDate))
            .SetActive(model.Active ?? false)
            .SetLandingPage(model.LandingPage)
            .SetRules(model.Rules.Select(r => new DiscountRuleConfig(ProviderSettings.AliasOf(r), ProviderSettings.From(r))))
            .SetRewards(model.Rewards.Select(r => new DiscountRewardConfig(ProviderSettings.AliasOf(r), ProviderSettings.From(r))));
        discounts.SaveDiscount(discount);
        return Task.CompletedTask;
    }

    protected override Task<IReadOnlyList<DiscountEditModel>> FindModelsAsync(string query, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<DiscountEditModel>>(discounts.GetDiscounts()
            .Where(d => d.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Select(ToEditModel)
            .ToList());

    protected override Task<IReadOnlyList<Problem>> ValidateModelAsync(DiscountEditModel discount, CancellationToken cancellationToken)
    {
        var problems = new List<Problem>();
        if (discount.StartDate is { } start && discount.EndDate is { } end && end < start)
        {
            problems.Add(ProblemAt(d => d.EndDate, "Must be on or after startDate."));
        }

        if (discount.Code is { Length: > 0 } code && discounts.GetDiscounts().Any(d => d.Id != discount.Key && d.Code == code))
        {
            problems.Add(ProblemAt(d => d.Code, $"Another discount already uses '{code}'."));
        }

        return Task.FromResult<IReadOnlyList<Problem>>(problems);
    }

    protected override string? GetEditUrl(Guid key) => $"/umbraco/section/commerce/discount/{key}";

    private DiscountEditModel ToEditModel(DiscountReadOnly discount) => new()
    {
        Key = discount.Id,
        Version = discount.Version,
        Name = discount.Name,
        Code = discount.Code,
        StartDate = ToDate(discount.StartDate),
        EndDate = ToDate(discount.ExpiryDate),
        LandingPage = discount.LandingPageId,
        Active = discount.IsActive,
        Rules = discount.Rules.Select((r, i) => ProviderSettings.To<DiscountRule>(ItemTypes, RuleGroup, r.RuleProviderAlias, r.Settings, DerivedKey(discount.Id, i, r.RuleProviderAlias))).ToList(),
        Rewards = discount.Rewards.Select((r, i) => ProviderSettings.To<DiscountReward>(ItemTypes, RewardGroup, r.RewardProviderAlias, r.Settings, DerivedKey(discount.Id, i, r.RewardProviderAlias))).ToList(),
    };

    private static DateOnly? ToDate(DateTime? utc) => utc is { } value ? DateOnly.FromDateTime(value) : null;

    private static DateTime? ToUtc(DateOnly? date) => date?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

    /// <summary>
    /// Commerce's rules and rewards have no ids, so an item's key is derived from its discount, position and provider.
    /// The same rule gets the same key on every load, but a new item's key changes when it is saved, and moving an item
    /// changes its key. Stable ids on rule nodes in Commerce would fix both.
    /// </summary>
    private static Guid DerivedKey(Guid discount, int index, string alias)
        => new(MD5.HashData(Encoding.UTF8.GetBytes($"{discount:N}/{index}/{alias}")));
}

/// <summary>
/// Maps a rule or reward edit model to and from Commerce's provider alias plus string settings. Written once for all
/// of Commerce's provider-based items, including third parties'.
/// </summary>
internal static class ProviderSettings
{
    public static string AliasOf(object item) => item.GetType().GetCustomAttribute<ItemTypeAttribute>()!.Alias;

    public static Dictionary<string, string> From(object item)
    {
        var settings = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var property in Properties(item.GetType()))
        {
            if (property.GetValue(item) is { } value)
            {
                var node = JsonSerializer.SerializeToNode(value, property.PropertyType, AbilityJson.Options)!;
                settings[property.Name] = node is JsonValue scalar && scalar.TryGetValue<string>(out var text) ? text : node.ToJsonString();
            }
        }

        return settings;
    }

    public static T To<T>(ItemTypeRegistry itemTypes, string group, string alias, IReadOnlyDictionary<string, string> settings, Guid key)
        where T : EntityItemModel
    {
        var type = itemTypes.Find(group, alias)?.ClrType ?? throw new InvalidOperationException($"No item type '{alias}' in '{group}'.");
        var item = (T)Activator.CreateInstance(type)!;
        item.Key = key;
        foreach (var property in Properties(type))
        {
            if (settings.TryGetValue(property.Name, out var text))
            {
                var target = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                var node = target.IsPrimitive || target == typeof(decimal) ? JsonNode.Parse(text) : JsonValue.Create(text);
                property.SetValue(item, node.Deserialize(property.PropertyType, AbilityJson.Options));
            }
        }

        return item;
    }

    private static IEnumerable<PropertyInfo> Properties(Type type)
        => type.GetProperties().Where(p => p.CanWrite && p.DeclaringType != typeof(EntityItemModel));
}

// ── A product ability that changes a discount, written against the model. ───────────────────────────────

public sealed record ActivateDiscountInput(
    [property: Description("The discount to switch on."), EntityKey("umbraco-commerce-discount")] Guid Discount);

/// <summary>Run directly it saves the discount; inside a change set it is recorded as a change to the discount.</summary>
[Ability("commerce.activateDiscount", "Activate discount", Kind = AbilityKind.Write,
    Description = "Switches a discount on so customers get it.")]
public sealed class ActivateDiscountAbility(DiscountDefinition discounts) : ModelEditAbilityBase<ActivateDiscountInput, DiscountEditModel>(discounts)
{
    protected override Guid KeyOf(ActivateDiscountInput input) => input.Discount;

    protected override void Edit(DiscountEditModel discount, ActivateDiscountInput input) => discount.Active = true;
}
