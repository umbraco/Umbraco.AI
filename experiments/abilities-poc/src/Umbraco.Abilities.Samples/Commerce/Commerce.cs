using System.ComponentModel;
using System.Text.Json.Nodes;

using Umbraco.Abilities.Entities;
using Umbraco.Abilities.Samples.Cms;

namespace Umbraco.Abilities.Samples.Commerce;

/// <summary>Product collections: found and referenced, not created here.</summary>
public sealed class CollectionDefinition(InMemoryDatabase database)
    : InMemoryEntityDefinition(database, "commerce.collection", "name", "/umbraco/section/commerce/collection")
{
    public override bool CanCreate => false;

    public Guid Seed(string name)
    {
        var key = Guid.NewGuid();
        var root = Element.New();
        Element.Fields(root)["name"] = name;
        Database.Put(EntityType, key, root);
        return key;
    }

    public override Task<EntityStructure> DescribeAsync(string? subtype, CancellationToken cancellationToken = default)
        => Task.FromResult(new EntityStructure(EntityType, "Collection", "A product collection.", ElementShape.Of(
            new FieldDefinition("name", "Collection name.", FieldType.String, Required: true))));
}

/// <summary>
/// A discount: a few fields plus two slots of typed items. Rule and reward types come from the registry,
/// so other packages can add their own (see Acme).
/// </summary>
public sealed class DiscountDefinition(InMemoryDatabase database)
    : InMemoryEntityDefinition(database, "commerce.discount", "name", "/umbraco/section/commerce/discount")
{
    public const string RuleGroup = "commerce.discount.rule";
    public const string RewardGroup = "commerce.discount.reward";

    public static void RegisterItemTypes(ItemTypeRegistry itemTypes)
    {
        itemTypes.Add(new ItemTypeDefinition(RuleGroup, "orderTotal", "Order total", "The order total is at least an amount.", ElementShape.Of(
            new FieldDefinition("minTotal", "Minimum order total, in the store's currency.", FieldType.Number, Required: true))));

        itemTypes.Add(new ItemTypeDefinition(RuleGroup, "customerGroup", "Customer group", "The customer belongs to a group.", ElementShape.Of(
            new FieldDefinition("group", "Customer group.", FieldType.String, Required: true) { Allowed = ["VIP", "Wholesale", "Retail"] })));

        itemTypes.Add(new ItemTypeDefinition(RuleGroup, "productCollection", "Product collection", "The order contains a product from a collection.", ElementShape.Of(
            new FieldDefinition("collection", "The collection.", FieldType.EntityReference, Required: true) { ReferencesEntityType = "commerce.collection" })));

        itemTypes.Add(new ItemTypeDefinition(RewardGroup, "percentageOff", "Percentage off", "A percentage off the order.", ElementShape.Of(
            new FieldDefinition("percent", "Percentage, 1 to 100.", FieldType.Number, Required: true))));

        itemTypes.Add(new ItemTypeDefinition(RewardGroup, "freeShipping", "Free shipping", "Shipping is free.", ElementShape.Of()));
    }

    public override Task<EntityStructure> DescribeAsync(string? subtype, CancellationToken cancellationToken = default)
        => Task.FromResult(new EntityStructure(EntityType, "Discount", "A discount applied when all its rules match.", ElementShape
            .Of(
                new FieldDefinition("name", "Name shown to editors.", FieldType.String, Required: true),
                new FieldDefinition("code", "Optional coupon code. Unique.", FieldType.String),
                new FieldDefinition("startDate", "First day the discount applies.", FieldType.Date),
                new FieldDefinition("endDate", "Last day the discount applies.", FieldType.Date),
                new FieldDefinition("landingPage", "Optional page that promotes the discount.", FieldType.EntityReference) { ReferencesEntityType = "cms.page" },
                new FieldDefinition("active", "Whether the discount is live. Use commerce.activateDiscount to switch it on.", FieldType.Boolean))
            .WithSlots(
                new SlotDefinition("rules", "All rules must match for the discount to apply.", RuleGroup) { Min = 1 },
                new SlotDefinition("rewards", "What the customer gets.", RewardGroup) { Min = 1, Max = 3 })));

    public override Task<IReadOnlyList<Problem>> ValidateAsync(EntityDocument document, CancellationToken cancellationToken = default)
    {
        var problems = new List<Problem>();

        if (DateOnly.TryParse(Text(document.Root, "startDate"), out var start)
            && DateOnly.TryParse(Text(document.Root, "endDate"), out var end)
            && end < start)
        {
            problems.Add(new Problem("endDate", "Must be on or after startDate."));
        }

        foreach (var reward in Element.Slot(document.Root, "rewards").OfType<JsonObject>().Where(r => Element.ItemType(r) == "percentageOff"))
        {
            if (Element.Fields(reward)["percent"] is JsonValue value
                && value.TryGetValue<double>(out var percent)
                && percent is < 1 or > 100)
            {
                problems.Add(new Problem($"rewards/{Element.ItemKey(reward)}/percent", "Must be between 1 and 100."));
            }
        }

        if (Text(document.Root, "code") is { Length: > 0 } code
            && Database.All(EntityType).Any(r => r.Key != document.Key && Element.Fields(r.Row.Root)["code"]?.GetValue<string>() == code))
        {
            problems.Add(new Problem("code", $"Another discount already uses '{code}'."));
        }

        return Task.FromResult<IReadOnlyList<Problem>>(problems);
    }
}

public sealed record ActivateDiscountInput(
    [property: Description("The discount to switch on."), EntityKey("commerce.discount")] Guid Discount);

/// <summary>A plain operation ability. Inside a draft set it is recorded and runs when the set is applied.</summary>
[Ability("commerce.activateDiscount", "Activate discount", Kind = AbilityKind.Write,
    Description = "Switches a discount on so customers get it.")]
public sealed class ActivateDiscountAbility(DiscountDefinition discounts) : AbilityBase<ActivateDiscountInput, EntityRef>
{
    protected override async Task<AbilityResult> ExecuteAsync(ActivateDiscountInput input, AbilityContext context, CancellationToken cancellationToken)
    {
        var discount = await discounts.LoadAsync(input.Discount, cancellationToken)
            ?? throw new AbilityInputException($"No discount {input.Discount}.", [new Problem("discount", "Not found.")]);

        Element.Fields(discount.Root)["active"] = true;
        return Success(await discounts.SaveAsync(discount, cancellationToken));
    }
}
