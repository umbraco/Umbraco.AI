# Plan: Shared Infrastructure for Entity Snapshot Service & Nested Element Support

## Summary

Create shared infrastructure in `Umbraco.AI.Core` that serves both the Entity Snapshot Service and the Nested Element Support feature for the Prompt add-on. The Prompt service will leverage `IAIEntitySnapshotService.CreateSnapshot(IPublishedElement)` for rich template context.

**Key Simplification (v1)**: Instead of path-based element resolution, use an **element index map** approach:
- Iterate the content model once to build a `Dictionary<Guid, IPublishedElement>`
- Look up elements directly by their key
- Frontend only needs to pass `elementKey` (Guid), not a complex path
- This avoids the frontend context hierarchy traversal issue entirely

This approach trades some memory overhead for significant implementation simplicity. For backoffice operations with infrequent traffic, the trade-off is acceptable.

---

## Architecture Overview

```
Umbraco.AI.Core
├── Content/                          # NEW: Shared content resolution infrastructure
│   ├── IPublishedContentResolver.cs
│   ├── PublishedContentResolver.cs
│   ├── IPropertyEditorInfoResolver.cs
│   ├── PropertyEditorInfoResolver.cs
│   ├── IAIContextBuilder.cs          # High-level context builder
│   ├── AIContextBuilder.cs
│   ├── AIContextRequest.cs           # Request model for context building
│   ├── AIResolvedContent.cs          # Lightweight resolved content (no snapshots)
│   └── Internal/
│       └── BlockStructureHelper.cs   # Shared block traversal + element indexing
│
├── Templates/                        # NEW: Moved from Snapshots/, enhanced for elements
│   ├── IAITemplateResolver.cs        # Moved from Snapshots/
│   ├── AITemplateResolver.cs         # Moved from Snapshots/
│   └── AITemplateContext.cs          # Moved + flattened (no more AIRequestContext)
│
├── Snapshots/                        # EXISTING: Simplified
│   ├── IAIEntitySnapshotService.cs
│   ├── AIEntitySnapshotService.cs    # Modified to use IPublishedContentResolver
│   ├── AIEntitySnapshot.cs
│   ├── AISnapshotOptions.cs
│   └── Serializers/
│       └── BlockEditorAiPropertyValueSerializer.cs  # Uses BlockStructureHelper
```

---

## Phase 1: Shared Content Infrastructure (Umbraco.AI.Core)

### 1.1 Published Content Resolver

**File**: `src/Umbraco.AI.Core/Content/IPublishedContentResolver.cs`

```csharp
public interface IPublishedContentResolver
{
    /// <summary>
    /// Resolves IPublishedContent from entity ID and type string.
    /// </summary>
    /// <param name="entityId">The entity's unique identifier.</param>
    /// <param name="entityType">The entity type: "document", "media", or "member".</param>
    /// <returns>The published content, or null if not found.</returns>
    IPublishedContent? Resolve(Guid entityId, string entityType);
}
```

**File**: `src/Umbraco.AI.Core/Content/PublishedContentResolver.cs`

Uses `IUmbracoContextAccessor` for content/media, with fallback to member services for members.

### 1.2 Element Index Map (Simplified Approach)

Instead of path-based resolution with multiple finders, build a flat index of all nested elements.

**File**: `src/Umbraco.AI.Core/Content/Internal/BlockStructureHelper.cs`

This helper uses generic property iteration with reflection to traverse any nested structure, rather than explicit handling for specific block types. This makes it maintainable and works with any property editor that contains `IPublishedElement` instances.

