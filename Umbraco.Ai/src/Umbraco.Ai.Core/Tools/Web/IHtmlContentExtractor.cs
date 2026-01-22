namespace Umbraco.Ai.Core.Tools.Web;

/// <summary>
/// Extracts main content from HTML.
/// </summary>
public interface IHtmlContentExtractor
{
    /// <summary>
    /// Extracts text content from HTML.
    /// </summary>
    /// <param name="html">The HTML content.</param>
    /// <param name="baseUrl">The base URL for resolving relative links.</param>
    /// <returns>Extracted content.</returns>
    Task<ExtractedContent> ExtractAsync(string html, string baseUrl);
}

/// <summary>
/// Extracted content from HTML.
/// </summary>
/// <param name="Title">Page title.</param>
/// <param name="TextContent">Extracted text content.</param>
/// <param name="Excerpt">Short excerpt.</param>
public record ExtractedContent(
    string? Title,
    string TextContent,
    string Excerpt);
