using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Html;

using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace Umbraco.AI.Core.Tools.Umbraco;

/// <summary>
/// Formats published content property values into LLM-friendly representations.
/// Handles editor-specific formatting (e.g., RichText HTML to plain text, media picker GUIDs to URLs).
/// </summary>
internal static partial class PropertyValueFormatter
{
    private const int MaxStringLength = 2000;

    /// <summary>
    /// Extracts properties from a published content item in an LLM-friendly format.
    /// </summary>
    /// <param name="content">The published content item.</param>
    /// <param name="umbracoContextAccessor">The Umbraco context accessor for resolving media URLs.</param>
    /// <param name="culture">Optional culture for variant content.</param>
    /// <returns>A list of formatted property items.</returns>
    public static IReadOnlyList<ContentPropertyItem> ExtractProperties(
        IPublishedContent content,
        IUmbracoContextAccessor umbracoContextAccessor,
        string? culture = null)
    {
        var properties = new List<ContentPropertyItem>();

        foreach (var property in content.Properties)
        {
            var value = property.GetValue(culture);
            var formattedValue = FormatValue(value, property.PropertyType.EditorAlias, umbracoContextAccessor);

            properties.Add(new ContentPropertyItem(
                property.Alias,
                property.PropertyType.DataType.EditorAlias,
                formattedValue));
        }

        return properties;
    }

    private static object? FormatValue(object? value, string editorAlias, IUmbracoContextAccessor umbracoContextAccessor)
    {
        if (value is null)
        {
            return null;
        }

        return editorAlias switch
        {
            "Umbraco.RichText" or "Umbraco.TinyMCE" => FormatRichText(value),
            "Umbraco.MediaPicker3" => FormatMediaPicker(value, umbracoContextAccessor),
            "Umbraco.MultiNodeTreePicker" => FormatContentPicker(value),
            _ => FormatDefault(value),
        };
    }

    private static object? FormatRichText(object? value)
    {
        // RichText values can be IHtmlContent, string with HTML, or complex objects
        string? html = null;

        if (value is IHtmlContent htmlContent)
        {
            using var writer = new System.IO.StringWriter();
            htmlContent.WriteTo(writer, System.Text.Encodings.Web.HtmlEncoder.Default);
            html = writer.ToString();
        }
        else if (value is string str)
        {
            html = str;
        }
        else
        {
            return FormatDefault(value);
        }

        if (string.IsNullOrWhiteSpace(html))
        {
            return null;
        }

        // Strip HTML tags to plain text for LLM consumption
        var plainText = StripHtmlRegex().Replace(html, " ");
        plainText = CollapseWhitespaceRegex().Replace(plainText, " ").Trim();

        return Truncate(plainText);
    }

    private static object? FormatMediaPicker(object? value, IUmbracoContextAccessor umbracoContextAccessor)
    {
        // MediaPicker3 typically resolves to IPublishedContent or IEnumerable<IPublishedContent>
        if (value is IPublishedContent media)
        {
            return new { name = media.Name, url = media.Url(), mediaType = media.ContentType.Alias };
        }

        if (value is IEnumerable<IPublishedContent> mediaItems)
        {
            return mediaItems.Select(m => new { name = m.Name, url = m.Url(), mediaType = m.ContentType.Alias }).ToArray();
        }

        return FormatDefault(value);
    }

    private static object? FormatContentPicker(object? value)
    {
        if (value is IPublishedContent content)
        {
            return new { key = content.Key, name = content.Name, url = content.Url() };
        }

        if (value is IEnumerable<IPublishedContent> contentItems)
        {
            return contentItems.Select(c => new { key = c.Key, name = c.Name, url = c.Url() }).ToArray();
        }

        return FormatDefault(value);
    }

    private static object? FormatDefault(object? value)
    {
        if (value is string str)
        {
            return Truncate(str);
        }

        return value;
    }

    private static string Truncate(string value)
    {
        if (value.Length <= MaxStringLength)
        {
            return value;
        }

        return string.Concat(value.AsSpan(0, MaxStringLength), "... (truncated)");
    }

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex StripHtmlRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex CollapseWhitespaceRegex();
}

/// <summary>
/// A single property from a content item, formatted for LLM consumption.
/// </summary>
/// <param name="Alias">The property alias.</param>
/// <param name="EditorAlias">The property editor alias (e.g., "Umbraco.TextBox").</param>
/// <param name="Value">The formatted property value.</param>
public record ContentPropertyItem(
    string Alias,
    string EditorAlias,
    object? Value);
