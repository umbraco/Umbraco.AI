# Test Context Resolution Plan

## Problem

The `PromptTestFeatureConfig` has a `ContextItems` field (`List<AIRequestContextItem>?`) for providing request context during test execution. This is **unrealistic** — the tester has no way of knowing what context items to provide.

In production, context items are built **automatically by the frontend**:

1. `UaiPromptInsertPropertyAction` resolves the workspace context (entity being edited)
2. `UaiDocumentAdapter.serializeForLlm()` serializes the entire document: all properties with aliases, labels, editor aliases, and current values
3. `createEntityContextItem()` wraps it as `{ description: "Currently editing document: Blog Post", value: JSON.stringify(serializedEntity) }`
4. This `AIRequestContextItem` is sent to the backend as the `context` array

On the backend, `SerializedEntityContributor` processes this to:
- Extract template variables (`entityType`, `entityId`, `entityName`, `contentType`, plus each property alias -> value)
- Build a system message describing the entity via `IAIEntityFormatter`
- Set `currentValue` from the property matching `request.PropertyAlias`

**There is no server-side entity serializer.** The entire serialization pipeline is frontend-only. So when running a test without a UI, there's nothing to produce these context items.

## Approach: Consolidate Formatters into Server Entity Adapters

Currently the backend has `IAIEntityFormatter` (format `AISerializedEntity` into LLM text) as a separate extensibility point. We need a new capability (serialize a persisted entity into `AISerializedEntity`). Rather than creating a second entity-type-keyed collection, **consolidate both responsibilities into one interface**.

### Before (two collections)

```
IAIEntityFormatter                    IAIServerEntityAdapter (proposed)
├── FormatForLlm(entity) → string           ├── SerializeAsync(entityId) → AISerializedEntity
├── AIDocumentEntityFormatter          ├── DocumentServerEntityAdapter
├── AIGenericEntityFormatter           ├── MediaServerEntityAdapter
└── (third-party registers here)       └── (third-party registers here too)
```

Third-party packages would register **two** plugins per entity type.

### After (one collection)

```
IAIServerEntityAdapter
├── EntityType
├── FormatForLlm(entity) → string
├── SerializeAsync(entityId) → AISerializedEntity?
├── DocumentServerEntityAdapter    (formats + serializes)
├── MediaServerEntityAdapter       (formats + serializes)
├── MemberServerEntityAdapter      (formats + serializes)
├── GenericServerEntityAdapter     (formats only, fallback, EntityType = null)
└── (third-party registers ONE plugin per entity type)
```

### Why consolidate

- **One extensibility point per entity type** — a commerce package registers one adapter for `"commerce-product"` that handles everything
- **Simpler mental model** — "the entity adapter handles all backend concerns for this entity type"
- **No duplication of collection builder boilerplate** — one collection, one builder, one extension method
- **Format-only adapters still work** — `SerializeAsync` returns null by default via base class

### Migration from `IAIEntityFormatter`

| Before | After |
|--------|-------|
| `IAIEntityFormatter` | Removed — replaced by `IAIServerEntityAdapter` |
| `AIEntityFormatterCollection` | Removed — replaced by `AIServerEntityAdapterCollection` |
| `AIEntityFormatterCollectionBuilder` | Removed — replaced by `AIServerEntityAdapterCollectionBuilder` |
| `builder.AIEntityFormatters()` | `builder.AIServerEntityAdapters()` |
| `AIDocumentEntityFormatter` | `DocumentServerEntityAdapter` (adds `SerializeAsync`) |
| `AIGenericEntityFormatter` | `GenericServerEntityAdapter` (format-only fallback) |
| `AIEntityContextHelper` injects `AIEntityFormatterCollection` | Injects `AIServerEntityAdapterCollection` |

## Implementation Steps

### 1. Create `IAIServerEntityAdapter` interface

**File:** `Umbraco.AI/src/Umbraco.AI.Core/EntityAdapter/IAIServerEntityAdapter.cs`

