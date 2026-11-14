using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

using Umbraco.Abilities.Entities;
using Umbraco.Abilities.Entities.Models;
using Umbraco.Abilities.Samples.Commerce;

namespace Umbraco.Abilities.Samples.Acme;

/// <summary>
/// A third-party package's discount rule. It subclasses Commerce's rule and registers the class; discounts using it can
/// then be created from text with no change to Commerce or Umbraco.AI.
/// </summary>
[ItemType("acmeDayOfWeek", "Day of week (Acme)", Description = "The order is placed on certain days.")]
public sealed class AcmeDayOfWeekRule : DiscountRule
{
    [Required, AllowedValues("weekdays", "weekends"), Description("Which days.")]
    public string? Days { get; set; }

    public static void Register(ItemTypeRegistry itemTypes) => itemTypes.Add<AcmeDayOfWeekRule>(DiscountDefinition.RuleGroup);
}
