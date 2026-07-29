using System.Text.RegularExpressions;

namespace Umbraco.AI.Extensions;

/// <summary>
/// Utility methods for working with Microsoft AI Foundry models.
/// </summary>
/// <remarks>
/// <para>
/// Foundry is a gateway: it fronts other vendors' models, so it inherits their per-model restrictions
/// without owning any of them. That makes the shape of these predicates deliberately different from the
/// first-party provider packages, and the difference is the whole point of this file — see
/// <see cref="SupportsSamplingParameters"/>.
/// </para>
/// <para>
/// The family patterns duplicate a small part of <c>OpenAIModelUtilities</c> and
/// <c>AnthropicModelUtilities</c>. That is intentional: making one provider package depend on another to
/// share them would couple two independently released packages, and lifting them into core would put
/// vendor knowledge where it does not belong. When a vendor's restrictions change, all the packages naming
/// that vendor's families need the same edit.
/// </para>
/// </remarks>
internal static class MicrosoftFoundryModelUtilities
{
    /// <summary>
    /// Publisher names, as reported by the deployments API, for vendors that restrict the sampling
    /// parameters on some of their models.
    /// </summary>
    /// <remarks>
    /// Used only to rule a restriction <em>out</em> — see <see cref="SupportsSamplingParameters"/>. Matched
    /// as a substring because the reported value is a display name (<c>OpenAI</c>, <c>Anthropic</c>) rather
    /// than a stable identifier, and Azure has shipped variations (<c>Azure OpenAI</c>).
    /// </remarks>
    private static readonly string[] RestrictiveVendorPublishers = ["openai", "anthropic"];

