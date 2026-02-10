# Agent > Tool Permission System Implementation Plan

## Overview

Implement a tool permission system for Umbraco.AI agents that allows administrators to control which tools each agent can access. This plan follows a **phased approach**: Phase 1 focuses on tool restrictions (implemented first), Phase 2 will add user group restrictions (future work).

## Context

### Current Architecture

- **Agents**: Defined in `AIAgent` entity, reference optional `ProfileId` for AI configuration
- **Tools**: Discovered via `[AITool]` attribute, managed by `AIToolCollection`
- **System Tools** (`IAISystemTool`): Always included, cannot be disabled
- **User Tools**: Regular tools that should be configurable per agent
- **Execution Flow**:
    1. HTTP Request → `RunAgentController`
    2. `AIAgentService.StreamAgentAsync()` orchestrates execution
    3. `AIAgentFactory.CreateAgentAsync()` creates MAF agent with tool list
    4. `ScopedAIAgent` manages per-execution scope
    5. Tools are called via Microsoft.Extensions.AI function calling

### Design Decisions (from user input)

- **Default tool behavior**: Allow common tool scopes (Search, Navigation, etc.) for existing agents
- **Phasing**: Implement tool restrictions first (Phase 1), user group restrictions later (Phase 2)
- **Validation**: Backend-only validation (no frontend pre-checks in V1)
- **User groups**: Deferred to Phase 2 - need to consider automation/API user scenarios

## Phase 1: Tool Permission System

### 1. Data Model Changes

#### 1.1 Update AIAgent Entity

**File**: `D:\Work\Umbraco\Umbraco.AI\Umbraco.AI.Agent\src\Umbraco.AI.Agent.Core\Agents\AIAgent.cs`

Add two properties:

```csharp
/// <summary>
/// Tool IDs explicitly enabled for this agent.
/// Empty list means no specific tools are enabled (only scopesapply).
/// </summary>
public IReadOnlyList<string> EnabledToolIds { get; set; } = [];

/// <summary>
/// Tool scopes to enable for this agent.
/// Tools matching these scopeswill be included automatically.
/// System tools are always included regardless of this setting.
/// </summary>
public IReadOnlyList<string> EnabledToolScopeIds { get; set; } = [];
```

**Tool Resolution Logic**:

1. System tools (`IAISystemTool`) are **always included** (cannot be disabled)
2. User tools are included if:
    - Tool ID is in `EnabledToolIds`, OR
    - Tool Scope is in `EnabledToolScopeIds`
3. Deduplication: If a tool matches both criteria, include it once

**Tool Scope Granularity Design**:

Tool scopes use **operation-level granularity** to separate read from write operations and accurately reflect destructiveness.

**Problem with Broad Scopes**:

- A "Content" scopecontaining both `content.get` (safe) and `content.delete` (destructive) must be marked as destructive
- Users can't enable read operations without also enabling write operations
- Ambiguity about what's actually being allowed
- Hard to create read-only agents

**Solution: Operation-Level Scopes**:

Split domain scopesby operation type:

| Scope           | Description               | Destructive | Example Tools                                                           |
| --------------- | ------------------------- | ----------- | ----------------------------------------------------------------------- |
| `content.read`  | Read content operations   | No          | `content.get`, `content.search`, `content.list`                         |
| `content.write` | Modify content operations | Yes         | `content.create`, `content.update`, `content.publish`, `content.delete` |
| `media.read`    | Read media operations     | No          | `media.get`, `media.search`, `media.list`                               |
| `media.write`   | Modify media operations   | Yes         | `media.upload`, `media.update`, `media.delete`, `media.move`            |
| `navigation`    | Site structure navigation | No          | `navigation.tree`, `navigation.breadcrumb`, `navigation.children`       |
| `search`        | Search operations         | No          | `search.fulltext`, `search.semantic`, `search.similar`                  |
| `translation`   | Translation operations    | No          | `translation.translate`, `translation.detect`                           |
| `web`           | External web operations   | No          | `web.fetch`, `web.scrape`                                               |
| `entity.read`   | Read entity operations    | No          | `entity.get`, `entity.serialize`                                        |
| `entity.write`  | Modify entity operations  | Yes         | `entity.setProperty`, `entity.save`                                     |

**Benefits**:

- **Clear destructiveness**: Scopes accurately marked as destructive or not
- **Read-only agents**: Enable only `.read` scopes
- **Bulk selection**: Enable all content read operations at once
- **Intuitive**: Users understand what each scopeallows
- **Scalable**: Works with many tools without overwhelming UI

**Agent Configuration Examples**:

**Read-Only Assistant**:

```json
{
    "enabledToolScopeIds": ["content.read", "media.read", "navigation", "search"]
}
```

**Content Editor Agent**:

```json
{
    "enabledToolScopeIds": ["content.read", "content.write", "media.read", "navigation", "search"]
}
```

**Translation Agent**:

```json
{
    "enabledToolScopeIds": ["content.read", "translation"],
    "enabledToolIds": [
        "content.update" // Can update content with translations
    ]
}
```

**Fine-Grained Control with Tool IDs**:

For even more precise control, combine scopeswith specific tool IDs:

```json
{
    "enabledToolScopeIds": ["navigation", "search"],
    "enabledToolIds": [
        "content.get", // Individual tool from content.read
        "content.update" // Individual tool from content.write (without delete/publish)
    ]
}
```

**UI Impact**:

- Scopes grouped by domain in UI: "Content", "Media", "Navigation", etc.
- Each domain shows read/write subscopes with destructive badges
- Clear visual distinction between safe and destructive operation groups

**Multiple Scopes per Tool**:

**Current Architecture**: Tools have a single `string ScopeId` property (not `ScopeIds[]`).

**Question**: Should tools support multiple scopes?

**Option A: Single Scope (Recommended for V1)**

- **Current state**: Each tool has one scope
- **Implementation**: Keep existing `string ScopeId` property
- **Rationale**:
    - Simpler data model and permission logic
    - If a tool truly serves multiple purposes, it should probably be split
    - Can always add multi-scope support later without breaking changes
- **Example**:
    ```csharp
    [AITool("content.get", "Get Content", Scope = "content.read")]
    [AITool("content.update", "Update Content", Scope = "content.write")]
    ```

**Option B: Multiple Scopes (Future Enhancement)**

- **Change**: Modify `ScopeId` to `ScopeIds` (string array)
- **Implementation**:

    ```csharp
    // IAITool interface
    IReadOnlyList<string> ScopeIds { get; }

    // Tool attribute
    [AITool("hybrid.tool", "Hybrid Tool", ScopeIds = new[] {"content.read", "search"})]
    ```

- **Permission logic**: Tool is enabled if ANY of its scopesare enabled
- **UI consideration**: Tool appears under multiple scopegroups
- **Trade-off**: More complexity, potential confusion about tool's primary purpose

