namespace Umbraco.AI.Core.Models;

/// <summary>
/// Well-known keys used in <see cref="AIModelDescriptor.Metadata"/>.
/// </summary>
/// <remarks>
/// Metadata is a string dictionary so it can travel to the backoffice with the model list. These
/// constants (plus <see cref="AIModelSettingSupport"/> for writing and the
/// <c>GetCapabilitySettingSupport</c> extension for reading) keep the convention in one place rather
/// than repeated as literals across provider packages and the frontend.
/// </remarks>
public static class AIModelMetadataKeys
{
    /// <summary>
    /// Comma-separated schema field keys of the provider-declared capability settings this model accepts.
    /// </summary>
    public const string CapabilitySettingsSupported = "capabilitySettings.supported";

    /// <summary>
    /// Comma-separated schema field keys of the provider-declared capability settings this model rejects.
    /// </summary>
    public const string CapabilitySettingsUnsupported = "capabilitySettings.unsupported";
}
