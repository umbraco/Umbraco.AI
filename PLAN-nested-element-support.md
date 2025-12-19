# Plan: Support Prompts in Nested Element Types (Block Grid, Block List, etc.)

## Problem Statement

Currently, the prompt system assumes properties live at the root level of a content item. When prompts are triggered from within block editors (Block Grid, Block List, Rich Text blocks, or third-party editors like Contentment), they fail because:

1. **Frontend Issue**: The property action only captures the main document's `entityId` and `entityType` from `UMB_CONTENT_WORKSPACE_CONTEXT`, not the nested element context
2. **Backend Prompt Service**: Assumes the property lives directly on the content item, not in nested block data
3. **Backend Scope Validator**: Resolves content type from the main entity, not the element type being edited

## Key Insight: Block Data Structure

Umbraco stores blocks as structured JSON within a property value:
- `BlockValue.ContentData[]` - Array of `BlockItemData` with `Key` (GUID) and `ContentTypeKey`
- Each `BlockItemData.Values[]` contains `BlockPropertyValue` entries
- Blocks can be arbitrarily nested (a Block Grid can contain a Block List which can contain another Block Grid, etc.)

**Important**: There is NO existing path/traversal mechanism in Umbraco CMS for identifying elements within nested structures. We need to build our own.

## Package Boundaries: Core vs Add-on

This feature requires infrastructure that will be useful to multiple add-ons. We need to clearly separate reusable core components (in `Umbraco.Ai`) from Prompt-specific features (in `Umbraco.Ai.Prompt`).

### Umbraco.Ai (Core) - Reusable Infrastructure

**Backend - New Services:**

| Component | Purpose |
|-----------|---------|
| `ElementPathSegment` | Model representing one step in a nested element path |
| `IPublishedContentResolver` | Resolves `IPublishedContent` from entityId + entityType (document/media/member) |
| `IElementPathResolver` | Traverses element path to resolve target `IPublishedElement` |
| `INestedElementFinder` | Interface for finding elements within property values |
| `BlockListNestedElementFinder` | Built-in finder for Block List |
| `BlockGridNestedElementFinder` | Built-in finder for Block Grid |
| `RichTextBlockNestedElementFinder` | Built-in finder for RTE blocks |
| `SingleBlockNestedElementFinder` | Built-in finder for Single Block |
| `IPropertyEditorInfoResolver` | Resolves property editor UI alias from an `IPublishedElement` + property alias |

**Frontend - New Infrastructure:**

| Component | Purpose |
|-----------|---------|
| `ElementPathSegment` | TypeScript type for path segment |
| `UaiElementPathSegmentProviderApi` | Interface for element path segment providers |
| `ManifestUaiElementPathSegmentProvider` | Manifest type for registering providers |
| `UaiBlockEntryElementPathSegmentProvider` | Built-in provider for Umbraco block editors |
| `buildElementPath()` | Utility to build full path from current context |

### Umbraco.Ai.Prompt (Add-on) - Prompt-Specific Features

**Backend - Consumes Core + Adds:**

| Component | Purpose |
|-----------|---------|
| `AiPromptExecutionRequest.ElementPath` | New property on existing request model |
| `PromptExecutionRequestModel.ElementPath` | New property on API DTO |
| Scope validator updates | Uses `IElementPathResolver` to validate against correct element type |
| Template context updates | Uses resolved element to enrich template variables |

**Frontend - Consumes Core + Adds:**

| Component | Purpose |
|-----------|---------|
| Property action updates | Calls `buildElementPath()` and passes to modal |
| Modal data type updates | Accepts element path in data |
| Server data source updates | Passes element path to API |

### Why This Split?

1. **Translation add-on** - Would need to identify which element a property belongs to for translation scoping
2. **Content analysis add-on** - Would need to traverse nested elements for analysis
3. **Audit/versioning add-on** - Would need to track changes at element level
4. **Any property-level add-on** - Needs the same element identification infrastructure

## Solution Architecture: Path-Based Traversal

### 1. Element Path Concept

The **element path** is the primary mechanism for identifying nested elements. It describes the complete traversal from the root content item to the target element:

```
ElementPath = [ElementPathSegment, ...]
ElementPathSegment = {
    propertyAlias: string,  // The property containing the block editor
    elementKey: Guid        // The specific element within that block editor
}
```

**Example**: Editing a "title" property on an element nested 2 levels deep:

```
Document (entityId: "doc-123", entityType: "document")
└── Property "blocks" (Block Grid)
    └── Element (key: "grid-item-abc")
        └── Property "contentBlocks" (Block List)
            └── Element (key: "list-item-xyz")
                └── Property "title" ← Being edited
```

The request would be:
```json
{
    "entityId": "doc-123",
    "entityType": "document",
    "propertyAlias": "title",
    "elementPath": [
        { "propertyAlias": "blocks", "elementKey": "grid-item-abc" },
        { "propertyAlias": "contentBlocks", "elementKey": "list-item-xyz" }
    ]
}
```