```csharp
internal static class BlockStructureHelper
{
    /// <summary>
    /// Builds an index of all IPublishedElement instances within the content,
    /// keyed by their element key. Excludes the root IPublishedContent itself.
    /// Uses generic property iteration to traverse any nested structure.
    /// </summary>
    public static Dictionary<Guid, IPublishedElement> BuildElementIndex(IPublishedContent content)
    {
        var index = new Dictionary<Guid, IPublishedElement>();
        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        IndexElements(content, index, visited);
        return index;
    }

    private static void IndexElements(
        IPublishedElement element,
        Dictionary<Guid, IPublishedElement> index,
        HashSet<object> visited)
    {
        foreach (var property in element.Properties)
        {
            var value = property.GetValue();
            IndexValue(value, index, visited);
        }
    }

    private static void IndexValue(
        object? value,
        Dictionary<Guid, IPublishedElement> index,
        HashSet<object> visited)
    {
        if (value is null)
            return;

        // Circular reference detection - skip if already visited
        if (!visited.Add(value))
            return;

        switch (value)
        {
            // IPublishedContent - stop propagation, this is a link to another content node
            // (e.g., content picker, media picker) not a nested element within this content
            case IPublishedContent:
                break;

            // IPublishedElement - add to index and traverse its properties
            case IPublishedElement element:
                index.TryAdd(element.Key, element);
                IndexElements(element, index, visited);
                break;

            // String - skip (common case, not a container)
            case string:
                break;

            // Dictionary<TKey, TValue> - iterate values
            case IDictionary dictionary:
                foreach (var item in dictionary.Values)
                {
                    IndexValue(item, index, visited);
                }
                break;

            // IEnumerable (arrays, lists, collections) - iterate items
            case IEnumerable enumerable:
                foreach (var item in enumerable)
                {
                    IndexValue(item, index, visited);
                }
                break;

            // Objects with properties - use reflection to traverse
            default:
                IndexObjectProperties(value, index, visited);
                break;
        }
    }

    private static void IndexObjectProperties(
        object obj,
        Dictionary<Guid, IPublishedElement> index,
        HashSet<object> visited)
    {
        var type = obj.GetType();

        // Skip primitive types, enums, and common value types that won't contain elements
        if (type.IsPrimitive || type.IsEnum ||
            type == typeof(decimal) || type == typeof(DateTime) ||
            type == typeof(DateTimeOffset) || type == typeof(TimeSpan) || type == typeof(Guid))
            return;

        // Skip non-class system types (structs without interesting properties)
        if (type.Namespace?.StartsWith("System") == true && !type.IsClass)
            return;

        // Get public instance properties
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in properties)
        {
            // Skip indexers and properties that can't be read
            if (!prop.CanRead || prop.GetIndexParameters().Length > 0)
                continue;

            try
            {
                var propValue = prop.GetValue(obj);
                IndexValue(propValue, index, visited);
            }
            catch
            {
                // Property access failed (e.g., lazy loading, disposed objects) - skip silently
            }
        }
    }
}
```

**Design notes:**
- **Generic traversal**: Iterates over all object properties using reflection rather than explicit type checks for `BlockListModel`, `BlockGridModel`, etc. This automatically supports any property editor that contains nested `IPublishedElement` instances.
- **Stops at IPublishedContent boundaries**: When encountering `IPublishedContent` (from content pickers, media pickers, etc.), traversal stops. These are links to other content nodes, not nested elements within the current content. We only index `IPublishedElement` instances that are true nested elements.
- **Circular reference detection**: Uses `HashSet<object>` with `ReferenceEqualityComparer.Instance` to track visited object references and prevent infinite loops.
- **Performance optimizations**: Skips strings early (common case), skips primitive/value types, and uses reference equality for the visited set.
- **Safe property access**: Wraps property access in try-catch to handle properties that throw exceptions during access.

### 1.3 Context Builder Service

A high-level service that encapsulates content resolution and template context building. Designed to avoid resolving content twice - once for validation, once for context.

**File**: `src/Umbraco.AI.Core/Content/AIContextRequest.cs`

```csharp
/// <summary>
/// Request model for building AI context.
/// </summary>
public class AIContextRequest
{
    public required Guid EntityId { get; init; }
    public required string EntityType { get; init; }

    /// <summary>
    /// Optional element key for nested elements.
    /// Null for root-level properties.
    /// </summary>
    public Guid? ElementKey { get; init; }

    public string? PropertyAlias { get; init; }
    public object? CurrentValue { get; init; }
    public string? Culture { get; init; }
    public string? Segment { get; init; }
    public IReadOnlyDictionary<string, object?>? CustomData { get; init; }
}
```

