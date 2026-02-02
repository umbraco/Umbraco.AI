# AIEntitySnapshotService Design Plan

## Overview

The `AIEntitySnapshotService` creates simplified, consistent snapshots of Umbraco entities (Content, Media, Member) to provide context for AI requests. These snapshots support mustache-style placeholder templates that allow dynamic property access via dot notation.

## Goals

1. **Consistent Serialization**: Convert Umbraco entities into a clean, predictable JSON structure
2. **Culture/Variant Awareness**: Each snapshot represents a specific culture/variant
3. **Template Integration**: Support mustache placeholders (e.g., `{{content.name}}`, `{{content.properties.title}}`)
4. **Current Value Context**: Merge snapshots with an "active context" for property-specific AI assistance
5. **Extensible Property Handling**: Handle complex property editors (Block Grid, RTE, pickers) with extensibility
6. **Core Dependencies Only**: Depend only on `Umbraco.Cms.Core` - no Delivery API dependencies

---

## Architecture

### Design Principle: Core Services Only

We explicitly avoid dependencies on `Umbraco.Cms.Core.DeliveryApi` namespace. Instead, we use:

- `IPublishedContent` / `IPublishedElement` - Core content models
- `IPublishedProperty.GetValue()` - Core property value conversion pipeline
- `IPropertyValueConverter` - Already handles Source → Intermediate → Object conversion
- `IVariationContextAccessor` - Culture/segment context
- `IPublishedContentCache` / `IPublishedMediaCache` - Published content access
- `PublishedItemType` - Reuse existing CMS enum for entity types

The core property value conversion pipeline already returns fully-typed models:
- `BlockListModel` / `BlockGridModel` for block editors
- `MediaWithCrops` for media pickers
- `IPublishedContent` for content pickers
- Strongly-typed values for primitives

### Feature Location

Following the project's feature-sliced architecture, create a new feature folder:

```
src/Umbraco.AI.Core/
├── Snapshots/
│   ├── IAIEntitySnapshotService.cs
│   ├── AIEntitySnapshotService.cs
│   ├── IAIPropertyValueSerializer.cs
│   ├── AIPropertyValueSerializerCollection.cs
│   ├── AIPropertyValueSerializerCollectionBuilder.cs
│   ├── AIEntitySnapshot.cs
│   ├── AISnapshotOptions.cs
│   ├── AIRequestContext.cs
│   ├── Serializers/
│   │   ├── DefaultAiPropertyValueSerializer.cs
│   │   ├── BlockEditorAiPropertyValueSerializer.cs
│   │   ├── RichTextAiPropertyValueSerializer.cs
│   │   ├── MediaPickerAiPropertyValueSerializer.cs
│   │   └── ContentPickerAiPropertyValueSerializer.cs
```

### Core Interfaces

#### IAIEntitySnapshotService

```csharp
public interface IAIEntitySnapshotService
{
    /// <summary>
    /// Creates a snapshot from published content by key.
    /// </summary>
    Task<AIEntitySnapshot?> CreateSnapshotAsync(
        Guid entityKey,
        PublishedItemType entityType,
        AISnapshotOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a snapshot from an IPublishedContent instance.
    /// </summary>
    AIEntitySnapshot CreateSnapshot(
        IPublishedContent content,
        AISnapshotOptions? options = null);

    /// <summary>
    /// Creates a snapshot from an IPublishedElement (for nested content like blocks).
    /// </summary>
    AIEntitySnapshot CreateSnapshot(
        IPublishedElement element,
        AISnapshotOptions? options = null);
}
```

#### IAIPropertyValueSerializer

Extensible property value serialization using Umbraco's collection builder pattern:

