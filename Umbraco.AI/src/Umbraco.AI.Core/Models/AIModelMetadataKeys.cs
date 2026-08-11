namespace Umbraco.AI.Core.Models;

/// <summary>
/// Well-known keys used in <see cref="AIModelDescriptor.Metadata"/>.
/// </summary>
/// <remarks>
/// Metadata is a string dictionary so it can travel to the backoffice with the model list. These
/// constants (plus <see cref="AIModelSettingsSupport"/> for writing and the extensions on
/// <see cref="Extensions.AIModelDescriptorExtensions"/> for reading) keep the convention in one place
/// rather than repeated as literals across provider packages and the frontend.
/// <para>
/// Every key here should have a reader alongside it. A key written by a provider and read by nobody is
/// how the image constraints spent their first release doing nothing at all.
/// </para>
/// </remarks>
public static class AIModelMetadataKeys
{
    /// <summary>
    /// Comma-separated schema field keys of the provider-declared capability settings this model rejects.
    /// Absent when the capability has nothing to declare for the model.
    /// </summary>
    public const string CapabilitySettingsUnsupported = "capabilitySettings.unsupported";

    /// <summary>
    /// Comma-separated field keys of the core profile settings this model rejects or ignores — the
    /// built-in settings every provider shares (<c>temperature</c> and friends), as opposed to the
    /// provider-declared ones in <see cref="CapabilitySettingsUnsupported"/>.
    /// Absent when the capability has nothing to declare for the model.
    /// </summary>
    public const string ProfileSettingsUnsupported = "profileSettings.unsupported";

    /// <summary>
    /// Comma-separated image sizes the model accepts, each as <c>"{width}x{height}"</c>.
    /// Absent when the capability declares no size constraint.
    /// </summary>
    public const string ImageSupportedSizes = "image.supportedSizes";

    /// <summary>
    /// The longest edge, in pixels, the model will produce.
    /// </summary>
    public const string ImageMaxEdge = "image.maxEdge";

    /// <summary>
    /// Whether the model supports editing a supplied image (<c>true</c> or <c>false</c>).
    /// </summary>
    public const string ImageSupportsEdit = "image.supportsEdit";

    /// <summary>
    /// Whether the model supports a mask when editing (<c>true</c> or <c>false</c>).
    /// </summary>
    public const string ImageSupportsMask = "image.supportsMask";
}
