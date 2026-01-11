# AI Request Interaction Flow

This document describes the complete interaction flow for AI requests in the Umbraco.Ai system, from frontend context detection through to LLM response.

## Architecture Overview

The system consists of two parallel pipelines that converge:

1. **Frontend Context Detection Pipeline** (TypeScript/Lit)
2. **Backend Request Processing Pipeline** (C#/.NET)

## Complete Flow Diagram

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                           FRONTEND (TypeScript/Lit)                             │
├─────────────────────────────────────────────────────────────────────────────────┤
│                                                                                 │
│  ┌──────────────────────────┐                                                   │
│  │ UAI_WORKSPACE_REGISTRY   │  Global context at backoffice root                │
│  │        _CONTEXT          │  Tracks all active workspaces                     │
│  └────────────┬─────────────┘                                                   │
│               │ watches                                                         │
│               ▼                                                                 │
│  ┌──────────────────────────┐     ┌─────────────────────────────┐               │
│  │  UaiEntityAdapterContext │── ─▶│    uaiEntityAdapter         │               │
│  │  (entity-adapter.context)│     │    (extension registry)     │               │
│  └────────────┬─────────────┘     │  - canHandle()              │               │
│               │                   │  - extractEntityContext()   │               │
│               │                   │  - serializeForLlm()        │               │
│               │                   │  - applyPropertyChange()    │               │
│               │                   └─────────────────────────────┘               │
│               │                                                                 │
│               ▼                                                                 │
│  ┌────────────────────────────────────────────────────────────┐                 │
│  │                  UaiSerializedEntity                       │                 │
│  │  { entityType, unique, name, contentType, properties[] }   │                 │
│  └──────────────────────────┬─────────────────────────────────┘                 │
│                             │                                                   │
│                             ▼                                                   │
│  ┌────────────────────────────────────────────────────────────┐                 │
│  │              UaiRequestContextItem[]                       │                 │
│  │  [ { description: "Editing: Page", value: entity } ]       │                 │
│  └──────────────────────────┬─────────────────────────────────┘                 │
│                             │                                                   │
└─────────────────────────────┼───────────────────────────────────────────────────┘
                              │
                   HTTP POST /execute (with context[])
                              │
                              ▼
┌────────────────────────────────────────────────────────────────────────────────┐
│                            BACKEND (C#/.NET)                                   │
├────────────────────────────────────────────────────────────────────────────────┤
│                                                                                │
│  ┌─────────────────────────────────────────────────────────────────────────┐   │
│  │                    PHASE 1: REQUEST CONTEXT PROCESSING                  │   │
│  ├─────────────────────────────────────────────────────────────────────────┤   │
│  │                                                                         │   │
│  │  AiRequestContextProcessorCollection.Process(contextItems)              │   │
│  │         │                                                               │   │
│  │         ├────▶ SerializedEntityProcessor                                │   │
│  │         │        • Deserializes UaiSerializedEntity                     │   │
│  │         │        • Extracts: EntityId, EntityType, ParentEntityId       │   │
│  │         │        • Builds Variables: $Document_Title, $Document_Body    │   │
│  │         │        • Adds SystemMessagePart: "Editing Document: X..."     │   │
│  │         │                                                               │   │
│  │         └────▶ DefaultSystemMessageProcessor (fallback)                 │   │
│  │                  • Adds description to SystemMessageParts               │   │
│  │                                                                         │   │
│  │         ┌──────────────────────────────────────────────────────────┐    │   │
│  │         │              AiRequestContext (output)                   │    │   │
│  │         │  • Items[]              - Original context items         │    │   │
│  │         │  • Variables{}          - Template variables             │    │   │
│  │         │  • SystemMessageParts[] - System prompt additions        │    │   │
│  │         │  • Data{}               - Typed data (EntityId, etc.)    │    │   │
│  │         └──────────────────────────────────────────────────────────┘    │   │
│  │                                                                         │   │
│  └─────────────────────────────────────────────────────────────────────────┘   │
│                              │                                                 │
│                              ▼                                                 │
│  ┌─────────────────────────────────────────────────────────────────────────┐   │
│  │                    PHASE 2: TEMPLATE PROCESSING                         │   │
│  ├─────────────────────────────────────────────────────────────────────────┤   │
│  │                                                                         │   │
│  │  IAiPromptTemplateService.ProcessTemplate(instructions, variables)      │   │
│  │                                                                         │   │
│  │    Input:  "Write description for {$Document_Title}"                    │   │
│  │    Variables: { "$Document_Title": "My Article" }                       │   │
│  │    Output: "Write description for My Article"                           │   │
│  │                                                                         │   │
│  └─────────────────────────────────────────────────────────────────────────┘   │
│                              │                                                 │
│                              ▼                                                 │
│  ┌─────────────────────────────────────────────────────────────────────────┐   │
│  │                    PHASE 3: CONTEXT RESOLUTION                          │   │
│  ├─────────────────────────────────────────────────────────────────────────┤   │
│  │                                                                         │   │
│  │  ChatOptions.AdditionalProperties = {                                   │   │
│  │    ProfileIdKey: profile.Id,     // For ProfileContextResolver          │   │
│  │    PromptIdKey: prompt.Id,       // For PromptContextResolver           │   │
│  │    ContentId: entity.Id,         // For ContentContextResolver          │   │
│  │    ParentEntityId: parent.Id,    // For new entities                    │   │
│  │    AgentIdKey: agent.Id          // For AgentContextResolver            │   │
│  │  }                                                                      │   │
│  │                                                                         │   │
│  │  IAiContextResolutionService.ResolveContextAsync(additionalProperties)  │   │
│  │         │                                                               │   │
│  │         ├────▶ ProfileContextResolver                                   │   │
│  │         │        • Reads ProfileIdKey → loads profile contexts          │   │
│  │         │                                                               │   │
│  │         ├────▶ ContentContextResolver                                   │   │
│  │         │        • Reads ContentId → walks tree for Context Picker      │   │
│  │         │                                                               │   │
│  │         ├────▶ PromptContextResolver                                    │   │
│  │         │        • Reads PromptIdKey → loads prompt contexts            │   │
│  │         │                                                               │   │
│  │         └────▶ AgentContextResolver                                     │   │
│  │                  • Reads AgentIdKey → loads agent contexts              │   │
│  │                                                                         │   │
│  │         ┌──────────────────────────────────────────────────────────┐    │   │
│  │         │              AiResolvedContext (output)                  │    │   │
│  │         │  • Sources[]           - Which resolvers contributed     │    │   │
│  │         │  • InjectedResources[] - "Always" mode (inject now)      │    │   │
│  │         │  • OnDemandResources[] - "OnDemand" (list for LLM)       │    │   │
│  │         └──────────────────────────────────────────────────────────┘    │   │
│  │                                                                         │   │
│  └─────────────────────────────────────────────────────────────────────────┘   │
│                              │                                                 │
│                              ▼                                                 │
│  ┌─────────────────────────────────────────────────────────────────────────┐   │
│  │                    PHASE 4: MESSAGE ASSEMBLY                            │   │
│  ├─────────────────────────────────────────────────────────────────────────┤   │
│  │                                                                         │   │
│  │  ContextInjectingChatClient (middleware)                                │   │
│  │                                                                         │   │
│  │  SYSTEM MESSAGE = Base instructions                                     │   │
│  │                  + RequestContext.SystemMessageParts[]                  │   │
│  │                  + Formatted InjectedResources                          │   │
│  │                  + OnDemand resources list (available via tools)        │   │
│  │                                                                         │   │
│  │  USER MESSAGE   = Processed prompt template                             │   │
│  │                                                                         │   │
│  └─────────────────────────────────────────────────────────────────────────┘   │
│                              │                                                 │
│                              ▼                                                 │
│                         LLM Request                                            │
│                              │                                                 │
│                              ▼                                                 │
│                        LLM Response                                            │
│                              │                                                 │
│                              ▼                                                 │
│  ┌─────────────────────────────────────────────────────────────────────────┐   │
│  │                    PHASE 5: RESPONSE PROCESSING                         │   │
│  ├─────────────────────────────────────────────────────────────────────────┤   │
│  │                                                                         │   │
│  │  AiPromptExecutionResult {                                              │   │
│  │    Content: string,              // LLM response text                   │   │
│  │    Usage: UsageDetails,          // Token counts                        │   │
│  │    PropertyChanges: [            // Optional structured changes         │   │
│  │      { Alias, Value, Culture?, Segment? }                               │   │
│  │    ]                                                                    │   │
│  │  }                                                                      │   │
│  │                                                                         │   │
│  │                        ↓ Maps to API response                           │   │
│  │                                                                         │   │
│  │  PromptExecutionResponseModel {                                         │   │
│  │    Content: string,                                                     │   │
│  │    Usage: UsageModel,                                                   │   │
│  │    PropertyChanges: PropertyChangeModel[]                               │   │
│  │  }                                                                      │   │
│  │                                                                         │   │
│  └─────────────────────────────────────────────────────────────────────────┘   │
│                              │                                                 │
└──────────────────────────────┼─────────────────────────────────────────────────┘
                               │
                    HTTP Response (with PropertyChanges[])
                               │
                               ▼
┌────────────────────────────────────────────────────────────────────────────────┐
│                    FRONTEND: APPLY PROPERTY CHANGES                            │
├────────────────────────────────────────────────────────────────────────────────┤
│                                                                                │
│  For each PropertyChangeModel in response:                                     │
│                                                                                │
│    UaiPropertyChange {                                                         │
│      alias: string,           // Property to update                            │
│      value: unknown,          // New value                                     │
│      culture?: string,        // For variant content                           │
│      segment?: string         // For segmented content                         │
│    }                                                                           │
│                                                                                │
│                        ↓                                                       │
│                                                                                │
│    adapter.applyPropertyChange(workspaceContext, change)                       │
│                                                                                │
│                        ↓                                                       │
│                                                                                │
│    UaiPropertyChangeResult {                                                   │
│      success: boolean,        // Whether change was applied                    │
│      error?: string           // Error message if failed                       │
│    }                                                                           │
│                                                                                │
│  Changes are STAGED in workspace - user must SAVE to persist                   │
│                                                                                │
└────────────────────────────────────────────────────────────────────────────────┘
```

## Key Component Responsibilities

| Component | Phase | Responsibility |
|-----------|-------|----------------|
| `UAI_WORKSPACE_REGISTRY_CONTEXT` | Frontend | Tracks all active workspaces globally |
| `UaiEntityAdapterContext` | Frontend | Matches adapters, serializes entities |
| `uaiEntityAdapter` | Frontend | Plugin system for entity-specific serialization |
| `IAiRequestContextProcessor` | Backend Phase 1 | Extracts variables, system parts, typed data |
| `IAiPromptTemplateService` | Backend Phase 2 | Variable substitution in prompts |
| `IAiContextResolver` | Backend Phase 3 | Resolves knowledge base resources |
| `ContextInjectingChatClient` | Backend Phase 4 | Assembles final LLM request |
| `AiPromptExecutionResult` | Backend Phase 5 | Packages LLM response with property changes |
| `uaiEntityAdapter.applyPropertyChange` | Frontend Response | Stages property changes in workspace |

## Data Transformation Summary

### Request Path
```
Frontend Entity → UaiSerializedEntity → UaiRequestContextItem[]
                                                │
                                    ┌───────────┴───────────┐
                                    ▼                       ▼
                           AiRequestContext         ChatOptions.AdditionalProperties
                           (Variables, System)      (Keys for resolvers)
                                    │                       │
                                    ▼                       ▼
                           Template Processing      Context Resolution
                                    │                       │
                                    └───────────┬───────────┘
                                                ▼
                                        Final LLM Message
```

### Response Path
```
LLM Response
     │
     ▼
AiPromptExecutionResult { Content, Usage, PropertyChanges[] }
     │
     ▼ (API mapping)
PromptExecutionResponseModel { Content, Usage, PropertyChanges[] }
     │
     ▼ (HTTP Response)
Frontend receives PropertyChangeModel[]
     │
     ▼ (for each change)
adapter.applyPropertyChange(workspaceContext, UaiPropertyChange)
     │
     ▼
UaiPropertyChangeResult { success, error? }
     │
     ▼
Changes STAGED in workspace (user saves to persist)
```

## Phase Details

### Phase 1: Request Context Processing

The `AiRequestContextProcessorCollection` processes incoming context items from the frontend:

**SerializedEntityProcessor** (`Umbraco.Ai.Core/RequestContext/Processors/SerializedEntityProcessor.cs`):
- Deserializes `UaiSerializedEntity` from JSON
- Extracts `EntityId`, `EntityType`, `ParentEntityId`
- Builds template variables (e.g., `$Document_Title`, `$Document_Body`)
- Generates system message parts describing the entity being edited

**DefaultSystemMessageProcessor** (`Umbraco.Ai.Core/RequestContext/Processors/DefaultSystemMessageProcessor.cs`):
- Fallback processor for unhandled items
- Adds item descriptions to system message parts

**Output: `AiRequestContext`**
```csharp
{
    Items,              // Original context items
    Variables,          // Template variables for substitution
    SystemMessageParts, // Parts to inject into system prompt
    Data                // Typed data bag (EntityId, SerializedEntity, etc.)
}
```

### Phase 2: Template Processing

The `IAiPromptTemplateService` performs variable substitution:

```
Input:  "Write description for {$Document_Title}"
Variables: { "$Document_Title": "My Article" }
Output: "Write description for My Article"
```

### Phase 3: Context Resolution

Multiple `IAiContextResolver` implementations check `ChatOptions.AdditionalProperties` to resolve knowledge base resources:

| Resolver | Key | Source |
|----------|-----|--------|
| `ProfileContextResolver` | `ProfileIdKey` | Profile's configured contexts |
| `ContentContextResolver` | `ContentId` / `ParentEntityId` | Content tree Context Picker inheritance |
| `PromptContextResolver` | `PromptIdKey` | Prompt's configured contexts |
| `AgentContextResolver` | `AgentIdKey` | Agent's configured contexts |

**Output: `AiResolvedContext`**
```csharp
{
    Sources,            // Which resolvers contributed
    InjectedResources,  // "Always" mode - inject immediately
    OnDemandResources   // "OnDemand" mode - list for LLM to request
}
```

### Phase 4: Message Assembly

The `ContextInjectingChatClient` middleware assembles the final LLM request:

**System Message**:
- Base instructions
- `RequestContext.SystemMessageParts[]`
- Formatted `InjectedResources`
- List of available `OnDemandResources`

**User Message**:
- Processed prompt template (from Phase 2)

### Phase 5: Response Processing & Property Changes

The LLM response is packaged into `AiPromptExecutionResult` which may include property changes:

**Core Models** (`Umbraco.Ai.Core/EntityAdapter/`):

```csharp
// Request to change a property value
public class AiPropertyChange
{
    public required string Alias { get; init; }  // Property alias
    public object? Value { get; init; }          // New value
    public string? Culture { get; init; }        // For variant content (null = invariant)
    public string? Segment { get; init; }        // For segmented content (null = no segment)
}

// Result of applying a property change
public class AiPropertyChangeResult
{
    public required bool Success { get; init; }  // Whether change was applied
    public string? Error { get; init; }          // Error message if failed
}
```

**API Models** (`Umbraco.Ai.Prompt.Web/Api/Management/Prompt/Models/`):

```csharp
// Response from prompt execution
public class PromptExecutionResponseModel
{
    public required string Content { get; init; }              // LLM response text
    public UsageModel? Usage { get; init; }                    // Token counts
    public IReadOnlyList<PropertyChangeModel>? PropertyChanges { get; init; }
}

// Property change in API response
public class PropertyChangeModel
{
    public required string Alias { get; init; }
    public object? Value { get; init; }
    public string? Culture { get; init; }
    public string? Segment { get; init; }
}
```

**Frontend Types** (`Umbraco.Ai/Client/src/entity-adapter/types.ts`):

```typescript
// Request to change a property
interface UaiPropertyChange {
    alias: string;      // Property alias
    value: unknown;     // New value
    culture?: string;   // For variant content
    segment?: string;   // For segmented content
}

// Result of property change operation
interface UaiPropertyChangeResult {
    success: boolean;   // Whether change was applied
    error?: string;     // Error message if failed
}
```

**Important**: Changes are **staged** in the workspace, not persisted immediately. The user must **save** the entity to persist the changes. This ensures users maintain control over AI-generated modifications.

## Key Architecture Seams

1. **Request Context Processing** - Transforms frontend context into template variables and system message parts
2. **Context Resolution** - Uses `AdditionalProperties` keys to fetch knowledge base resources from multiple sources

## Key Files Reference

| Component | File |
|-----------|------|
| Workspace Registry | `Umbraco.Ai/Client/src/workspace-registry/workspace-registry.context.ts` |
| Entity Adapter Context | `Umbraco.Ai/Client/src/entity-adapter/entity-adapter.context.ts` |
| Entity Adapter Types | `Umbraco.Ai/Client/src/entity-adapter/types.ts` |
| Request Context Item | `Umbraco.Ai/Client/src/request-context/types.ts` |
| Processor Collection | `Umbraco.Ai.Core/RequestContext/AiRequestContextProcessorCollection.cs` |
| Serialized Entity Processor | `Umbraco.Ai.Core/RequestContext/Processors/SerializedEntityProcessor.cs` |
| Default System Message Processor | `Umbraco.Ai.Core/RequestContext/Processors/DefaultSystemMessageProcessor.cs` |
| Request Context | `Umbraco.Ai.Core/RequestContext/AiRequestContext.cs` |
| Context Resolution Service | `Umbraco.Ai.Core/Contexts/AiContextResolutionService.cs` |
| Context Injecting Client | `Umbraco.Ai.Core/Contexts/Middleware/ContextInjectingChatClient.cs` |
| Profile Context Resolver | `Umbraco.Ai.Core/Contexts/Resolvers/ProfileContextResolver.cs` |
| Content Context Resolver | `Umbraco.Ai.Core/Contexts/Resolvers/ContentContextResolver.cs` |
| Prompt Context Resolver | `Umbraco.Ai.Prompt.Core/Context/PromptContextResolver.cs` |
| Agent Context Resolver | `Umbraco.Ai.Agent.Core/Context/AgentContextResolver.cs` |
| Property Change (Core) | `Umbraco.Ai.Core/EntityAdapter/AiPropertyChange.cs` |
| Property Change Result (Core) | `Umbraco.Ai.Core/EntityAdapter/AiPropertyChangeResult.cs` |
| Property Change Model (API) | `Umbraco.Ai.Prompt.Web/Api/Management/Prompt/Models/PropertyChangeModel.cs` |
| Execution Result (Core) | `Umbraco.Ai.Prompt.Core/Prompts/AiPromptExecutionResult.cs` |
| Execution Response (API) | `Umbraco.Ai.Prompt.Web/Api/Management/Prompt/Models/PromptExecutionResponseModel.cs` |

## Extension Points

### Add Custom Request Processor

```csharp
public class MyCustomProcessor : IAiRequestContextProcessor
{
    public bool CanHandle(AiRequestContextItem item) { ... }
    public void Process(AiRequestContextItem item, AiRequestContext context) { ... }
}

// Register in Composer:
builder.AiRequestContextProcessors().Append<MyCustomProcessor>();
```

### Add Custom Context Resolver

```csharp
public class MyContextResolver : IAiContextResolver
{
    public async Task<AiContextResolverResult> ResolveAsync(
        AiContextResolverRequest request,
        CancellationToken ct) { ... }
}

// Register in Composer:
builder.AiContextResolvers().Append<MyContextResolver>();
```

### Add Entity Adapter (Frontend)

Create a manifest with `type: "uaiEntityAdapter"` and implement `UaiEntityAdapterApi` interface.
