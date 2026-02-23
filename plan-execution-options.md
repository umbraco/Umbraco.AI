# Test Execution Options Plan

## Problem

When running tests, the `PromptTestFeature` calls `IAIPromptService.ExecutePromptAsync` which triggers scope validation. The `AIPromptScopeValidator` calls `IEntityService.Get(entityId, objectType)` requiring a **real CMS content item** in the database to resolve `ContentTypeAlias` and `PropertyEditorUiAlias`. During test execution, these entities may not exist, causing scope validation to fail.

Additionally, both `PromptTestFeature` and `AgentTestFeature` have TODOs for threading `profileIdOverride` and `contextIdsOverride` into their respective service calls.

### Why bypassing scope validation is safe

- Scope validation is an **access control** concern (ensuring prompts only run where configured)
- Tests validate **output quality**, not access rules — the test author explicitly chose the prompt
- The options parameter is only accessible from server-side C# code, not the Management API
- API controllers will always use the default (validation enabled)

## Approach: Execution Options Pattern

Add options classes that control execution behavior, following standard .NET options pattern.

## Implementation Steps

### 1. Create `AIPromptExecutionOptions`

**File:** `Umbraco.AI.Prompt/src/Umbraco.AI.Prompt.Core/Prompts/AIPromptExecutionOptions.cs`

```csharp
namespace Umbraco.AI.Prompt.Core.Prompts;

/// <summary>
/// Options for controlling prompt execution behavior.
/// </summary>
public class AIPromptExecutionOptions
{
    /// <summary>
    /// Whether to validate scope rules before execution. Default is true.
    /// Set to false for test execution where scope validation is not relevant.
    /// </summary>
    public bool ValidateScope { get; init; } = true;

    /// <summary>
    /// Optional profile ID to override the prompt's configured profile.
    /// Used for cross-model comparison testing.
    /// </summary>
    public Guid? ProfileIdOverride { get; init; }

    /// <summary>
    /// Optional context IDs to override the prompt's configured contexts.
    /// Used for context comparison testing.
    /// </summary>
    public IReadOnlyList<Guid>? ContextIdsOverride { get; init; }
}
```

### 2. Add overload to `IAIPromptService`

**File:** `Umbraco.AI.Prompt/src/Umbraco.AI.Prompt.Core/Prompts/IAIPromptService.cs`

Add new method:

```csharp
/// <summary>
/// Executes a prompt with execution options controlling validation and overrides.
/// </summary>
Task<AIPromptExecutionResult> ExecutePromptAsync(
    Guid promptId,
    AIPromptExecutionRequest request,
    AIPromptExecutionOptions options,
    CancellationToken cancellationToken = default);
```

### 3. Update `AIPromptService` implementation

**File:** `Umbraco.AI.Prompt/src/Umbraco.AI.Prompt.Core/Prompts/AIPromptService.cs`

- Existing `ExecutePromptAsync(promptId, request, ct)` delegates to the new overload with `options: new AIPromptExecutionOptions()`
- New overload contains the full implementation:
  - If `options.ValidateScope` is false, skip scope validation entirely
  - If `options.ProfileIdOverride` is set, use it instead of `prompt.ProfileId` when calling the chat service
  - If `options.ContextIdsOverride` is set, use those context IDs instead of the prompt's defaults

### 4. Create `AIAgentExecutionOptions`

**File:** `Umbraco.AI.Agent/src/Umbraco.AI.Agent.Core/Agents/AIAgentExecutionOptions.cs`

```csharp
namespace Umbraco.AI.Agent.Core.Agents;

/// <summary>
/// Options for controlling agent execution behavior.
/// </summary>
public class AIAgentExecutionOptions
{
    /// <summary>
    /// Optional profile ID to override the agent's configured profile.
    /// Used for cross-model comparison testing.
    /// </summary>
    public Guid? ProfileIdOverride { get; init; }

    /// <summary>
    /// Optional context IDs to override the agent's configured contexts.
    /// Used for context comparison testing.
    /// </summary>
    public IReadOnlyList<Guid>? ContextIdsOverride { get; init; }
}
```

No `ValidateScope` needed — agents don't have scope validation.

### 5. Add overload to `IAIAgentService`

**File:** `Umbraco.AI.Agent/src/Umbraco.AI.Agent.Core/Agents/IAIAgentService.cs`

Add new method:

```csharp
/// <summary>
/// Streams agent execution with options controlling overrides.
/// </summary>
IAsyncEnumerable<IAGUIEvent> StreamAgentAsync(
    Guid agentId,
    AGUIRunRequest request,
    IEnumerable<AIFrontendTool>? frontendTools,
    AIAgentExecutionOptions options,
    CancellationToken cancellationToken = default);
```

### 6. Update `AIAgentService` implementation

**File:** `Umbraco.AI.Agent/src/Umbraco.AI.Agent.Core/Agents/AIAgentService.cs`

- Existing `StreamAgentAsync(agentId, request, frontendTools, ct)` delegates to the new overload with `options: new AIAgentExecutionOptions()`
- New overload applies profile/context overrides when creating the MAF agent

### 7. Update `PromptTestFeature`

**File:** `Umbraco.AI.Prompt/src/Umbraco.AI.Prompt.Core/Tests/PromptTestFeature.cs`

Replace the TODO and service call:

```csharp
var options = new AIPromptExecutionOptions
{
    ValidateScope = false,
    ProfileIdOverride = profileIdOverride,
    ContextIdsOverride = contextIdsOverride?.ToList()
};

result = await _promptService.ExecutePromptAsync(promptId, request, options, cancellationToken);
```

### 8. Update `AgentTestFeature`

**File:** `Umbraco.AI.Agent/src/Umbraco.AI.Agent.Core/Tests/AgentTestFeature.cs`

Replace the TODO and service call:

```csharp
var options = new AIAgentExecutionOptions
{
    ProfileIdOverride = profileIdOverride,
    ContextIdsOverride = contextIdsOverride?.ToList()
};

await foreach (var evt in _agentService.StreamAgentAsync(
    agentId, request, frontendTools, options, cancellationToken))
```

## Summary of changes

| File | Change |
|------|--------|
| `AIPromptExecutionOptions.cs` | **New** — Options class with `ValidateScope`, `ProfileIdOverride`, `ContextIdsOverride` |
| `IAIPromptService.cs` | Add `ExecutePromptAsync` overload accepting options |
| `AIPromptService.cs` | Implement overload: conditional scope validation + override support |
| `AIAgentExecutionOptions.cs` | **New** — Options class with `ProfileIdOverride`, `ContextIdsOverride` |
| `IAIAgentService.cs` | Add `StreamAgentAsync` overload accepting options |
| `AIAgentService.cs` | Implement overload: override support |
| `PromptTestFeature.cs` | Use new overload with `ValidateScope = false` + pass overrides |
| `AgentTestFeature.cs` | Use new overload + pass overrides |
