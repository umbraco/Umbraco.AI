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

    [Items(DiscountDefinition.RuleGroup, Min = 1), Description("All rules must match for the discount to apply.")]
    public List<DynamicItem> Rules { get; set; } = [];

    [Items(DiscountDefinition.RewardGroup, Min = 1, Max = 3), Description("What the customer gets.")]
    public List<DynamicItem> Rewards { get; set; } = [];
}

// Rules and rewards are a plugin seam: Commerce and other packages register providers (Providers.cs), so their item
// types come from the provider collections at run time (DiscountProviderItemTypes below), not from classes here.

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
public sealed class DiscountDefinition(
    DiscountService discounts,
    DiscountProviderCollection ruleProviders,
    DiscountProviderCollection rewardProviders,
    InMemoryDatabase unitOfWork,
    ItemTypeRegistry itemTypes)
    : EntityDefinitionBase<DiscountEditModel>(itemTypes, unitOfWork), IAuthorizingEntityDefinition
{
    /// <summary>Real Commerce would check the Commerce section and the user's access to the discount's store.</summary>
    public SampleAccess? Access { get; set; }

    public Task<string?> AuthorizeAsync(EntityAccessRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult(Access?.Authorize(EntityType, request));

    public const string RuleGroup = "commerce.discount.rule";
    public const string RewardGroup = "commerce.discount.reward";

    /// <summary>One source per seam. Providers registered later, by any package, show up with no change here.</summary>
    public static void RegisterItemTypes(ItemTypeRegistry itemTypes, DiscountProviderCollection rules, DiscountProviderCollection rewards, Func<Guid, bool> availableTo) => itemTypes
        .AddSource(new DiscountProviderItemTypes(RuleGroup, rules, availableTo))
        .AddSource(new DiscountProviderItemTypes(RewardGroup, rewards, availableTo));

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
            .SetRules(model.Rules.Select(ProviderItems.ToRuleConfig))
            .SetRewards(model.Rewards.Select(r => new DiscountRewardConfig(r.Type, ProviderItems.Settings(r))));
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
        Rules = discount.Rules.Select((r, i) => ProviderItems.ToItem(ruleProviders, r, DerivedKey(discount.Id, i, r.RuleProviderAlias))).ToList(),
        Rewards = discount.Rewards.Select((r, i) => ProviderItems.ToItem(rewardProviders, r.RewardProviderAlias, r.Settings, DerivedKey(discount.Id, i, r.RewardProviderAlias))).ToList(),
    };

    private static DateOnly? ToDate(DateTime? utc) => utc is { } value ? DateOnly.FromDateTime(value) : null;

    private static DateTime? ToUtc(DateOnly? date) => date?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

    /// <summary>
    /// Commerce's rules and rewards have no ids, so an item's key is derived from its discount, position and provider.
    /// The same rule gets the same key on every load, but a new item's key changes when it is saved, and moving an item
    /// changes its key. Stable ids on rule nodes in Commerce would fix both.
    /// </summary>
    internal static Guid DerivedKey(Guid parent, int index, string alias)
        => new(MD5.HashData(Encoding.UTF8.GetBytes($"{parent:N}/{index}/{alias}")));
}

/// <summary>
/// The item types of one provider seam (rules or rewards), read from Commerce's provider collection whenever they're
/// needed. This is all Commerce writes per seam: providers, including third parties', need no Abilities code.
/// </summary>
public sealed class DiscountProviderItemTypes(string group, DiscountProviderCollection providers, Func<Guid, bool> availableTo) : IItemTypeSource
{
    /// <summary>Commerce's table from an editor UI alias to what it picks. Editors not listed stay plain values.</summary>
    public static readonly IReadOnlyDictionary<string, string[]> Editors = new Dictionary<string, string[]>
    {
        ["Uc.PropertyEditorUi.CollectionPicker"] = ["umbraco-commerce-collection"],
    };

    public string Group => group;

    public IEnumerable<ItemTypeDefinition> GetItemTypes() => providers.Select(provider =>
    {
        var fields = provider.SettingDefinitions
            .Select(s => ItemTypeFields.For(new SettingDescriptor(s.Key, s.Label, s.Description, s.PropertyType, s.ValidationRules, s.EditorUiAlias, s.SortOrder), Editors))
            .ToList();
        if (provider.HasChildren)
        {
            fields.Add(FieldDefinition.ItemsOf("children", "The rules in this group.", group) with { Min = 1 });
        }

        return new ItemTypeDefinition(
            group,
            provider.Alias,
            ItemTypeFields.Text(provider.Label) ?? provider.Alias,
            ItemTypeFields.Text(provider.Description)
                ?? $"Commerce gives this provider no description, only a localization key. Describe it in plain words from its alias, '{provider.Alias}'.",
            new ElementShape(fields))
        {
            AvailableTo = availableTo,
        };
    });
}

/// <summary>
/// Maps rules and rewards between their items and Commerce's provider alias plus string settings, using each provider's
/// setting types. Written once for every provider-based item, including third parties'.
/// </summary>
internal static class ProviderItems
{
    public static DynamicItem ToItem(DiscountProviderCollection providers, DiscountRuleConfig rule, Guid key)
    {
        var item = ToItem(providers, rule.RuleProviderAlias, rule.Settings, key);
        if (rule.Children is { Count: > 0 } children)
        {
            item.Items["children"] = children.Select((c, i) => ToItem(providers, c, DiscountDefinition.DerivedKey(key, i, c.RuleProviderAlias))).ToList();
        }

        return item;
    }

    public static DynamicItem ToItem(DiscountProviderCollection providers, string alias, IReadOnlyDictionary<string, string> settings, Guid key)
    {
        var definitions = providers.Find(alias)?.SettingDefinitions ?? [];
        var item = new DynamicItem { Key = key, Type = alias };
        foreach (var (settingKey, text) in settings)
        {
            var type = definitions.FirstOrDefault(d => d.Key == settingKey)?.PropertyType ?? typeof(string);
            var target = Nullable.GetUnderlyingType(type) ?? type;
            item.Fields[settingKey] = target.IsPrimitive || target == typeof(decimal) ? JsonNode.Parse(text) : JsonValue.Create(text);
        }

        return item;
    }

    public static DiscountRuleConfig ToRuleConfig(DynamicItem item)
        => new(item.Type, Settings(item), item.Items.GetValueOrDefault("children")?.Select(ToRuleConfig).ToList());

    public static Dictionary<string, string> Settings(DynamicItem item)
        => item.Fields
            .Where(f => f.Value is not null)
            .ToDictionary(f => f.Key, f => f.Value is JsonValue scalar && scalar.TryGetValue<string>(out var text) ? text : f.Value!.ToJsonString(), StringComparer.Ordinal);
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