```csharp
namespace Umbraco.AI.Core.EntityAdapter;

/// <summary>
/// Server-side entity adapter that handles backend concerns for an entity type:
/// formatting serialized entities for LLM consumption and optionally serializing
/// persisted entities from the database.
/// </summary>
/// <remarks>
/// This is the backend counterpart of the frontend <c>UaiEntityAdapterApi</c>.
/// Register implementations via the collection builder:
/// <code>builder.AIServerEntityAdapters().Add&lt;MyAdapter&gt;();</code>
/// </remarks>
public interface IAIServerEntityAdapter
{
    /// <summary>
    /// The entity type this adapter handles (e.g., "document", "media", "commerce-product").
    /// Returns null for the default/fallback adapter.
    /// </summary>
    string? EntityType { get; }

    /// <summary>
    /// Display name for this entity type (e.g., "Document", "Media", "Commerce Product").
    /// Used in UI dropdowns for entity type selection.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Icon for this entity type (e.g., "icon-document", "icon-picture").
    /// Used in UI dropdowns and entity pickers.
    /// </summary>
    string? Icon { get; }

    // --- Formatting ---

    /// <summary>
    /// Formats a serialized entity as a system message for LLM context.
    /// </summary>
    string FormatForLlm(AISerializedEntity entity);

    // --- Serialization ---

    /// <summary>
    /// Serializes a persisted entity by its ID into the standard format
    /// used by runtime context contributors.
    /// </summary>
    /// <returns>The serialized entity, or null if not found or not supported.</returns>
    Task<AISerializedEntity?> SerializeAsync(
        Guid entityId,
        CancellationToken cancellationToken = default);

    // --- Entity browsing (for entity picker UI) ---

    /// <summary>
    /// Lists entities of this type, optionally under a parent.
    /// Supports tree structures (parentId = null for roots, parentId = guid for children)
    /// and flat lists (ignore parentId, return all).
    /// </summary>
    /// <returns>The entities, or empty if browsing not supported.</returns>
    Task<IEnumerable<AIEntityItem>> GetEntitiesAsync(
        Guid? parentId = null,
        CancellationToken cancellationToken = default);

    // --- Property inspection (for property picker UI) ---

    /// <summary>
    /// Lists the properties available on a specific entity.
    /// Used to populate property alias dropdowns in test config.
    /// </summary>
    /// <returns>The properties, or empty if not supported.</returns>
    Task<IEnumerable<AIEntityProperty>> GetPropertiesAsync(
        Guid entityId,
        CancellationToken cancellationToken = default);
}
```

**Supporting models:**

**File:** `Umbraco.AI/src/Umbraco.AI.Core/EntityAdapter/AIEntityItem.cs`

```csharp
/// <summary>
/// An entity item for browsing/picking in the UI.
/// </summary>
public sealed class AIEntityItem
{
    /// <summary>Entity unique identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Display name.</summary>
    public required string Name { get; init; }

    /// <summary>Optional icon override (falls back to adapter icon).</summary>
    public string? Icon { get; init; }

    /// <summary>Whether this item has children (enables tree expansion).</summary>
    public bool HasChildren { get; init; }
}
```

**File:** `Umbraco.AI/src/Umbraco.AI.Core/EntityAdapter/AIEntityProperty.cs`

```csharp
/// <summary>
/// A property on an entity, for property picker UI.
/// </summary>
public sealed class AIEntityProperty
{
    /// <summary>Property alias (used as the value).</summary>
    public required string Alias { get; init; }

    /// <summary>Property display name (used as the label).</summary>
    public required string Name { get; init; }

    /// <summary>Property editor UI alias (e.g., "Umbraco.TextBox").</summary>
    public string? EditorAlias { get; init; }
}
```

### 2. Create base class with sensible defaults

**File:** `Umbraco.AI/src/Umbraco.AI.Core/EntityAdapter/AIServerEntityAdapterBase.cs`