**File**: `src/Umbraco.AI.Core/Content/AIResolvedContent.cs`

```csharp
/// <summary>
/// Lightweight model containing resolved content references (no snapshots yet).
/// </summary>
public class AIResolvedContent
{
    /// <summary>
    /// The root content item (document/media/member).
    /// </summary>
    public IPublishedContent? Content { get; init; }

    /// <summary>
    /// The target element. Same as Content for root-level properties,
    /// or the nested element if ElementKey was provided.
    /// </summary>
    public IPublishedElement? TargetElement { get; init; }
}
```

**File**: `src/Umbraco.AI.Core/Content/IAIContextBuilder.cs`

```csharp
/// <summary>
/// Builds AI context by resolving content and creating template contexts.
/// </summary>
public interface IAIContextBuilder
{
    /// <summary>
    /// Resolves content and target element from the request.
    /// Builds an element index and looks up by key if ElementKey is provided.
    /// </summary>
    AIResolvedContent ResolveContent(AIContextRequest request);

    /// <summary>
    /// Builds template context from already-resolved content.
    /// Expensive operation - creates snapshots. Only call after validation passes.
    /// </summary>
    AITemplateContext BuildTemplateContext(AIResolvedContent resolved, AIContextRequest request);
}
```

**File**: `src/Umbraco.AI.Core/Content/AIContextBuilder.cs`

```csharp
internal sealed class AIContextBuilder : IAIContextBuilder
{
    private readonly IPublishedContentResolver _contentResolver;
    private readonly IAIEntitySnapshotService _snapshotService;

    public AIResolvedContent ResolveContent(AIContextRequest request)
    {
        var content = _contentResolver.Resolve(request.EntityId, request.EntityType);
        if (content is null)
            return new AIResolvedContent();

        IPublishedElement? targetElement = content;

        if (request.ElementKey.HasValue)
        {
            var elementIndex = BlockStructureHelper.BuildElementIndex(content);
            if (elementIndex.TryGetValue(request.ElementKey.Value, out var element))
            {
                targetElement = element;
            }
            // If element not found, fall back to root content
        }

        return new AIResolvedContent
        {
            Content = content,
            TargetElement = targetElement
        };
    }

    public AITemplateContext BuildTemplateContext(AIResolvedContent resolved, AIContextRequest request)
    {
        AIEntitySnapshot? contentSnapshot = null;
        AIEntitySnapshot? elementSnapshot = null;

        var options = new AISnapshotOptions { Culture = request.Culture };

        if (resolved.Content is not null)
        {
            contentSnapshot = _snapshotService.CreateSnapshot(resolved.Content, options);
        }

        // Only create element snapshot if target differs from root
        if (resolved.TargetElement is not null &&
            resolved.TargetElement != resolved.Content)
        {
            elementSnapshot = _snapshotService.CreateSnapshot(resolved.TargetElement, options);
        }

        return new AITemplateContext
        {
            Content = contentSnapshot,
            Element = elementSnapshot,
            PropertyAlias = request.PropertyAlias,
            CurrentValue = request.CurrentValue,
            Culture = request.Culture,
            CustomData = request.CustomData
        };
    }
}
```

**Usage Flow (in Prompt service):**

```csharp
// 1. Resolve content (builds element index, looks up by key)
var resolved = _contextBuilder.ResolveContent(request);
if (resolved.TargetElement is null)
    return NotFound();

// 2. Validate scope using resolved element
var validation = await _scopeValidator.ValidateAsync(prompt, resolved.TargetElement, request.PropertyAlias);
if (!validation.IsAllowed)
    return Denied(validation.DenialReason);

// 3. Build template context (expensive - creates snapshots)
var templateContext = _contextBuilder.BuildTemplateContext(resolved, request);

// 4. Resolve template and execute
var processedPrompt = _templateResolver.Resolve(prompt.Content, templateContext);
// ... execute AI request
```

This ensures content is resolved only once, and snapshots are only created if validation passes.

### 1.4 Property Editor Info Resolver

