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

    /// <summary>
    /// The model's context window, in tokens, as an integer in invariant culture.
    /// Absent when the provider does not know it.
    /// </summary>
    public const string ModelContextWindow = "model.contextWindow";

    /// <summary>
    /// The price of one million input tokens, as a decimal in invariant culture, in the currency named
    /// by <see cref="PricingCurrency"/>. Only meaningful alongside the other pricing keys.
    /// </summary>
    public const string PricingInputPerMillionTokens = "pricing.inputPerMillionTokens";

    /// <summary>
    /// The price of one million output tokens, as a decimal in invariant culture, in the currency named
    /// by <see cref="PricingCurrency"/>. Only meaningful alongside the other pricing keys.
    /// </summary>
    public const string PricingOutputPerMillionTokens = "pricing.outputPerMillionTokens";

    /// <summary>
    /// The ISO 4217 currency code the pricing keys are expressed in (e.g. <c>USD</c>).
    /// </summary>
    public const string PricingCurrency = "pricing.currency";
}
