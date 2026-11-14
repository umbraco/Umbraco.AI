using System.Text.Json;
using System.Text.Json.Nodes;

namespace Umbraco.Abilities.Samples.Commerce.Domain;

// A stand-in for Commerce's own discount model, shaped like the real one: an immutable read-only entity with internal
// constructors, a writable copy from AsWritable, UTC dates, and rules and rewards stored as a provider alias plus
// string settings, with no ids of their own. Nothing here knows about abilities; the definition maps to it.

public sealed record DiscountRuleConfig(string RuleProviderAlias, IReadOnlyDictionary<string, string> Settings);

public sealed record DiscountRewardConfig(string RewardProviderAlias, IReadOnlyDictionary<string, string> Settings);

internal sealed record DiscountState(
    Guid Id,
    string Name,
    string? Code,
    DateTime? StartDate,
    DateTime? ExpiryDate,
    bool IsActive,
    Guid? LandingPageId,
    IReadOnlyList<DiscountRuleConfig> Rules,
    IReadOnlyList<DiscountRewardConfig> Rewards);

public class DiscountReadOnly
{
    internal DiscountReadOnly(DiscountState state, long version) => (State, Version) = (state, version);

    internal DiscountState State { get; private protected set; }

    public Guid Id => State.Id;

    public string Name => State.Name;

    public string? Code => State.Code;

    public DateTime? StartDate => State.StartDate;

    public DateTime? ExpiryDate => State.ExpiryDate;

    public bool IsActive => State.IsActive;

    public Guid? LandingPageId => State.LandingPageId;

    public IReadOnlyList<DiscountRuleConfig> Rules => State.Rules;

    public IReadOnlyList<DiscountRewardConfig> Rewards => State.Rewards;

    public long Version { get; }

    public Discount AsWritable() => new(State, Version);
}

public sealed class Discount : DiscountReadOnly
{
    internal Discount(DiscountState state, long version)
        : base(state, version)
    {
    }

    public Discount SetName(string name) => Set(State with { Name = name });

    public Discount SetCode(string? code) => Set(State with { Code = code });

    public Discount SetDates(DateTime? startDate, DateTime? expiryDate)
        => expiryDate < startDate
            ? throw new ArgumentException("The expiry date must be on or after the start date.")
            : Set(State with { StartDate = startDate, ExpiryDate = expiryDate });

    public Discount SetActive(bool isActive) => Set(State with { IsActive = isActive });

    public Discount SetLandingPage(Guid? pageId) => Set(State with { LandingPageId = pageId });

    public Discount SetRules(IEnumerable<DiscountRuleConfig> rules) => Set(State with { Rules = rules.ToList() });

    public Discount SetRewards(IEnumerable<DiscountRewardConfig> rewards) => Set(State with { Rewards = rewards.ToList() });

    private Discount Set(DiscountState state)
    {
        State = state;
        return this;
    }
}

/// <summary>Commerce's discount service. Codes are unique, as a database constraint would enforce on save.</summary>
public sealed class DiscountService(InMemoryDatabase database)
{
    private const string Table = "commerce.discount";

    public DiscountReadOnly? GetDiscount(Guid id)
        => database.Get(Table, id) is { } row ? new DiscountReadOnly(Deserialize(row.Root), row.Version) : null;

    public IEnumerable<DiscountReadOnly> GetDiscounts()
        => database.All(Table).Select(r => new DiscountReadOnly(Deserialize(r.Row.Root), r.Row.Version));

    public Discount CreateDiscount(Guid id, string name)
        => new(new DiscountState(id, name, null, null, null, false, null, [], []), 0);

    public void SaveDiscount(Discount discount)
    {
        if (discount.Code is { Length: > 0 } code && GetDiscounts().Any(d => d.Id != discount.Id && d.Code == code))
        {
            throw new InvalidOperationException($"The discount code '{code}' is already in use.");
        }

        database.Put(Table, discount.Id, JsonSerializer.SerializeToNode(discount.State)!.AsObject());
    }

    private static DiscountState Deserialize(JsonObject json) => json.Deserialize<DiscountState>()!;
}