**Key points**:
- `entityId` + `entityType` = the root content item (document/media/member)
- `elementPath` = how to navigate from root to the target element
- `propertyAlias` = the property being edited ON the target element
- Empty/null `elementPath` = root-level property (backwards compatible)

### 2. API Model Changes

#### `AiPromptExecutionRequest` (Backend)
```csharp
public class AiPromptExecutionRequest
{
    // Root content item identification
    public required Guid EntityId { get; init; }
    public required string EntityType { get; init; }

    // The property being edited (on root or on target element)
    public required string PropertyAlias { get; init; }

    // Variant context
    public string? Culture { get; init; }
    public string? Segment { get; init; }

    // NEW: Path to nested element (null/empty for root-level properties)
    /// <summary>
    /// Path from the root content item to the target element.
    /// Each segment identifies a block property and specific element within it.
    /// Null or empty for root-level properties.
    /// </summary>
    public IReadOnlyList<ElementPathSegment>? ElementPath { get; init; }

    // Existing...
    public IReadOnlyDictionary<string, object?>? LocalContent { get; init; }
    public IReadOnlyDictionary<string, object?>? Context { get; init; }
}

/// <summary>
/// Represents one step in the path from root content to a nested element.
/// </summary>
public class ElementPathSegment
{
    /// <summary>
    /// The property alias of the block editor containing the element.
    /// </summary>
    public required string PropertyAlias { get; init; }

    /// <summary>
    /// The unique key of the element within the block editor.
    /// </summary>
    public required Guid ElementKey { get; init; }
}
```

#### `PromptExecutionRequestModel` (Web API DTO)
```csharp
public class PromptExecutionRequestModel
{
    public required Guid EntityId { get; init; }
    public required string EntityType { get; init; }
    public required string PropertyAlias { get; init; }
    public string? Culture { get; init; }
    public string? Segment { get; init; }
    public IReadOnlyList<ElementPathSegmentModel>? ElementPath { get; init; }
    public IReadOnlyDictionary<string, object?>? LocalContent { get; init; }
    public IReadOnlyDictionary<string, object?>? Context { get; init; }
}

public class ElementPathSegmentModel
{
    public required string PropertyAlias { get; init; }
    public required Guid ElementKey { get; init; }
}
```

### 3. Backend Changes - Umbraco.Ai (Core)

These services live in the core `Umbraco.Ai` package for reuse by all add-ons.

#### 3.1 Published Content Resolver

Resolves `IPublishedContent` from entity identifiers. This consolidates the common pattern of loading content by ID and type:

```csharp
// Umbraco.Ai.Core.Content
public interface IPublishedContentResolver
{
    /// <summary>
    /// Resolves IPublishedContent from entity ID and type.
    /// </summary>
    /// <param name="entityId">The entity's unique identifier.</param>
    /// <param name="entityType">The entity type: "document", "media", or "member".</param>
    /// <returns>The published content, or null if not found.</returns>
    IPublishedContent? Resolve(Guid entityId, string entityType);
}

internal sealed class PublishedContentResolver : IPublishedContentResolver
{
    private readonly IPublishedContentQuery _contentQuery;
    private readonly IMemberService _memberService;
    private readonly IPublishedMemberCache _memberCache;

    public PublishedContentResolver(
        IPublishedContentQuery contentQuery,
        IMemberService memberService,
        IPublishedMemberCache memberCache)
    {
        _contentQuery = contentQuery;
        _memberService = memberService;
        _memberCache = memberCache;
    }

    public IPublishedContent? Resolve(Guid entityId, string entityType)
    {
        return entityType.ToLowerInvariant() switch
        {
            "document" => _contentQuery.Content(entityId),
            "media" => _contentQuery.Media(entityId),
            "member" => ResolveMember(entityId),
            _ => null
        };
    }

    private IPublishedContent? ResolveMember(Guid id)
    {
        var member = _memberService.GetByKey(id);
        return member is not null ? _memberCache.Get(member) : null;
    }
}
```

#### 3.2 Element Path Resolver

Traverses element path to resolve target `IPublishedElement`:

```csharp
public interface IElementPathResolver
{
    /// <summary>
    /// Resolves the element at the end of the path by traversing block properties.
    /// </summary>
    /// <param name="content">The root content item to start traversal from.</param>
    /// <param name="path">The path segments to traverse.</param>
    /// <returns>The resolved element, or null if the path is invalid.</returns>
    IPublishedElement? ResolveElement(
        IPublishedContent content,
        IReadOnlyList<ElementPathSegment> path);
}
```

The returned `IPublishedElement` provides everything we need:
- `Key` - the element's unique identifier
- `ContentType.Key` - the content type GUID
- `ContentType.Alias` - the content type alias
- `Properties` - all property values for template context

**Single responsibility**: The resolver only handles path traversal. The caller is responsible for loading the `IPublishedContent` (via `IPublishedContentResolver`).