```csharp
public interface IAIPropertyValueSerializer
{
    /// <summary>
    /// Returns true if this serializer handles the given property type.
    /// </summary>
    bool CanSerialize(IPublishedPropertyType propertyType);

    /// <summary>
    /// Serializes the property value to an AI-friendly format.
    /// </summary>
    /// <param name="property">The property to serialize.</param>
    /// <param name="culture">The culture for variant properties.</param>
    /// <param name="options">Snapshot options.</param>
    /// <param name="serializerCollection">Collection for recursive serialization.</param>
    /// <returns>A JSON-serializable object representing the property value.</returns>
    object? Serialize(
        IPublishedProperty property,
        string? culture,
        AISnapshotOptions options,
        AIPropertyValueSerializerCollection serializerCollection);
}
```

#### Collection Builder

```csharp
public class AIPropertyValueSerializerCollectionBuilder
    : OrderedCollectionBuilderBase<
        AIPropertyValueSerializerCollectionBuilder,
        AIPropertyValueSerializerCollection,
        IAIPropertyValueSerializer>
{
    protected override AIPropertyValueSerializerCollectionBuilder This => this;
}

public class AIPropertyValueSerializerCollection
    : BuilderCollectionBase<IAIPropertyValueSerializer>
{
    public AIPropertyValueSerializerCollection(
        Func<IEnumerable<IAIPropertyValueSerializer>> items)
        : base(items)
    {
    }

    /// <summary>
    /// Serializes a property using the first matching serializer.
    /// </summary>
    public object? Serialize(
        IPublishedProperty property,
        string? culture,
        AISnapshotOptions options)
    {
        var serializer = this.FirstOrDefault(s => s.CanSerialize(property.PropertyType));
        return serializer?.Serialize(property, culture, options, this);
    }
}
```

---

## Models

### AIEntitySnapshot

A snapshot represents a single entity at a specific culture/variant:

```csharp
public class AIEntitySnapshot
{
    /// <summary>
    /// The entity's unique key (GUID).
    /// </summary>
    public Guid Key { get; init; }

    /// <summary>
    /// Entity type: Content, Media, Member, or Element.
    /// Uses Umbraco's PublishedItemType enum.
    /// </summary>
    public PublishedItemType ItemType { get; init; }

    /// <summary>
    /// Content type alias (e.g., "blogPost", "image").
    /// </summary>
    public string ContentTypeAlias { get; init; } = string.Empty;

    /// <summary>
    /// Display name of the entity.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Creation timestamp.
    /// </summary>
    public DateTime CreateDate { get; init; }

    /// <summary>
    /// Last update timestamp.
    /// </summary>
    public DateTime UpdateDate { get; init; }

    /// <summary>
    /// URL path (for content/media). Null for elements.
    /// </summary>
    public string? Url { get; init; }

    /// <summary>
    /// The culture this snapshot was created for. Null for invariant content.
    /// </summary>
    public string? Culture { get; init; }

    /// <summary>
    /// All properties as a flat dictionary for template access.
    /// Keys are property aliases, values are serialized property values.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Properties { get; init; }
        = new Dictionary<string, object?>();

    /// <summary>
    /// Aggregated plain text from all text-based properties.
    /// Useful for sending full content context to AI models.
    /// Includes: name, text properties, rich text (HTML stripped), and text from nested blocks.
    /// </summary>
    public string AllText { get; init; } = string.Empty;
}
```

### AISnapshotOptions

```csharp
public class AISnapshotOptions
{
    /// <summary>
    /// The culture to serialize. If null, uses the current variation context.
    /// </summary>
    public string? Culture { get; init; }

    /// <summary>
    /// Maximum depth for nested content (Block Grid, Block List).
    /// Default: 3
    /// </summary>
    public int MaxDepth { get; init; } = 3;

    /// <summary>
    /// Whether to strip HTML from rich text properties.
    /// Default: true
    /// </summary>
    public bool StripHtml { get; init; } = true;

    /// <summary>
    /// Internal: Current depth for recursion tracking.
    /// </summary>
    internal int CurrentDepth { get; init; }
}
```

### AIRequestContext

For property-specific AI assistance with a "current value":