**File**: `src/Umbraco.AI.Core/Content/IPropertyEditorInfoResolver.cs`

```csharp
public interface IPropertyEditorInfoResolver
{
    /// <summary>
    /// Resolves the property editor UI alias for a property on an element.
    /// </summary>
    Task<string?> ResolveEditorUiAliasAsync(
        IPublishedElement element,
        string propertyAlias,
        CancellationToken cancellationToken = default);
}
```

Uses `IDataTypeService` to get `EditorUiAlias` from the property's DataType.

### 1.5 DI Registration

**File**: `src/Umbraco.AI.Core/Configuration/UmbracoBuilderExtensions.Content.cs`

```csharp
public static IUmbracoBuilder AddAiContent(this IUmbracoBuilder builder)
{
    builder.Services.AddSingleton<IPublishedContentResolver, PublishedContentResolver>();
    builder.Services.AddSingleton<IPropertyEditorInfoResolver, PropertyEditorInfoResolver>();
    builder.Services.AddScoped<IAIContextBuilder, AIContextBuilder>();

    return builder;
}
```

---

## Phase 1b: Templates Feature (Moved from Snapshots)

Move template resolution from `Snapshots/` to a dedicated `Templates/` feature and enhance for element support.

### 1b.1 Updated Template Context

**File**: `src/Umbraco.AI.Core/Templates/AITemplateContext.cs` (moved + enhanced)

```csharp
/// <summary>
/// Context for template resolution with entity snapshots.
/// </summary>
public class AITemplateContext
{
    /// <summary>
    /// Snapshot of the root content item.
    /// Template access: {{content.name}}, {{content.properties.title}}
    /// </summary>
    public AIEntitySnapshot? Content { get; init; }

    /// <summary>
    /// Snapshot of the target element (if nested).
    /// Null for root-level properties.
    /// Template access: {{element.name}}, {{element.properties.title}}
    /// </summary>
    public AIEntitySnapshot? Element { get; init; }

    /// <summary>
    /// The property alias being edited.
    /// Template access: {{propertyAlias}}
    /// </summary>
    public string? PropertyAlias { get; init; }

    /// <summary>
    /// The current value of the property being edited.
    /// Template access: {{currentValue}}
    /// </summary>
    public object? CurrentValue { get; init; }

    /// <summary>
    /// The culture code.
    /// Template access: {{culture}}
    /// </summary>
    public string? Culture { get; init; }

    /// <summary>
    /// Custom data to include in template resolution.
    /// Template access: {{customKey}}
    /// </summary>
    public IReadOnlyDictionary<string, object?>? CustomData { get; init; }
}
```

### 1b.2 Template Resolver

**File**: `src/Umbraco.AI.Core/Templates/IAITemplateResolver.cs` (moved)
**File**: `src/Umbraco.AI.Core/Templates/AITemplateResolver.cs` (moved)

The implementation remains largely the same but the data dictionary now includes:
- `content` → Content snapshot
- `element` → Element snapshot (if present)
- `propertyAlias` → Property being edited
- `currentValue` → Current value
- `culture` → Culture code
- Plus any CustomData entries

### 1b.3 Remove AIRequestContext

**Delete**: `src/Umbraco.AI.Core/Snapshots/AIRequestContext.cs`

This model is now redundant - its fields are flattened into `AITemplateContext`.

---

## Phase 2: Refactor Snapshot Service

### 2.1 Update AIEntitySnapshotService

**File**: `src/Umbraco.AI.Core/Snapshots/AIEntitySnapshotService.cs`

**Change**: Inject `IPublishedContentResolver` and use it in `CreateSnapshotAsync`:

```csharp
// Before (inline):
IPublishedContent? content = entityType switch
{
    PublishedItemType.Content => umbracoContext.Content?.GetById(entityKey),
    PublishedItemType.Media => umbracoContext.Media?.GetById(entityKey),
    _ => null
};

// After (using shared service):
var entityTypeString = entityType switch
{
    PublishedItemType.Content => "document",
    PublishedItemType.Media => "media",
    PublishedItemType.Member => "member",
    _ => null
};

IPublishedContent? content = entityTypeString != null
    ? _contentResolver.Resolve(entityKey, entityTypeString)
    : null;
```

