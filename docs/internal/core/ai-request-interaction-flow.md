# AI Request Interaction Flow

This document describes the complete interaction flow for AI requests in the Umbraco.AI system, from frontend context detection through to LLM response.

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
│               │                   │  - applyValueChange()    │               │
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
│  │  AIRequestContextProcessorCollection.Process(contextItems)              │   │
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
│  │         │              AIRequestContext (output)                   │    │   │
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
│  │  IAIPromptTemplateService.ProcessTemplate(instructions, variables)      │   │
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
│  │  IAIContextResolutionService.ResolveContextAsync(additionalProperties)  │   │
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
│  │         │              AIResolvedContext (output)                  │    │   │
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
│  │  AIPromptExecutionResult {                                              │   │
│  │    Content: string,              // LLM response text                   │   │
│  │    Usage: UsageDetails,          // Token counts                        │   │
│  │    ValueChanges: [               // Optional structured changes         │   │
│  │      { Path, Value, Culture?, Segment? }                                │   │
│  │    ]                                                                    │   │
│  │  }                                                                      │   │
│  │                                                                         │   │
│  │                        ↓ Maps to API response                           │   │
│  │                                                                         │   │
│  │  PromptExecutionResponseModel {                                         │   │
│  │    Content: string,                                                     │   │
│  │    Usage: UsageModel,                                                   │   │
│  │    ValueChanges: ValueChangeModel[]                                  │   │
│  │  }                                                                      │   │
│  │                                                                         │   │
│  └─────────────────────────────────────────────────────────────────────────┘   │
│                              │                                                 │
└──────────────────────────────┼─────────────────────────────────────────────────┘
                               │
                    HTTP Response (with ValueChanges[])
                               │
                               ▼
┌────────────────────────────────────────────────────────────────────────────────┐
│                    FRONTEND: APPLY PROPERTY CHANGES                            │
├────────────────────────────────────────────────────────────────────────────────┤
│                                                                                │
│  For each ValueChangeModel in response:                                     │
│                                                                                │
│    UaiValueChange {                                                         │
│      path: string,            // JSON path to value                             │
│      value: unknown,          // New value                                     │
│      culture?: string,        // For variant content                           │
│      segment?: string         // For segmented content                         │
│    }                                                                           │
│                                                                                │
│                        ↓                                                       │
│                                                                                │
│    adapter.applyValueChange(workspaceContext, change)                          │
│                                                                                │
│                        ↓                                                       │
│                                                                                │
│    UaiValueChangeResult {                                                   │
│      success: boolean,        // Whether change was applied                    │
│      error?: string           // Error message if failed                       │
│    }                                                                           │
│                                                                                │
│  Changes are STAGED in workspace - user must SAVE to persist                   │
│                                                                                │
└────────────────────────────────────────────────────────────────────────────────┘
```

## Key Component Responsibilities

| Component                              | Phase             | Responsibility                                  |
| -------------------------------------- | ----------------- | ----------------------------------------------- |
| `UAI_WORKSPACE_REGISTRY_CONTEXT`       | Frontend          | Tracks all active workspaces globally           |
| `UaiEntityAdapterContext`              | Frontend          | Matches adapters, serializes entities           |
| `uaiEntityAdapter`                     | Frontend          | Plugin system for entity-specific serialization |
| `IAIRequestContextProcessor`           | Backend Phase 1   | Extracts variables, system parts, typed data    |
| `IAIPromptTemplateService`             | Backend Phase 2   | Variable substitution in prompts                |
| `IAIContextResolver`                   | Backend Phase 3   | Resolves knowledge base resources               |
| `ContextInjectingChatClient`           | Backend Phase 4   | Assembles final LLM request                     |
| `AIPromptExecutionResult`              | Backend Phase 5   | Packages LLM response with property changes     |
| `uaiEntityAdapter.applyValueChange` | Frontend Response | Stages property changes in workspace            |

## Data Transformation Summary

### Request Path

```
Frontend Entity → UaiSerializedEntity → UaiRequestContextItem[]
                                                │
                                    ┌───────────┴───────────┐
                                    ▼                       ▼
                           AIRequestContext         ChatOptions.AdditionalProperties
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
AIPromptExecutionResult { Content, Usage, ValueChanges[] }
     │
     ▼ (API mapping)