```csharp
public class AIRequestContext
{
    /// <summary>
    /// The entity snapshot providing context.
    /// </summary>
    public AIEntitySnapshot? EntitySnapshot { get; init; }

    /// <summary>
    /// The property alias being edited (if applicable).
    /// </summary>
    public string? CurrentPropertyAlias { get; init; }

    /// <summary>
    /// The current value of the property being edited.
    /// </summary>
    public object? CurrentValue { get; init; }

    /// <summary>
    /// Additional context data (key-value pairs).
    /// </summary>
    public IReadOnlyDictionary<string, object?>? AdditionalContext { get; init; }
}
```

---

## Implementation Strategy

### Phase 1: Core Snapshot Service

Using core `IPublishedProperty.GetValue()` which returns fully-converted objects:

```csharp
public class AIEntitySnapshotService : IAIEntitySnapshotService
{
    private readonly IPublishedContentCache _contentCache;
    private readonly IPublishedMediaCache _mediaCache;
    private readonly IVariationContextAccessor _variationContextAccessor;
    private readonly IPublishedUrlProvider _urlProvider;
    private readonly AIPropertyValueSerializerCollection _serializers;

    public AIEntitySnapshot CreateSnapshot(
        IPublishedContent content,
        AISnapshotOptions? options = null)
    {
        options ??= new AISnapshotOptions();
        var culture = ResolveCulture(options);
        var properties = SerializeProperties(content, culture, options);
        var name = content.Name(culture);

        return new AIEntitySnapshot
        {
            Key = content.Key,
            ItemType = content.ItemType,
            ContentTypeAlias = content.ContentType.Alias,
            Name = name,
            CreateDate = content.CreateDate,
            UpdateDate = content.UpdateDate,
            Url = GetUrl(content, culture),
            Culture = culture,
            Properties = properties,
            AllText = BuildAllText(name, properties)
        };
    }

    public AIEntitySnapshot CreateSnapshot(
        IPublishedElement element,
        AISnapshotOptions? options = null)
    {
        options ??= new AISnapshotOptions();
        var culture = ResolveCulture(options);
        var properties = SerializeProperties(element, culture, options);

        return new AIEntitySnapshot
        {
            Key = element.Key,
            ItemType = PublishedItemType.Element,
            ContentTypeAlias = element.ContentType.Alias,
            Culture = culture,
            Properties = properties,
            AllText = BuildAllText(null, properties)
        };
    }

    private string? GetUrl(IPublishedContent content, string? culture)
    {
        // Only content and media have URLs
        if (content.ItemType is PublishedItemType.Content or PublishedItemType.Media)
        {
            return content.Url(_urlProvider, culture);
        }
        return null;
    }

    private Dictionary<string, object?> SerializeProperties(
        IPublishedElement element,
        string? culture,
        AISnapshotOptions options)
    {
        var result = new Dictionary<string, object?>();

        foreach (var property in element.Properties)
        {
            if (!property.HasValue(culture))
                continue;

            var value = _serializers.Serialize(property, culture, options);
            result[property.Alias] = value;
        }

        return result;
    }

    private string? ResolveCulture(AISnapshotOptions options)
    {
        if (!string.IsNullOrEmpty(options.Culture))
            return options.Culture;

        return _variationContextAccessor.VariationContext?.Culture;
    }

    private string BuildAllText(string? name, Dictionary<string, object?> properties)
    {
        var textParts = new List<string>();

        // Include name first
        if (!string.IsNullOrWhiteSpace(name))
            textParts.Add(name);

        // Recursively extract text from all properties
        foreach (var property in properties.Values)
        {
            ExtractText(property, textParts);
        }

        return string.Join("\n\n", textParts.Where(t => !string.IsNullOrWhiteSpace(t)));
    }

    private void ExtractText(object? value, List<string> textParts)
    {
        switch (value)
        {
            case null:
                break;

            case string text when !string.IsNullOrWhiteSpace(text):
                textParts.Add(text);
                break;

            case IDictionary<string, object?> dict:
                // Handle nested objects (blocks, etc.)
                // Skip reference objects (they have key/url/contentType pattern)
                if (dict.ContainsKey("key") && dict.ContainsKey("url"))
                    break;

                // Extract from properties if present
                if (dict.TryGetValue("properties", out var props))
                {
                    ExtractText(props, textParts);
                }
                else
                {
                    // Extract from all values
                    foreach (var v in dict.Values)
                    {
                        ExtractText(v, textParts);
                    }
                }
                break;

            case IEnumerable<object> list:
                foreach (var item in list)
                {
                    ExtractText(item, textParts);
                }
                break;

            // Anonymous types from serializers (items, properties, etc.)
            case { } obj when obj.GetType().IsAnonymous():
                var objProps = obj.GetType().GetProperties();
                foreach (var prop in objProps)
                {
                    var propValue = prop.GetValue(obj);
                    ExtractText(propValue, textParts);
                }
                break;
        }
    }
}
```

