using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Html;

using Umbraco.Cms.Core.Models.Blocks;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Strings;
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
    /// Caps how deeply nested blocks/elements we walk before bailing out with a placeholder. The
    /// CMS published model graph has cycles (every <see cref="IPublishedContentType"/> exposes its
    /// <c>PropertyTypes</c>, each of which references its parent <c>ContentType</c>) and
    /// <c>BlockListModel</c> values can also contain themselves. We never serialize the cyclic
    /// metadata but a defensive cap keeps an unexpected new wrapper type from blowing the stack.
    /// </summary>
    private const int MaxElementDepth = 8;

    /// <summary>
    /// Options for copying an unrecognised property value to JSON: cycles are written as null and
    /// deep graphs fail fast instead of producing a huge payload for the model.
    /// </summary>
    private static readonly JsonSerializerOptions SafeValueSerializerOptions = new(Constants.DefaultJsonSerializerOptions)
    {
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        MaxDepth = 16,
        Converters = { new PublishedModelReferenceConverter() },
    };

    /// <summary>
    /// Caps the JSON copied from an unrecognised property value; anything longer is sent as
    /// truncated text so one property can't flood the model's context.
    /// </summary>
    private const int MaxJsonLength = MaxStringLength * 4;

    /// <summary>
    /// Extracts properties from a published content item in an LLM-friendly format.
    /// </summary>
    /// <param name="content">The published content item.</param>
    /// <param name="culture">Optional culture for variant content.</param>
    /// <returns>A list of formatted property items.</returns>
    public static IReadOnlyList<ContentPropertyItem> ExtractProperties(
        IPublishedContent content,
        string? culture = null)
    {
        var properties = new List<ContentPropertyItem>();

        foreach (var property in content.Properties)
        {
            var value = property.GetValue(culture);
            var formattedValue = FormatValue(value, property.PropertyType.EditorAlias, culture, depth: 0);

            properties.Add(new ContentPropertyItem(
                property.Alias,
                property.PropertyType.DataType.EditorAlias,
                formattedValue));
        }

        return properties;
    }

    private static object? FormatValue(object? value, string editorAlias, string? culture, int depth)
    {
        if (value is null)
        {
            return null;
        }

        return editorAlias switch
        {
            "Umbraco.RichText" or "Umbraco.TinyMCE" or "Umbraco.MarkdownEditor" => FormatRichText(value),
            "Umbraco.MediaPicker3" => FormatMediaPicker(value),
            "Umbraco.MultiNodeTreePicker" => FormatContentPicker(value),
            _ => FormatElementOrDefault(value, culture, depth),
        };
    }

    private static object? FormatRichText(object? value)
    {
        // RichText and Markdown values can be IHtmlContent, IHtmlEncodedString, string with HTML, or complex objects
        string? html = null;

        if (value is IHtmlContent htmlContent)
        {
            using var writer = new System.IO.StringWriter();
            htmlContent.WriteTo(writer, System.Text.Encodings.Web.HtmlEncoder.Default);
            html = writer.ToString();
        }
        else if (value is IHtmlEncodedString encodedString)
        {
            // Published rich text and Markdown (rendered to HTML) are IHtmlEncodedString, which is not IHtmlContent
            html = encodedString.ToHtmlString();
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
        plainText = WebUtility.HtmlDecode(plainText);
        plainText = CollapseWhitespaceRegex().Replace(plainText, " ").Trim();

        return Truncate(plainText);
    }

    private static object? FormatMediaPicker(object? value)
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

    private static object FormatElement(IPublishedElement element, string? culture, int depth)
    {
        var props = new Dictionary<string, object?>();

        foreach (var property in element.Properties)
        {
            var val = property.GetValue(culture);
            props[property.Alias] = FormatValue(val, property.PropertyType.EditorAlias, culture, depth + 1);
        }

        // Only the alias is emitted from element.ContentType — the IPublishedContentType graph
        // self-references its property types and would cycle if walked further.
        return new { contentType = element.ContentType.Alias, properties = props };
    }

    private static object FormatBlockItem(IBlockReference<IPublishedElement, IPublishedElement> block, string? culture, int depth)
    {
        var content = FormatElement(block.Content, culture, depth);
        var settings = block.Settings is not null
            ? FormatElement(block.Settings, culture, depth)
            : null;

        // SettingsKey lives on the concrete subclasses (BlockListItem / BlockGridItem /
        // RichTextBlockItem), not on IBlockReference, so we pattern-match the known shipped types
        // rather than reaching for reflection. Unknown wrapper types fall through with a null key.
        Guid? settingsKey = block switch
        {
            BlockListItem li => li.SettingsKey,
            BlockGridItem gi => gi.SettingsKey,
            _ => null,
        };

        // BlockGridItem also carries grid layout (rowSpan/colSpan/areas). Areas hold further block
        // items so they need recursive formatting; a missing Areas list (block-list, RTE-block,
        // anything else implementing the same interface) just emits content + settings.
        if (block is BlockGridItem gridItem)
        {
            var areas = gridItem.Areas?.Select(a => new
            {
                alias = a.Alias,
                rowSpan = a.RowSpan,
                columnSpan = a.ColumnSpan,
                items = a.Select(item => FormatBlockItem(item, culture, depth + 1)).ToArray(),
            }).ToArray();

            return new
            {
                contentKey = gridItem.ContentKey,
                settingsKey,
                rowSpan = gridItem.RowSpan,
                columnSpan = gridItem.ColumnSpan,
                content,
                settings,
                areas,
            };
        }

        return new
        {
            contentKey = block.ContentKey,
            settingsKey,
            content,
            settings,
        };
    }

    private static object? FormatElementOrDefault(object? value, string? culture, int depth)
    {
        if (depth >= MaxElementDepth)
        {
            return $"… (nested {depth} levels deep — truncated)";
        }

        // IPublishedContent extends IPublishedElement, so check it first.
        // IPublishedContent = cross-reference to another content/media node (show as link).
        // IPublishedElement = embedded data owned by this property (extract nested properties).
        if (value is IEnumerable<IPublishedContent> contentItems)
        {
            return contentItems.Select(c => new { key = c.Key, name = c.Name, url = c.Url() }).ToArray();
        }

        if (value is IPublishedContent content)
        {
            return new { key = content.Key, name = content.Name, url = content.Url() };
        }

        // BlockListModel / BlockGridModel / RichTextBlockModel are all collections of items
        // implementing IBlockReference<IPublishedElement, IPublishedElement>; using the interface
        // covers strongly-typed subclasses like `BlockListItem<MyHero>` for free.
        if (value is IEnumerable<IBlockReference<IPublishedElement, IPublishedElement>> blockItems)
        {
            return blockItems.Select(b => FormatBlockItem(b, culture, depth + 1)).ToArray();
        }

        if (value is IBlockReference<IPublishedElement, IPublishedElement> blockItem)
        {
            return FormatBlockItem(blockItem, culture, depth + 1);
        }

        if (value is IEnumerable<IPublishedElement> elements)
        {
            return elements.Select(e => FormatElement(e, culture, depth + 1)).ToArray();
        }

        if (value is IPublishedElement element)
        {
            return FormatElement(element, culture, depth + 1);
        }

        return FormatDefault(value);
    }

    private static object? FormatDefault(object? value)
    {
        switch (value)
        {
            case null:
                return null;
            case string str:
                return Truncate(str);
            case JsonElement or JsonNode:
                return value;
            case Enum:
                return value.ToString();
        }

        Type type = value.GetType();
        if (type.IsPrimitive || value is decimal or DateTime or DateTimeOffset or DateOnly or TimeOnly or TimeSpan or Guid)
        {
            return value;
        }

        // Anything else is a property value converter's own model (a link, a colour, or a package's
        // type such as Commerce variants). Some of those hold published model graphs that refer back
        // to themselves, which fail serialization for the AI provider and take the whole tool call
        // down. Copy it to JSON here: published elements and content types are written as short
        // references, cycles are dropped, and depth and size are capped.
        try
        {
            JsonElement json = JsonSerializer.SerializeToElement(value, type, SafeValueSerializerOptions);
            var rawJson = json.GetRawText();
            return rawJson.Length <= MaxJsonLength ? json : Truncate(rawJson);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Deliberate: one value that can't be serialized (or whose getters throw) must not fail
            // the whole tool call, so the model gets its text form instead.
            return FormatFallbackText(value, type);
        }
    }

    private static string FormatFallbackText(object value, Type type)
    {
        try
        {
            return Truncate(value.ToString() ?? type.Name);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return type.Name;
        }
    }

    /// <summary>
    /// Writes published model types found inside an unrecognised value as short references. Their
    /// full graphs (content type schema, property types, data type configuration) carry no
    /// property values and would repeat for every item, so they only cost tokens.
    /// </summary>
    private sealed class PublishedModelReferenceConverter : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert) =>
            typeof(IPublishedElement).IsAssignableFrom(typeToConvert)
            || typeof(IPublishedContentType).IsAssignableFrom(typeToConvert)
            || typeof(IPublishedPropertyType).IsAssignableFrom(typeToConvert);

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
            (JsonConverter)Activator.CreateInstance(typeof(Writer<>).MakeGenericType(typeToConvert))!;

        private sealed class Writer<T> : JsonConverter<T>
        {
            public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
                throw new NotSupportedException();

            public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
            {
                switch (value)
                {
                    case IPublishedElement element:
                        writer.WriteStartObject();
                        writer.WriteString("key", element.Key);
                        writer.WriteString("name", element.Name);
                        writer.WriteString("contentType", element.ContentType.Alias);
                        writer.WriteEndObject();
                        break;
                    case IPublishedContentType contentType:
                        writer.WriteStringValue(contentType.Alias);
                        break;
                    case IPublishedPropertyType propertyType:
                        writer.WriteStringValue(propertyType.Alias);
                        break;
                    default:
                        writer.WriteNullValue();
                        break;
                }
            }
        }
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