PromptExecutionResponseModel { Content, Usage, ValueChanges[] }
     │
     ▼ (HTTP Response)
Frontend receives ValueChangeModel[]
     │
     ▼ (for each change)
adapter.applyValueChange(workspaceContext, UaiValueChange)
     │
     ▼
UaiValueChangeResult { success, error? }
     │
     ▼
Changes STAGED in workspace (user saves to persist)
```

## Phase Details

### Phase 1: Request Context Processing

The `AIRequestContextProcessorCollection` processes incoming context items from the frontend:

**SerializedEntityProcessor** (`Umbraco.AI.Core/RequestContext/Processors/SerializedEntityProcessor.cs`):

- Deserializes `UaiSerializedEntity` from JSON
- Extracts `EntityId`, `EntityType`, `ParentEntityId`
- Builds template variables (e.g., `$Document_Title`, `$Document_Body`)
- Generates system message parts describing the entity being edited

**DefaultSystemMessageProcessor** (`Umbraco.AI.Core/RequestContext/Processors/DefaultSystemMessageProcessor.cs`):

- Fallback processor for unhandled items
- Adds item descriptions to system message parts

**Output: `AIRequestContext`**

```csharp
{
    Items,              // Original context items
    Variables,          // Template variables for substitution
    SystemMessageParts, // Parts to inject into system prompt
    Data                // Typed data bag (EntityId, SerializedEntity, etc.)
}
```

### Phase 2: Template Processing

The `IAIPromptTemplateService` performs variable substitution:

```
Input:  "Write description for {$Document_Title}"
Variables: { "$Document_Title": "My Article" }
Output: "Write description for My Article"
```

### Phase 3: Context Resolution

Multiple `IAIContextResolver` implementations check `ChatOptions.AdditionalProperties` to resolve knowledge base resources:

| Resolver                 | Key                            | Source                                  |
| ------------------------ | ------------------------------ | --------------------------------------- |
| `ProfileContextResolver` | `ProfileIdKey`                 | Profile's configured contexts           |
| `ContentContextResolver` | `ContentId` / `ParentEntityId` | Content tree Context Picker inheritance |
| `PromptContextResolver`  | `PromptIdKey`                  | Prompt's configured contexts            |
| `AgentContextResolver`   | `AgentIdKey`                   | Agent's configured contexts             |

**Output: `AIResolvedContext`**

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

The LLM response is packaged into `AIPromptExecutionResult` which may include property changes:

**Core Models** (`Umbraco.AI.Core/EntityAdapter/`):

```csharp
// Request to change a property value
public class AIValueChange
{
    public required string Alias { get; init; }  // Property alias
    public object? Value { get; init; }          // New value
    public string? Culture { get; init; }        // For variant content (null = invariant)
    public string? Segment { get; init; }        // For segmented content (null = no segment)
}

// Result of applying a property change
public class AIValueChangeResult
{
    public required bool Success { get; init; }  // Whether change was applied
    public string? Error { get; init; }          // Error message if failed
}
```

**API Models** (`Umbraco.AI.Prompt.Web/Api/Management/Prompt/Models/`):

```csharp
// Response from prompt execution
public class PromptExecutionResponseModel
{
    public required string Content { get; init; }              // LLM response text
    public UsageModel? Usage { get; init; }                    // Token counts
    public IReadOnlyList<ValueChangeModel>? ValueChanges { get; init; }
}

// Property change in API response
public class ValueChangeModel
{
    public required string Alias { get; init; }
    public object? Value { get; init; }
    public string? Culture { get; init; }
    public string? Segment { get; init; }
}
```

**Frontend Types** (`Umbraco.AI/Client/src/entity-adapter/types.ts`):

```typescript
// Request to change a value via JSON path
interface UaiValueChange {
    path: string; // JSON path to the value (e.g., "title", "price.amount")
    value: unknown; // New value
    culture?: string; // For variant content
    segment?: string; // For segmented content
}

