using Umbraco.Abilities.Entities;
using Umbraco.Abilities.Samples.Commerce;

namespace Umbraco.Abilities.Samples.Acme;

/// <summary>
/// A third-party package that adds a discount rule type. It registers one item type and nothing else;
/// discounts using it can then be created from text with no change to Commerce or Umbraco.AI.
/// </summary>
public static class AcmeWeekdayRule
{
    public static void Register(ItemTypeRegistry itemTypes)
        => itemTypes.Add(new ItemTypeDefinition(
            DiscountDefinition.RuleGroup,
            "acmeDayOfWeek",
            "Day of week (Acme)",
            "The order is placed on certain days.",
            ElementShape.Of(new FieldDefinition("days", "Which days.", FieldType.String, Required: true) { Allowed = ["weekdays", "weekends"] })));
}