---

## Phase 3: Prompt Add-on Integration

### 3.1 Update Request Model

**File**: `src/Umbraco.AI.Prompt.Core/Prompts/AIPromptExecutionRequest.cs`

```csharp
public class AIPromptExecutionRequest
{
    // Existing properties...

    /// <summary>
    /// Optional key of the nested element being edited.
    /// Null for root-level properties.
    /// </summary>
    public Guid? ElementKey { get; init; }
}
```

### 3.2 Update Scope Validator

**File**: `src/Umbraco.AI.Prompt.Core/Prompts/IAIPromptScopeValidator.cs`

Update interface to accept resolved element directly:

```csharp
public interface IAIPromptScopeValidator
{
    /// <summary>
    /// Validates whether a prompt can execute against the given element and property.
    /// </summary>
    Task<AIPromptScopeValidationResult> ValidateAsync(
        AIPrompt prompt,
        IPublishedElement targetElement,
        string propertyAlias,
        CancellationToken cancellationToken = default);
}
```

**File**: `src/Umbraco.AI.Prompt.Core/Prompts/AIPromptScopeValidator.cs`

Simplified - no longer resolves content itself:

```csharp
internal sealed class AIPromptScopeValidator : IAIPromptScopeValidator
{
    private readonly IPropertyEditorInfoResolver _propertyEditorInfoResolver;

    public async Task<AIPromptScopeValidationResult> ValidateAsync(
        AIPrompt prompt,
        IPublishedElement targetElement,
        string propertyAlias,
        CancellationToken cancellationToken = default)
    {
        var context = new ResolvedScopeContext
        {
            ContentTypeAlias = targetElement.ContentType.Alias,
            PropertyAlias = propertyAlias,
            PropertyEditorUiAlias = await _propertyEditorInfoResolver.ResolveEditorUiAliasAsync(
                targetElement, propertyAlias, cancellationToken)
        };

        // Existing validation logic unchanged...
    }
}
```

### 3.3 Update Prompt Service

**File**: `src/Umbraco.AI.Prompt.Core/Prompts/AIPromptService.cs`

Simplified using `IAIContextBuilder`:

```csharp
internal sealed class AIPromptService : IAIPromptService
{
    private readonly IAIPromptRepository _repository;
    private readonly IAIPromptScopeValidator _scopeValidator;
    private readonly IAIContextBuilder _contextBuilder;
    private readonly IAITemplateResolver _templateResolver;
    private readonly IAIChatService _chatService;

    public async Task<AIPromptExecutionResult> ExecuteAsync(
        Guid promptId,
        AIPromptExecutionRequest request,
        CancellationToken cancellationToken = default)
    {
        // 1. Fetch prompt
        var prompt = await _repository.GetByIdAsync(promptId, cancellationToken);
        if (prompt is null)
            return AIPromptExecutionResult.Failed("Prompt not found.");

        // 2. Build context request
        var contextRequest = new AIContextRequest
        {
            EntityId = request.EntityId,
            EntityType = request.EntityType,
            ElementKey = request.ElementKey,
            PropertyAlias = request.PropertyAlias,
            CurrentValue = request.CurrentValue,
            Culture = request.Culture,
            Segment = request.Segment,
            CustomData = request.Context
        };

        // 3. Resolve content (builds element index, looks up by key)
        var resolved = _contextBuilder.ResolveContent(contextRequest);
        if (resolved.TargetElement is null)
            return AIPromptExecutionResult.Failed("Content not found.");

        // 4. Validate scope
        var validation = await _scopeValidator.ValidateAsync(
            prompt, resolved.TargetElement, request.PropertyAlias, cancellationToken);
        if (!validation.IsAllowed)
            return AIPromptExecutionResult.Failed(validation.DenialReason ?? "Scope validation failed.");

        // 5. Build template context (creates snapshots)
        var templateContext = _contextBuilder.BuildTemplateContext(resolved, contextRequest);

        // 6. Resolve template
        var processedContent = _templateResolver.Resolve(prompt.Content, templateContext);

        // 7. Execute AI request
        var chatResponse = await _chatService.CompleteAsync(
            prompt.ProfileId,
            new ChatMessage(ChatRole.User, processedContent),
            cancellationToken: cancellationToken);

        return AIPromptExecutionResult.Success(chatResponse);
    }
}
```

