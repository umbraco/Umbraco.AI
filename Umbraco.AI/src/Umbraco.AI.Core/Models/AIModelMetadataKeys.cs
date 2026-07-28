namespace Umbraco.AI.Core.Models;

/// <summary>
/// Well-known keys used in <see cref="AIModelDescriptor.Metadata"/>.
/// </summary>
/// <remarks>
/// Metadata is a string dictionary so it can travel to the backoffice with the model list. This
/// constant (plus <see cref="AIModelSettingSupport"/> for writing and the
/// <c>IsCapabilitySettingSupported</c> extension for reading) keeps the convention in one place
/// rather than repeated as literals across provider packages and the frontend.
/// </remarks>
public static class AIModelMetadataKeys
{
    /// <summary>
    /// Comma-separated schema field keys of the provider-declared capability settings this model rejects.
    /// Absent when the capability has nothing to declare for the model.
    /// </summary>
    public const string CapabilitySettingsUnsupported = "capabilitySettings.unsupported";
}