Implementation will:
1. Traverse each path segment:
   - Get the property value for `PropertyAlias`
   - Get the block model (BlockListModel, BlockGridModel, etc.)
   - Find the element with matching `ElementKey`
   - If more segments remain, continue traversal into that element's properties
2. Return the final `IPublishedElement` (or null if path is invalid)

#### 3.3 Nested Element Finders

The resolver uses an extensible provider pattern to support different property editor types that contain nested elements (including third-party editors):

```csharp
/// <summary>
/// Finds nested elements within a property value by key.
/// Implement this interface to add support for custom property editors that contain nested elements.
/// </summary>
public interface INestedElementFinder
{
    /// <summary>
    /// Attempts to find an element by key within the given property value.
    /// </summary>
    /// <param name="propertyValue">The property value (e.g., BlockListModel, BlockGridModel, or custom types).</param>
    /// <param name="elementKey">The element key to find.</param>
    /// <returns>The element if found, otherwise null.</returns>
    IPublishedElement? FindElement(object propertyValue, Guid elementKey);

    /// <summary>
    /// Whether this finder can handle the given property value type.
    /// </summary>
    bool CanHandle(object propertyValue);
}
```

**Built-in implementations**:

```csharp
internal sealed class BlockListNestedElementFinder : INestedElementFinder
{
    public bool CanHandle(object propertyValue) => propertyValue is BlockListModel;

    public IPublishedElement? FindElement(object propertyValue, Guid elementKey)
    {
        if (propertyValue is not BlockListModel blockList)
            return null;

        foreach (var item in blockList)
        {
            if (item.Content.Key == elementKey)
                return item.Content;
            if (item.Settings?.Key == elementKey)
                return item.Settings;
        }
        return null;
    }
}

internal sealed class BlockGridNestedElementFinder : INestedElementFinder
{
    public bool CanHandle(object propertyValue) => propertyValue is BlockGridModel;

    public IPublishedElement? FindElement(object propertyValue, Guid elementKey)
    {
        if (propertyValue is not BlockGridModel blockGrid)
            return null;

        return FindInGrid(blockGrid, elementKey);
    }

    private static IPublishedElement? FindInGrid(BlockGridModel grid, Guid elementKey)
    {
        foreach (var item in grid)
        {
            if (item.Content.Key == elementKey)
                return item.Content;
            if (item.Settings?.Key == elementKey)
                return item.Settings;

            // Recursively search areas
            foreach (var area in item.Areas)
            {
                var found = FindInGrid(area, elementKey);
                if (found is not null)
                    return found;
            }
        }
        return null;
    }
}

// Similar implementations for RichTextBlockModel, SingleBlockModel, etc.
```

**The resolver**:

```csharp
internal sealed class ElementPathResolver : IElementPathResolver
{
    private readonly IEnumerable<INestedElementFinder> _finders;

    public ElementPathResolver(IEnumerable<INestedElementFinder> finders)
    {
        _finders = finders;
    }

    public IPublishedElement? ResolveElement(
        IPublishedContent content,
        IReadOnlyList<ElementPathSegment> path)
    {
        if (path.Count == 0)
            return null;

        IPublishedElement current = content;

        foreach (var segment in path)
        {
            var property = current.GetProperty(segment.PropertyAlias);
            if (property is null)
                return null;

            var propertyValue = property.GetValue();
            if (propertyValue is null)
                return null;

            var element = FindElementByKey(propertyValue, segment.ElementKey);
            if (element is null)
                return null;

            current = element;
        }

        return current;
    }

    private IPublishedElement? FindElementByKey(object propertyValue, Guid elementKey)
    {
        foreach (var finder in _finders)
        {
            if (finder.CanHandle(propertyValue))
            {
                return finder.FindElement(propertyValue, elementKey);
            }
        }
        return null;
    }
}
```

**DI Registration**:

```csharp
// In UmbracoBuilderExtensions
services.AddSingleton<INestedElementFinder, BlockListNestedElementFinder>();
services.AddSingleton<INestedElementFinder, BlockGridNestedElementFinder>();
services.AddSingleton<INestedElementFinder, RichTextBlockNestedElementFinder>();
services.AddSingleton<INestedElementFinder, SingleBlockNestedElementFinder>();
services.AddSingleton<IElementPathResolver, ElementPathResolver>();
```

**Third-party extensibility**: Custom property editors can register their own `INestedElementFinder` implementation to enable nested element support:

```csharp
// Example: Contentment or other third-party editor with nested elements
public class ContentmentNestedElementFinder : INestedElementFinder
{
    public bool CanHandle(object propertyValue) => propertyValue is ContentmentListModel;

    public IPublishedElement? FindElement(object propertyValue, Guid elementKey)
    {
        // Custom traversal logic
    }
}
```

#### 3.4 Property Editor Info Resolver