**Recommendation**: Use **Option A (single scope)** for V1. The operation-level scopescheme (`content.read`, `content.write`) already provides good granularity. If multi-scope support is needed later, it can be added as a non-breaking change by:

1. Adding a `ScopeIds` property alongside existing `ScopeId`
2. Treating `ScopeId` as a fallback if `ScopeIds` is empty
3. Updating permission logic to check both

**Tool Scope Implementation** (Aligned with IAIAgentScope Pattern):

Tool scopes will follow the same pattern as `IAIAgentScope` for consistency:

#### Create Tool Scope Infrastructure

**New Interface**: `IAIToolScope` (similar to `IAIAgentScope`)

```csharp
namespace Umbraco.AI.Core.Tools.Scopes;

/// <summary>
/// Interface for tool scope definitions.
/// </summary>
/// <remarks>
/// <para>
/// Tool scopes categorize tools by their operational scope (e.g., content.read, content.write).
/// They enable bulk tool enablement via scopes and clear destructiveness marking.
/// </para>
/// <para>
/// Scopes are discovered via the <see cref="AIToolScopeAttribute"/> and registered
/// in the <see cref="AIToolScopeCollection"/>.
/// </para>
/// <para>
/// Localization is handled by the frontend using the convention:
/// <list type="bullet">
///   <item>Name: <c>uaiToolScope_{scopeId}Label</c></item>
///   <item>Description: <c>uaiToolScope_{scopeId}Description</c></item>
/// </list>
/// </para>
/// </remarks>
public interface IAIToolScope
{
    /// <summary>
    /// Gets the unique identifier for this scope.
    /// </summary>
    /// <remarks>
    /// Examples: "content.read", "content.write", "media.read", "search"
    /// </remarks>
    string Id { get; }

    /// <summary>
    /// Gets the icon to display for this scope.
    /// </summary>
    /// <remarks>
    /// Uses Umbraco icon names (e.g., "icon-folder", "icon-edit").
    /// </remarks>
    string Icon { get; }

    /// <summary>
    /// Gets whether tools in this scope are destructive (modify data).
    /// </summary>
    bool IsDestructive { get; }

    /// <summary>
    /// Gets the domain grouping for UI organization.
    /// </summary>
    /// <remarks>
    /// Used to group related scopes in the UI (e.g., "Content", "Media", "General").
    /// </remarks>
    string Domain { get; }
}
```

**New Attribute**: `AIToolScopeAttribute`

```csharp
namespace Umbraco.AI.Core.Tools.Scopes;

/// <summary>
/// Attribute to mark tool scope implementations for auto-discovery.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class AIToolScopeAttribute : Attribute
{
    public string Id { get; }
    public string Icon { get; set; } = "icon-tag";
    public bool IsDestructive { get; set; }
    public string Domain { get; set; } = "General";

    public AIToolScopeAttribute(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        Id = id;
    }
}
```

**Base Class**: `AIToolScopeBase`

```csharp
namespace Umbraco.AI.Core.Tools.Scopes;

/// <summary>
/// Base class for tool scope implementations.
/// </summary>
public abstract class AIToolScopeBase : IAIToolScope, IDiscoverable
{
    public abstract string Id { get; }
    public abstract string Icon { get; }
    public abstract bool IsDestructive { get; }
    public abstract string Domain { get; }
}
```

**Collection**: `AIToolScopeCollection` and `AIToolScopeCollectionBuilder`

```csharp
namespace Umbraco.AI.Core.Tools.Scopes;

public class AIToolScopeCollection : BuilderCollectionBase<IAIToolScope>
{
    public IAIToolScope? GetById(string id) =>
        this.FirstOrDefault(s => s.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

    public IEnumerable<IAIToolScope> GetByDomain(string domain) =>
        this.Where(s => s.Domain.Equals(domain, StringComparison.OrdinalIgnoreCase));
}

public class AIToolScopeCollectionBuilder
    : LazyCollectionBuilderBase<AIToolScopeCollectionBuilder, AIToolScopeCollection, IAIToolScope>
{
    // Auto-discovers scopes marked with [AIToolScope]
}
```

#### Built-in Tool Scopes

**Register built-in scopes**:

```csharp
// Content scopes
[AIToolScope("content.read", Icon = "icon-folder", Domain = "Content")]
public class ContentReadScope : AIToolScopeBase
{
    public override string Id => "content.read";
    public override string Icon => "icon-folder";
    public override bool IsDestructive => false;
    public override string Domain => "Content";
}

[AIToolScope("content.write", Icon = "icon-edit", IsDestructive = true, Domain = "Content")]
public class ContentWriteScope : AIToolScopeBase
{
    public override string Id => "content.write";
    public override string Icon => "icon-edit";
    public override bool IsDestructive => true;
    public override string Domain => "Content";
}

// Media scopes
[AIToolScope("media.read", Icon = "icon-picture", Domain = "Media")]
public class MediaReadScope : AIToolScopeBase { /* ... */ }

[AIToolScope("media.write", Icon = "icon-picture-add", IsDestructive = true, Domain = "Media")]
public class MediaWriteScope : AIToolScopeBase { /* ... */ }

// General scopes
[AIToolScope("search", Icon = "icon-search", Domain = "General")]
public class SearchScope : AIToolScopeBase { /* ... */ }

[AIToolScope("navigation", Icon = "icon-sitemap", Domain = "General")]
public class NavigationScope : AIToolScopeBase { /* ... */ }

[AIToolScope("translation", Icon = "icon-globe", Domain = "General")]
public class TranslationScope : AIToolScopeBase { /* ... */ }

// etc.
```

#### Update Tools to Reference Scopes

Change from `ScopeId` property to `ScopeId` property:

```csharp
// Before (current)
[AITool("content.get", "Get Content", ScopeId = "Content")]

// After (aligned with IAIAgentScope pattern)
[AITool("content.get", "Get Content", ScopeId = "content.read")]
[AITool("content.update", "Update Content", ScopeId = "content.write")]
[AITool("media.search", "Search Media", ScopeId = "media.read")]
```

**Update IAITool interface**:

```csharp
public interface IAITool
{
    string Id { get; }
    string Name { get; }
    string Description { get; }
    string ScopeId { get; }  // ← Changed from Category
    // ... other properties
}
```

#### Benefits of Aligning with IAIAgentScope

1. **Consistency**: Same pattern for both agent scopes and tool scopes
2. **Type safety**: Scopes are first-class objects, not just strings
3. **Metadata**: Scopes carry icon, destructiveness, domain grouping
4. **Extensibility**: Add-ons can register custom scopes via attributes
5. **Validation**: Can validate scope IDs exist in collection
6. **UI**: Can query `AIToolScopeCollection` to build UI with icons, groups, metadata

