namespace Umbraco.AI.Core.Profiles;

/// <summary>
/// Profile settings specific to the image-generation capability.
/// </summary>
/// <remarks>
/// These are use-case <em>policy</em> defaults ("house style") for a profile, overridable per call.
/// Request mechanics such as image count and response format are intentionally <em>not</em> here — they
/// are passed at generation time (e.g. via the builder options or the REST request).
/// </remarks>
public sealed class AIImageGenerationProfileSettings : IAIProfileSettings
{
    /// <summary>
    /// Default image size as <c>"{width}x{height}"</c> (e.g. <c>"1024x1024"</c>).
    /// </summary>
    public string? Size { get; init; }

    /// <summary>
    /// Provider-specific quality hint (e.g. <c>"hd"</c> for DALL·E 3, <c>"high"</c> for gpt-image-1).
    /// Supported values vary by model.
    /// </summary>
    public string? Quality { get; init; }

    /// <summary>
    /// Provider-specific style hint (e.g. <c>"vivid"</c>, <c>"natural"</c> for DALL·E 3).
    /// Supported values vary by model.
    /// </summary>
    public string? Style { get; init; }

    /// <summary>
    /// Default output media type (MIME type) of the generated images, e.g. <c>"image/png"</c>,
    /// <c>"image/jpeg"</c>, <c>"image/webp"</c>. Supported values vary by model.
    /// </summary>
    public string? MediaType { get; init; }
}
