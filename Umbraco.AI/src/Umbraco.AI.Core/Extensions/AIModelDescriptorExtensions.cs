using Umbraco.AI.Core.Models;

namespace Umbraco.AI.Extensions;

/// <summary>
/// Extension methods for <see cref="AIModelDescriptor"/>.
/// </summary>
public static class AIModelDescriptorExtensions
{
    /// <summary>
    /// Whether the capability declared that this model rejects a provider-declared capability setting,
    /// via <see cref="Core.Providers.IAICapability.GetSettingSupport"/>.
    /// </summary>
    /// <param name="model">The model descriptor.</param>
    /// <param name="fieldKey">The schema field key (or property name) of the setting.</param>
    /// <returns>
    /// <c>true</c> only when the setting was explicitly declared unsupported. A capability declares
    /// nothing for settings it has no knowledge of, so the absence of a declaration means the setting
    /// applies.
    /// </returns>
    public static bool IsCapabilitySettingUnsupported(this AIModelDescriptor model, string fieldKey)
    {
        ArgumentNullException.ThrowIfNull(model);

        if (string.IsNullOrWhiteSpace(fieldKey))
        {
            return false;
        }

        if (!model.Metadata.TryGetValue(AIModelMetadataKeys.CapabilitySettingsUnsupported, out var declared))
        {
            return false;
        }

        var key = fieldKey.Trim().ToCamelCase();

        return declared
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