```csharp
namespace Umbraco.AI.Core.EntityAdapter;

/// <summary>
/// Base class for server entity adapters.
/// Provides defaults for optional capabilities so adapters only
/// implement what they support.
/// </summary>
public abstract class AIServerEntityAdapterBase : IAIServerEntityAdapter
{
    /// <inheritdoc />
    public abstract string? EntityType { get; }

    /// <inheritdoc />
    public abstract string Name { get; }

    /// <inheritdoc />
    public virtual string? Icon => null;

    /// <inheritdoc />
    public abstract string FormatForLlm(AISerializedEntity entity);

    /// <inheritdoc />
    /// <remarks>Default: serialization not supported (returns null).</remarks>
    public virtual Task<AISerializedEntity?> SerializeAsync(
        Guid entityId, CancellationToken cancellationToken = default)
        => Task.FromResult<AISerializedEntity?>(null);

    /// <inheritdoc />
    /// <remarks>Default: browsing not supported (returns empty).</remarks>
    public virtual Task<IEnumerable<AIEntityItem>> GetEntitiesAsync(
        Guid? parentId = null, CancellationToken cancellationToken = default)
        => Task.FromResult<IEnumerable<AIEntityItem>>([]);

    /// <inheritdoc />
    /// <remarks>Default: property inspection not supported (returns empty).</remarks>
    public virtual Task<IEnumerable<AIEntityProperty>> GetPropertiesAsync(
        Guid entityId, CancellationToken cancellationToken = default)
        => Task.FromResult<IEnumerable<AIEntityProperty>>([]);
}
```

### 3. Create collection and builder

**File:** `Umbraco.AI/src/Umbraco.AI.Core/EntityAdapter/AIServerEntityAdapterCollection.cs`

```csharp
public sealed class AIServerEntityAdapterCollection
    : BuilderCollectionBase<IAIServerEntityAdapter>
{
    public AIServerEntityAdapterCollection(
        Func<IEnumerable<IAIServerEntityAdapter>> items) : base(items) { }

    /// <summary>
    /// Gets the adapter for the specified entity type.
    /// Returns the default adapter (EntityType = null) if no specific adapter is found.
    /// </summary>
    public IAIServerEntityAdapter GetAdapter(string entityType)
    {
        var adapter = this.FirstOrDefault(a =>
            a.EntityType != null &&
            string.Equals(a.EntityType, entityType, StringComparison.OrdinalIgnoreCase));

        adapter ??= this.FirstOrDefault(a => a.EntityType == null);

        if (adapter == null)
        {
            throw new InvalidOperationException(
                "No default server entity adapter found. Ensure GenericServerEntityAdapter is registered.");
        }

        return adapter;
    }

    /// <summary>
    /// Gets all registered entity type adapters (excluding the generic fallback).
    /// Used to populate entity type dropdowns in UI.
    /// </summary>
    public IEnumerable<IAIServerEntityAdapter> GetEntityTypeAdapters()
        => this.Where(a => a.EntityType != null);
}
```

**File:** `Umbraco.AI/src/Umbraco.AI.Core/EntityAdapter/AIServerEntityAdapterCollectionBuilder.cs`

Standard Umbraco collection builder.

### 4. Migrate existing formatters to adapters

#### Serialization Consistency: Use Management API, Not Raw DB Models

**Critical:** The frontend gets property values through the CMS Management API, which applies property value conversion (e.g., `IPropertyValueConverter` pipeline). Raw database values from `IContent.GetValue()` may be formatted differently.

For example:
- A media picker might return a raw key from `IContent` but a structured object from the Management API
- Rich text might be stored differently at the DB level vs the converted API representation
- Block editors, nested content, etc. all have conversion layers

**The server-side adapters must use the same Management API services/models** that the frontend consumes, not the raw `IContentService` / `IContent` models. This ensures the serialized entity produced server-side is identical in value format to what the frontend adapter would produce from the workspace context.

