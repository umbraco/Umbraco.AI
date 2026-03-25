using System.Net;
using System.Text.RegularExpressions;

namespace Umbraco.AI.Search.Core;

/// <summary>
/// Utility for cleaning text before chunking and embedding.
/// </summary>
public static partial class AITextSanitizer
{
    /// <summary>
    /// Strips HTML tags, decodes HTML entities, and normalizes whitespace.
    /// </summary>
    public static string StripHtml(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        var plainText = HtmlTagRegex().Replace(text, " ");
        plainText = WebUtility.HtmlDecode(plainText);
        plainText = WhitespaceRegex().Replace(plainText, " ").Trim();

        return plainText;
    }

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