### Phase 2: Property Value Serializers

Built-in serializers that handle core `IPublishedProperty.GetValue()` return types:

#### Default Serializer (Fallback)

```csharp
public class DefaultAiPropertyValueSerializer : IAIPropertyValueSerializer
{
    public bool CanSerialize(IPublishedPropertyType propertyType) => true;

    public object? Serialize(
        IPublishedProperty property,
        string? culture,
        AISnapshotOptions options,
        AIPropertyValueSerializerCollection serializerCollection)
    {
        var value = property.GetValue(culture);

        // Handle common simple types
        return value switch
        {
            null => null,
            string s => s,
            int or long or float or double or decimal or bool => value,
            DateTime dt => dt,
            Guid g => g,
            IHtmlEncodedString html => options.StripHtml
                ? StripHtml(html.ToHtmlString())
                : html.ToHtmlString(),
            IEnumerable<string> strings => strings.ToList(),
            _ => value.ToString() // Fallback to string representation
        };
    }

    private static string StripHtml(string html)
    {
        var text = System.Text.RegularExpressions.Regex.Replace(
            html, "<[^>]*>", " ");
        text = System.Net.WebUtility.HtmlDecode(text);
        text = System.Text.RegularExpressions.Regex.Replace(
            text, @"\s+", " ").Trim();
        return text;
    }
}
```

#### Block Editor Serializer

```csharp
public class BlockEditorAiPropertyValueSerializer : IAIPropertyValueSerializer
{
    public bool CanSerialize(IPublishedPropertyType propertyType)
        => propertyType.EditorAlias is
            Constants.PropertyEditors.Aliases.BlockList or
            Constants.PropertyEditors.Aliases.BlockGrid;

    public object? Serialize(
        IPublishedProperty property,
        string? culture,
        AISnapshotOptions options,
        AIPropertyValueSerializerCollection serializerCollection)
    {
        if (options.CurrentDepth >= options.MaxDepth)
            return "[Block content - max depth reached]";

        var nestedOptions = options with { CurrentDepth = options.CurrentDepth + 1 };
        var value = property.GetValue(culture);

        return value switch
        {
            BlockGridModel grid => SerializeBlockGrid(grid, culture, nestedOptions, serializerCollection),
            BlockListModel list => SerializeBlockList(list, culture, nestedOptions, serializerCollection),
            BlockListItem item => SerializeBlockItem(item, culture, nestedOptions, serializerCollection),
            _ => null
        };
    }

    private object SerializeBlockGrid(
        BlockGridModel grid,
        string? culture,
        AISnapshotOptions options,
        AIPropertyValueSerializerCollection serializers)
    {
        return new
        {
            gridColumns = grid.GridColumns,
            items = grid.Select(item => SerializeBlockGridItem(item, culture, options, serializers)).ToList()
        };
    }

    private object SerializeBlockGridItem(
        BlockGridItem item,
        string? culture,
        AISnapshotOptions options,
        AIPropertyValueSerializerCollection serializers)
    {
        return new
        {
            contentType = item.Content.ContentType.Alias,
            rowSpan = item.RowSpan,
            columnSpan = item.ColumnSpan,
            properties = SerializeElement(item.Content, culture, options, serializers),
            settings = item.Settings != null
                ? SerializeElement(item.Settings, culture, options, serializers)
                : null,
            areas = item.Areas.Select(area => new
            {
                alias = area.Alias,
                items = area.Select(nested =>
                    SerializeBlockGridItem(nested, culture, options, serializers)).ToList()
            }).ToList()
        };
    }

    private object SerializeBlockList(
        BlockListModel list,
        string? culture,
        AISnapshotOptions options,
        AIPropertyValueSerializerCollection serializers)
    {
        return new
        {
            items = list.Select(item => SerializeBlockItem(item, culture, options, serializers)).ToList()
        };
    }

    private object SerializeBlockItem(
        BlockListItem item,
        string? culture,
        AISnapshotOptions options,
        AIPropertyValueSerializerCollection serializers)
    {
        return new
        {
            contentType = item.Content.ContentType.Alias,
            properties = SerializeElement(item.Content, culture, options, serializers),
            settings = item.Settings != null
                ? SerializeElement(item.Settings, culture, options, serializers)
                : null
        };
    }

    private Dictionary<string, object?> SerializeElement(
        IPublishedElement element,
        string? culture,
        AISnapshotOptions options,
        AIPropertyValueSerializerCollection serializers)
    {
        var result = new Dictionary<string, object?>();

        foreach (var property in element.Properties)
        {
            if (property.HasValue(culture))
            {
                result[property.Alias] = serializers.Serialize(property, culture, options);
            }
        }

        return result;
    }
}
```