**Key changes:**
- Removed `IAIPromptTemplateService` - uses shared `IAITemplateResolver` instead
- Uses `IAIContextBuilder` for all content resolution and snapshot creation
- Clean separation: resolve → validate → build context → execute

### 3.4 Update Web Layer

**File**: `src/Umbraco.AI.Prompt.Web/Api/Management/Prompt/Models/PromptExecutionRequestModel.cs`

```csharp
public class PromptExecutionRequestModel
{
    // Existing properties...

    /// <summary>
    /// Optional key of the nested element being edited.
    /// </summary>
    public Guid? ElementKey { get; init; }
}
```

**File**: `src/Umbraco.AI.Prompt.Web/Api/Management/Prompt/Mapping/PromptExecutionMapDefinition.cs`

Add mapping for `ElementKey` property.

---

## Phase 4: Frontend

With the element index map approach, the frontend implementation is **dramatically simplified**:

### 4.1 Frontend Changes (Umbraco.AI.Prompt)

The frontend only needs to:
1. Get the current element's key from the block entry context
2. Pass it to the API

**File**: `Client/src/prompt/property-actions/prompt-insert.property-action.ts`

```typescript
// Get element key from block context (if editing inside a block)
const blockContext = this.getContext(UMB_BLOCK_ENTRY_CONTEXT);
const elementKey = blockContext?.getKey(); // or null for root-level
```

**File**: `Client/src/prompt/repository/execution/prompt-execution.server.data-source.ts`

```typescript
interface PromptExecutionRequest {
    // ... existing fields
    elementKey?: string; // Guid as string, optional
}
```

### 4.2 No Complex Path Building Required

The previous open issue about context hierarchy traversal is **no longer relevant**. We don't need to:
- Build element paths
- Walk up the host hierarchy
- Query multiple context levels
- Create path segment providers

The frontend simply reads the element key from the immediate block context. If there's no block context, the property is at the root level.

---

## Template Examples

With element snapshots integrated, prompts can use rich template variables:

```
# Root content context
Document: {{content.name}}
URL: {{content.url}}
Summary: {{content.properties.summary}}

# Nested element context (when editing inside a block)
Block type: {{element.contentTypeAlias}}
Block title: {{element.properties.title}}
Block body: {{element.properties.bodyText}}

# Current property being edited
Property: {{propertyAlias}}
Culture: {{culture}}
```

---

## Files Summary

### New Files (Umbraco.AI.Core)

| File | Purpose |
|------|---------|
| `Content/IPublishedContentResolver.cs` | Interface for content resolution |
| `Content/PublishedContentResolver.cs` | Implementation using UmbracoContext |
| `Content/IPropertyEditorInfoResolver.cs` | Interface for editor UI alias |
| `Content/PropertyEditorInfoResolver.cs` | Implementation using DataTypeService |
| `Content/IAIContextBuilder.cs` | High-level context builder interface |
| `Content/AIContextBuilder.cs` | Context builder with element index |
| `Content/AIContextRequest.cs` | Request model (with ElementKey) |
| `Content/AIResolvedContent.cs` | Lightweight resolved content model |
| `Content/Internal/BlockStructureHelper.cs` | Shared block traversal + element indexing |
| `Configuration/UmbracoBuilderExtensions.Content.cs` | DI registration for Content services |
| `Templates/IAITemplateResolver.cs` | Moved from Snapshots/ |
| `Templates/AITemplateResolver.cs` | Moved from Snapshots/ |
| `Templates/AITemplateContext.cs` | Moved from Snapshots/, enhanced with Element |
| `Configuration/UmbracoBuilderExtensions.Templates.cs` | DI registration for Templates services |

