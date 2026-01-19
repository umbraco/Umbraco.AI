using System.Text.RegularExpressions;
using HtmlAgilityPack;
using SmartReader;

namespace Umbraco.Ai.Core.Tools.Web;

/// <summary>
/// Extracts main content from HTML using SmartReader for article extraction
/// and HtmlAgilityPack for additional sanitization.
/// </summary>
public class HtmlContentExtractor : IHtmlContentExtractor
{
    private static readonly string[] DangerousElements = {
        "script", "style", "iframe", "frame", "frameset",
        "object", "embed", "applet", "link", "meta", "base"
    };

    /// <inheritdoc />
    public Task<ExtractedContent> ExtractAsync(string html, string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(html))
            return Task.FromResult(new ExtractedContent(null, string.Empty, string.Empty));

        // Try SmartReader first for intelligent article extraction
        var article = TryExtractWithSmartReader(html, baseUrl);

        if (article != null && !string.IsNullOrWhiteSpace(article.TextContent))
        {
            // SmartReader succeeded - use its extracted content
            var textContent = CleanText(article.TextContent);
            var excerpt = !string.IsNullOrWhiteSpace(article.Excerpt)
                ? article.Excerpt
                : GenerateExcerpt(textContent);

            return Task.FromResult(new ExtractedContent(article.Title, textContent, excerpt));
        }

        // Fallback to basic HtmlAgilityPack extraction
        return Task.FromResult(ExtractWithHtmlAgilityPack(html));
    }

    /// <summary>
    /// Attempts to extract article content using SmartReader.
    /// </summary>
    private static Article? TryExtractWithSmartReader(string html, string baseUrl)
    {
        try
        {
            // Create a valid URI for SmartReader
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
            {
                uri = new Uri("https://example.com");
            }

            var reader = new Reader(baseUrl, html);
            var article = reader.GetArticle();

            // Check if SmartReader found meaningful content
            if (article.IsReadable)
            {
                return article;
            }

            return null;
        }
        catch
        {
            // SmartReader failed - will fall back to basic extraction
            return null;
        }
    }

    /// <summary>
    /// Fallback extraction using HtmlAgilityPack when SmartReader fails.
    /// </summary>
    private static ExtractedContent ExtractWithHtmlAgilityPack(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // Remove dangerous elements
        RemoveDangerousElements(doc);

        // Strip event handlers
        StripEventHandlers(doc);

        // Extract title
        var title = ExtractTitle(doc);

        // Extract main content
        var contentNode = doc.DocumentNode.SelectSingleNode("//article") ??
                          doc.DocumentNode.SelectSingleNode("//main") ??
                          doc.DocumentNode.SelectSingleNode("//div[@class='content']") ??
                          doc.DocumentNode.SelectSingleNode("//div[@id='content']") ??
                          doc.DocumentNode;

        // Get text content
        var textContent = CleanText(contentNode.InnerText);

        // Generate excerpt
        var excerpt = GenerateExcerpt(textContent);

        return new ExtractedContent(title, textContent, excerpt);
    }

    /// <summary>
    /// Generates an excerpt from text content (first 200 characters).
    /// </summary>
    private static string GenerateExcerpt(string textContent)
    {
        if (string.IsNullOrWhiteSpace(textContent))
            return string.Empty;

        return textContent.Length > 200
            ? textContent.Substring(0, 200) + "..."
            : textContent;
    }

    /// <summary>
    /// Removes dangerous HTML elements that could pose security risks.
    /// </summary>
    private static void RemoveDangerousElements(HtmlDocument doc)
    {
        foreach (var elementName in DangerousElements)
        {
            var nodes = doc.DocumentNode.SelectNodes($"//{elementName}");
            if (nodes != null)
            {
                foreach (var node in nodes.ToList())
                {
                    node.Remove();
                }
            }
        }
    }

    /// <summary>
    /// Strips event handler attributes from all elements.
    /// </summary>
    private static void StripEventHandlers(HtmlDocument doc)
    {
        var allNodes = doc.DocumentNode.SelectNodes("//*[@*]");
        if (allNodes == null)
            return;

        foreach (var node in allNodes)
        {
            var attributesToRemove = node.Attributes
                .Where(attr => attr.Name.StartsWith("on", StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var attr in attributesToRemove)
            {
                node.Attributes.Remove(attr);
            }
        }
    }

    /// <summary>
    /// Extracts the page title from HTML.
    /// </summary>
    private static string? ExtractTitle(HtmlDocument doc)
    {
        // Try <title> tag first
        var titleNode = doc.DocumentNode.SelectSingleNode("//title");
        if (titleNode != null && !string.IsNullOrWhiteSpace(titleNode.InnerText))
            return HtmlEntity.DeEntitize(titleNode.InnerText.Trim());

        // Fallback to first <h1>
        var h1Node = doc.DocumentNode.SelectSingleNode("//h1");
        if (h1Node != null && !string.IsNullOrWhiteSpace(h1Node.InnerText))
            return HtmlEntity.DeEntitize(h1Node.InnerText.Trim());

        return null;
    }

    /// <summary>
    /// Cleans text content by normalizing whitespace and removing excess newlines.
    /// </summary>
    private static string CleanText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        // Decode HTML entities
        text = HtmlEntity.DeEntitize(text);

        // Normalize line breaks
        text = Regex.Replace(text, @"\r\n|\r|\n", "\n");

        // Remove multiple consecutive newlines
        text = Regex.Replace(text, @"\n{3,}", "\n\n");

        // Normalize whitespace on each line
        var lines = text.Split('\n');
        lines = lines.Select(line => Regex.Replace(line.Trim(), @"\s+", " ")).ToArray();

        // Join lines and trim
        text = string.Join("\n", lines).Trim();

        return text;
    }
}
