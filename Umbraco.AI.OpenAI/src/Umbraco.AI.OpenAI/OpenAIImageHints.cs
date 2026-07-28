using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using SdkImageGenerationOptions = OpenAI.Images.ImageGenerationOptions;

#pragma warning disable MEAI001 // ImageGenerationOptions is experimental in M.E.AI

namespace Umbraco.AI.OpenAI;

/// <summary>
/// Translates the provider-specific image hints (<c>quality</c>, <c>style</c>) into the OpenAI SDK's own
/// options, which is the only channel that reaches the request.
/// </summary>
/// <remarks>
/// <para>
/// The hints travel as <see cref="ImageGenerationOptions.AdditionalProperties"/> because
/// Microsoft.Extensions.AI has no first-class property for either. The OpenAI adapter ignores that
/// dictionary entirely — <c>OpenAIImageOptionsWireTests</c> pins both halves of that: the hints are absent
/// from the body when passed as additional properties, and present when passed as a raw representation.
/// </para>
/// <para>
/// Kept separate from the decorator that calls it so the same translation can be reused when these hints
/// move to provider-declared capability settings, where the value arrives typed instead of as a loose
/// dictionary entry.
/// </para>
/// </remarks>
internal static class OpenAIImageHints
{
    internal const string QualityKey = "quality";
    internal const string StyleKey = "style";

    /// <summary>
    /// The wire values the SDK understands. Which subset a given model accepts differs (<c>hd</c> and
    /// <c>standard</c> are DALL·E 3; <c>low</c>, <c>medium</c>, <c>high</c> and <c>auto</c> are gpt-image),
    /// so this is a spelling check rather than a support check — the API rejects a valid value sent to the
    /// wrong model, and says so clearly.
    /// </summary>
    private static readonly HashSet<string> KnownQualities =
        new(StringComparer.OrdinalIgnoreCase) { "hd", "standard", "low", "medium", "high", "auto" };

    private static readonly HashSet<string> KnownStyles =
        new(StringComparer.OrdinalIgnoreCase) { "vivid", "natural" };

    /// <summary>
    /// Returns options carrying the hints as a raw representation, or the options unchanged when there is
    /// nothing to translate.
    /// </summary>
    /// <param name="options">The caller's options. Never mutated.</param>
    /// <param name="logger">Optional logger, used when a hint is skipped.</param>
    public static ImageGenerationOptions? Apply(ImageGenerationOptions? options, ILogger? logger)
    {
        if (options?.AdditionalProperties is null)
        {
            return options;
        }

        var quality = Read(options.AdditionalProperties, QualityKey, KnownQualities, logger);
        var style = Read(options.AdditionalProperties, StyleKey, KnownStyles, logger);

        if (quality is null && style is null)
        {
            return options;
        }

        if (options.RawRepresentationFactory is not null)
        {
            // A caller that has gone to the trouble of building the SDK options itself is more specific than
            // a profile-level hint, and overwriting it would silently drop whatever else it set.
            logger?.LogDebug(
                "Image quality/style hints were ignored because the caller supplied its own raw representation.");
            return options;
        }

        // Clone so the caller's instance is never mutated, then hand the SDK its own options type. The
        // adapter fills everything the representation leaves empty (model, n, output format).
        var effective = options.Clone();
        effective.RawRepresentationFactory = _ =>
        {
            var raw = new SdkImageGenerationOptions();

            if (quality is not null)
            {
                raw.Quality = new global::OpenAI.Images.GeneratedImageQuality(quality);
            }

            if (style is not null)
            {
                raw.Style = new global::OpenAI.Images.GeneratedImageStyle(style);
            }

            return raw;
        };

        return effective;
    }

    private static string? Read(
        AdditionalPropertiesDictionary additionalProperties,
        string key,
        HashSet<string> known,
        ILogger? logger)
    {
        if (!additionalProperties.TryGetValue(key, out var raw))
        {
            return null;
        }

        var value = (raw as string)?.Trim();
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        if (!known.Contains(value))
        {
            // Skipped rather than sent: an unrecognised spelling would fail the request outright, and a
            // profile that has been saved with one should still be able to generate an image.
            logger?.LogDebug(
                "Ignoring unrecognised image {Hint} value '{Value}'.",
                key,
                value);
            return null;
        }

        return value.ToLowerInvariant();
    }
}