#### 1.2 Update Persistence Entity

**File**: `D:\Work\Umbraco\Umbraco.AI\Umbraco.AI.Agent\src\Umbraco.AI.Agent.Persistence\Agents\AIAgentEntity.cs`

Add corresponding entity properties with JSON serialization:

```csharp
/// <summary>
/// JSON-serialized array of enabled tool IDs.
/// </summary>
public string? EnabledToolIds { get; set; }

/// <summary>
/// JSON-serialized array of enabled tool scopes.
/// </summary>
public string? EnabledToolScopeIds { get; set; }
```

#### 1.3 EF Core Configuration

**File**: `D:\Work\Umbraco\Umbraco.AI\Umbraco.AI.Agent\src\Umbraco.AI.Agent.Persistence\UmbracoAIAgentDbContext.cs`

Update `OnModelCreating()` to handle JSON conversion (consistent with existing `ContextIds`, `ScopeIds` pattern):

```csharp
builder.Property(e => e.EnabledToolIds)
    .HasMaxLength(4000)
    .HasConversion(
        v => v == null ? null : JsonSerializer.Serialize(v, JsonOptions),
        v => v == null ? null : JsonSerializer.Deserialize<List<string>>(v, JsonOptions));

builder.Property(e => e.EnabledToolScopeIds)
    .HasMaxLength(2000)
    .HasConversion(
        v => v == null ? null : JsonSerializer.Serialize(v, JsonOptions),
        v => v == null ? null : JsonSerializer.Deserialize<List<string>>(v, JsonOptions));
```

#### 1.4 Database Migration

**Migration Name**: `UmbracoAIAgent_AddToolPermissions`

**Changes**:

- Add nullable `EnabledToolIds` column (nvarchar(4000))
- Add nullable `EnabledToolScopeIds` column (nvarchar(2000))
- Create SQL Server and SQLite variants

**Default Values for Existing Agents**:
Set common safe scopesfor backward compatibility:

```sql
-- Migrate existing agents to have default tool scopes
UPDATE UmbracoAIAgent_Agents
SET EnabledToolScopeIds = '["Search","Navigation","Translation","Web"]'
WHERE EnabledToolScopeIds IS NULL;
```

### 2. Update IAIAgentService Interface

**File**: `D:\Work\Umbraco\Umbraco.AI\Umbraco.AI.Agent\src\Umbraco.AI.Agent.Core\Agents\IAIAgentService.cs`

Add two new methods to the existing interface:

```csharp
/// <summary>
/// Gets the tools that are enabled for the specified agent.
/// Includes system tools (always) + user tools matching agent configuration.
/// </summary>
/// <param name="agent">The agent.</param>
/// <param name="cancellationToken">Cancellation token.</param>
/// <returns>Collection of enabled tool IDs.</returns>
Task<IReadOnlyList<string>> GetEnabledToolIdsAsync(
    AIAgent agent,
    CancellationToken cancellationToken = default);

/// <summary>
/// Validates that a specific tool call is permitted for the agent.
/// </summary>
/// <param name="agent">The agent.</param>
/// <param name="toolId">The tool ID being called.</param>
/// <param name="cancellationToken">Cancellation token.</param>
/// <returns>True if tool is enabled, false otherwise.</returns>
Task<bool> IsToolEnabledAsync(
    AIAgent agent,
    string toolId,
    CancellationToken cancellationToken = default);
```

**Overloads for convenience** (optional):

```csharp
/// <summary>
/// Gets enabled tools by agent ID.
/// </summary>
Task<IReadOnlyList<string>> GetEnabledToolIdsAsync(
    Guid agentId,
    CancellationToken cancellationToken = default);

/// <summary>
/// Validates tool call by agent ID.
/// </summary>
Task<bool> IsToolEnabledAsync(
    Guid agentId,
    string toolId,
    CancellationToken cancellationToken = default);
```

### 3. Update AIAgentService Implementation

**File**: `D:\Work\Umbraco\Umbraco.AI\Umbraco.AI.Agent\src\Umbraco.AI.Agent.Core\Agents\AIAgentService.cs`

Inject `AIToolCollection` into constructor (if not already present):

```csharp
private readonly AIToolCollection _toolCollection;

public AIAgentService(
    /* existing parameters */,
    AIToolCollection toolCollection)
{
    /* existing assignments */
    _toolCollection = toolCollection;
}
```

Add implementation methods:

```csharp
public Task<IReadOnlyList<string>> GetEnabledToolIdsAsync(
    AIAgent agent,
    CancellationToken cancellationToken = default)
{
    ArgumentNullException.ThrowIfNull(agent);

    var enabledTools = new List<string>();

    // 1. Always include system tools
    var systemToolIds = _toolCollection
        .Where(t => t is IAISystemTool)
        .Select(t => t.Id);
    enabledTools.AddRange(systemToolIds);

    // 2. Add explicitly enabled tool IDs
    if (agent.EnabledToolIds.Count > 0)
    {
        enabledTools.AddRange(agent.EnabledToolIds);
    }

    // 3. Add tools from enabled scopes
    if (agent.EnabledToolScopeIds.Count > 0)
    {
        foreach (var scope in agent.EnabledToolScopeIds)
        {
            var scopeToolIds = _toolCollection.GetByScope(scope)
                .Where(t => t is not IAISystemTool) // Don't duplicate system tools
                .Select(t => t.Id);
            enabledTools.AddRange(scopeToolIds);
        }
    }

    // 4. Deduplicate and return
    var result = enabledTools
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();

    return Task.FromResult<IReadOnlyList<string>>(result);
}

public async Task<bool> IsToolEnabledAsync(
    AIAgent agent,
    string toolId,
    CancellationToken cancellationToken = default)
{
    ArgumentNullException.ThrowIfNull(agent);
    ArgumentException.ThrowIfNullOrWhiteSpace(toolId);

    // System tools are always enabled
    var tool = _toolCollection.FirstOrDefault(t =>
        t.Id.Equals(toolId, StringComparison.OrdinalIgnoreCase));

    if (tool is IAISystemTool)
    {
        return true;
    }

    // Check if tool is in enabled list
    var enabledToolIds = await GetEnabledToolIdsAsync(agent, cancellationToken);
    return enabledToolIds.Contains(toolId, StringComparer.OrdinalIgnoreCase);
}

// Optional overloads
public async Task<IReadOnlyList<string>> GetEnabledToolIdsAsync(
    Guid agentId,
    CancellationToken cancellationToken = default)
{
    var agent = await GetAgentAsync(agentId, cancellationToken);
    if (agent == null)
    {
        throw new InvalidOperationException($"Agent with ID '{agentId}' not found");
    }
    return await GetEnabledToolIdsAsync(agent, cancellationToken);
}

public async Task<bool> IsToolEnabledAsync(
    Guid agentId,
    string toolId,
    CancellationToken cancellationToken = default)
{
    var agent = await GetAgentAsync(agentId, cancellationToken);
    if (agent == null)
    {
        return false;
    }
    return await IsToolEnabledAsync(agent, toolId, cancellationToken);
}
```