Resolves property editor UI alias from an element and property. This is commonly needed for scope validation and feature detection:

```csharp
// Umbraco.Ai.Core.Content
public interface IPropertyEditorInfoResolver
{
    /// <summary>
    /// Resolves the property editor UI alias for a property on an element.
    /// </summary>
    /// <param name="element">The element containing the property.</param>
    /// <param name="propertyAlias">The property alias.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The editor UI alias, or null if not found.</returns>
    Task<string?> ResolveEditorUiAliasAsync(
        IPublishedElement element,
        string propertyAlias,
        CancellationToken cancellationToken = default);
}

internal sealed class PropertyEditorInfoResolver : IPropertyEditorInfoResolver
{
    private readonly IDataTypeService _dataTypeService;

    public PropertyEditorInfoResolver(IDataTypeService dataTypeService)
    {
        _dataTypeService = dataTypeService;
    }

    public async Task<string?> ResolveEditorUiAliasAsync(
        IPublishedElement element,
        string propertyAlias,
        CancellationToken cancellationToken = default)
    {
        var property = element.GetProperty(propertyAlias);
        if (property?.PropertyType.DataType.Key is not { } dataTypeKey)
            return null;

        var dataType = await _dataTypeService.GetAsync(dataTypeKey);
        return dataType?.EditorUiAlias;
    }
}
```

#### 3.5 Core DI Registration (Umbraco.Ai)

```csharp
// In Umbraco.Ai UmbracoBuilderExtensions
services.AddSingleton<IPublishedContentResolver, PublishedContentResolver>();
services.AddSingleton<IPropertyEditorInfoResolver, PropertyEditorInfoResolver>();
services.AddSingleton<INestedElementFinder, BlockListNestedElementFinder>();
services.AddSingleton<INestedElementFinder, BlockGridNestedElementFinder>();
services.AddSingleton<INestedElementFinder, RichTextBlockNestedElementFinder>();
services.AddSingleton<INestedElementFinder, SingleBlockNestedElementFinder>();
services.AddSingleton<IElementPathResolver, ElementPathResolver>();
```

---

### 4. Backend Changes - Umbraco.Ai.Prompt (Add-on Specific)

These changes are specific to the Prompt add-on and consume the core services defined above.

#### 4.1 Scope Validator Updates

The validator is simplified by using the core services:

```csharp
internal sealed class AiPromptScopeValidator : IAiPromptScopeValidator
{
    private readonly IPublishedContentResolver _contentResolver;
    private readonly IElementPathResolver _elementPathResolver;
    private readonly IPropertyEditorInfoResolver _propertyEditorInfoResolver;

    public AiPromptScopeValidator(
        IPublishedContentResolver contentResolver,
        IElementPathResolver elementPathResolver,
        IPropertyEditorInfoResolver propertyEditorInfoResolver)
    {
        _contentResolver = contentResolver;
        _elementPathResolver = elementPathResolver;
        _propertyEditorInfoResolver = propertyEditorInfoResolver;
    }

    private async Task<ResolvedScopeContext> ResolveContextAsync(
        AiPromptExecutionRequest request,
        CancellationToken cancellationToken)
    {
        var context = new ResolvedScopeContext
        {
            PropertyAlias = request.PropertyAlias
        };

        // Load the content item using core service
        var content = _contentResolver.Resolve(request.EntityId, request.EntityType);
        if (content is null)
            return context;

        // Determine which element we're validating against
        IPublishedElement targetElement;

        if (request.ElementPath is { Count: > 0 })
        {
            // Nested element - traverse the path using core service
            var element = _elementPathResolver.ResolveElement(content, request.ElementPath);
            if (element is null)
                return context; // Element not found - validation will fail

            targetElement = element;
        }
        else
        {
            // Root-level property - use the content itself
            targetElement = content;
        }

        // Resolve context from the target element using core service
        context.ContentTypeAlias = targetElement.ContentType.Alias;
        context.PropertyEditorUiAlias = await _propertyEditorInfoResolver.ResolveEditorUiAliasAsync(
            targetElement,
            request.PropertyAlias,
            cancellationToken);

        return context;
    }
}
```

**Key simplifications vs. original**:
- Uses `IPublishedContentResolver` instead of inline `GetPublishedContent()` method
- Uses `IElementPathResolver` for path traversal
- Uses `IPropertyEditorInfoResolver` instead of inline `ResolvePropertyEditorUiAliasAsync()` method
- All dependencies are reusable core services

#### 4.2 Template Context Enhancement

The `AiPromptService` uses core services to load content and resolve elements for template context:

