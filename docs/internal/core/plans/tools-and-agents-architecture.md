# Umbraco.Ai Tools & Agents Architecture Plan

## Summary

Add tool infrastructure to Umbraco.Ai.Core and implement the full Umbraco.Ai.Agents layer for governed AI assistant capabilities.

### Design Decisions Made

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Safety Mode | No enforcement in Core | Governance is consumer's responsibility; Agents provides it |
| Profile Tools | No profile-level tool config | Tools pass via ChatOptions; Agents controls capabilities |
| Tool Execution | Auto-enable `UseFunctionInvocation()` | When tools present in ChatOptions, middleware is applied |
| System Prompt | Optional/layered | Profile can have template, Agent/caller can override |

---

## Part 1: Core Tool Infrastructure (Umbraco.Ai.Core)

### 1.1 New Files to Create

#### `src/Umbraco.Ai.Core/Tools/IAiTool.cs`
```csharp
namespace Umbraco.Ai.Core.Tools;

/// <summary>
/// Defines an AI tool that can be invoked by AI models.
/// </summary>
public interface IAiTool
{
    string Id { get; }
    string Name { get; }
    string Description { get; }
    string Category { get; }
    bool IsDestructive { get; }
    IReadOnlyList<string> Tags { get; }

    /// <summary>
    /// Creates the AIFunction for MEAI integration.
    /// </summary>
    AIFunction CreateFunction(IServiceProvider serviceProvider);
}
```

#### `src/Umbraco.Ai.Core/Tools/AiToolAttribute.cs`
```csharp
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class AiToolAttribute : Attribute
{
    public string Id { get; }
    public string Name { get; }
    public string Category { get; set; } = "General";
    public bool IsDestructive { get; set; } = false;

    public AiToolAttribute(string id, string name) { ... }
}
```

#### `src/Umbraco.Ai.Core/Tools/AiToolBase.cs`
Base class for implementing tools with automatic `AIFunction` creation via reflection on an `Execute`/`ExecuteAsync` method.

#### `src/Umbraco.Ai.Core/Tools/IAiToolRegistry.cs`
```csharp
public interface IAiToolRegistry
{
    IEnumerable<IAiTool> Tools { get; }
    IAiTool? GetTool(string toolId);
    IEnumerable<IAiTool> GetToolsByCategory(string category);
    IEnumerable<IAiTool> GetToolsWithTag(string tag);
    IReadOnlyList<AITool> ToAITools(IEnumerable<string> toolIds, IServiceProvider sp);
    IReadOnlyList<AITool> ToAITools(Func<IAiTool, bool> predicate, IServiceProvider sp);
}
```

#### `src/Umbraco.Ai.Core/Tools/AiToolRegistry.cs`
Implementation using dictionary lookup, following `AiRegistry` pattern.

### 1.2 Files to Modify

#### `src/Umbraco.Ai.Core/Factories/AiChatClientFactory.cs`
- Add `UseFunctionInvocation()` middleware to enable automatic tool execution when tools are present
- This should be applied as the innermost middleware (closest to provider)

```csharp
private IChatClient BuildClient(IChatClient baseClient, bool hasTools)
{
    var builder = baseClient.AsBuilder();

    // Add function invocation if tools will be used
    if (hasTools)
    {
        builder = builder.UseFunctionInvocation();
    }

    // Apply custom middleware
    foreach (var middleware in _middleware.OrderBy(m => m.Order))
    {
        // Apply middleware
    }

    return builder.Build();
}
```

**Issue**: The factory doesn't know if tools will be used at client creation time. Options:
1. Always add `UseFunctionInvocation()` (safe, slight overhead)
2. Create client lazily when options are known
3. Add a parameter to `CreateClientAsync` indicating tool usage

**Recommendation**: Always add `UseFunctionInvocation()` - it's a no-op when no tools are in options.

#### `src/Umbraco.Ai.Core/Configuration/UmbracoBuilderExtensions.cs`
Add tool registration:
```csharp
// Tool infrastructure
services.AddSingleton<IAiToolRegistry, AiToolRegistry>();

// Scan and register tools (follows provider pattern)
RegisterTools(services);
```

### 1.3 Project Structure Addition

```
src/Umbraco.Ai.Core/
└── Tools/
    ├── IAiTool.cs
    ├── AiToolAttribute.cs
    ├── AiToolBase.cs
    ├── IAiToolRegistry.cs
    └── AiToolRegistry.cs
```

---

## Part 2: Agents Layer (Umbraco.Ai.Agents)