### 4. Integration Points

#### 4.1 Tool Resolution in Agent Factory

**File**: `D:\Work\Umbraco\Umbraco.AI\Umbraco.AI.Agent\src\Umbraco.AI.Agent.Core\Chat\AIAgentFactory.cs`

**Method**: `CreateAgentAsync` (around line 46-97)

**Changes**:

1. Inject `IAIAgentService` into constructor (if not already present)
2. Get enabled tools for agent before creating tool list
3. Filter tools to only include enabled ones

```csharp
public async Task<AIAgent> CreateAgentAsync(
    AIAgent agent,
    IEnumerable<AIRequestContextItem>? contextItems = null,
    IEnumerable<AITool>? additionalTools = null,
    IReadOnlyDictionary<string, object?>? additionalProperties = null,
    CancellationToken cancellationToken = default)
{
    ArgumentNullException.ThrowIfNull(agent);

    // NEW: Get enabled tools for this agent
    var enabledToolIds = await _agentService.GetEnabledToolIdsAsync(
        agent,
        cancellationToken);

    // Build tool list using only enabled tools
    var tools = new List<AITool>();

    // MODIFIED: Filter system and user tools based on permissions
    var enabledTools = _toolCollection
        .Where(t => enabledToolIds.Contains(t.Id, StringComparer.OrdinalIgnoreCase));

    tools.AddRange(enabledTools.Select(t => _functionFactory.Create(t)));

    // Frontend tools - filter through permissions
    // Note: Tool metadata (scope info) is extracted from forwardedProps in RunAgentController
    // and passed to this method via additionalProperties
    var frontendTools = additionalTools?.ToList() ?? [];
    if (frontendTools.Count > 0)
    {
        // Extract tool metadata from additional properties (populated by RunAgentController)
        var toolMetadata = ExtractToolMetadata(additionalProperties);

        var allowedFrontendTools = frontendTools
            .Where(t => {
                var toolName = t.Metadata?.Name ?? string.Empty;

                // Find metadata for this tool
                var metadata = toolMetadata.FirstOrDefault(m =>
                    m.Name.Equals(toolName, StringComparer.OrdinalIgnoreCase));

                if (metadata == null)
                {
                    // No metadata - only allow if explicitly in EnabledToolIds
                    return enabledToolIds.Contains(toolName, StringComparer.OrdinalIgnoreCase);
                }

                // Check if tool ID is explicitly enabled
                if (enabledToolIds.Contains(toolName, StringComparer.OrdinalIgnoreCase))
                    return true;

                // Check if tool scope is enabled
                if (!string.IsNullOrEmpty(metadata.Scope) &&
                    agent.EnabledToolScopeIds.Contains(metadata.Scope, StringComparer.OrdinalIgnoreCase))
                    return true;

                return false;
            });
        tools.AddRange(allowedFrontendTools);
    }

    // ... rest of method unchanged
}
```

**Why here**: This ensures only permitted tools are passed to the AI model. The LLM cannot invoke tools that weren't included in its tool list.

**Frontend Tool Permission Approach**:

Frontend tools (e.g., `setPropertyValue`, entity mutation tools) will respect agent permissions for consistency and defense-in-depth:

- **Rationale**:
    - Consistency - all tools governed by same permission rules
    - Defense in depth - permissions checked before tools are even available to the LLM
    - Prevents the LLM from attempting to call tools the agent shouldn't have access to
    - Better error messages - permission denied vs. tool not found
- **Implementation**: Extract tool metadata from `forwardedProps` and validate against agent permissions
- **Frontend Consideration**: Frontend sends tool metadata via AGUI `forwardedProps` field

**Frontend Tool Metadata via forwardedProps**:

Frontend tool manifests (`uaiAgentTool`) include scope information in their `meta` field. This metadata is sent to the backend via the AGUI protocol's `forwardedProps` field.

**1. Add Scope to Frontend Tool Manifests**:

```typescript
const setPropertyValueManifest: ManifestUaiAgentTool = {
    type: "uaiAgentTool",
    alias: "Uai.AgentTool.SetPropertyValue",
    meta: {
        toolName: "setPropertyValue",
        description: "Update a property value...",
        scope: "entity.write", // ← NEW: Add scope (operation-level)
        isDestructive: true,
        parameters: {
            /* ... */
        },
    },
};
```

**2. Frontend Sends Metadata via forwardedProps**:

In `copilot-run.controller.ts`, when calling the agent API:

```typescript
// Extract tool metadata from manifests
const toolMetadata = this.#toolManager.frontendTools.map((manifest) => ({
    name: manifest.meta.toolName,
    scope: manifest.meta.scope,
    isDestructive: manifest.meta.isDestructive ?? false,
}));

// Send via forwardedProps
await this.#client.sendMessage(
    nextMessages,
    this.#toolManager.frontendTools, // Standard AGUI tools
    this.#pendingContext,
    {
        toolMetadata: toolMetadata, // ← Metadata in forwardedProps
    },
);
```

**3. Backend Extracts and Validates Metadata**:

In `RunAgentController`, extract metadata from `forwardedProps`:

```csharp
// Extract tool metadata from forwardedProps
var toolMetadata = ExtractToolMetadata(request.ForwardedProps);

// Validate each frontend tool against agent permissions
var allowedFrontendTools = frontendTools
    .Where(t => {
        var toolName = t.Metadata?.Name ?? string.Empty;

        // Find metadata for this tool
        var metadata = toolMetadata.FirstOrDefault(m =>
            m.Name.Equals(toolName, StringComparer.OrdinalIgnoreCase));

        if (metadata == null)
        {
            // No metadata - only allow if explicitly in EnabledToolIds
            return enabledToolIds.Contains(toolName, StringComparer.OrdinalIgnoreCase);
        }

        // Check if tool ID is explicitly enabled
        if (enabledToolIds.Contains(toolName, StringComparer.OrdinalIgnoreCase))
            return true;

        // Check if tool scope is enabled
        if (!string.IsNullOrEmpty(metadata.Scope) &&
            agent.EnabledToolScopeIds.Contains(metadata.Scope, StringComparer.OrdinalIgnoreCase))
            return true;

        return false;
    });
```

**Helper Method for Extracting Metadata**:

