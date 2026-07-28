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
        => IsSettingSupported(model, AIModelMetadataKeys.CapabilitySettingsUnsupported, fieldKey);

    /// <summary>
    /// Whether a core profile setting (e.g. <c>temperature</c>) applies to this model, per the
    /// capability's declaration via <see cref="Core.Providers.IAICapability.GetSettingsSupport"/>.
    /// </summary>
    /// <param name="model">The model descriptor.</param>
    /// <param name="fieldKey">The field key (or property name) of the setting.</param>
    /// <returns>
    /// <c>false</c> only when the capability explicitly declared that this model rejects the setting.
    /// Declarations are negative, so this is "not known to be rejected" rather than an affirmative claim
    /// of support.
    /// </returns>
    public static bool IsProfileSettingSupported(this AIModelDescriptor model, string fieldKey)
        => IsSettingSupported(model, AIModelMetadataKeys.ProfileSettingsUnsupported, fieldKey);

    /// <summary>
    /// The image sizes this model accepts, each as <c>"{width}x{height}"</c>.
    /// </summary>
    /// <param name="model">The model descriptor.</param>
    /// <returns>
    /// The declared sizes, or an empty list when the capability declared none. Empty means "unknown", not
    /// "none supported" — a consumer should keep accepting a free-typed size rather than blocking one.
    /// </returns>
    public static IReadOnlyList<string> GetSupportedImageSizes(this AIModelDescriptor model)
    {
        ArgumentNullException.ThrowIfNull(model);

        if (!model.Metadata.TryGetValue(AIModelMetadataKeys.ImageSupportedSizes, out var declared))
        {
            return [];
        }

        return declared
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
    }

    /// <summary>
    /// The longest edge, in pixels, this model will produce, or <c>null</c> when not declared.
    /// </summary>
    /// <param name="model">The model descriptor.</param>
    public static int? GetImageMaxEdge(this AIModelDescriptor model)
        => ReadInt(model, AIModelMetadataKeys.ImageMaxEdge);

    /// <summary>
    /// Whether this model supports editing a supplied image, or <c>null</c> when not declared.
    /// </summary>
    /// <param name="model">The model descriptor.</param>
    /// <remarks>
    /// Nullable on purpose: an absent declaration is silence, which is not the same as an explicit
    /// <c>false</c>. A consumer that treats the two alike will hide a feature on every model a provider
    /// happens not to describe.
    /// </remarks>
    public static bool? SupportsImageEdit(this AIModelDescriptor model)
        => ReadBool(model, AIModelMetadataKeys.ImageSupportsEdit);

    /// <summary>
    /// Whether this model supports a mask when editing, or <c>null</c> when not declared.
    /// </summary>
    /// <param name="model">The model descriptor.</param>
    public static bool? SupportsImageMask(this AIModelDescriptor model)
        => ReadBool(model, AIModelMetadataKeys.ImageSupportsMask);

    private static int? ReadInt(AIModelDescriptor model, string metadataKey)
    {
        ArgumentNullException.ThrowIfNull(model);

        return model.Metadata.TryGetValue(metadataKey, out var raw)
            && int.TryParse(raw.Trim(), out var value)
                ? value
                : null;
    }

    private static bool? ReadBool(AIModelDescriptor model, string metadataKey)
    {
        ArgumentNullException.ThrowIfNull(model);

        return model.Metadata.TryGetValue(metadataKey, out var raw)
            && bool.TryParse(raw.Trim(), out var value)
                ? value
                : null;
    }

    private static bool IsSettingSupported(AIModelDescriptor model, string metadataKey, string fieldKey)
    {
        ArgumentNullException.ThrowIfNull(model);

        if (string.IsNullOrWhiteSpace(fieldKey))
        {
            return true;
        }

        if (!model.Metadata.TryGetValue(metadataKey, out var declared))
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
