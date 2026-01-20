namespace Umbraco.Ai.Prompt.Core.Media;

/// <summary>
/// Interface for resolving images from various value formats (GUIDs, paths, JSON).
/// </summary>
public interface IAiMediaImageResolver
{
    /// <summary>
    /// Attempts to resolve an image from the given value.
    /// </summary>
    /// <param name="value">
    /// The value to resolve. Accepts various formats:
    /// <list type="bullet">
    ///     <item><description>Direct <see cref="Guid"/> - fetch media by ID</description></item>
    ///     <item><description>GUID string - parse and fetch</description></item>
    ///     <item><description>Media picker JSON: {"mediaKey": "guid"} or [{"mediaKey": "guid"}]</description></item>
    ///     <item><description>Image cropper JSON: {"src": "/media/..."}</description></item>
    ///     <item><description>File path string - read directly from storage</description></item>
    /// </list>
    /// </param>
    /// <returns>
    /// The resolved image content, or <c>null</c> if the value cannot be resolved to an image.
    /// </returns>
    AiImageContent? Resolve(object? value);
}