```csharp
private IReadOnlyList<ToolMetadata> ExtractToolMetadata(object? forwardedProps)
{
    if (forwardedProps == null)
        return Array.Empty<ToolMetadata>();

    // Deserialize forwardedProps to extract toolMetadata
    var json = JsonSerializer.Serialize(forwardedProps);
    var props = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

    if (props?.TryGetValue("toolMetadata", out var metadataElement) == true)
    {
        return JsonSerializer.Deserialize<List<ToolMetadata>>(metadataElement.GetRawText())
            ?? Array.Empty<ToolMetadata>();
    }

    return Array.Empty<ToolMetadata>();
}

private record ToolMetadata(string Name, string? Scope, bool IsDestructive);
```

**Benefits of Using forwardedProps**:

- **AGUI Protocol Compliant**: Uses official extensibility mechanism
- **No Custom Events**: Simpler than creating custom event handlers
- **Defense in Depth**: Backend validates all tools regardless of frontend filtering
- **Standard Tool Format**: AGUI tools remain unchanged, metadata travels separately

#### 4.2 Constructor Injection

**File**: Same as above

Add constructor parameter (if not already present):

```csharp
private readonly IAIAgentService _agentService;

public AIAgentFactory(
    /* existing parameters */,
    IAIAgentService agentService)
{
    /* existing assignments */
    _agentService = agentService;
}
```

**Note**: `AIAgentFactory` may already have `IAIAgentService` injected. If so, just use the existing field.

#### 4.3 Tool Metadata Extraction in RunAgentController

**File**: `D:\Work\Umbraco\Umbraco.AI\Umbraco.AI.Agent\src\Umbraco.AI.Agent.Web\Api\Management\Agent\Controllers\RunAgentController.cs`

**Method**: Agent run endpoint handler

**Changes**:

1. Extract tool metadata from `request.ForwardedProps`
2. Pass metadata to `AIAgentFactory` via `additionalProperties`
3. Log validation failures for audit

```csharp
[HttpPost("{agentIdOrAlias}/run")]
public async Task<IActionResult> RunAgent(
    string agentIdOrAlias,
    [FromBody] AGUIRunRequestModel request,
    CancellationToken ct)
{
    // Resolve agent
    var agent = await ResolveAgent(agentIdOrAlias, ct);
    if (agent == null)
        return NotFound();

    // Extract tool metadata from forwardedProps
    var toolMetadata = ExtractToolMetadata(request.ForwardedProps);

    // Pass metadata to factory via additionalProperties
    var additionalProperties = new Dictionary<string, object?>
    {
        ["toolMetadata"] = toolMetadata,
        // ... other properties
    };

    // Convert frontend tools to AITool format
    var frontendTools = ConvertAGUITools(request.Tools);

    // Create agent with tools and metadata
    var agentInstance = await _agentFactory.CreateAgentAsync(
        agent,
        contextItems,
        frontendTools,
        additionalProperties,
        ct);

    // Stream response
    return StreamAgentResponse(agentInstance, request.Messages, ct);
}

private IReadOnlyList<ToolMetadata> ExtractToolMetadata(object? forwardedProps)
{
    if (forwardedProps == null)
        return Array.Empty<ToolMetadata>();

    try
    {
        // Deserialize forwardedProps to extract toolMetadata
        var json = JsonSerializer.Serialize(forwardedProps);
        var props = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

        if (props?.TryGetValue("toolMetadata", out var metadataElement) == true)
        {
            return JsonSerializer.Deserialize<List<ToolMetadata>>(
                metadataElement.GetRawText()) ?? Array.Empty<ToolMetadata>();
        }
    }
    catch (JsonException ex)
    {
        _logger.LogWarning(ex, "Failed to extract tool metadata from forwardedProps");
    }

    return Array.Empty<ToolMetadata>();
}

private record ToolMetadata(string Name, string? Scope, bool IsDestructive);
```

**Why here**: RunAgentController is the entry point for agent execution. It's responsible for:

- Receiving AGUI protocol requests
- Extracting metadata from `forwardedProps` (AGUI extensibility field)
- Passing validated data to the service layer

### 5. API Changes

#### 5.1 Request/Response Models

**Files**:

- `D:\Work\Umbraco\Umbraco.AI\Umbraco.AI.Agent\src\Umbraco.AI.Agent.Web\Api\Management\Agent\Models\AgentCreateRequestModel.cs`
- `D:\Work\Umbraco\Umbraco.AI\Umbraco.AI.Agent\src\Umbraco.AI.Agent.Web\Api\Management\Agent\Models\AgentUpdateRequestModel.cs`
- `D:\Work\Umbraco\Umbraco.AI\Umbraco.AI.Agent\src\Umbraco.AI.Agent.Web\Api\Management\Agent\Models\AgentResponseModel.cs`

Add properties to all three:

```csharp
public IReadOnlyList<string>? EnabledToolIds { get; set; }
public IReadOnlyList<string>? EnabledToolScopeIds { get; set; }
```

#### 5.2 Mapping Configuration

**File**: `D:\Work\Umbraco\Umbraco.AI\Umbraco.AI.Agent\src\Umbraco.AI.Agent.Web\Mapping\AgentMapDefinition.cs`

Update mapper to include new properties in both directions (domain ↔ API models).

#### 5.3 Optional: Tool Query Endpoint

**New File**: `D:\Work\Umbraco\Umbraco.AI\Umbraco.AI.Agent\src\Umbraco.AI.Agent.Web\Api\Management\Agent\Controllers\GetAgentToolsController.cs`

```csharp
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class GetAgentToolsController : AgentControllerBase
{
    private readonly IAIAgentService _agentService;

    public GetAgentToolsController(IAIAgentService agentService)
    {
        _agentService = agentService;
    }

    [HttpGet("{agentIdOrAlias:regex(^[a-zA-Z0-9-_]+$)}/enabled-tools")]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEnabledTools(string agentIdOrAlias)
    {
        // Resolve agent by ID or alias
        var agent = Guid.TryParse(agentIdOrAlias, out var agentId)
            ? await _agentService.GetAgentAsync(agentId, HttpContext.RequestAborted)
            : await _agentService.GetAgentByAliasAsync(agentIdOrAlias, HttpContext.RequestAborted);

        if (agent == null)
        {
            return NotFound();
        }

        // Get enabled tools
        var toolIds = await _agentService.GetEnabledToolIdsAsync(
            agent,
            HttpContext.RequestAborted);

        return Ok(toolIds);
    }
}
```

**Endpoint**: `GET /umbraco/ai/management/api/v1/agents/{agentId}/enabled-tools`

### 6. Frontend UI for Permission Management

#### 6.1 Agent Edit Workspace - Tool Permission Section

**Location**: Agent edit workspace (similar to Profile, Context sections)

**Component Structure**:

