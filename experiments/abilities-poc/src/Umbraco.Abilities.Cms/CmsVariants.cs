using Umbraco.Abilities.Entities;
using Umbraco.Cms.Core.Models;
using Umbraco.Extensions;

namespace Umbraco.Abilities.Cms;

/// <summary>The CMS's two dimensions, culture and segment, on the framework's generic variant.</summary>
internal static class CmsVariants
{
    public static VariantId Of(string? culture, string? segment)
        => new([KeyValuePair.Create(VariationDimensions.Culture, culture), KeyValuePair.Create(VariationDimensions.Segment, segment)]);

    /// <summary>What a property or element type that varies as given varies by.</summary>
    public static IReadOnlyList<string> DimensionsOf(ContentVariation variation)
        => [.. variation.VariesByCulture() ? [VariationDimensions.Culture] : Array.Empty<string>(), .. variation.VariesBySegment() ? [VariationDimensions.Segment] : Array.Empty<string>()];

    extension(VariantId variant)
    {
        public string? Culture => variant[VariationDimensions.Culture];

        public string? Segment => variant[VariationDimensions.Segment];
    }
}