    /// <summary>
    /// Model names that read as OpenAI's naming, whether or not this package recognises the specific model.
    /// </summary>
    /// <remarks>
    /// Covers the GPT line, the ChatGPT-branded snapshots and the o-series. Matching one of these means
    /// OpenAI's allow-list below decides, so an unrecognised <c>gpt-*</c> model fails safe rather than
    /// falling through to "no known restriction".
    /// </remarks>
    private static readonly Regex[] OpenAiNamingPatterns =
    [
        new(@"^(gpt|chatgpt)-", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"^o[134](-|$)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
    ];

    /// <summary>
    /// OpenAI families that accept the sampling parameters. Mirrors
    /// <c>OpenAIModelUtilities.SupportsSamplingParameters</c>: the reasoning models (the o-series and the
    /// GPT-5 line) reject a non-default <c>temperature</c> rather than ignoring it.
    /// </summary>
    /// <remarks>
    /// Not quite a copy — Azure names GPT-3.5 without the dot (<c>gpt-35-turbo</c>), because a model name
    /// there cannot contain one. OpenAI's own <c>^gpt-3\.5</c> never has to match that spelling, so the
    /// undotted form is listed here as well. Missing it would read <c>gpt-35-turbo</c> as an unrecognised
    /// OpenAI model and drop a temperature the deployment accepts.
    /// </remarks>
    private static readonly Regex[] OpenAiSamplingPatterns =
    [
        new(@"^gpt-3\.5", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"^gpt-35", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"^gpt-4", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"^chatgpt-", RegexOptions.IgnoreCase | RegexOptions.Compiled),
    ];

    /// <summary>
    /// Model names that read as Anthropic's naming.
    /// </summary>
    private static readonly Regex[] AnthropicNamingPatterns =
    [
        new(@"^claude-", RegexOptions.IgnoreCase | RegexOptions.Compiled),
    ];

    /// <summary>
    /// Claude families that accept the sampling parameters. Mirrors
    /// <c>AnthropicModelUtilities.SupportsSamplingParameters</c>: Anthropic removed them from Claude Opus
    /// 4.7 onwards, so 4.7 and 4.8 are deliberately outside the minor versions listed here.
    /// </summary>
    private static readonly Regex[] AnthropicSamplingPatterns =
    [
        new(@"^claude-3(-|$)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"^claude-(opus|sonnet|haiku)-4(-[0156])?(-\d{8})?$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled),
    ];

    /// <summary>
    /// OpenAI embedding models that accept a <c>dimensions</c> request parameter. Shortening an embedding
    /// is a <c>text-embedding-3</c> feature; <c>ada-002</c> predates it.
    /// </summary>
    private static readonly Regex[] OpenAiDimensionsPatterns =
    [
        new(@"^text-embedding-3", RegexOptions.IgnoreCase | RegexOptions.Compiled),
    ];

    /// <summary>
    /// Model names that read as OpenAI's embedding naming.
    /// </summary>
    private static readonly Regex[] OpenAiEmbeddingNamingPatterns =
    [
        new(@"^text-embedding-", RegexOptions.IgnoreCase | RegexOptions.Compiled),
    ];

    /// <summary>
    /// Determines whether a Foundry model accepts the sampling parameters (<c>temperature</c>,
    /// <c>top_p</c>).
    /// </summary>
    /// <param name="modelId">The model ID the profile carries — a catalogue model name on the models API path, or a user-chosen deployment name on the deployments API path.</param>
    /// <param name="modelName">The underlying model the deployment fronts, when the deployments API reported one.</param>
    /// <param name="publisher">The publisher the deployments API reported, when it reported one.</param>
    /// <remarks>
    /// <para>
    /// <strong>Unknown models are treated as supported.</strong> This inverts the failure direction the
    /// first-party packages chose, and the reason is that Foundry's unknown-model population is different
    /// in kind. OpenAI and Anthropic each own a closed set of models with a known restriction, so there an
    /// allow-list of the families that accept the parameters means an unrecognised model degrades (the
    /// value is dropped) instead of failing (a 400). Foundry fronts Mistral, Llama, Cohere, Phi, Nova,
    /// DeepSeek and whatever ships next, almost none of which restrict anything — so the same allow-list
    /// would silently stop sending a temperature that those models honour today. A regression across most
    /// of the catalogue is a worse trade than a 400 confined to two vendors' newest models.
    /// </para>
    /// <para>
    /// Within a vendor known to restrict, the allow-list semantics do apply: a model whose name reads as
    /// OpenAI's or Anthropic's but which this package does not recognise (a future <c>gpt-6</c>, a
    /// <c>claude-opus-4-9</c>) is treated as restricted. So the fail-safe behaviour is kept exactly where
    /// it is warranted, and the "no known restriction" default only catches names that look like neither.
    /// </para>
    /// <para>
    /// <paramref name="publisher"/> can only rule a restriction <em>out</em>, never in. A deployment the
    /// API attributes to Meta or Mistral skips family matching entirely, so a Llama deployment a user
    /// happened to name <c>o3-llama</c> is not mistaken for OpenAI's o3. It is never used to infer that a
    /// restriction applies, because knowing the vendor without knowing the model would mean guessing, and
    /// guessing "restricted" would drop a value from a deployment that works today.
    /// </para>
    /// <para>
    /// The <em>unrestricted</em> default is also why a blank model is supported here rather than dropped:
    /// no model resolved means the client falls back to the capability's default, which accepts them.
    /// </para>
    /// </remarks>
    public static bool SupportsSamplingParameters(string? modelId, string? modelName = null, string? publisher = null)
    {
        var identity = ResolveIdentity(modelId, modelName);

        if (identity is null || IsUnrestrictedPublisher(publisher))
        {
            return true;
        }

        if (Matches(OpenAiNamingPatterns, identity))
        {
            return Matches(OpenAiSamplingPatterns, identity);
        }

        if (Matches(AnthropicNamingPatterns, identity))
        {
            return Matches(AnthropicSamplingPatterns, identity);
        }

        return true;
    }

    /// <summary>
    /// Determines whether a Foundry embedding model accepts a <c>dimensions</c> request parameter.
    /// </summary>
    /// <param name="modelId">The model ID the profile carries — a catalogue model name, or a deployment name.</param>
    /// <param name="modelName">The underlying model the deployment fronts, when the deployments API reported one.</param>
    /// <param name="publisher">The publisher the deployments API reported, when it reported one.</param>
    /// <remarks>
    /// Same shape and the same reasoning as <see cref="SupportsSamplingParameters"/>: only a name that
    /// reads as OpenAI's embedding family is held to OpenAI's rule, and anything else — a Cohere embed
    /// model, a deployment called <c>embeddings-prod</c> — keeps today's behaviour. Note the asymmetry with
    /// the sampling case: <c>ada-002</c> is the <em>older</em> model here, so an unrecognised
    /// <c>text-embedding-*</c> name reads as unsupported and the parameter is dropped.
    /// </remarks>
    public static bool SupportsDimensions(string? modelId, string? modelName = null, string? publisher = null)
    {
        var identity = ResolveIdentity(modelId, modelName);

        if (identity is null || IsUnrestrictedPublisher(publisher))
        {
            return true;
        }

        return !Matches(OpenAiEmbeddingNamingPatterns, identity)
               || Matches(OpenAiDimensionsPatterns, identity);
    }

    /// <summary>
    /// Builds the label the model picker shows for a Foundry model.
    /// </summary>
    /// <param name="modelId">The model ID, which on the deployments API path is the deployment name.</param>
    /// <param name="modelName">The underlying model the deployment fronts, when known.</param>
    /// <param name="modelVersion">The deployed model version, when known.</param>
    /// <returns>
    /// The model ID alone when nothing more is known, otherwise the underlying model and version with the
    /// deployment name in parentheses — e.g. <c>gpt-4o 2024-11-20 (prod-chat)</c>.
    /// </returns>
    /// <remarks>
    /// A deployment name is user-chosen and can say nothing about what it fronts, so <c>prod-chat-1</c> on
    /// its own is a poor label. The deployment name is still shown because it is the value stored on the
    /// profile, so a user comparing the picker against their Azure resource needs to see it.
    /// </remarks>
    public static string FormatDisplayName(string modelId, string? modelName = null, string? modelVersion = null)
    {
        if (string.IsNullOrWhiteSpace(modelName)
            || modelName.Equals(modelId, StringComparison.OrdinalIgnoreCase))
        {
            return modelId;
        }

        var label = string.IsNullOrWhiteSpace(modelVersion)
            ? modelName.Trim()
            : $"{modelName.Trim()} {modelVersion.Trim()}";

        return $"{label} ({modelId})";
    }

    /// <summary>
    /// The name to match patterns against: the underlying model when the deployments API reported one,
    /// otherwise the model ID, which is all the models API path provides.
    /// </summary>
    private static string? ResolveIdentity(string? modelId, string? modelName)
    {
        if (!string.IsNullOrWhiteSpace(modelName))
        {
            return modelName.Trim();
        }

        return string.IsNullOrWhiteSpace(modelId) ? null : modelId.Trim();
    }

    /// <summary>
    /// Whether the reported publisher is one this package knows imposes no sampling restriction, which
    /// short-circuits family matching.
    /// </summary>
    private static bool IsUnrestrictedPublisher(string? publisher)
        => !string.IsNullOrWhiteSpace(publisher)
           && !RestrictiveVendorPublishers.Any(v => publisher.Contains(v, StringComparison.OrdinalIgnoreCase));

    private static bool Matches(Regex[] patterns, string value)
        => patterns.Any(p => p.IsMatch(value));
}
