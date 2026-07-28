using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using SdkImageGenerationOptions = OpenAI.Images.ImageGenerationOptions;

#pragma warning disable MEAI001 // ImageGenerationOptions is experimental in M.E.AI

namespace Umbraco.AI.OpenAI;

/// <summary>
/// Translates the provider-specific image hints (quality, style) into the OpenAI SDK's own options, which
/// is the only channel that reaches the request, and holds which models accept which values.
/// </summary>
/// <remarks>
/// <para>
/// Microsoft.Extensions.AI has no first-class property for either hint, and the OpenAI adapter ignores
/// <see cref="ImageGenerationOptions.AdditionalProperties"/> entirely —
/// <c>OpenAIImageOptionsWireTests</c> pins both halves of that. So the hints have to be handed over as a
/// raw representation.
/// </para>
/// <para>
/// Two callers share this. The profile's capability settings arrive typed and gated per model; a direct
/// <c>IImageGenerator</c> consumer can still pass them as additional properties, handled by
/// <see cref="OpenAIImageHintGenerator"/>. One translation, so the two cannot drift.
/// </para>
/// </remarks>
internal static class OpenAIImageHints
{
    internal const string QualityKey = "quality";
    internal const string StyleKey = "style";

    /// <summary>
    /// Quality vocabularies by model family. DALL·E 2 accepts only <c>standard</c>, DALL·E 3 adds
    /// <c>hd</c>, and gpt-image uses a different scale entirely.
    /// </summary>
    private static readonly Dictionary<string, HashSet<string>> QualitiesByFamily =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["gpt-image"] = new(StringComparer.OrdinalIgnoreCase) { "auto", "low", "medium", "high" },
            ["dall-e-3"] = new(StringComparer.OrdinalIgnoreCase) { "standard", "hd" },
            ["dall-e-2"] = new(StringComparer.OrdinalIgnoreCase) { "standard" },
        };

    /// <summary>
    /// Every value any known family accepts, used when the model itself is unrecognised — we cannot know
    /// what it takes, so a deliberate value is passed through rather than dropped.
    /// </summary>
    private static readonly HashSet<string> AllKnownQualities =
        new(QualitiesByFamily.Values.SelectMany(v => v), StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> KnownStyles =
        new(StringComparer.OrdinalIgnoreCase) { "vivid", "natural" };

    /// <summary>
    /// Whether the model supports a style hint. DALL·E 3 only.
    /// </summary>
    /// <remarks>
    /// Drives both the per-model declaration the editor reads and the request-time gate, so the field the
    /// user sees and the value actually sent cannot disagree.
    /// </remarks>
    public static bool SupportsStyle(string? modelId)
        => modelId?.StartsWith("dall-e-3", StringComparison.OrdinalIgnoreCase) == true;

    /// <summary>
    /// Builds a raw-representation factory carrying the given hints, or <c>null</c> when neither survives
    /// gating.
    /// </summary>
    /// <param name="quality">The requested quality, or <c>null</c>.</param>
    /// <param name="style">The requested style, or <c>null</c>.</param>
    /// <param name="modelId">The model the request will run against, used to gate the values.</param>
    /// <param name="logger">Optional logger, used when a hint is dropped.</param>
    public static Func<IImageGenerator, object?>? CreateRawFactory(
        string? quality,
        string? style,
        string? modelId,
        ILogger? logger)
    {
        var resolvedQuality = ResolveQuality(quality, modelId, logger);
        var resolvedStyle = ResolveStyle(style, modelId, logger);

        if (resolvedQuality is null && resolvedStyle is null)
        {
            return null;
        }

        return _ =>
        {
            var raw = new SdkImageGenerationOptions();

            if (resolvedQuality is not null)
            {
                raw.Quality = new global::OpenAI.Images.GeneratedImageQuality(resolvedQuality);
            }

            if (resolvedStyle is not null)
            {
                raw.Style = new global::OpenAI.Images.GeneratedImageStyle(resolvedStyle);
            }

            return raw;
        };
    }

    /// <summary>
    /// Returns options carrying any hints found in <see cref="ImageGenerationOptions.AdditionalProperties"/>
    /// as a raw representation, or the options unchanged when there is nothing to translate.
    /// </summary>
    /// <param name="options">The caller's options. Never mutated.</param>
    /// <param name="boundModelId">The model the generator was created for.</param>
    /// <param name="logger">Optional logger, used when a hint is dropped.</param>
    public static ImageGenerationOptions? Apply(
        ImageGenerationOptions? options,
        string? boundModelId,
        ILogger? logger)
    {
        if (options?.AdditionalProperties is null)
        {
            return options;
        }

        if (options.RawRepresentationFactory is not null)
        {
            // Whoever built the SDK options — a caller directly, or the profile's capability settings
            // upstream of here — is more specific than a loose dictionary entry, and overwriting the factory
            // would drop whatever else it set.
            logger?.LogDebug(
                "Image hints in additional properties were ignored because a raw representation is already set.");
            return options;
        }

        var factory = CreateRawFactory(
            Read(options.AdditionalProperties, QualityKey),
            Read(options.AdditionalProperties, StyleKey),
            options.ModelId ?? boundModelId,
            logger);

        if (factory is null)
        {
            return options;
        }

        // Clone so the caller's instance is never mutated.
        var effective = options.Clone();
        effective.RawRepresentationFactory = factory;
        return effective;
    }

    private static string? ResolveQuality(string? value, string? modelId, ILogger? logger)
    {
        var quality = value?.Trim();
        if (string.IsNullOrEmpty(quality))
        {
            return null;
        }

        var family = QualitiesByFamily
            .FirstOrDefault(f => modelId?.StartsWith(f.Key, StringComparison.OrdinalIgnoreCase) == true);
        var accepted = family.Value ?? AllKnownQualities;

        if (!accepted.Contains(quality))
        {
            // Dropped rather than sent: the API rejects the whole request for a quality the model does not
            // know, and a profile carried across models should degrade instead of failing outright.
            logger?.LogDebug(
                "Ignoring image quality '{Quality}', which model '{ModelId}' does not accept.",
                quality,
                modelId);
            return null;
        }

        return quality.ToLowerInvariant();
    }

    private static string? ResolveStyle(string? value, string? modelId, ILogger? logger)
    {
        var style = value?.Trim();
        if (string.IsNullOrEmpty(style))
        {
            return null;
        }

        if (!KnownStyles.Contains(style) || !SupportsStyle(modelId))
        {
            logger?.LogDebug(
                "Ignoring image style '{Style}' for model '{ModelId}'.",
                style,
                modelId);
            return null;
        }

        return style.ToLowerInvariant();
    }

    private static string? Read(AdditionalPropertiesDictionary additionalProperties, string key)
        => additionalProperties.TryGetValue(key, out var raw) ? raw as string : null;
}
