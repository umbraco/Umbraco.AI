using System.ComponentModel.DataAnnotations;

using Umbraco.Abilities.Samples.Commerce.Domain;

namespace Umbraco.Abilities.Samples.Acme;

// A third party's discount rule, written as a plain Commerce rule provider: a settings class and a provider class, added
// to Commerce's provider collection. There is no Abilities code here at all, yet discounts using it can be created from
// text, because Commerce's item type source reads the collection.

public sealed class AcmeDayOfWeekSettings
{
    [ProviderSetting(Label = "Days", Description = "Which days the order must be placed on.")]
    [Required]
    [AllowedValues("weekdays", "weekends")]
    public string? Days { get; set; }
}

[DiscountRuleProvider("acmeDayOfWeek", Label = "Day of week (Acme)", Description = "The order is placed on certain days.")]
public sealed class AcmeDayOfWeekRuleProvider : DiscountRuleProviderBase<AcmeDayOfWeekSettings>;