### Modified Files (Umbraco.AI.Core)

| File | Change |
|------|--------|
| `Snapshots/AIEntitySnapshotService.cs` | Use `IPublishedContentResolver` |
| `Snapshots/Serializers/BlockEditorAiPropertyValueSerializer.cs` | Can optionally use `BlockStructureHelper` for traversal |

### Deleted Files (Umbraco.AI.Core)

| File | Reason |
|------|--------|
| `Snapshots/AIRequestContext.cs` | Redundant - fields flattened into `AITemplateContext` |
| `Snapshots/IAITemplateResolver.cs` | Moved to `Templates/` |
| `Snapshots/AITemplateResolver.cs` | Moved to `Templates/` |
| `Snapshots/AITemplateContext.cs` | Moved to `Templates/` |

### Modified Files (Umbraco.AI.Prompt)

| File | Change |
|------|--------|
| `Core/Prompts/AIPromptExecutionRequest.cs` | Add `ElementKey` property (Guid?) |
| `Core/Prompts/IAIPromptScopeValidator.cs` | Signature change: accepts `IPublishedElement` |
| `Core/Prompts/AIPromptScopeValidator.cs` | Simplified: uses `IPropertyEditorInfoResolver` |
| `Core/Prompts/AIPromptService.cs` | Use `IAIContextBuilder` + `IAITemplateResolver` |
| `Web/.../Models/PromptExecutionRequestModel.cs` | Add `ElementKey` property |
| `Web/.../Mapping/PromptExecutionMapDefinition.cs` | Map element key |
| `Client/.../prompt-insert.property-action.ts` | Get element key from block context |
| `Client/.../prompt-execution.server.data-source.ts` | Send element key to API |

### Deleted Files (Umbraco.AI.Prompt)

| File | Reason |
|------|--------|
| `Core/Prompts/IAIPromptTemplateService.cs` | Replaced by shared `IAITemplateResolver` |
| `Core/Prompts/AIPromptTemplateService.cs` | Replaced by shared `IAITemplateResolver` |

---

## Implementation Order

1. **Phase 1a**: Create `Content/` feature in Umbraco.AI.Core
   - `IPublishedContentResolver`, `PublishedContentResolver`
   - `IPropertyEditorInfoResolver`, `PropertyEditorInfoResolver`
   - `BlockStructureHelper` with `BuildElementIndex()`
   - `IAIContextBuilder`, `AIContextBuilder`, `AIContextRequest`, `AIResolvedContent`

2. **Phase 1b**: Create `Templates/` feature in Umbraco.AI.Core
   - Move `IAITemplateResolver`, `AITemplateResolver` from Snapshots/
   - Update `AITemplateContext` with flattened properties + Element support
   - Delete `AIRequestContext`

3. **Phase 2**: Refactor `Snapshots/` in Umbraco.AI.Core
   - Update `AIEntitySnapshotService` to use `IPublishedContentResolver`

4. **Phase 3**: Update Umbraco.AI.Prompt.Core
   - Add `ElementKey` to `AIPromptExecutionRequest`
   - Update `IAIPromptScopeValidator` signature
   - Refactor `AIPromptService` to use `IAIContextBuilder` + `IAITemplateResolver`
   - Delete `IAIPromptTemplateService` and implementation

5. **Phase 4**: Update Umbraco.AI.Prompt.Web API layer
   - Add `ElementKey` to request model
   - Update mapping

6. **Phase 5**: Frontend implementation
   - Read element key from block context
   - Pass to API

7. **Phase 6**: Testing
   - Unit tests for element index building
   - Unit tests for context builder
   - Integration tests with nested blocks

---

## Future Optimization (If Needed)

If the element index approach becomes a performance bottleneck (unlikely for backoffice operations), we can optimize by:

1. **Lazy indexing**: Only build index when `ElementKey` is provided
2. **Caching**: Cache element indices per content item with cache invalidation
3. **Path-based resolution**: Fall back to the original path-based approach for specific scenarios

For v1, the simple index approach is preferred for its implementation simplicity and frontend ease of use.