#### Rich Text Serializer

```csharp
public class RichTextAiPropertyValueSerializer : IAIPropertyValueSerializer
{
    public bool CanSerialize(IPublishedPropertyType propertyType)
        => propertyType.EditorAlias == Constants.PropertyEditors.Aliases.TinyMce ||
           propertyType.EditorAlias == Constants.PropertyEditors.Aliases.RichText;

    public object? Serialize(
        IPublishedProperty property,
        string? culture,
        AISnapshotOptions options,
        AIPropertyValueSerializerCollection serializerCollection)
    {
        var value = property.GetValue(culture);

        if (value is null)
            return null;

        // RichText can return IHtmlEncodedString or RichTextModel (with blocks)
        string? markup = value switch
        {
            IHtmlEncodedString html => html.ToHtmlString(),
            { } obj when obj.GetType().Name == "RichTextModel" =>
                obj.GetType().GetProperty("Markup")?.GetValue(obj)?.ToString(),
            string s => s,
            _ => value.ToString()
        };

        if (string.IsNullOrEmpty(markup))
            return null;

        return options.StripHtml ? StripHtml(markup) : markup;
    }

    private static string StripHtml(string html)
    {
        var text = System.Text.RegularExpressions.Regex.Replace(
            html, "<[^>]*>", " ");
        text = System.Net.WebUtility.HtmlDecode(text);
        text = System.Text.RegularExpressions.Regex.Replace(
            text, @"\s+", " ").Trim();
        return text;
    }
}
```

#### Media Picker Serializer

Returns a simple reference model (no expansion):

```csharp
public class MediaPickerAiPropertyValueSerializer : IAIPropertyValueSerializer
{
    private readonly IPublishedUrlProvider _urlProvider;

    public MediaPickerAiPropertyValueSerializer(IPublishedUrlProvider urlProvider)
    {
        _urlProvider = urlProvider;
    }

    public bool CanSerialize(IPublishedPropertyType propertyType)
        => propertyType.EditorAlias == Constants.PropertyEditors.Aliases.MediaPicker3;

    public object? Serialize(
        IPublishedProperty property,
        string? culture,
        AISnapshotOptions options,
        AIPropertyValueSerializerCollection serializerCollection)
    {
        var value = property.GetValue(culture);

        return value switch
        {
            MediaWithCrops media => SerializeMediaReference(media.Content, culture),
            IEnumerable<MediaWithCrops> mediaList =>
                mediaList.Select(m => SerializeMediaReference(m.Content, culture)).ToList(),
            IPublishedContent content => SerializeMediaReference(content, culture),
            IEnumerable<IPublishedContent> contentList =>
                contentList.Select(c => SerializeMediaReference(c, culture)).ToList(),
            _ => null
        };
    }

    private object SerializeMediaReference(IPublishedContent content, string? culture)
    {
        return new
        {
            key = content.Key,
            name = content.Name,
            url = content.Url(_urlProvider),
            mediaType = content.ContentType.Alias
        };
    }
}
```

