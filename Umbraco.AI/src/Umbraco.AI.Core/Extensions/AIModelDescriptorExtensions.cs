using Umbraco.AI.Core.Models;

namespace Umbraco.AI.Extensions;

/// <summary>
/// Extension methods for <see cref="AIModelDescriptor"/>.
/// </summary>
public static class AIModelDescriptorExtensions
{
    /// <summary>
    /// Whether a provider-declared capability setting applies to this model, per the capability's
    /// declaration via <see cref="Core.Providers.IAICapability.GetSettingsSupport"/>.
    /// </summary>
    /// <param name="model">The model descriptor.</param>
    /// <param name="fieldKey">The schema field key (or property name) of the setting.</param>
    /// <returns>
    /// <c>false</c> only when the capability explicitly declared that this model rejects the setting.
    /// Declarations are negative — a capability says nothing about models it has no knowledge of — so
    /// this is "not known to be rejected" rather than an affirmative claim of support.
    /// </returns>
    public static bool IsCapabilitySettingSupported(this AIModelDescriptor model, string fieldKey)
    {
        ArgumentNullException.ThrowIfNull(model);

        if (string.IsNullOrWhiteSpace(fieldKey))
        {
            return true;
        }

        if (!model.Metadata.TryGetValue(AIModelMetadataKeys.CapabilitySettingsUnsupported, out var declared))
        {
            return true;
        }

        var key = fieldKey.Trim().ToCamelCase();

        return !declared
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Returns a copy of the descriptor with the supplied metadata merged in. Existing entries win, so
    /// a provider's own metadata is never overwritten.
    /// </summary>
    /// <param name="model">The model descriptor.</param>
    /// <param name="additionalMetadata">The metadata entries to merge.</param>
    internal static AIModelDescriptor WithMetadata(
        this AIModelDescriptor model,
        IReadOnlyDictionary<string, string> additionalMetadata)
    {
        if (additionalMetadata.Count == 0)
        {
            return model;
        }

        var merged = new Dictionary<string, string>(model.Metadata);
        foreach (var (key, value) in additionalMetadata)
        {
            merged.TryAdd(key, value);
        }

        return new AIModelDescriptor(model.Model, model.Name, merged);
    }
}