### 2.1 New Project Structure

```
src/Umbraco.Ai.Agents/
├── Umbraco.Ai.Agents.csproj
├── Models/
│   ├── AiAgent.cs
│   ├── AgentSession.cs
│   ├── AgentMessage.cs
│   ├── AgentContext.cs
│   ├── AgentResponse.cs
│   ├── ToolInvocation.cs
│   └── ToolApproval.cs
├── Tools/
│   └── BuiltIn/
│       └── Content/                    # MVP: Content tools only
│           ├── ContentSearchTool.cs
│           ├── ContentGetTool.cs
│           ├── ContentCreateTool.cs
│           └── ContentUpdateTool.cs
├── Services/
│   ├── IAiAgentService.cs
│   ├── AiAgentService.cs
│   ├── IAiAgentExecutor.cs
│   ├── AiAgentExecutor.cs
│   ├── IAgentSessionService.cs
│   └── AgentSessionService.cs
├── Approval/
│   ├── IToolApprovalService.cs
│   ├── ToolApprovalService.cs
│   └── ApprovalInterceptingChatClient.cs
├── Repositories/
│   ├── IAiAgentRepository.cs
│   ├── InMemoryAgentRepository.cs
│   ├── IAgentSessionRepository.cs      # Interface for DB-ready swap
│   └── InMemorySessionRepository.cs
├── Api/                                 # API in same project
│   ├── Controllers/
│   │   ├── AgentsController.cs
│   │   ├── SessionsController.cs
│   │   └── ToolsController.cs
│   └── Models/
│       ├── AgentChatRequest.cs
│       └── AgentChatResponse.cs
└── Configuration/
    └── UmbracoBuilderExtensions.cs
```

### 2.2 Core Models

#### `AiAgent.cs`
```csharp
public sealed class AiAgent
{
    public required Guid Id { get; init; }
    public required string Alias { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }

    /// <summary>
    /// Profile to use for AI model configuration.
    /// </summary>
    public required Guid ProfileId { get; init; }

    /// <summary>
    /// Agent-specific system prompt. Overrides profile's prompt if set.
    /// </summary>
    public string? SystemPrompt { get; init; }

    /// <summary>
    /// Tool IDs this agent can use.
    /// </summary>
    public IReadOnlyList<string> EnabledToolIds { get; init; } = [];

    /// <summary>
    /// User group aliases that can use this agent.
    /// Empty means all users.
    /// </summary>
    public IReadOnlyList<string> AllowedUserGroups { get; init; } = [];

    /// <summary>
    /// Whether this agent is currently active.
    /// </summary>
    public bool IsEnabled { get; init; } = true;
}
```

#### `AgentSession.cs`
```csharp
public sealed class AgentSession
{
    public required string Id { get; init; }
    public required Guid AgentId { get; init; }
    public required Guid UserId { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
    public IList<AgentMessage> Messages { get; } = [];
    public IList<ToolApproval> PendingApprovals { get; } = [];

    /// <summary>
    /// Current context (e.g., content being edited).
    /// </summary>
    public AgentContext? Context { get; set; }
}

public sealed class AgentContext
{
    public string? ContentId { get; init; }
    public string? ContentTypeAlias { get; init; }
    public string? Culture { get; init; }
}
```

#### `ToolApproval.cs`
```csharp
public sealed class ToolApproval
{
    public required Guid Id { get; init; }
    public required string ToolId { get; init; }
    public required string ToolName { get; init; }
    public required object Parameters { get; init; }
    public string? ParametersSummary { get; init; }
    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;
    public DateTime RequestedAt { get; init; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
    public string? RejectionReason { get; set; }
}

public enum ApprovalStatus
{
    Pending,
    Approved,
    Rejected
}
```

### 2.3 Key Services

#### `IAiAgentService.cs`
```csharp
public interface IAiAgentService
{
    Task<AiAgent?> GetAgentAsync(Guid id, CancellationToken ct = default);
    Task<AiAgent?> GetAgentByAliasAsync(string alias, CancellationToken ct = default);
    Task<IEnumerable<AiAgent>> GetAllAgentsAsync(CancellationToken ct = default);
    Task<IEnumerable<AiAgent>> GetAgentsForUserAsync(ClaimsPrincipal user, CancellationToken ct = default);
    Task<AiAgent> SaveAgentAsync(AiAgent agent, CancellationToken ct = default);
    Task DeleteAgentAsync(Guid id, CancellationToken ct = default);
}
```