```
┌─────────────────────────────────────────────────────────────┐
│ Agent: Content Assistant                                    │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│ [Profile]  [Instructions]  [Context]  [Tools]  [Scopes]    │
│                                        ^^^^^^               │
├─────────────────────────────────────────────────────────────┤
│ TOOLS                                                       │
│                                                             │
│ ┌───────────────────────────────────────────────────────┐   │
│ │ ENABLED TOOL IDs                                      │   │
│ │                                                       │   │
│ │ [fetch_webpage                        ] [Remove]      │   │
│ │ [search.semantic                      ] [Remove]      │   │
│ │                                                       │   │
│ │ [+ Add Tool]                                          │   │
│ │                                                       │   │
│ │ ┌─────────────────────────────────────────────────┐   │   │
│ │ │ Available Tools (select to add)                 │   │   │
│ │ │ ┌─────────────────────────────────────────────┐ │   │   │
│ │ │ │ 🔍 content.search - Search content          │ │   │   │
│ │ │ │ 📄 content.get - Get content by ID          │ │   │   │
│ │ │ │ ⚠️  content.update - Update content (dest.) │ │   │   │
│ │ │ │ 🌐 translation.translate - Translate text   │ │   │   │
│ │ │ └─────────────────────────────────────────────┘ │   │   │
│ │ │ [Search tools...]                               │   │   │
│ │ └─────────────────────────────────────────────────┘   │   │
│ └───────────────────────────────────────────────────────┘   │
│                                                             │
│ ┌───────────────────────────────────────────────────────┐   │
│ │ ENABLED SCOPES                                        │   │
│ │                                                       │   │
│ │ Content Operations:                                   │   │
│ │   ☑ content.read       (includes 3 tools)             │   │
│ │   ☐ content.write      (includes 4 tools) ⚠️           │   │
│ │                                                       │   │
│ │ Media Operations:                                     │   │
│ │   ☐ media.read         (includes 3 tools)             │   │
│ │   ☐ media.write        (includes 4 tools) ⚠️           │   │
│ │                                                       │   │
│ │ General Operations:                                   │   │
│ │   ☑ search             (includes 4 tools)             │   │
│ │   ☑ navigation         (includes 5 tools)             │   │
│ │   ☐ translation        (includes 2 tools)             │   │
│ │   ☐ web                (includes 3 tools)             │   │
│ │                                                       │   │
│ │ Entity Operations:                                    │   │
│ │   ☐ entity.read        (includes 2 tools)             │   │
│ │   ☐ entity.write       (includes 3 tools) ⚠️           │   │
│ │                                                       │   │
│ └───────────────────────────────────────────────────────┘   │
│                                                             │
│ 💡 System tools are always included and cannot be removed  │
│                                                             │
│ [Save]  [Cancel]                                            │
└─────────────────────────────────────────────────────────────┘
```

**Key Features**:

1. **Dual Selection Model**:
    - **Specific Tools**: Tag-style input with dropdown for available tools
    - **Scopes**: Checkbox list with tool count indicators
    - Both can be used simultaneously

2. **Tool Indicators**:
    - Icon per tool scope
    - ⚠️ badge for destructive tools
    - 🔒 badge for system tools (shown but disabled)
    - Tool count per scope: "(includes X tools)"

3. **Search/Filter**:
    - Search box to filter tool list by name/description
    - Filter by scope
    - Filter by destructive/non-destructive

4. **Preview Pane** (optional):
    ```
    ┌───────────────────────────────────────┐
    │ EFFECTIVE TOOLS (12)                  │
    ├───────────────────────────────────────┤
    │ System Tools (2):                     │
    │  • list_context_resources             │
    │  • get_context_resource               │
    │                                       │
    │ Enabled Tools (10):                   │
    │  • fetch_webpage                      │
    │  • search.semantic                    │
    │  • search.fulltext (via "Search")     │
    │  • navigation.tree (via "Navigation") │
    │  • ...                                │
    └───────────────────────────────────────┘
    ```

#### 6.2 Tool Selector Component

**New Component**: `<uai-tool-selector>` (Lit element)

**Location**: `D:\Work\Umbraco\Umbraco.AI\Umbraco.AI.Agent\src\Umbraco.AI.Agent.Web.StaticAssets\Client\src\agent\components\tool-selector\`

**Props**:

```typescript
@property({ type: Array })
enabledToolIds: string[] = [];

@property({ type: Array })
enabledScopes: string[] = [];

