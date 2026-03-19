using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Models.Blocks;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Strings;

namespace Umbraco.AI.Core.SemanticSearch;

/// <summary>
/// Extracts embeddable text from published content by iterating properties
/// and concatenating text-friendly values.
/// </summary>
internal partial class ContentTextExtractor : IContentTextExtractor
{
    private const int MaxDepth = 5;

    private readonly AISemanticSearchOptions _options;

    public ContentTextExtractor(IOptions<AISemanticSearchOptions> options)
    {
        _options = options.Value;
    }

    /// <inheritdoc />
    public string? ExtractText(IPublishedContent content)
    {
        var sb = new StringBuilder();

        // Start with the content name as a header
        if (!string.IsNullOrWhiteSpace(content.Name))
        {
            sb.AppendLine(content.Name);
            sb.AppendLine();
        }

        ExtractElementText(content, sb, depth: 0);

        var result = sb.ToString().Trim();

        if (string.IsNullOrWhiteSpace(result))
        {
            return null;
        }

        // Truncate to max length
        if (result.Length > _options.MaxTextLength)
        {
            result = result[.._options.MaxTextLength];
        }

        return result;
    }

    private static void ExtractElementText(IPublishedElement element, StringBuilder sb, int depth)
    {
        if (depth > MaxDepth)
        {
            return;
        }

        foreach (var property in element.Properties)
        {
            ExtractPropertyText(property, sb, depth);
        }
    }

    private static void ExtractPropertyText(IPublishedProperty property, StringBuilder sb, int depth)
    {
        var value = property.GetValue();
        if (value is null)
        {
            return;
        }

        switch (value)
        {
            case string s when !string.IsNullOrWhiteSpace(s):
                sb.AppendLine(s);
                break;

            // IHtmlEncodedString is the type returned by RTE properties
            case IHtmlEncodedString html:
                var stripped = StripHtml(html.ToHtmlString());
                if (!string.IsNullOrWhiteSpace(stripped))
                {
                    sb.AppendLine(stripped);
                }

                break;

            // Collections (e.g., tags)
            case IEnumerable<string> strings:
                var joined = string.Join(", ", strings);
                if (!string.IsNullOrWhiteSpace(joined))
                {
                    sb.AppendLine(joined);
                }

                break;

            // Block Grid - recurse into items and their areas
            case BlockGridModel blockGrid:
                ExtractBlockGridText(blockGrid, sb, depth);
                break;

            // Block List - recurse into items
            case BlockListModel blockList:
                ExtractBlockItemsText(blockList, sb, depth);
                break;
        }
    }

    private static void ExtractBlockGridText(IEnumerable<BlockGridItem> items, StringBuilder sb, int depth)
    {
        foreach (var item in items)
        {
            ExtractElementText(item.Content, sb, depth + 1);

            foreach (var area in item.Areas)
            {
                ExtractBlockGridText(area, sb, depth);
            }
        }
    }

    private static void ExtractBlockItemsText(
        IEnumerable<IBlockReference<IPublishedElement, IPublishedElement>> items,
        StringBuilder sb,
        int depth)
    {
        foreach (var item in items)
        {
            ExtractElementText(item.Content, sb, depth + 1);
        }
    }

    private static string? StripHtml(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return null;
        }

        // Remove HTML tags
        var text = HtmlTagRegex().Replace(html, " ");

        // Collapse whitespace
        text = WhitespaceRegex().Replace(text, " ").Trim();

        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