// Result of property change operation
interface UaiValueChangeResult {
    success: boolean; // Whether change was applied
    error?: string; // Error message if failed
}
```

**Important**: Changes are **staged** in the workspace, not persisted immediately. The user must **save** the entity to persist the changes. This ensures users maintain control over AI-generated modifications.

## Key Architecture Seams

1. **Request Context Processing** - Transforms frontend context into template variables and system message parts
2. **Context Resolution** - Uses `AdditionalProperties` keys to fetch knowledge base resources from multiple sources

## Key Files Reference

| Component                        | File                                                                                 |
| -------------------------------- | ------------------------------------------------------------------------------------ |
| Workspace Registry               | `Umbraco.AI/Client/src/workspace-registry/workspace-registry.context.ts`             |
| Entity Adapter Context           | `Umbraco.AI/Client/src/entity-adapter/entity-adapter.context.ts`                     |
| Entity Adapter Types             | `Umbraco.AI/Client/src/entity-adapter/types.ts`                                      |
| Request Context Item             | `Umbraco.AI/Client/src/request-context/types.ts`                                     |
| Processor Collection             | `Umbraco.AI.Core/RequestContext/AIRequestContextProcessorCollection.cs`              |
| Serialized Entity Processor      | `Umbraco.AI.Core/RequestContext/Processors/SerializedEntityProcessor.cs`             |
| Default System Message Processor | `Umbraco.AI.Core/RequestContext/Processors/DefaultSystemMessageProcessor.cs`         |
| Request Context                  | `Umbraco.AI.Core/RequestContext/AIRequestContext.cs`                                 |
| Context Resolution Service       | `Umbraco.AI.Core/Contexts/AIContextResolutionService.cs`                             |
| Context Injecting Client         | `Umbraco.AI.Core/Contexts/Middleware/ContextInjectingChatClient.cs`                  |
| Profile Context Resolver         | `Umbraco.AI.Core/Contexts/Resolvers/ProfileContextResolver.cs`                       |
| Content Context Resolver         | `Umbraco.AI.Core/Contexts/Resolvers/ContentContextResolver.cs`                       |
| Prompt Context Resolver          | `Umbraco.AI.Prompt.Core/Context/PromptContextResolver.cs`                            |
| Agent Context Resolver           | `Umbraco.AI.Agent.Core/Context/AgentContextResolver.cs`                              |
| Property Change (Core)           | `Umbraco.AI.Core/EntityAdapter/AIValueChange.cs`                                  |
| Property Change Result (Core)    | `Umbraco.AI.Core/EntityAdapter/AIValueChangeResult.cs`                            |
| Property Change Model (API)      | `Umbraco.AI.Prompt.Web/Api/Management/Prompt/Models/ValueChangeModel.cs`          |
| Execution Result (Core)          | `Umbraco.AI.Prompt.Core/Prompts/AIPromptExecutionResult.cs`                          |
| Execution Response (API)         | `Umbraco.AI.Prompt.Web/Api/Management/Prompt/Models/PromptExecutionResponseModel.cs` |

## Extension Points

### Add Custom Request Processor

```csharp
public class MyCustomProcessor : IAIRequestContextProcessor
{
    public bool CanHandle(AIRequestContextItem item) { ... }
    public void Process(AIRequestContextItem item, AIRequestContext context) { ... }
}

// Register in Composer:
builder.AIRequestContextProcessors().Append<MyCustomProcessor>();
```

### Add Custom Context Resolver

```csharp
public class MyContextResolver : IAIContextResolver
{
    public async Task<AIContextResolverResult> ResolveAsync(
        AIContextResolverRequest request,
        CancellationToken ct) { ... }
}

// Register in Composer:
builder.AIContextResolvers().Append<MyContextResolver>();
```

### Add Entity Adapter (Frontend)

Create a manifest with `type: "uaiEntityAdapter"` and implement `UaiEntityAdapterApi` interface.