```csharp
internal sealed class AiPromptService : IAiPromptService
{
    private readonly IPublishedContentResolver _contentResolver;
    private readonly IElementPathResolver _elementPathResolver;
    // ... other existing dependencies

    private Dictionary<string, object?> BuildExecutionContext(AiPromptExecutionRequest request)
    {
        var context = new Dictionary<string, object?>
        {
            ["entityId"] = request.EntityId.ToString(),
            ["entityType"] = request.EntityType,
            ["propertyAlias"] = request.PropertyAlias,
        };

        // Add element context if available
        if (request.ElementPath is { Count: > 0 })
        {
            // Use core service instead of inline method
            var content = _contentResolver.Resolve(request.EntityId, request.EntityType);
            if (content is not null)
            {
                var element = _elementPathResolver.ResolveElement(content, request.ElementPath);
                if (element is not null)
                {
                    context["elementKey"] = element.Key.ToString();
                    context["elementContentTypeAlias"] = element.ContentType.Alias;

                    // Include element's property values for template replacement
                    context["element"] = ExtractElementValues(element);
                }
            }
        }

        // Add variant context
        if (!string.IsNullOrEmpty(request.Culture))
            context["culture"] = request.Culture;
        if (!string.IsNullOrEmpty(request.Segment))
            context["segment"] = request.Segment;

        // Add custom context
        if (request.Context is not null)
        {
            foreach (var kvp in request.Context)
                context[kvp.Key] = kvp.Value;
        }

        if (request.LocalContent is not null)
            context["localContent"] = request.LocalContent;

        return context;
    }

    private static Dictionary<string, object?> ExtractElementValues(IPublishedElement element)
    {
        var values = new Dictionary<string, object?>();
        foreach (var property in element.Properties)
        {
            values[property.Alias] = property.GetValue();
        }
        return values;
    }
}
```

**Key change**: Uses `IPublishedContentResolver` instead of duplicating the `GetPublishedContent()` / `GetPublishedMember()` methods.

This enables templates like:
```
Generate a summary for: {{element.title}}
The element type is: {{elementContentTypeAlias}}
```

### 5. Frontend Changes - Umbraco.Ai (Core)

These frontend components live in the core `Umbraco.Ai` package for reuse by all add-ons.

#### 5.1 Element Path Segment Provider - Manifest Extension Type

Define a new manifest extension type that allows third parties to register their own element path segment providers:

**File: `Client/src/prompt/element-path/types.ts`**

```typescript
import type { UmbControllerHost } from '@umbraco-cms/backoffice/controller-api';
import type { ManifestApi } from '@umbraco-cms/backoffice/extension-api';

export interface ElementPathSegment {
    propertyAlias: string;
    elementKey: string;
}

/**
 * Interface for element path segment providers.
 * Implement this to add support for custom nested element editors.
 */
export interface UaiElementPathSegmentProviderApi {
    /**
     * Attempts to get a path segment from the current host context.
     * Returns null if this provider cannot handle the current context.
     */
    getPathSegment(host: UmbControllerHost): Promise<ElementPathSegment | null>;
}

/**
 * Manifest type for element path segment providers.
 */
export interface ManifestUaiElementPathSegmentProvider extends ManifestApi<UaiElementPathSegmentProviderApi> {
    type: 'uaiElementPathSegmentProvider';
}

declare global {
    interface UmbExtensionManifestMap {
        uaiElementPathSegmentProvider: ManifestUaiElementPathSegmentProvider;
    }
}
```

#### 5.2 Built-in Block Entry Provider

**File: `Client/src/prompt/element-path/providers/block-entry.element-path-segment-provider.ts`**

```typescript
import type { UmbControllerHost } from '@umbraco-cms/backoffice/controller-api';
import { UMB_BLOCK_ENTRY_CONTEXT, UMB_BLOCK_ENTRIES_CONTEXT } from '@umbraco-cms/backoffice/block';
import type { UaiElementPathSegmentProviderApi, ElementPathSegment } from '../types.js';

/**
 * Element path segment provider for Umbraco's built-in block editors (Block List, Block Grid, RTE blocks).
 */
export class UaiBlockEntryElementPathSegmentProvider implements UaiElementPathSegmentProviderApi {
    async getPathSegment(host: UmbControllerHost): Promise<ElementPathSegment | null> {
        try {
            const blockEntry = await host.getContext(UMB_BLOCK_ENTRY_CONTEXT);
            if (!blockEntry) return null;

            const contentKey = blockEntry.getContentKey();
            if (!contentKey) return null;

            // Get the property alias from the entries context
            const entriesContext = await host.getContext(UMB_BLOCK_ENTRIES_CONTEXT);
            const propertyAlias = entriesContext?.getPropertyAlias?.();
            if (!propertyAlias) return null;

            return {
                propertyAlias,
                elementKey: contentKey
            };
        } catch {
            return null;
        }
    }
}

export { UaiBlockEntryElementPathSegmentProvider as api };
```

**Manifest registration:**

```typescript
const manifests: Array<ManifestUaiElementPathSegmentProvider> = [
    {
        type: 'uaiElementPathSegmentProvider',
        alias: 'Uai.ElementPathSegmentProvider.BlockEntry',
        name: 'Block Entry Element Path Segment Provider',
        api: () => import('./providers/block-entry.element-path-segment-provider.js'),
    },
];
```

