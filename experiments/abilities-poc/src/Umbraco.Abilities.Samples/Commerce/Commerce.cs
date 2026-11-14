using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

using Umbraco.Abilities.Entities;
using Umbraco.Abilities.Entities.Models;

namespace Umbraco.Abilities.Samples.Commerce;

// ── The models: plain C# classes. Everything the AI and change sets need is derived from them. ─────────────

[Entity("commerce.collection", "Collection", Description = "A product collection.")]
public sealed class Collection
{
    [Key]
    public Guid Key { get; set; }

    [EntityVersion]
    public long? Version { get; set; }

    [EntityName, Required, Description("Collection name.")]
    public string? Name { get; set; }
}

[Entity("commerce.discount", "Discount", Description = "A discount applied when all its rules match.")]
public sealed class Discount
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

    [EntityKey("cms.page"), Description("Optional page that promotes the discount.")]
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
    [Required, EntityKey("commerce.collection"), Description("The collection.")]
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

public sealed class DiscountDefinition(InMemoryDatabase database, ItemTypeRegistry itemTypes)
    : InMemoryModelDefinition<Discount>(database, itemTypes, "/umbraco/section/commerce/discount")
{
    public const string RuleGroup = "commerce.discount.rule";
    public const string RewardGroup = "commerce.discount.reward";

    public static void RegisterItemTypes(ItemTypeRegistry itemTypes) => itemTypes
        .Add<OrderTotalRule>(RuleGroup)
        .Add<CustomerGroupRule>(RuleGroup)
        .Add<ProductCollectionRule>(RuleGroup)
        .Add<PercentageOffReward>(RewardGroup)
        .Add<FreeShippingReward>(RewardGroup);

    protected override Task<IReadOnlyList<Problem>> ValidateModelAsync(Discount discount, CancellationToken cancellationToken)
    {
        var problems = new List<Problem>();
        if (discount.StartDate is { } start && discount.EndDate is { } end && end < start)
        {
            problems.Add(ProblemAt(d => d.EndDate, "Must be on or after startDate."));
        }

        if (CodeInUse(discount))
        {
            problems.Add(ProblemAt(d => d.Code, $"Another discount already uses '{discount.Code}'."));
        }

        return Task.FromResult<IReadOnlyList<Problem>>(problems);
    }

    /// <summary>Codes are unique, as a database constraint would enforce on save.</summary>
    public override Task SaveModelAsync(Discount discount, CancellationToken cancellationToken = default)
        => CodeInUse(discount)
            ? throw new InvalidOperationException($"The discount code '{discount.Code}' is already in use.")
            : base.SaveModelAsync(discount, cancellationToken);

    private bool CodeInUse(Discount discount)
        => discount.Code is { Length: > 0 } code && All().Any(d => d.Key != discount.Key && d.Code == code);
}

// ── A product ability that changes a discount, written against the model. ───────────────────────────────

public sealed record ActivateDiscountInput(
    [property: Description("The discount to switch on."), EntityKey("commerce.discount")] Guid Discount);

/// <summary>Run directly it saves the discount; inside a change set it is recorded as a change to the discount.</summary>
[Ability("commerce.activateDiscount", "Activate discount", Kind = AbilityKind.Write,
    Description = "Switches a discount on so customers get it.")]
public sealed class ActivateDiscountAbility(DiscountDefinition discounts) : ModelEditAbilityBase<ActivateDiscountInput, Discount>(discounts)
{
    protected override Guid KeyOf(ActivateDiscountInput input) => input.Discount;

    protected override void Edit(Discount discount, ActivateDiscountInput input) => discount.Active = true;
}