#### `IAiAgentExecutor.cs`
```csharp
public interface IAiAgentExecutor
{
    /// <summary>
    /// Sends a message to an agent and gets a response.
    /// </summary>
    Task<AgentResponse> ChatAsync(
        Guid agentId,
        string sessionId,
        string message,
        AgentContext? context = null,
        CancellationToken ct = default);

    /// <summary>
    /// Streams a response from an agent.
    /// </summary>
    IAsyncEnumerable<AgentResponseChunk> ChatStreamingAsync(
        Guid agentId,
        string sessionId,
        string message,
        AgentContext? context = null,
        CancellationToken ct = default);

    /// <summary>
    /// Approves a pending tool invocation.
    /// </summary>
    Task<AgentResponse> ApproveToolAsync(
        string sessionId,
        Guid approvalId,
        CancellationToken ct = default);

    /// <summary>
    /// Rejects a pending tool invocation.
    /// </summary>
    Task<AgentResponse> RejectToolAsync(
        string sessionId,
        Guid approvalId,
        string? reason = null,
        CancellationToken ct = default);
}

public sealed class AgentResponse
{
    public required string Text { get; init; }
    public IReadOnlyList<ToolApproval> PendingApprovals { get; init; } = [];
    public IReadOnlyList<ToolInvocation> ExecutedTools { get; init; } = [];
    public bool IsComplete { get; init; } = true;
}
```

### 2.4 Approval Workflow

The approval workflow intercepts destructive tool calls:

1. Agent requests tool invocation via MEAI function calling
2. `AiAgentExecutor` intercepts before execution
3. If tool is destructive (`IsDestructive = true`):
   - Create `ToolApproval` record with `Pending` status
   - Return response with `PendingApprovals` list
   - Do NOT execute the tool yet
4. Frontend shows approval UI
5. User approves/rejects via `ApproveToolAsync`/`RejectToolAsync`
6. On approve: Execute tool, continue conversation with result
7. On reject: Continue conversation with rejection message

**Implementation approach**: Custom `IChatClient` wrapper that intercepts function call responses:

```csharp
internal class ApprovalInterceptingChatClient : DelegatingChatClient
{
    protected override async Task<ChatResponse> CompleteAsync(
        IList<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken ct = default)
    {
        var response = await base.CompleteAsync(chatMessages, options, ct);

        // Check for function calls that need approval
        foreach (var functionCall in response.GetFunctionCalls())
        {
            var tool = _toolRegistry.GetTool(functionCall.Name);
            if (tool?.IsDestructive == true)
            {
                // Queue for approval instead of executing
                _approvalService.QueueApproval(functionCall, tool);
            }
        }

        return response;
    }
}
```

### 2.5 Built-in Tools

Tools follow the Core `IAiTool` pattern. Example:

```csharp
[AiTool("content.search", "Search Content", Category = "Content")]
public class ContentSearchTool : AiToolBase
{
    private readonly IContentService _contentService;

    public override string Description =>
        "Searches Umbraco content by text query. Returns matching content items.";

    public ContentSearchTool(IContentService contentService)
    {
        _contentService = contentService;
    }

    [Description("Search for content matching a query")]
    public Task<ContentSearchResult[]> ExecuteAsync(
        [Description("The search query")] string query,
        [Description("Content type to filter by")] string? contentType = null,
        [Description("Maximum results")] int take = 10)
    {
        // Implementation
    }
}
```

```csharp
[AiTool("content.update", "Update Content", Category = "Content", IsDestructive = true)]
public class ContentUpdateTool : AiToolBase
{
    public override string Description =>
        "Updates properties on an existing content item. Requires approval.";

    [Description("Update content properties")]
    public Task<ContentUpdateResult> ExecuteAsync(
        [Description("Content ID or key")] string contentId,
        [Description("Properties to update as key-value pairs")] Dictionary<string, object> properties)
    {
        // Implementation
    }
}
```

---

## Part 3: Web Layer (Umbraco.Ai.Agents.Web)

### 3.1 API Endpoints

```
POST   /umbraco/ai/api/v1/agents/{id}/chat           # Send message
POST   /umbraco/ai/api/v1/agents/{id}/chat/stream    # Streaming chat
POST   /umbraco/ai/api/v1/sessions/{id}/approve/{approvalId}  # Approve tool
POST   /umbraco/ai/api/v1/sessions/{id}/reject/{approvalId}   # Reject tool
GET    /umbraco/ai/api/v1/agents                     # List available agents
GET    /umbraco/ai/api/v1/agents/{id}                # Get agent details
GET    /umbraco/ai/api/v1/tools                      # List all tools
GET    /umbraco/ai/api/v1/tools/categories           # List tool categories
```