#### 5.3 Element Path Builder Service

The builder service loads all registered segment providers and uses them to construct the full path:

**File: `Client/src/prompt/element-path/element-path-builder.ts`**

```typescript
import type { UmbControllerHost } from '@umbraco-cms/backoffice/controller-api';
import { umbExtensionsRegistry } from '@umbraco-cms/backoffice/extension-registry';
import { createExtensionApi } from '@umbraco-cms/backoffice/extension-api';
import type { ElementPathSegment, UaiElementPathSegmentProviderApi } from './types.js';

/**
 * Builds the element path from the current context by walking up the host hierarchy
 * and consulting registered element path segment providers at each level.
 */
export async function buildElementPath(host: UmbControllerHost): Promise<ElementPathSegment[]> {
    const path: ElementPathSegment[] = [];

    // Get all registered element path segment providers
    const providerManifests = umbExtensionsRegistry.getByType('uaiElementPathSegmentProvider');

    // Instantiate providers
    const providers: UaiElementPathSegmentProviderApi[] = [];
    for (const manifest of providerManifests) {
        const api = await createExtensionApi(host, manifest);
        if (api) providers.push(api);
    }

    // Walk up the context tree
    let currentHost: UmbControllerHost | undefined = host;

    while (currentHost) {
        // Try each provider at this level
        for (const provider of providers) {
            const segment = await provider.getPathSegment(currentHost);
            if (segment) {
                // Insert at beginning (we're walking up, but path is root-to-leaf)
                path.unshift(segment);
                break; // Only one provider should match per level
            }
        }

        // Move to parent host
        currentHost = currentHost.getParentHost?.();
    }

    return path;
}
```

#### 5.4 Third-Party Example (Contentment)

Third parties can register their own segment provider via manifest:

```typescript
// In Contentment's package
const manifests = [
    {
        type: 'uaiElementPathSegmentProvider',
        alias: 'Contentment.ElementPathSegmentProvider',
        name: 'Contentment Element Path Segment Provider',
        api: () => import('./contentment.element-path-segment-provider.js'),
    },
];

// contentment.element-path-segment-provider.ts
export class ContentmentElementPathSegmentProvider implements UaiElementPathSegmentProviderApi {
    async getPathSegment(host: UmbControllerHost): Promise<ElementPathSegment | null> {
        try {
            // Use Contentment's own context
            const contentmentEntry = await host.getContext(CONTENTMENT_ENTRY_CONTEXT);
            if (!contentmentEntry) return null;

            return {
                propertyAlias: contentmentEntry.getPropertyAlias(),
                elementKey: contentmentEntry.getElementKey()
            };
        } catch {
            return null;
        }
    }
}
```

---

### 6. Frontend Changes - Umbraco.Ai.Prompt (Add-on Specific)

These changes are specific to the Prompt add-on and consume the core frontend infrastructure.

#### 6.1 Property Action Updates

The property action imports and uses the core element path builder:

```typescript
import { buildElementPath } from '../element-path/element-path-builder.js';

export class UaiPromptInsertPropertyAction extends UmbPropertyActionBase<UaiPromptPropertyActionMeta> {
    #propertyContext?: typeof UMB_PROPERTY_CONTEXT.TYPE;
    #workspaceContext?: typeof UMB_CONTENT_WORKSPACE_CONTEXT.TYPE;
    #init: Promise<unknown>;

    constructor(host: UmbControllerHost, args: UmbPropertyActionArgs<UaiPromptPropertyActionMeta>) {
        super(host, args);

        this.#init = Promise.all([
            this.consumeContext(UMB_PROPERTY_CONTEXT, (context) => {
                this.#propertyContext = context;
            }).asPromise({ preventTimeout: true }),
            // Use passContextAliasMatches() to skip any intermediate block workspace contexts
            // and get the root content workspace (document/media/member)
            this.consumeContext(UMB_CONTENT_WORKSPACE_CONTEXT, (context) => {
                this.#workspaceContext = context;
            }).passContextAliasMatches().asPromise({ preventTimeout: true }),
        ]);
    }

    override async execute() {
        await this.#init;

        if (!this.#propertyContext || !this.#workspaceContext) {
            throw new Error('Required contexts not available');
        }

        const meta = this.args.meta;
        if (!meta) {
            throw new Error('Property action meta is not available');
        }

        const entityId = this.#workspaceContext.getUnique();
        const entityType = this.#workspaceContext.getEntityType();
        const propertyAlias = this.#propertyContext.getAlias();

        if (!entityId || !entityType || !propertyAlias) {
            throw new Error('Required entity context not available');
        }

        // Build the element path using registered providers
        const elementPath = await buildElementPath(this);

        try {
            const result = await umbOpenModal(this, UAI_PROMPT_PREVIEW_MODAL, {
                data: {
                    promptUnique: meta.promptUnique,
                    promptName: meta.label,
                    promptDescription: meta.promptDescription,
                    entityId,
                    entityType,
                    propertyAlias,
                    culture: this.#propertyContext.getVariantId?.()?.culture ?? undefined,
                    segment: this.#propertyContext.getVariantId?.()?.segment ?? undefined,
                    // NEW: Element path for nested content
                    elementPath: elementPath.length > 0 ? elementPath : undefined,
                },
            });

            if (result.action === 'insert' && result.content) {
                this.#propertyContext.setValue(result.content);
            }
        } catch {
            // Modal was rejected/cancelled
        }
    }
}
```

