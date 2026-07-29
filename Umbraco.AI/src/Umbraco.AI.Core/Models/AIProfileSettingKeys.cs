namespace Umbraco.AI.Core.Models;

/// <summary>
/// Field keys for the core, cross-provider request options a capability can declare a model does not
/// accept, via <see cref="AIModelSettingsSupport.UnsupportedProfileSettings"/>.
/// </summary>
/// <remarks>
/// <para>
/// These are the keys core knows how to <em>enforce</em>: a declaration naming one of them is applied to
/// every request by the capability bases, not merely shown to the editor. That is what makes a declaration
/// a single source of truth rather than a hint the provider must remember to honour separately.
/// </para>
/// <para>
/// Only options Microsoft.Extensions.AI models as first-class properties appear here, and only ones that
/// are safe to remove. <c>MaxOutputTokens</c> is deliberately absent: some providers require a limit, so
/// stripping it would break a request rather than degrade it.
/// </para>
/// <para>
/// Keys a capability declares that are not listed here still reach the editor — a provider can describe
/// its own settings by their schema field key — they simply have no core option to strip.
/// </para>
/// </remarks>
public static class AIProfileSettingKeys
{
    /// <summary>Chat sampling temperature.</summary>
    public const string Temperature = "temperature";

    /// <summary>Chat nucleus-sampling probability mass.</summary>
    public const string TopP = "topP";

    /// <summary>Chat top-k sampling.</summary>
    public const string TopK = "topK";

    /// <summary>Chat frequency penalty.</summary>
    public const string FrequencyPenalty = "frequencyPenalty";

    /// <summary>Chat presence penalty.</summary>
    public const string PresencePenalty = "presencePenalty";

    /// <summary>Embedding output dimension count.</summary>
    public const string Dimensions = "dimensions";

    /// <summary>
    /// The sampling parameters, which providers typically restrict as a group: a model that rejects a
    /// temperature usually rejects the others too.
    /// </summary>
    public static readonly string[] Sampling = [Temperature, TopP, TopK, FrequencyPenalty, PresencePenalty];
}