The exact services to use need to be determined during implementation (e.g., `IContentEditingService`, the Management API's internal mapping layer, or equivalent), but the principle is: **the property values in the serialized entity must match what the Management API returns, not what `IContent.GetValue()` returns.**

#### `AIDocumentEntityFormatter` → `DocumentServerEntityAdapter`

**File:** `Umbraco.AI/src/Umbraco.AI.Core/EntityAdapter/Adapters/DocumentServerEntityAdapter.cs`

- Keeps the existing `FormatForLlm()` logic from `AIDocumentEntityFormatter`
- Adds `SerializeAsync()` that fetches the document via the Management API layer and serializes using the converted property values

```csharp
internal sealed class DocumentServerEntityAdapter : AIServerEntityAdapterBase
{
    // Use the Management API service layer — NOT IContentService directly
    // The exact service depends on what Umbraco 17 exposes for fetching
    // content with converted property values (e.g., IContentEditingService
    // or the mapping used by Management API controllers).

    public override string? EntityType => "document";

    // FormatForLlm() — existing AIDocumentEntityFormatter logic moved here

    public override async Task<AISerializedEntity?> SerializeAsync(
        Guid entityId, CancellationToken ct)
    {
        // 1. Fetch content via Management API service (converted values)
        // 2. Build properties array matching frontend format:
        //    { alias, label, editorAlias, value }
        //    where value is the Management API representation, not raw DB value
        // 3. Return AISerializedEntity with { contentType, properties }
    }
}
```

#### `AIGenericEntityFormatter` → `GenericServerEntityAdapter`

**File:** `Umbraco.AI/src/Umbraco.AI.Core/EntityAdapter/Adapters/GenericServerEntityAdapter.cs`

- Keeps the existing `FormatForLlm()` logic (JSON pretty-print fallback)
- `SerializeAsync` uses default (returns null) — generic adapter can't know how to fetch arbitrary entities

#### New: `MediaServerEntityAdapter`, `MemberServerEntityAdapter`

Similar to `DocumentServerEntityAdapter` but using the corresponding Management API services for media/members. Same principle: property values must come through the conversion pipeline, not raw DB models.

### 5. Update `AIEntityContextHelper`

**File:** `Umbraco.AI/src/Umbraco.AI.Core/EntityAdapter/AIEntityContextHelper.cs`

Change dependency from `AIEntityFormatterCollection` to `AIServerEntityAdapterCollection`:

```csharp
internal sealed class AIEntityContextHelper : IAIEntityContextHelper
{
    private readonly AIServerEntityAdapterCollection _adapters;

    public AIEntityContextHelper(AIServerEntityAdapterCollection adapters)
    {
        _adapters = adapters;
    }

    public string FormatForLlm(AISerializedEntity entity)
    {
        var adapter = _adapters.GetAdapter(entity.EntityType);
        return adapter.FormatForLlm(entity);
    }

    // BuildContextDictionary() stays the same — no dependency on formatters
}
```

### 6. Update DI registration

**File:** `Umbraco.AI/src/Umbraco.AI.Core/Configuration/UmbracoBuilderExtensions.cs`

Replace:
```csharp
builder.AIEntityFormatters()
    .Add<AIDocumentEntityFormatter>()
    .Add<AIGenericEntityFormatter>();
```

With:
```csharp
builder.AIServerEntityAdapters()
    .Add<DocumentServerEntityAdapter>()
    .Add<MediaServerEntityAdapter>()
    .Add<MemberServerEntityAdapter>()
    .Add<GenericServerEntityAdapter>();
```

**File:** `Umbraco.AI/src/Umbraco.AI.Core/Configuration/UmbracoBuilderExtensions.EntityFormatters.cs`

Rename/replace with `UmbracoBuilderExtensions.ServerEntityAdapters.cs`:
```csharp
public static AIServerEntityAdapterCollectionBuilder AIServerEntityAdapters(
    this IUmbracoBuilder builder)
    => builder.WithCollectionBuilder<AIServerEntityAdapterCollectionBuilder>();
```

### 7. Delete old formatter files

- `IAIEntityFormatter.cs`
- `AIEntityFormatterCollection.cs`
- `AIEntityFormatterCollectionBuilder.cs`
- `AIDocumentEntityFormatter.cs`
- `AIGenericEntityFormatter.cs`
- `UmbracoBuilderExtensions.EntityFormatters.cs`

### 8. Shared entity context resolution for both test features

Both `PromptTestFeature` and `AgentTestFeature` share the same core context resolution: pick an entity, serialize it via the adapter collection, build context items. The logic should live in a shared helper to avoid duplication.

**File:** `Umbraco.AI/src/Umbraco.AI.Core/Tests/AITestContextResolver.cs`

```csharp
/// <summary>
/// Resolves entity context items for test execution.
/// Shared by all test features that need entity context.
/// </summary>
internal sealed class AITestContextResolver
{
    private readonly AIServerEntityAdapterCollection _adapters;

    public async Task<List<AIRequestContextItem>> ResolveContextItemsAsync(
        Guid? entityId,
        string entityType,
        IEnumerable<AIRequestContextItem>? additionalItems,
        CancellationToken cancellationToken)
    {
        var items = new List<AIRequestContextItem>();

        // Auto-resolve entity context if EntityId is provided
        if (entityId is { } id && id != Guid.Empty)
        {
            var adapter = _adapters.GetAdapter(entityType);
            var serialized = await adapter.SerializeAsync(id, cancellationToken);

            if (serialized != null)
            {
                items.Add(new AIRequestContextItem
                {
                    Description = $"Currently editing {serialized.EntityType}: {serialized.Name}",
                    Value = JsonSerializer.Serialize(serialized)
                });
            }
        }

        // Merge any additional manual context items
        if (additionalItems != null)
        {
            items.AddRange(additionalItems);
        }

        return items;
    }
}
```

### 9. Update `PromptTestFeature` to use resolver

**File:** `Umbraco.AI.Prompt/src/Umbraco.AI.Prompt.Core/Tests/PromptTestFeature.cs`

Inject `AITestContextResolver`. Before calling prompt execution:

```csharp
var contextItems = await _contextResolver.ResolveContextItemsAsync(
    config.EntityId, config.EntityType, config.ContextItems, cancellationToken);

var request = new AIPromptExecutionRequest
{
    EntityId = config.EntityId ?? Guid.Empty,
    EntityType = config.EntityType,
    PropertyAlias = config.PropertyAlias,
    Culture = config.Culture,
    Segment = config.Segment,
    Context = contextItems.Count > 0 ? contextItems : null
};
```

### 10. Update `AgentTestFeature` to use resolver

**File:** `Umbraco.AI.Agent/src/Umbraco.AI.Agent.Core/Tests/AgentTestFeature.cs`

Inject `AITestContextResolver`. The agent config needs `EntityId` and `EntityType` fields added (same as prompt config). Before building the AG-UI run request:

```csharp
var resolvedItems = await _contextResolver.ResolveContextItemsAsync(
    config.EntityId, config.EntityType, config.ContextItems, cancellationToken);

// Convert to AG-UI context items
var aguiContext = resolvedItems.Select(item => new AGUIContextItem
{
    Description = item.Description,
    Value = item.Value ?? ""
}).ToList();

// Merge with any existing AG-UI context from config
if (config.Context is { Count: > 0 })
{
    aguiContext.AddRange(config.Context);
}

var request = new AGUIRunRequest
{
    ThreadId = config.ThreadId ?? test.Id.ToString(),
    RunId = $"{test.Id}-run-{runNumber}",
    Messages = config.Messages,
    Tools = config.Tools,
    State = config.State,
    Context = aguiContext
};
```

### 11. Update `AgentTestFeatureConfig`

**File:** `Umbraco.AI.Agent/src/Umbraco.AI.Agent.Core/Tests/AgentTestFeatureConfig.cs`

Add entity fields (same pattern as `PromptTestFeatureConfig`):

```csharp
/// <summary>
/// The entity type for context resolution (e.g., "document", "media").
/// </summary>
[AIField("Entity Type", ...)]
public string EntityType { get; set; } = "document";

/// <summary>
/// Optional entity ID for auto-resolving entity context.
/// When set, the entity is serialized and added to the agent's context automatically.
/// </summary>
[AIField("Entity", ...)]
public Guid? EntityId { get; set; }
```

The existing `Context` field (`List<AGUIContextItem>?`) becomes optional additional context — renamed/re-described as "Additional Context Items" for clarity.

### 12. Update `ContextItems` field descriptions

In both `PromptTestFeatureConfig` and `AgentTestFeatureConfig`, the manual context fields become "additional" context beyond the auto-resolved entity. Most tests won't need them.

### 13. Update unit tests

Migrate `AIEntityContextHelperTests` to use `AIServerEntityAdapterCollection` instead of `AIEntityFormatterCollection`.

## Shared config pattern

Both test feature configs converge on the same entity context fields:

| Field | PromptTestFeatureConfig | AgentTestFeatureConfig |
|-------|------------------------|----------------------|
| `EntityType` | `string` (default: `"document"`) | `string` (default: `"document"`) — **new** |
| `EntityId` | `Guid?` | `Guid?` — **new** |
| `ContextItems` / `Context` | `List<AIRequestContextItem>?` (additional) | `List<AGUIContextItem>?` (additional) |

Plus type-specific fields:

| Prompt-specific | Agent-specific |
|----------------|---------------|
| `PropertyAlias` | `Messages` |
| `Culture` | `Tools` |
| `Segment` | `State` |
| | `ThreadId` |

### 14. Replace hardcoded test config UI dropdowns

Currently `PromptTestFeatureConfig` uses hardcoded entity type options and property pickers. With the adapter collection providing metadata, browsing, and property inspection, the config UI becomes fully dynamic.

**Entity Type dropdown** — currently hardcoded `["document", "media", "member"]`:
- Replace with API endpoint: `GET /tests/entity-types`
- Returns `collection.GetEntityTypeAdapters()` mapped to `{ entityType, name, icon }`
- Third-party adapters automatically appear

**Entity picker** — currently basic entity picker linked to entity type:
- Replace with API endpoint: `GET /tests/entity-types/{entityType}/entities?parentId=...`
- Returns `adapter.GetEntitiesAsync(parentId)` → tree-browsable entity list
- `HasChildren` flag enables lazy tree expansion in the UI

**Property picker** — currently basic text field or hardcoded dropdown:
- Replace with API endpoint: `GET /tests/entity-types/{entityType}/entities/{entityId}/properties`
- Returns `adapter.GetPropertiesAsync(entityId)` → property alias/name/editor list
- Populates a dropdown with the actual properties from the selected entity

This makes the test config UI fully extensible — a commerce package registering `CommerceProductServerEntityAdapter` would automatically get its entity type in the dropdown, its products browsable in the picker, and its product properties in the property dropdown.

### Potential future addition: `GetEntitySchemaAsync`

The current interface has instance-level property inspection (`GetPropertiesAsync(entityId)`). There may also be a need for **type-level** schema information — e.g., "what does a Blog Post content type look like?" rather than "what properties does this specific blog post have?"

```csharp
/// <summary>
/// Gets the schema for an entity type/subtype.
/// For CMS entities, this would be based on the content type definition.
/// </summary>
Task<AIEntitySchema?> GetEntitySchemaAsync(
    string subType,  // e.g., content type alias "blogPost"
    CancellationToken cancellationToken = default);
```

Potential use cases:
- Property picker before a specific entity is selected (pick content type → see its properties)
- Template variable discovery (what variables will be available for this content type?)
- Test config validation (does this property alias exist on this type?)

**Not committed for v1** — noting as a potential extension point. The base class would default to returning null. If needed, it can be added without breaking existing adapters.

### Frontend tools limitation (v1)

Frontend tools (e.g., `set_value`, `get_page_info`) are **not supported in test execution** for v1:
- They are only discoverable in the browser's JS extension registry
- They require a live browser context to execute
- The AG-UI protocol expects tool results to come back from the frontend, which can't happen during a server-side test

Tests exercise **backend tools only**. The `Tools` field in `AgentTestFeatureConfig` remains available for manually providing tool schemas, but without a frontend to execute them, the LLM would be stuck waiting for results. A future enhancement could support tool response mocking.

## Data flow after this change

### Prompt test flow
```
Test Config: { EntityId: "abc-123", EntityType: "document", PropertyAlias: "bodyText" }
    |
    v
PromptTestFeature.ExecuteAsync()
    |
    +-- AITestContextResolver.ResolveContextItemsAsync("abc-123", "document", ...)
    |       -> AIServerEntityAdapterCollection.GetAdapter("document")
    |       -> DocumentServerEntityAdapter.SerializeAsync("abc-123")
    |       -> Returns AISerializedEntity → wrapped as AIRequestContextItem
    |
    v
AIPromptService.ExecutePromptAsync(promptId, request, options)
    |
    +-- SerializedEntityContributor → template variables, system message, currentValue
    +-- Template processing → Chat execution
```

### Agent test flow
```
Test Config: { EntityId: "abc-123", EntityType: "document", Messages: [...] }
    |
    v
AgentTestFeature.ExecuteAsync()
    |
    +-- AITestContextResolver.ResolveContextItemsAsync("abc-123", "document", ...)
    |       -> Same adapter pipeline as prompts
    |       -> Returns AIRequestContextItem → converted to AGUIContextItem
    |
    v
IAIAgentService.StreamAgentAsync(agentId, request, frontendTools, options)
    |
    +-- AGUIContextConverter → AIRequestContextItem
    +-- ScopedAIAgent → SerializedEntityContributor → system message injection
    +-- Agent reasoning → backend tool calls → streaming response
```

## Third-party extensibility

A commerce package registers **one** adapter that handles everything:

```csharp
// In Umbraco.Commerce.AI composer:
builder.AIServerEntityAdapters()
    .Add<CommerceProductServerEntityAdapter>();
```

```csharp
public class CommerceProductServerEntityAdapter : AIServerEntityAdapterBase
{
    public override string? EntityType => "commerce-product";

    public override string FormatForLlm(AISerializedEntity entity)
    {
        // Commerce-specific formatting (SKU, price, variants, etc.)
    }

    public override Task<AISerializedEntity?> SerializeAsync(
        Guid entityId, CancellationToken ct)
    {
        // Fetch product from commerce database and serialize
    }
}
```

## Summary of changes

### New files

| File | Description |
|------|-------------|
| `IAIServerEntityAdapter.cs` | Combined interface: metadata, format, serialize, browse, properties |
| `AIServerEntityAdapterBase.cs` | Base class with sensible defaults for optional capabilities |
| `AIServerEntityAdapterCollection.cs` | Collection with `GetAdapter()` and `GetEntityTypeAdapters()` |
| `AIServerEntityAdapterCollectionBuilder.cs` | Standard Umbraco collection builder |
| `AIEntityItem.cs` | Entity item model for browsing/picking (Id, Name, Icon, HasChildren) |
| `AIEntityProperty.cs` | Entity property model for property picker (Alias, Name, EditorAlias) |
| `DocumentServerEntityAdapter.cs` | Migrated from `AIDocumentEntityFormatter` + serialize, browse, properties |
| `MediaServerEntityAdapter.cs` | Serialize, browse, properties for media |
| `MemberServerEntityAdapter.cs` | Serialize, browse, properties for members |
| `GenericServerEntityAdapter.cs` | Migrated from `AIGenericEntityFormatter` (format-only fallback) |
| `UmbracoBuilderExtensions.ServerEntityAdapters.cs` | Builder extension method (replaces EntityFormatters) |
| `AITestContextResolver.cs` | Shared entity context resolution for test features |

### Modified files

| File | Change |
|------|--------|
| `AIEntityContextHelper.cs` | Inject `AIServerEntityAdapterCollection` instead of `AIEntityFormatterCollection` |
| `UmbracoBuilderExtensions.cs` | Register new adapters + `AITestContextResolver` |
| `PromptTestFeature.cs` | Inject resolver, auto-resolve context from EntityId |
| `PromptTestFeatureConfig.cs` | Update `ContextItems` description (now "additional" context) |
| `AgentTestFeature.cs` | Inject resolver, auto-resolve context from EntityId |
| `AgentTestFeatureConfig.cs` | Add `EntityType` + `EntityId` fields, update `Context` description |
| `AIEntityContextHelperTests.cs` | Migrate to new collection type |

### Deleted files

| File | Reason |
|------|--------|
| `IAIEntityFormatter.cs` | Replaced by `IAIServerEntityAdapter` |
| `AIEntityFormatterCollection.cs` | Replaced by `AIServerEntityAdapterCollection` |
| `AIEntityFormatterCollectionBuilder.cs` | Replaced by `AIServerEntityAdapterCollectionBuilder` |
| `AIDocumentEntityFormatter.cs` | Migrated to `DocumentServerEntityAdapter` |
| `AIGenericEntityFormatter.cs` | Migrated to `GenericServerEntityAdapter` |
| `UmbracoBuilderExtensions.EntityFormatters.cs` | Replaced by `ServerEntityAdapters` extension |
