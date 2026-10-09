using System.Globalization;

namespace Umbraco.AI.Core.Models;

/// <summary>
/// Writes the standard model facts into the metadata carried by <see cref="AIModelDescriptor.Metadata"/>.
/// </summary>
/// <remarks>
/// The write-side counterpart of <c>GetContextWindow()</c> and <c>GetPricing()</c> on
/// <see cref="Extensions.AIModelDescriptorExtensions"/>, mirroring <see cref="AIModelSettingsSupport.ToMetadata"/>.
/// Values are always formatted in invariant culture, so a server running under a culture with a decimal
/// comma writes what every reader parses.
/// </remarks>
public static class AIModelMetadata
{
    /// <summary>
    /// Metadata entries declaring a model's context window.
    /// </summary>
    /// <param name="tokens">The context window, in tokens.</param>
    public static IReadOnlyDictionary<string, string> ForContextWindow(int tokens)
        => new Dictionary<string, string>
        {
            [AIModelMetadataKeys.ModelContextWindow] = tokens.ToString(CultureInfo.InvariantCulture),
        };

    /// <summary>
    /// Metadata entries declaring a model's price per million tokens.
    /// </summary>
    /// <param name="pricing">The pricing to declare.</param>
    public static IReadOnlyDictionary<string, string> ForPricing(AIModelPricing pricing)
    {
        ArgumentNullException.ThrowIfNull(pricing);

        return new Dictionary<string, string>
        {
            [AIModelMetadataKeys.PricingInputPerMillionTokens] = pricing.InputPerMillionTokens.ToString(CultureInfo.InvariantCulture),
            [AIModelMetadataKeys.PricingOutputPerMillionTokens] = pricing.OutputPerMillionTokens.ToString(CultureInfo.InvariantCulture),
            [AIModelMetadataKeys.PricingCurrency] = pricing.Currency.Trim(),
        };
    }
}
