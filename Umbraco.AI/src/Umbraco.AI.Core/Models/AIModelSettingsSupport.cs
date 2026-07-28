using Umbraco.AI.Extensions;

namespace Umbraco.AI.Core.Models;

/// <summary>
/// A capability's declaration of which settings a given model does not accept.
/// </summary>
/// <remarks>
/// <para>
/// Support for a setting usually varies by <em>model</em> rather than by provider — OpenAI's reasoning
/// effort applies to the o-series and GPT-5 but not to gpt-4o; Anthropic's thinking budget is rejected
/// by the newest Claude models. A capability returns this from
/// <see cref="Providers.IAICapability.GetSettingsSupport"/> and the core capability bases project it into
/// <see cref="AIModelDescriptor.Metadata"/> when the model list is built, so the backoffice can hide the
/// settings that don't apply to the selected model without an extra round trip.
/// </para>
/// <para>
/// Declarations are negative only: a capability names the settings a model rejects and says nothing
/// otherwise, so <see cref="Default"/> means "nothing to declare" and the setting renders. Whether the
/// capability arrives at that by keeping an allow-list of models that support the setting or a deny-list
/// of models that don't is its own business — write whichever set is more stable and convert at this
/// boundary.
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
public sealed class AIModelSettingsSupport
{
    /// <summary>
    /// An empty declaration — the capability has nothing to say about this model, so every setting
    /// applies.
    /// </summary>
    public static readonly AIModelSettingsSupport Default = new();

    /// <summary>
    /// The provider-declared capability settings this model rejects or ignores.
    /// </summary>
    public IReadOnlyCollection<string> UnsupportedCapabilitySettings { get; init; } = [];

    /// <summary>
    /// Whether this declaration says anything at all.
    /// </summary>
    internal bool IsEmpty => UnsupportedCapabilitySettings.Count == 0;

    /// <summary>
    /// Projects the declaration into the metadata entries carried by <see cref="AIModelDescriptor.Metadata"/>.
    /// </summary>
    /// <remarks>
    /// The capability bases call this for you when they project
    /// <see cref="Providers.IAICapability.GetSettingsSupport"/> over a model list. It is public for the other
    /// route: a capability whose vendor reports support as data (Anthropic's models endpoint does) can build
    /// its descriptors with the declaration already attached instead of implementing the hook.
    /// </remarks>
    public IReadOnlyDictionary<string, string> ToMetadata()
        => IsEmpty
            ? new Dictionary<string, string>()
            : new Dictionary<string, string>
            {
                // Normalise to the same camelCase key the schema builder derives from the property name,
                // so a declaration written as nameof(TSettings.ReasoningEffort) matches the field key.
                [AIModelMetadataKeys.CapabilitySettingsUnsupported] = string.Join(
                    ',',
                    UnsupportedCapabilitySettings
                        .Where(k => !string.IsNullOrWhiteSpace(k))
                        .Select(k => k.Trim().ToCamelCase())),
            };
}
