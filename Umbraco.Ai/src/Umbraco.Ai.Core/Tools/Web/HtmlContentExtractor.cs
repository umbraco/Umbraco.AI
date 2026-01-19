using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace Umbraco.Ai.Core.Tools.Web;

/// <summary>
/// Extracts main content from HTML using HtmlAgilityPack.
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

        // Parse HTML
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // Remove dangerous elements
        RemoveDangerousElements(doc);

        // Strip event handlers
        StripEventHandlers(doc);

        // Extract title
        var title = ExtractTitle(doc);

        // Extract main content
        // Try to find article or main content area first
        var contentNode = doc.DocumentNode.SelectSingleNode("//article") ??
                          doc.DocumentNode.SelectSingleNode("//main") ??
                          doc.DocumentNode.SelectSingleNode("//div[@class='content']") ??
                          doc.DocumentNode.SelectSingleNode("//div[@id='content']") ??
                          doc.DocumentNode;

        // Get text content
        var textContent = CleanText(contentNode.InnerText);

        // Generate excerpt (first 200 characters)
        var excerpt = textContent.Length > 200
            ? textContent.Substring(0, 200) + "..."
            : textContent;

        return Task.FromResult(new ExtractedContent(title, textContent, excerpt));
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
