namespace Umbraco.AI.Core.Settings;

/// <summary>
/// Configuration options for experimental Umbraco.AI features.
/// </summary>
/// <remarks>
/// <para>
/// Experimental features are hidden and inert by default while the underlying APIs stabilise.
/// Enable one by setting its flag to <c>true</c> under the <c>Umbraco:AI:Experimental</c>
/// configuration section, e.g.:
/// </para>
/// <code>
/// { "Umbraco": { "AI": { "Experimental": { "ImageGeneration": true } } } }
/// </code>
/// <para>
/// Add a new property here for each experimental feature and wire it into
/// <see cref="IAIExperimentalFeatures"/>.
/// </para>
/// </remarks>
public sealed class AIExperimentalOptions
{
    /// <summary>
    /// Gets or sets whether the image-generation capability is enabled. Default is <c>false</c>.
    /// </summary>
    /// <remarks>
    /// When disabled the capability is hidden from discovery (not selectable in the profile editor),
    /// profiles using it cannot be created, and its REST endpoint returns 404.
    /// </remarks>
    public bool ImageGeneration { get; set; } = false;
}
