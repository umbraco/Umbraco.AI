using Umbraco.AI.Extensions;

namespace Umbraco.AI.Core.Models;

/// <summary>
/// Whether a setting applies to a specific model.
/// </summary>
public enum AISettingSupport
{
    /// <summary>
    /// The capability made no statement about this setting for this model. Consumers should render
    /// the setting normally — a capability only ever declares what it positively knows.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// The model accepts this setting.
    /// </summary>
    Supported = 1,

    /// <summary>
    /// The model rejects or ignores this setting.
    /// </summary>
    Unsupported = 2,
}

/// <summary>
/// A capability's declaration of which settings apply to a given model.
/// </summary>
/// <remarks>
/// <para>
/// Support for a setting usually varies by <em>model</em> rather than by provider — OpenAI's reasoning
/// effort applies to the o-series and GPT-5 but not to gpt-4o; Anthropic's thinking budget is rejected
/// by the newest Claude models. A capability returns this from
/// <see cref="Providers.IAICapability.GetSettingSupport"/> and the core capability bases project it into
/// <see cref="AIModelDescriptor.Metadata"/> when the model list is built, so the backoffice can render only
/// the settings that apply to the selected model without an extra round trip.
/// </para>
/// <para>
/// This is the declaration channel for the <em>UI</em>. It is not an enforcement mechanism: the model
/// list is fetched from the vendor and cannot be consulted per request, so a capability that declares a
/// setting unsupported must also refrain from sending it (see
/// <c>AIChatCapabilityBase&lt;TSettings, TCapabilitySettings&gt;.ApplyCapabilitySettings</c>, which receives
/// the resolved model ID for exactly that purpose). Declare and enforce from one shared predicate.
/// </para>
/// <para>
/// Entries may be given as property names (<c>nameof(MySettings.ReasoningEffort)</c>) or as schema field
/// keys; both are normalised to the schema's camelCase key, so they cannot drift from the settings type.
/// </para>
/// </remarks>
public sealed class AIModelSettingSupport
{
    /// <summary>
    /// An empty declaration — every setting is <see cref="AISettingSupport.Unknown"/>.
    /// </summary>
    public static readonly AIModelSettingSupport Default = new();

    /// <summary>
    /// The provider-declared capability settings this model accepts.
    /// </summary>
    public IReadOnlyCollection<string> SupportedCapabilitySettings { get; init; } = [];

    /// <summary>
    /// The provider-declared capability settings this model rejects or ignores.
    /// </summary>
    public IReadOnlyCollection<string> UnsupportedCapabilitySettings { get; init; } = [];

    /// <summary>
    /// Whether this declaration says anything at all.
    /// </summary>
    internal bool IsEmpty => SupportedCapabilitySettings.Count == 0 && UnsupportedCapabilitySettings.Count == 0;

    /// <summary>
    /// Projects the declaration into the metadata entries carried by <see cref="AIModelDescriptor.Metadata"/>.
    /// Only non-empty collections produce an entry.
    /// </summary>
    internal IReadOnlyDictionary<string, string> ToMetadata()
    {
        var metadata = new Dictionary<string, string>();

        if (SupportedCapabilitySettings.Count > 0)
        {
            metadata[AIModelMetadataKeys.CapabilitySettingsSupported] = Join(SupportedCapabilitySettings);
        }

        if (UnsupportedCapabilitySettings.Count > 0)
        {
            metadata[AIModelMetadataKeys.CapabilitySettingsUnsupported] = Join(UnsupportedCapabilitySettings);
        }

        return metadata;
    }

    // Normalise to the same camelCase key the schema builder derives from the property name, so a
    // declaration written as nameof(TSettings.ReasoningEffort) matches the field key "reasoningEffort".
    private static string Join(IReadOnlyCollection<string> keys)
        => string.Join(
            ',',
            keys.Where(k => !string.IsNullOrWhiteSpace(k)).Select(k => k.Trim().ToCamelCase()));
}
