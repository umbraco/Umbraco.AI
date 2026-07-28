namespace Umbraco.AI.Core.Profiles;

/// <summary>
/// Profile settings specific to the image-generation capability.
/// </summary>
/// <remarks>
/// <para>
/// These are use-case <em>policy</em> defaults ("house style") for a profile, overridable per call.
/// Request mechanics such as image count and response format are intentionally <em>not</em> here — they
/// are passed at generation time (e.g. via the builder options or the REST request).
/// </para>
/// <para>
/// Only settings Microsoft.Extensions.AI models as first-class options live here. Provider-specific hints
/// such as quality and style are declared by the provider as capability settings instead, so their values
/// can be offered per model rather than typed blind.
/// </para>
/// </remarks>
public sealed class AIImageGenerationProfileSettings : IAIProfileSettings
{
    /// <summary>
    /// Default image size as <c>"{width}x{height}"</c> (e.g. <c>"1024x1024"</c>).
    /// </summary>
    public string? Size { get; init; }

    /// <summary>
    /// Default output media type (MIME type) of the generated images, e.g. <c>"image/png"</c>,
    /// <c>"image/jpeg"</c>, <c>"image/webp"</c>. Supported values vary by model.
    /// </summary>
    public string? MediaType { get; init; }
}
