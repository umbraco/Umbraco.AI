namespace Umbraco.AI.Core.Media;

/// <summary>
/// Interface for resolving Umbraco media from various value formats (GUIDs, paths, JSON).
/// </summary>
public interface IAIUmbracoMediaResolver
{
    /// <summary>
    /// Attempts to resolve an media from the given value.
    /// </summary>
    /// <param name="value">
    ///     The value to resolve. Accepts various formats:
    ///     <list type="bullet">
    ///         <item><description>Direct <see cref="Guid"/> - fetch media by ID</description></item>
    ///         <item><description>GUID string - parse and fetch</description></item>
    ///         <item><description>Media picker JSON: {"mediaKey": "guid"} or [{"mediaKey": "guid"}]</description></item>
    ///         <item><description>Image cropper JSON: {"src": "/media/..."}</description></item>
    ///         <item><description>File path string - read directly from storage</description></item>
    ///     </list>
    /// </param>
    /// <param name="cropAlias">
    ///     Optional image cropper crop alias. When supplied, the resolver will apply the
    ///     matching crop defined on the image cropper payload before returning. Callers
    ///     requesting a crop that does not exist, or values without an image cropper
    ///     payload, fall back to the uncropped original.
    /// </param>
    /// <param name="cancellationToken"></param>
    /// <returns>
    /// The resolved media content, or <c>null</c> if the value cannot be resolved to media.
    /// </returns>
    Task<AIMediaContent?> ResolveAsync(object? value, string? cropAlias = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves the MIME type of the media referenced by <paramref name="value"/> without reading
    /// its file content, by sourcing it from the actual umbracoFile property rather than a
    /// caller-supplied display name.
    /// </summary>
    /// <param name="value">The value to resolve. Accepts the same formats as <see cref="ResolveAsync"/>.</param>
    /// <returns>The resolved MIME type, or <c>null</c> if it can't be determined.</returns>
    string? GetMediaType(object? value);
}
