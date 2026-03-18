using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace Umbraco.AI.Core.SemanticSearch;

/// <summary>
/// Extracts embeddable text from published content by iterating properties
/// and concatenating text-friendly values.
/// </summary>
internal partial class ContentTextExtractor : IContentTextExtractor
{
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

        // Iterate properties and extract text
        foreach (var property in content.Properties)
        {
            var text = ExtractPropertyText(property);
            if (!string.IsNullOrWhiteSpace(text))
            {
                sb.AppendLine(text);
            }
        }

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

    private static string? ExtractPropertyText(IPublishedProperty property)
    {
        var value = property.GetValue();
        if (value is null)
        {
            return null;
        }

        return value switch
        {
            string s => string.IsNullOrWhiteSpace(s) ? null : s,

            // IHtmlEncodedString is the type returned by RTE properties
            IHtmlEncodedString html => StripHtml(html.ToHtmlString()),

            // Collections (e.g., tags)
            IEnumerable<string> strings => string.Join(", ", strings),

            // Other value types - skip non-text types like pickers, media references, etc.
            _ => null
        };
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