#### Content Picker Serializer

Returns a simple reference model (no expansion):

```csharp
public class ContentPickerAiPropertyValueSerializer : IAIPropertyValueSerializer
{
    private readonly IPublishedUrlProvider _urlProvider;

    public ContentPickerAiPropertyValueSerializer(IPublishedUrlProvider urlProvider)
    {
        _urlProvider = urlProvider;
    }

    public bool CanSerialize(IPublishedPropertyType propertyType)
        => propertyType.EditorAlias == Constants.PropertyEditors.Aliases.ContentPicker ||
           propertyType.EditorAlias == Constants.PropertyEditors.Aliases.MultiNodeTreePicker;

    public object? Serialize(
        IPublishedProperty property,
        string? culture,
        AISnapshotOptions options,
        AIPropertyValueSerializerCollection serializerCollection)
    {
        var value = property.GetValue(culture);

        return value switch
        {
            IPublishedContent content => SerializeContentReference(content, culture),
            IEnumerable<IPublishedContent> contentList =>
                contentList.Select(c => SerializeContentReference(c, culture)).ToList(),
            _ => null
        };
    }

    private object SerializeContentReference(IPublishedContent content, string? culture)
    {
        return new
        {
            key = content.Key,
            name = content.Name(culture),
            url = content.Url(_urlProvider, culture),
            contentType = content.ContentType.Alias
        };
    }
}
```

### Phase 3: Template Integration

```csharp
public interface IAITemplateResolver
{
    /// <summary>
    /// Resolves mustache placeholders in a template string.
    /// </summary>
    string Resolve(string template, AITemplateContext context);
}

public class AITemplateContext
{
    public AIEntitySnapshot? Content { get; init; }
    public AIRequestContext? Context { get; init; }
    public string? Culture { get; init; }
    public IReadOnlyDictionary<string, object?>? CustomData { get; init; }
}
```

