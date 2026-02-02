namespace Umbraco.AI.Prompt.Core.Media;

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
    /// <param name="cancellationToken"></param>
    /// <returns>
    /// The resolved media content, or <c>null</c> if the value cannot be resolved to media.
    /// </returns>
    Task<AIMediaContent?> ResolveAsync(object? value, CancellationToken cancellationToken = default);
}
