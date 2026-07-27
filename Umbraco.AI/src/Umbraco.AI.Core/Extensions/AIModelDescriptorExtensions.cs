using Umbraco.AI.Core.Models;

namespace Umbraco.AI.Extensions;

/// <summary>
/// Extension methods for <see cref="AIModelDescriptor"/>.
/// </summary>
public static class AIModelDescriptorExtensions
{
    /// <summary>
    /// Reads whether a provider-declared capability setting applies to this model, as declared by the
    /// capability via <see cref="Core.Providers.IAICapability.GetSettingSupport"/>.
    /// </summary>
    /// <param name="model">The model descriptor.</param>
    /// <param name="fieldKey">The schema field key (or property name) of the setting.</param>
    /// <returns>
    /// <see cref="AISettingSupport.Unsupported"/> when the capability declared the setting unsupported,
    /// <see cref="AISettingSupport.Supported"/> when it declared it supported, otherwise
    /// <see cref="AISettingSupport.Unknown"/>.
    /// </returns>
    /// <remarks>
    /// Unsupported takes precedence, so a capability that (incorrectly) lists a key in both collections
    /// gets the safer answer.
    /// </remarks>
    public static AISettingSupport GetCapabilitySettingSupport(this AIModelDescriptor model, string fieldKey)
    {
        ArgumentNullException.ThrowIfNull(model);

        if (string.IsNullOrWhiteSpace(fieldKey))
        {
            return AISettingSupport.Unknown;
        }

        var key = fieldKey.Trim().ToCamelCase();

        if (Contains(model, AIModelMetadataKeys.CapabilitySettingsUnsupported, key))
        {
            return AISettingSupport.Unsupported;
        }

        return Contains(model, AIModelMetadataKeys.CapabilitySettingsSupported, key)
            ? AISettingSupport.Supported
            : AISettingSupport.Unknown;
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

    private static bool Contains(AIModelDescriptor model, string metadataKey, string fieldKey)
        => model.Metadata.TryGetValue(metadataKey, out var value)
           && value
               .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
               .Any(k => string.Equals(k, fieldKey, StringComparison.OrdinalIgnoreCase));
}
