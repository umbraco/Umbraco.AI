using System.ComponentModel;

namespace Umbraco.Ai.Core.Tools.Web;

/// <summary>
/// Arguments for the FetchWebPage tool.
/// </summary>
/// <param name="Url">The URL to fetch.</param>
public record FetchWebPageArgs(
    [property: Description("The URL of the webpage to fetch and extract text content from")]
    string Url);

/// <summary>
/// Result of the FetchWebPage tool.
/// </summary>
/// <param name="Success">Whether the fetch succeeded.</param>
/// <param name="Content">The extracted content, if successful.</param>
/// <param name="Error">Error message if failed.</param>
public record FetchWebPageResult(
    bool Success,
    WebPageContent? Content,
    string? Error);

/// <summary>
/// Extracted web page content.
/// </summary>
/// <param name="Url">The URL that was fetched.</param>
/// <param name="Title">The page title.</param>
/// <param name="TextContent">The extracted text content.</param>
/// <param name="Excerpt">A short excerpt (first 200 chars).</param>
/// <param name="WordCount">Approximate word count.</param>
/// <param name="FetchedAt">When the content was fetched.</param>
public record WebPageContent(
    string Url,
    string? Title,
    string TextContent,
    string Excerpt,
    int WordCount,
    DateTime FetchedAt);