#### 6.2 Update Request Models

```typescript
export interface ElementPathSegment {
    propertyAlias: string;
    elementKey: string;
}

export interface UaiPromptExecutionRequest {
    entityId: string;
    entityType: string;
    propertyAlias: string;
    culture?: string;
    segment?: string;
    elementPath?: ElementPathSegment[];
    localContent?: Record<string, unknown>;
    context?: Record<string, unknown>;
}
```

#### 6.3 Update Server Data Source

```typescript
export class UaiPromptExecutionServerDataSource {
    async execute(
        promptIdOrAlias: string,
        request: UaiPromptExecutionRequest,
        _signal?: AbortSignal
    ): Promise<{ data?: UaiPromptExecutionResponse; error?: unknown }> {
        const body: PromptExecutionRequestModel = {
            entityId: request.entityId,
            entityType: request.entityType,
            propertyAlias: request.propertyAlias,
            culture: request.culture,
            segment: request.segment,
            elementPath: request.elementPath,  // NEW
            localContent: request.localContent,
            context: request.context,
        };

        // ... rest of implementation
    }
}
```

### 7. Frontend Scope Matching (Prompt Add-on)

The scope condition needs to resolve the correct content type when inside a block:

```typescript
// In the scope condition/matcher
async function resolveContentTypeForScope(host: UmbControllerHost): Promise<string | undefined> {
    // First check if we're in a block
    try {
        const blockEntry = await host.getContext(UMB_BLOCK_ENTRY_CONTEXT);
        if (blockEntry) {
            // Use the element's content type, not the document's
            return blockEntry.getContentElementTypeAlias();
        }
    } catch {
        // Not in a block
    }

    // Fall back to document content type
    const workspace = await host.getContext(UMB_CONTENT_WORKSPACE_CONTEXT);
    return workspace?.getContentTypeAlias?.();
}
```

### 8. Implementation Order

#### Umbraco.Ai (Core) - Implement First

1. **Phase 1: Core Backend Models & Services**
   - Add `ElementPathSegment` model class
   - Implement `IPublishedContentResolver` and `PublishedContentResolver`
   - Implement `IPropertyEditorInfoResolver` and `PropertyEditorInfoResolver`
   - Implement `INestedElementFinder` interface and built-in finders (BlockList, BlockGrid, RTE, SingleBlock)
   - Implement `IElementPathResolver` and `ElementPathResolver`
   - Register all core DI services

2. **Phase 2: Core Frontend Infrastructure**
   - Create `ElementPathSegment` type
   - Create `UaiElementPathSegmentProviderApi` interface and manifest type
   - Implement `UaiBlockEntryElementPathSegmentProvider`
   - Implement `buildElementPath()` utility
   - Register manifests

#### Umbraco.Ai.Prompt (Add-on) - Implement Second

3. **Phase 3: Prompt Backend Updates**
   - Update `AiPromptExecutionRequest` with `ElementPath` property
   - Update `PromptExecutionRequestModel` and mapping
   - Update `AiPromptScopeValidator` to use core services
   - Update `AiPromptService.BuildExecutionContext` to use core services

4. **Phase 4: Prompt Frontend Updates**
   - Update property action to call `buildElementPath()`
   - Update modal data types to include element path
   - Update server data source to pass element path
   - Update scope condition to use element type

5. **Phase 5: Testing**
   - Unit tests for core path resolution
   - Unit tests for scope validation with paths
   - Integration tests with nested blocks
   - Manual testing: Block List, Block Grid, RTE blocks, nested combinations

## Open Issues (Must Resolve Before Implementation)

### ⚠️ Frontend Context Resolution Flaw

**Problem**: The current `buildElementPath()` design has a fundamental flaw when multiple nested element editors are involved.

When walking up the host hierarchy, each provider calls `host.getContext(SOME_CONTEXT)`. However, `getContext()` traverses UP the entire context tree until it finds a match - it doesn't check "this level only".

**Example scenario**:
```
Document
└── Block List (provides UMB_BLOCK_ENTRY_CONTEXT)
    └── Contentment (provides CONTENTMENT_ENTRY_CONTEXT)
        └── Property being edited ← buildElementPath() starts here
```