### 3.2 Request/Response Models

```csharp
public record AgentChatRequest(
    string Message,
    string? SessionId = null,
    AgentContextRequest? Context = null);

public record AgentChatResponse(
    string SessionId,
    string Text,
    IReadOnlyList<ToolApprovalResponse> PendingApprovals,
    bool IsComplete);

public record ToolApprovalResponse(
    Guid Id,
    string ToolId,
    string ToolName,
    string Description,
    object Parameters,
    string ParametersSummary);
```

---

## Part 4: Frontend Components

### 4.1 Header App (AI Button)
- Button in backoffice header bar
- Opens sidebar panel
- Shows agent selector dropdown

### 4.2 AI Sidebar
- Agent selection dropdown
- Context banner (current content item)
- Chat message history
- Input field with send button
- Approval widgets inline with messages

### 4.3 Approval Widget
- Shows tool name and description
- Displays parameters being passed
- Approve/Reject buttons
- Loading state during execution

### 4.4 Entity Actions
- Context menu items on content tree
- Quick actions: Generate Summary, Translate, AI Suggestions
- Opens focused modals for specific tasks

---

## Part 5: Implementation Phases

### Phase 1: Core Tool Infrastructure
1. Create `Tools/` folder in Umbraco.Ai.Core
2. Implement `IAiTool`, `AiToolAttribute`, `AiToolBase`
3. Implement `IAiToolRegistry`, `AiToolRegistry`
4. Update `AiChatClientFactory` to add `UseFunctionInvocation()`
5. Update DI registration
6. Add unit tests

### Phase 2: Agents Foundation
1. Create `Umbraco.Ai.Agents` project (combined services + API)
2. Implement models: `AiAgent`, `AgentSession`, `ToolApproval`
3. Implement `IAiAgentService`, `IAgentSessionService`
4. Implement in-memory repositories with `IAgentSessionRepository` interface (DB-ready pattern)
5. Add unit tests

### Phase 3: Agent Execution
1. Implement `IAiAgentExecutor`
2. Implement approval workflow with `ApprovalInterceptingChatClient`
3. Implement `IToolApprovalService`
4. Integration tests

### Phase 4: Built-in Tools (Content Only for MVP)
1. Implement `ContentSearchTool` - search content by text
2. Implement `ContentGetTool` - get content by ID/key
3. Implement `ContentCreateTool` (destructive) - create new content
4. Implement `ContentUpdateTool` (destructive) - update content properties
5. Additional tools (media, navigation, search) deferred to later phases

### Phase 5: Web API (in Umbraco.Ai.Agents project)
1. Implement API controllers in the Agents project
2. Add OpenAPI documentation
3. Integration tests

### Phase 6: Frontend
1. Header app component
2. Sidebar component with chat UI
3. Approval widget component
4. Entity action components
5. Agent management UI (if time permits)

---

## Critical Files to Modify

### Umbraco.Ai.Core
- `src/Umbraco.Ai.Core/Factories/AiChatClientFactory.cs` - Add `UseFunctionInvocation()`
- `src/Umbraco.Ai.Core/Configuration/UmbracoBuilderExtensions.cs` - Register tool services
- `src/Umbraco.Ai.Core/Services/AiChatService.cs` - No changes needed (tools flow via ChatOptions)

### Reference Files (Patterns to Follow)
- `src/Umbraco.Ai.Core/Registry/AiRegistry.cs` - Pattern for tool registry
- `src/Umbraco.Ai.Core/Providers/AiProviderBase.cs` - Pattern for tool base class
- `src/Umbraco.Ai.Core/Middleware/IAiChatMiddleware.cs` - Pattern for middleware
- `src/Umbraco.Ai.OpenAi/OpenAiProvider.cs` - Pattern for attribute-based discovery

---

## Final Decisions

| Question | Decision |
|----------|----------|
| Session Storage | In-memory with repository pattern (DB-ready for later) |
| Project Structure | Combined `Umbraco.Ai.Agents` project (services + API) |
| Initial Tools | Content only (search, get, create, update) |
| Safety Mode | No enforcement in Core - governance is consumer responsibility |
| Profile Tools | No profile-level tool config - tools via ChatOptions only |
| Tool Execution | Auto-enable `UseFunctionInvocation()` when tools present |
| System Prompt | Optional/layered - Profile can have template, Agent/caller overrides |