Using [Stubble](https://github.com/StubbleOrg/Stubble) for mustache template rendering:

```csharp
public class AITemplateResolver : IAITemplateResolver
{
    private readonly StubbleVisitorRenderer _renderer;

    public AITemplateResolver()
    {
        _renderer = new StubbleBuilder()
            .Configure(settings =>
            {
                settings.SetMaxRecursionDepth(10);
                settings.SetIgnoreCaseOnKeyLookup(true);
            })
            .Build();
    }

    public string Resolve(string template, AITemplateContext context)
    {
        var data = new Dictionary<string, object?>
        {
            ["content"] = context.Content,
            ["context"] = context.Context,
            ["culture"] = context.Culture
        };

        if (context.CustomData != null)
        {
            foreach (var kvp in context.CustomData)
                data[kvp.Key] = kvp.Value;
        }

        return _renderer.Render(template, data);
    }
}
```

### Phase 4: DI Registration

```csharp
public static class AISnapshotComposerExtensions
{
    public static IUmbracoBuilder AddAiSnapshots(this IUmbracoBuilder builder)
    {
        // Register serializer collection with default serializers
        builder.AIPropertyValueSerializers()
            .Append<BlockEditorAiPropertyValueSerializer>()
            .Append<RichTextAiPropertyValueSerializer>()
            .Append<MediaPickerAiPropertyValueSerializer>()
            .Append<ContentPickerAiPropertyValueSerializer>()
            .Append<DefaultAiPropertyValueSerializer>(); // Fallback last

        builder.Services.AddSingleton<IAIEntitySnapshotService, AIEntitySnapshotService>();
        builder.Services.AddSingleton<IAITemplateResolver, AITemplateResolver>();

        return builder;
    }
}
```

---

## Template System

### Placeholder Syntax

```
{{content.name}}                    - Entity name
{{content.contentTypeAlias}}        - Content type alias
{{content.properties.title}}        - Property value by alias
{{content.properties.bodyText}}     - Another property
{{content.url}}                     - Entity URL
{{content.culture}}                 - Culture code for this snapshot
{{content.allText}}                 - All text content aggregated (name + all text properties)

{{context.currentPropertyAlias}}    - The property being edited
{{context.currentValue}}            - Current value of that property

{{culture}}                         - Current culture code (top-level)
```

---

## Property Output Examples

### Block Grid / Block List

```json
{
  "heroSection": {
    "items": [
      {
        "contentType": "heroBlock",
        "properties": {
          "heading": "Welcome",
          "subheading": "To our site"
        }
      }
    ]
  },
  "contentBlocks": {
    "items": [
      {
        "contentType": "textBlock",
        "properties": {
          "text": "Some content here..."
        }
      },
      {
        "contentType": "imageBlock",
        "properties": {
          "image": {
            "key": "media-key",
            "name": "Featured Image",
            "url": "/media/image.jpg",
            "mediaType": "Image"
          }
        }
      }
    ]
  }
}
```

### Media Picker

```json
{
  "featuredImage": {
    "key": "media-key",
    "name": "Featured Image",
    "url": "/media/image.jpg",
    "mediaType": "Image"
  }
}
```

### Content Picker

```json
{
  "relatedArticle": {
    "key": "content-key",
    "name": "Related Article Title",
    "url": "/blog/related-article",
    "contentType": "blogPost"
  }
}
```

### Rich Text (StripHtml: true)

```json
{
  "bodyText": "This is the content with all HTML tags removed. Links and formatting become plain text."
}
```

---

## Dependencies

### New NuGet Packages

```xml
<PackageVersion Include="Stubble.Core" Version="1.10.8" />
```

### Umbraco CMS Core Services (Injected)

- `IPublishedContentCache` - Get published content by key
- `IPublishedMediaCache` - Get published media by key
- `IVariationContextAccessor` - Get/set culture context
- `IPublishedUrlProvider` - Generate URLs for content/media
- `PublishedItemType` - Existing enum for entity types

**NOT used** (Delivery API specific):
- ~~`IApiPropertyRenderer`~~
- ~~`IOutputExpansionStrategy`~~
- ~~`IApiContentBuilder`~~

---

## Testing Strategy

### Unit Tests

1. **Snapshot Creation**: Test snapshot creation for Content, Media, Member, Element
2. **Property Serialization**: Test each property type serializer
3. **Culture Handling**: Test culture resolution from options vs. variation context
4. **Template Resolution**: Test mustache placeholder resolution
5. **Max Depth**: Test recursion limits for nested content

### Integration Tests

1. **Full Pipeline**: Create snapshot → Resolve template → AI request
2. **Complex Properties**: Block Grid, Block List with nested content
3. **Culture Variants**: Multi-language content scenarios

---

## Future Considerations

1. **Caching**: Cache snapshots for frequently accessed content
2. **Custom Serializers**: Package authors can register custom serializers via collection builder
3. **Schema Generation**: Generate JSON schema for snapshots
4. **AI Context Service**: Higher-level service combining site context + entity context
5. **Draft Support**: Support unpublished content via `IContentService`

---

## Open Questions

1. **Draft Content**: Should we support snapshots of unpublished/draft content?
   - Requires `IContentService` instead of published cache
   - Different property value resolution path

2. **Member Snapshots**: How much member data should be included?
   - Privacy considerations
   - Sensitive field handling

3. **Performance**: Should we implement snapshot caching?
   - Cache invalidation complexity
   - Memory vs. computation trade-off

4. **Placeholder Syntax**: Mustache vs. custom syntax?
   - Mustache is well-known but limited
   - Custom syntax could support filters (e.g., `{{content.date | formatDate}}`)