@property({ type: Boolean })
readonly: boolean = false;
```

**Events**:

```typescript
// Dispatched when tool selection changes
"tool-selection-change": CustomEvent<{
  enabledToolIds: string[];
  enabledScopes: string[];
}>
```

**API Integration**:

- Fetch all available tools: `GET /umbraco/ai/management/api/v1/tools` (if exists)
- Or: Extract from `AIToolCollection` via new API endpoint
- Group tools by scope
- Mark system tools

#### 6.3 Scope Checkbox Component

**Component**: `<uai-scope-checkbox-list>`

Shows all tool scopes with:

- Checkbox per scope
- Tool count per scope
- Destructive warning if scope contains destructive tools
- Expandable to show tools in scope

#### 6.4 Tool Tag Input Component

**Component**: `<uai-tool-tag-input>`

Tag-style input with autocomplete:

- Shows selected tools as removable tags
- Dropdown with available tools
- Search/filter capability
- Groups by scope in dropdown

#### 6.5 Backend API for Tool Metadata

**New Endpoint** (required for UI):

```
GET /umbraco/ai/management/api/v1/tools
```

**Response**:

```json
{
    "tools": [
        {
            "id": "fetch_webpage",
            "name": "Fetch Web Page",
            "description": "Fetches content from a web page",
            "scope": "Web",
            "isDestructive": false,
            "isSystem": false,
            "tags": ["web", "http"]
        },
        {
            "id": "content.update",
            "name": "Update Content",
            "description": "Updates content properties",
            "scope": "Content",
            "isDestructive": true,
            "isSystem": false,
            "tags": ["content", "write"]
        }
    ],
    "scopes": [
        {
            "name": "Search",
            "toolCount": 4,
            "hasDestructiveTools": false
        },
        {
            "name": "Content",
            "toolCount": 7,
            "hasDestructiveTools": true
        }
    ]
}
```

**Controller**: `GetToolsController.cs`

```csharp
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class GetToolsController : ControllerBase
{
    private readonly AIToolCollection _toolCollection;

    [HttpGet]
    [ProducesResponseType(typeof(ToolMetadataResponse), StatusCodes.Status200OK)]
    public IActionResult GetTools()
    {
        var tools = _toolCollection.Select(t => new ToolMetadata
        {
            Id = t.Id,
            Name = t.Name,
            Description = t.Description,
            Scope = t.ScopeId,
            IsDestructive = t.IsDestructive,
            IsSystem = t is IAISystemTool,
            Tags = t.Tags.ToArray()
        });

        var scopes = _toolCollection
            .GroupBy(t => t.ScopeId)
            .Select(g => new ScopeMetadata
            {
                Name = g.Key,
                ToolCount = g.Count(),
                HasDestructiveTools = g.Any(t => t.IsDestructive)
            });

        return Ok(new { tools, scopes });
    }
}
```

#### 6.6 Validation & Feedback

**Client-Side Validation**:

- Warn if no tools are enabled (only system tools will be available)
- Show preview of effective tools when selection changes
- Highlight conflicts (tool ID selected + scope containing same tool)

**Server-Side Validation**:

- Accept empty lists (valid - only system tools)
- Validate tool IDs exist in registry (return error for unknown tools)
- Validate scope names exist

**User Feedback**:

- Success toast: "Agent tool permissions updated"
- Preview before save: "This agent will have access to X tools"
- Warning for restrictive configs: "⚠️ Only system tools enabled. Agent capabilities will be limited."

### 7. Backward Compatibility

**Breaking Changes**: None - new properties are additive and optional

**Migration Strategy**:

- Existing agents will have NULL for new columns
- Migration script sets default scopes: `["Search","Navigation","Translation","Web"]`
- This preserves reasonable defaults while being more restrictive than "all tools"

**Empty Lists Behavior**:

- Empty `EnabledToolIds` + Empty `EnabledToolScopeIds` = Only system tools (most restrictive)
- NULL values after migration will be replaced with default scopes (balanced)

### 8. Testing Strategy

#### Unit Tests

**File**: `D:\Work\Umbraco\Umbraco.AI\Umbraco.AI.Agent\tests\Umbraco.AI.Agent.Tests.Unit\Agents\AIAgentServiceTests.cs`

Add test methods for new functionality:

- System tools always included
- Tool ID filtering works
- Scope filtering works
- Combination of IDs + scopes
- Deduplication
- Case-insensitive matching
- `IsToolEnabledAsync` returns correct results

**Existing File Updates**:

- `AIAgentFactoryTests.cs` - Verify tool filtering in CreateAgentAsync

#### Integration Tests

**New File**: `D:\Work\Umbraco\Umbraco.AI\Umbraco.AI.Agent\tests\Umbraco.AI.Agent.Tests.Integration\Permissions\ToolPermissionIntegrationTests.cs`

Test scenarios:

- End-to-end agent execution with different tool configurations
- Verify LLM cannot call non-enabled tools
- API endpoints return correct tool lists
- Migration script applies default values correctly

### 9. Documentation Updates

**Files to Update**:

1. `D:\Work\Umbraco\Umbraco.AI\docs\internal\core\umbraco-ai-agents-design.md` - Update Security and Permissions section
2. `D:\Work\Umbraco\Umbraco.AI\CLAUDE.md` - Add notes about agent tool permissions
3. **New File**: `D:\Work\Umbraco\Umbraco.AI\docs\internal\agent\tool-permissions.md` - Detailed permission system documentation

## Phase 2: User Group Permissions (Future Work)

**Deferred to Phase 2** based on user feedback. Considerations:

### Key Questions to Resolve

1. **Automation scenarios**: How do agents run in scheduled tasks, webhooks, or event handlers?
    - Option A: Require API user credentials
    - Option B: Service-level bypass for trusted internal calls
    - Option C: Special "system" user context for automation

2. **Default behavior**: What should empty `AllowedUserGroupAliases` mean?
    - Option A: All authenticated backoffice users (more permissive)
    - Option B: No access / deny by default (more secure)

3. **Integration with tool permissions**:
    - Both layers check independently (user must pass both)
    - OR user groups check happens first (fail-fast)

### Proposed Phase 2 Design (Tentative)

**Add to AIAgent**:

```csharp
public IReadOnlyList<string> AllowedUserGroupAliases { get; set; } = [];
```

**Service changes**:

- Inject `IBackOfficeSecurityAccessor`
- Add user validation in `StreamAgentAsync` before tool resolution
- Return structured error if user not in allowed groups

**Note**: This will be designed in detail when Phase 2 begins, after understanding automation requirements better.

## Implementation Checklist

### Domain Model (Core) - Tool Scopes Infrastructure

- [ ] Create `IAIToolScope` interface (Umbraco.AI.Core/Tools/Scopes/)
- [ ] Create `AIToolScopeAttribute` class for discovery
- [ ] Create `AIToolScopeBase` base class
- [ ] Create `AIToolScopeCollection` and `AIToolScopeCollectionBuilder`
- [ ] Register built-in scopes (content.read, content.write, media.read, media.write, search, navigation, translation, web, entity.read, entity.write)
- [ ] Add `UmbracoBuilderExtensions.AIToolScopes()` extension method
- [ ] **Update IAITool interface**: Change `ScopeId` to `ScopeId`
- [ ] **Update AIToolAttribute**: Change `ScopeId` to `ScopeId`
- [ ] **Update AIToolBase**: Change `ScopeId` property to `ScopeId`
- [ ] **Update existing tool definitions** to use new `ScopeId` with operation-level scope IDs
- [ ] Add scope validation in `AIToolCollectionBuilder` (validate scope IDs exist)

### Domain Model (Core) - Agent Permissions

- [ ] Add `EnabledToolIds` property to `AIAgent.cs`
- [ ] Add `EnabledToolScopeIds` property to `AIAgent.cs`
- [ ] Add `GetEnabledToolIdsAsync` method to `IAIAgentService.cs`
- [ ] Add `IsToolEnabledAsync` method to `IAIAgentService.cs`
- [ ] Implement tool permission methods in `AIAgentService.cs`
- [ ] Inject `AIToolCollection` and `AIToolScopeCollection` into `AIAgentService`

### Persistence Layer

- [ ] Add properties to `AIAgentEntity.cs`
- [ ] Update `UmbracoAIAgentDbContext.cs` with JSON converters
- [ ] Update `AIAgentRepository.cs` mapping (if needed)
- [ ] Create SQL Server migration: `UmbracoAIAgent_AddToolPermissions`
- [ ] Create SQLite migration: `UmbracoAIAgent_AddToolPermissions`
- [ ] Add migration script for default values

### Service Integration

- [ ] Update `AIAgentFactory.cs` constructor (inject `IAIAgentService` if not present)
- [ ] Update `CreateAgentAsync` to call `_agentService.GetEnabledToolIdsAsync()`
- [ ] Filter tools based on enabled tool IDs

### Web API Layer

- [ ] Update `AgentCreateRequestModel.cs`
- [ ] Update `AgentUpdateRequestModel.cs`
- [ ] Update `AgentResponseModel.cs`
- [ ] Update `AgentMapDefinition.cs` mapping
- [ ] Create `GetAgentToolsController.cs` - get enabled tools for agent (optional)
- [ ] Create `GetToolsController.cs` - get all available tools metadata (required for UI)

### Frontend - Tool Metadata via forwardedProps

- [ ] Update `ManifestUaiAgentTool` type to include optional `scope` field in `meta`
- [ ] Add `scope` to existing frontend tool manifests (e.g., `setPropertyValue`)
- [ ] Update `copilot-run.controller.ts` to extract tool metadata from manifests
- [ ] Update `copilot-run.controller.ts` to send metadata via `forwardedProps` in AGUI request
- [ ] Update `RunAgentController` to extract tool metadata from `request.ForwardedProps`
- [ ] Update `AIAgentFactory.CreateAgentAsync()` to receive metadata via `additionalProperties`
- [ ] Add helper method in `AIAgentFactory` to extract tool metadata from `additionalProperties`
- [ ] Update tool filtering logic to validate against metadata

### Frontend UI Components

- [ ] Create `<uai-tool-selector>` element (agent/components/tool-selector/)
- [ ] Create `<uai-scope-checkbox-list>` element
- [ ] Create `<uai-tool-tag-input>` element
- [ ] Add "Tools" workspace view to agent editor
- [ ] Implement tool API client (fetch available tools)
- [ ] Add validation logic for tool selection
- [ ] Add preview pane showing effective tools
- [ ] Style components consistently with Umbraco design system

### Testing

- [ ] Add unit tests to `AIAgentServiceTests.cs` for tool permission methods
- [ ] Update `AIAgentFactoryTests.cs` (verify tool filtering)
- [ ] Create `ToolPermissionIntegrationTests.cs` (end-to-end tests)
- [ ] Test migration script with sample data

### Documentation

- [ ] Update `umbraco-ai-agents-design.md` (Security section)
- [ ] Create `tool-permissions.md` (detailed design doc)
- [ ] Update `CLAUDE.md` (repository overview)
- [ ] Add API documentation / Swagger comments

## Critical Files Reference

| File                                                        | Purpose                | Changes                                                                                                |
| ----------------------------------------------------------- | ---------------------- | ------------------------------------------------------------------------------------------------------ |
| `Umbraco.AI.Agent.Core/Agents/AIAgent.cs`                   | Domain model           | Add `EnabledToolIds`, `EnabledToolScopeIds`                                                            |
| `Umbraco.AI.Agent.Core/Agents/IAIAgentService.cs`           | Service interface      | Add `GetEnabledToolIdsAsync`, `IsToolEnabledAsync` methods                                             |
| `Umbraco.AI.Agent.Core/Agents/AIAgentService.cs`            | Service implementation | Implement tool permission methods, inject `AIToolCollection`                                           |
| `Umbraco.AI.Agent.Core/Chat/AIAgentFactory.cs`              | Agent creation         | Call `_agentService.GetEnabledToolIdsAsync()`, filter tools using metadata from `additionalProperties` |
| `Umbraco.AI.Agent.Persistence/Agents/AIAgentEntity.cs`      | EF entity              | Add JSON columns                                                                                       |
| `Umbraco.AI.Agent.Persistence/UmbracoAIAgentDbContext.cs`   | EF configuration       | Add JSON converters                                                                                    |
| `Umbraco.AI.Agent.Web/.../RunAgentController.cs`            | Agent execution        | Extract tool metadata from `forwardedProps`, pass to factory                                           |
| `Umbraco.AI.Agent.Web/...Models/AgentCreateRequestModel.cs` | API model              | Add properties                                                                                         |
| `Umbraco.AI.Agent.Web/...Models/AgentUpdateRequestModel.cs` | API model              | Add properties                                                                                         |
| `Umbraco.AI.Agent.Web/...Models/AgentResponseModel.cs`      | API model              | Add properties                                                                                         |
| `Umbraco.AI.Agent.Web/Mapping/AgentMapDefinition.cs`        | Mapping config         | Map new properties                                                                                     |
| `Umbraco.AI.Agent.Copilot/.../copilot-run.controller.ts`    | Frontend controller    | Extract tool metadata, send via `forwardedProps`                                                       |

## Verification

### How to Test End-to-End

1. **Create Test Agent**:

    ```http
    POST /umbraco/ai/management/api/v1/agents
    {
      "alias": "test-agent",
      "name": "Test Agent",
      "enabledToolIds": ["fetch_webpage"],
      "enabledToolScopeIds": ["Search"]
    }
    ```

2. **Run Agent**:

    ```http
    POST /umbraco/ai/management/api/v1/agents/test-agent/run
    {
      "messages": [{"role": "user", "content": "Search for X"}]
    }
    ```

3. **Verify Tool Resolution**:

    ```http
    GET /umbraco/ai/management/api/v1/agents/test-agent/enabled-tools
    ```

    Should return: system tools + "fetch_webpage" + tools in "Search" scope

4. **Test LLM Cannot Call Disabled Tools**:
    - Ask agent to perform action requiring disabled tool
    - Should fail with "tool not found" or similar error

5. **Run Unit Tests**:
    ```bash
    dotnet test Umbraco.AI.Agent.Tests.Unit
    dotnet test Umbraco.AI.Agent.Tests.Integration
    ```

## Design Rationale

### Why Tool Permissions First?

- Simpler to implement (no user context complexity)
- Immediate value for limiting agent capabilities
- No automation/API user concerns to resolve
- Can be tested independently

### Why Scopes + IDs?

- **Scopes**: Convenience for bulk enablement (e.g., "enable all Search tools")
- **IDs**: Fine-grained control for specific tools
- **Both**: Maximum flexibility without introducing ToolSets entity

### Why Backend-Only Validation?

- Simpler for V1
- Frontend would need to duplicate permission logic
- Backend is authoritative anyway
- Frontend HITL approval already provides UI for destructive tools

### Why forwardedProps for Tool Metadata?

- **AGUI Protocol Compliant**: `forwardedProps` is an official AGUI field designed for extensibility
- **No Custom Events**: Simpler than creating custom event handlers
- **Standard Tool Format**: AGUI tools remain unchanged (`{ name, description, parameters }`)
- **Defense in Depth**: Backend validates all tools regardless of frontend
- **Single Request**: Metadata travels with run request, no additional round trips

### Why NOT Tool-Level Permissions in V1?

- Agent-level control is sufficient for most cases
- Tool-level permissions add complexity (two governance layers)
- Unclear if needed based on current requirements
- Can be added later without breaking changes if justified

## Extension Points for Future

### Phase 2: User Group Permissions

- Add `AllowedUserGroupAliases` property
- Inject `IBackOfficeSecurityAccessor`
- Add validation before tool resolution

### Phase 3: ToolSets (If Needed)

- Add `AIToolSet` entity
- Add `EnabledToolSetAliases` to agent
- Tool resolution includes tools from sets
- Optional: ToolSets can have their own user group restrictions

### Other Future Enhancements

- Rate limiting per tool
- Audit logging for tool calls
- Permission contributors (extension point)
- Time-based access restrictions
- Custom permission logic via plugins