If `UaiBlockEntryElementPathSegmentProvider` is checked first at the property level, `getContext(UMB_BLOCK_ENTRY_CONTEXT)` will find the Block List's context (skipping over Contentment) and return a segment - but this is the **wrong** segment for the current nesting level. We need the Contentment segment first, then the Block List segment as we walk up.

**Potential solutions to investigate**:

1. **Providers check "am I the immediate parent?"** - Each provider needs a way to verify it's the direct parent, not an ancestor. May require checking some identifier on the host itself.

2. **Single shared context token** - All nested element editors provide the same context token (e.g., `UAI_NESTED_ELEMENT_CONTEXT`). The builder reads this one context at each level. Third parties would need to provide this standard context.

3. **Host-level marker** - Instead of relying on context, check if the host itself has a property/marker indicating it's a nested element boundary.

4. **Provider ordering with exclusion** - Providers declare which contexts they "consume", and once matched, that context is excluded from ancestor checks.

**Status**: This must be resolved before frontend implementation can proceed. The backend design is unaffected.

---

## Considerations

### Security
- **Path validation is mandatory** - the backend traverses and validates the path
- Don't trust any frontend-provided content type information
- The path must resolve to an actual element within the content item

### Performance
- Path traversal requires loading the content item
- Block JSON parsing happens once per request
- Consider caching for repeated requests to same content

### Backwards Compatibility
- `ElementPath` is optional (null/empty)
- Existing prompts on root-level properties work unchanged
- No breaking changes to existing API contracts

### Block Editor Support
- **Block List**: Single level, path has 1 segment
- **Block Grid**: Can have nested areas, path can have N segments
- **RTE Blocks**: Similar to Block List
- **Third-party (Contentment, etc.)**: Works if they register their own `INestedElementFinder` (backend) and `UaiElementPathSegmentProviderApi` (frontend)

### Deeply Nested Blocks
- Path supports arbitrary depth
- Each segment = one level of nesting
- Example: Grid → List → Grid → List = 4 segments

## Files to Modify

### Umbraco.Ai (Core) - Backend New Files
- `src/Umbraco.Ai.Core/Content/ElementPathSegment.cs`
- `src/Umbraco.Ai.Core/Content/IPublishedContentResolver.cs`
- `src/Umbraco.Ai.Core/Content/PublishedContentResolver.cs`
- `src/Umbraco.Ai.Core/Content/IPropertyEditorInfoResolver.cs`
- `src/Umbraco.Ai.Core/Content/PropertyEditorInfoResolver.cs`
- `src/Umbraco.Ai.Core/Content/IElementPathResolver.cs`
- `src/Umbraco.Ai.Core/Content/ElementPathResolver.cs`
- `src/Umbraco.Ai.Core/Content/INestedElementFinder.cs`
- `src/Umbraco.Ai.Core/Content/Finders/BlockListNestedElementFinder.cs`
- `src/Umbraco.Ai.Core/Content/Finders/BlockGridNestedElementFinder.cs`
- `src/Umbraco.Ai.Core/Content/Finders/RichTextBlockNestedElementFinder.cs`
- `src/Umbraco.Ai.Core/Content/Finders/SingleBlockNestedElementFinder.cs`

### Umbraco.Ai (Core) - Backend Modified Files
- `src/Umbraco.Ai.Core/Extensions/UmbracoBuilderExtensions.cs` (DI registration)

### Umbraco.Ai (Core) - Frontend New Files
- `Client/src/core/element-path/types.ts`
- `Client/src/core/element-path/element-path-builder.ts`
- `Client/src/core/element-path/providers/block-entry.element-path-segment-provider.ts`
- `Client/src/core/element-path/manifests.ts`

### Umbraco.Ai.Prompt (Add-on) - Backend New Files
- `src/Umbraco.Ai.Prompt.Web/Api/Management/Prompt/Models/ElementPathSegmentModel.cs`

### Umbraco.Ai.Prompt (Add-on) - Backend Modified Files
- `src/Umbraco.Ai.Prompt.Core/Prompts/AiPromptExecutionRequest.cs`
- `src/Umbraco.Ai.Prompt.Core/Prompts/AiPromptScopeValidator.cs`
- `src/Umbraco.Ai.Prompt.Core/Prompts/AiPromptService.cs`
- `src/Umbraco.Ai.Prompt.Web/Api/Management/Prompt/Models/PromptExecutionRequestModel.cs`
- `src/Umbraco.Ai.Prompt.Web/Api/Management/Prompt/Mapping/PromptExecutionMapDefinition.cs`

### Umbraco.Ai.Prompt (Add-on) - Frontend Modified Files
- `Client/src/prompt/property-actions/prompt-insert.property-action.ts`
- `Client/src/prompt/property-actions/prompt-preview-modal.element.ts`
- `Client/src/prompt/property-actions/prompt-preview-modal.token.ts`
- `Client/src/prompt/repository/execution/prompt-execution.server.data-source.ts`
- `Client/src/prompt/conditions/` (scope condition files)
