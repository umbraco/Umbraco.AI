# AG-UI Event Protocol Integration Plan

## Overview

This plan introduces AG-UI (Agent User Interface Protocol) event patterns into Umbraco.Ai using a **two-tier approach**:

- **Core (Umbraco.Ai.Web)**: Lightweight AG-UI subset for streaming endpoints - no external dependencies
- **Agents (Umbraco.Ai.Agents)**: Full AG-UI with Microsoft Agent Framework

This ensures consistency so developers learn one event pattern and can transition seamlessly from Core to Agents.

## Background

### What is AG-UI?

AG-UI is an open, event-based protocol that standardizes communication between AI agents and user-facing applications. Key concepts:

- **Event-driven streaming** - All communication via typed events (not raw text)
- **Lifecycle events** - Explicit run start/end, message start/end boundaries
- **Tool events** - Standardized tool call/result flow
- **Interrupts** - Human-in-the-loop approval patterns
- **Shared state** - Bidirectional context synchronization

Documentation: https://docs.ag-ui.com

### Why Adopt AG-UI Patterns?

1. **Future-proofing** - Agent API will need these patterns; adopt them now
2. **Single vocabulary** - Core and Agents speak the same language
3. **Easy transition** - Developers learn one pattern, add capabilities incrementally
4. **Ecosystem alignment** - Compatible with AG-UI tooling and frontends

## Two-Tier Architecture

### Layer Stack

```
┌─────────────────────────────────────────────────────────────────┐
│                    Umbraco Backoffice UI                        │
│   (Handles AG-UI events via SSE)                                │
└───────────────────────────┬─────────────────────────────────────┘
                            │ AG-UI Events (SSE)
                            │ - text_message_* (streaming chat)
                            │ - tool_call_* (tool execution)
                            │ - run_started/finished/error
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│   Core: Custom AiStreamEvent types (lightweight)                │
│   Agents: Microsoft.Agents.AI.Hosting.AGUI.AspNetCore           │
│           MapAGUI() - converts AIAgent streams to AG-UI SSE     │
└───────────────────────────┬─────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│          Microsoft.Agents.AI.ChatClientAgent                    │
│   - Wraps IChatClient (M.E.AI)                                  │
│   - Handles tool invocation                                     │
│   - Thread/conversation management                              │
└───────────────────────────┬─────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│              Umbraco.Ai IChatClient (existing)                  │
│   - Created via IAiChatClientFactory                            │
│   - Profile-configured (model, temperature, etc.)               │
│   - Middleware pipeline applied                                 │
└─────────────────────────────────────────────────────────────────┘
```

### Event Types by Layer

| Event | Core | Agents |
|-------|:----:|:------:|
| `run_started` | ✓ | ✓ |
| `run_finished` | ✓ | ✓ |
| `run_error` | ✓ | ✓ |
| `text_message_start` | ✓ | ✓ |
| `text_message_content` | ✓ | ✓ |
| `text_message_end` | ✓ | ✓ |
| `tool_call_*` | - | ✓ |
| `state_snapshot` | - | ✓ |
| `state_delta` | - | ✓ |
| `messages_snapshot` | - | ✓ |
| `approval_*` (custom) | - | ✓ |

### Package Dependencies

```
Umbraco.Ai.Core
└── No AG-UI dependency (custom event types)

Umbraco.Ai.Agents
├── Microsoft.Agents.AI
└── Microsoft.Agents.AI.Hosting.AGUI.AspNetCore
```

## Current State

### Existing Streaming Implementation

```csharp
// StreamChatController.cs - Current approach
await Response.WriteAsync($"data: {json}\n\n", cancellationToken);
await Response.WriteAsync("data: [DONE]\n\n", cancellationToken);
```

```json
// Current event shape
{ "content": "Hello", "finishReason": null, "isComplete": false }
```

### Problems with Current Approach

- No event type discriminator - can't mix event types
- Magic `[DONE]` marker instead of explicit lifecycle
- No correlation IDs for multi-message scenarios
- Different vocabulary than AG-UI agents will use

## Tier 1: Core Implementation (Umbraco.Ai.Web)

### Target State

- Same endpoint, AG-UI event envelope
- Uses `snake_case` event types matching AG-UI spec
- **No external AG-UI package dependency** (custom lightweight types)

### Event Types

**File:** `src/Umbraco.Ai.Web/Api/Common/Events/AiStreamEvent.cs`

```csharp
using System.Text.Json.Serialization;

namespace Umbraco.Ai.Web.Api.Common.Events;

/// <summary>
/// Base type for AG-UI compatible streaming events.
/// Uses snake_case naming per AG-UI specification.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(RunStartedEvent), "run_started")]
[JsonDerivedType(typeof(RunFinishedEvent), "run_finished")]
[JsonDerivedType(typeof(RunErrorEvent), "run_error")]
[JsonDerivedType(typeof(TextMessageStartEvent), "text_message_start")]
[JsonDerivedType(typeof(TextMessageContentEvent), "text_message_content")]
[JsonDerivedType(typeof(TextMessageEndEvent), "text_message_end")]
public abstract record AiStreamEvent
{
    [JsonPropertyName("timestamp")]
    public long Timestamp { get; init; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}

// Lifecycle events
public record RunStartedEvent : AiStreamEvent
{
    [JsonPropertyName("run_id")]
    public string RunId { get; init; } = Guid.NewGuid().ToString();
}

public record RunFinishedEvent : AiStreamEvent
{
    [JsonPropertyName("run_id")]
    public string? RunId { get; init; }
}

public record RunErrorEvent : AiStreamEvent
{
    [JsonPropertyName("message")]
    public string? Message { get; init; }
}

// Text message events
public record TextMessageStartEvent : AiStreamEvent
{
    [JsonPropertyName("message_id")]
    public string MessageId { get; init; } = Guid.NewGuid().ToString();

    [JsonPropertyName("role")]
    public string Role { get; init; } = "assistant";
}

public record TextMessageContentEvent : AiStreamEvent
{
    [JsonPropertyName("message_id")]
    public required string MessageId { get; init; }

    [JsonPropertyName("delta")]
    public string? Delta { get; init; }
}

public record TextMessageEndEvent : AiStreamEvent
{
    [JsonPropertyName("message_id")]
    public required string MessageId { get; init; }
}
```

### Updated StreamChatController

**File:** `src/Umbraco.Ai.Web/Api/Management/Chat/Controllers/StreamChatController.cs`

```csharp
[HttpPost("stream")]
public async Task StreamChat(ChatRequestModel requestModel, CancellationToken ct)
{
    Response.ContentType = "text/event-stream";
    Response.Headers.CacheControl = "no-cache";
    Response.Headers.Connection = "keep-alive";

    var runId = Guid.NewGuid().ToString();
    var messageId = Guid.NewGuid().ToString();

    try
    {
        await WriteEventAsync(new RunStartedEvent { RunId = runId }, ct);

        // ... resolve profile, get messages ...

        await WriteEventAsync(new TextMessageStartEvent
        {
            MessageId = messageId
        }, ct);

        await foreach (var update in stream.WithCancellation(ct))
        {
            if (!string.IsNullOrEmpty(update.Text))
            {
                await WriteEventAsync(new TextMessageContentEvent
                {
                    MessageId = messageId,
                    Delta = update.Text
                }, ct);
            }
        }

        await WriteEventAsync(new TextMessageEndEvent { MessageId = messageId }, ct);
        await WriteEventAsync(new RunFinishedEvent { RunId = runId }, ct);
    }
    catch (Exception ex)
    {
        await WriteEventAsync(new RunErrorEvent { Message = ex.Message }, ct);
    }
}

private async Task WriteEventAsync(AiStreamEvent evt, CancellationToken ct)
{
    var json = JsonSerializer.Serialize(evt, evt.GetType(), _jsonOptions);
    await Response.WriteAsync($"data: {json}\n\n", ct);
    await Response.Body.FlushAsync(ct);
}
```

## Tier 2: Agents Implementation (Umbraco.Ai.Agents)

### .NET SDK Options

#### Option A: Microsoft Agent Framework (Recommended)

**Source:** https://github.com/microsoft/agent-framework

Microsoft is adding official AG-UI support via two packages:
- **`Microsoft.Agents.AI`** - Core agent abstractions
- **`Microsoft.Agents.AI.Hosting.AGUI.AspNetCore`** - Server-side ASP.NET Core integration

**Key Components:**
- **`ChatClientAgent`** - Wraps `IChatClient` directly (perfect fit for Umbraco.Ai)
- **`MapAGUI()`** - ASP.NET Core endpoint extension
- Built-in OpenTelemetry, memory providers, workflow support

**Event naming:** `snake_case` (e.g., `text_message_content`) - matches AG-UI spec

#### Option B: AG-UI Community SDK

**Source:** https://github.com/ag-ui-protocol/ag-ui/pull/38

Lighter-weight alternative with:
- `ChatClientAgent` wrapping `IChatClient`
- `MapAgentEndpoint()` route builder extension

**Event naming:** `SCREAMING_CASE` (e.g., `TEXT_MESSAGE_CONTENT`) - differs from spec

#### Recommendation

Use **Microsoft Agent Framework** because:
1. **`ChatClientAgent` wraps `IChatClient` directly** - perfect fit for Umbraco.Ai's architecture
2. **Official Microsoft support** - long-term investment, consistent with .NET ecosystem
3. **Built-in features** - OpenTelemetry, logging, memory providers included
4. **Broader ecosystem** - Copilot Studio, Azure AI, A2A protocol integration
5. **Standard AG-UI** - uses `snake_case` matching the AG-UI specification

### Agent Factory

**File:** `src/Umbraco.Ai.Agents/Services/UmbracoAgentFactory.cs`

```csharp
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Umbraco.Ai.Core.Chat;
using Umbraco.Ai.Core.Profiles;

namespace Umbraco.Ai.Agents.Services;

/// <summary>
/// Factory for creating ChatClientAgent instances from Umbraco.Ai profiles.
/// </summary>
public class UmbracoAgentFactory
{
    private readonly IAiChatClientFactory _chatClientFactory;
    private readonly IAiProfileService _profileService;
    private readonly IAiToolRegistry _toolRegistry;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILoggerFactory _loggerFactory;

    public async Task<ChatClientAgent> CreateAgentAsync(
        AiAgent agent,
        CancellationToken ct = default)
    {
        var chatClient = await _chatClientFactory.CreateClientAsync(agent.ProfileId, ct);
        var profile = await _profileService.GetAsync(agent.ProfileId, ct);

        // Get tools for this agent
        var tools = _toolRegistry.ToAITools(agent.EnabledToolIds, _serviceProvider);

        return new ChatClientAgent(
            chatClient,
            instructions: agent.SystemPrompt ?? profile?.SystemPrompt,
            name: agent.Name,
            description: agent.Description,
            tools: tools,
            loggerFactory: _loggerFactory);
    }
}
```

### Agent Endpoints

**File:** `src/Umbraco.Ai.Agents/Api/Controllers/AgentChatController.cs`

```csharp
[UmbracoAiVersionedManagementApiRoute("agents")]
public class AgentChatController : AgentControllerBase
{
    [HttpPost("{agentIdOrAlias}/chat/stream")]
    public async Task<IResult> StreamChat(
        string agentIdOrAlias,
        [FromBody] RunAgentInput input,
        CancellationToken ct)
    {
        var agent = await ResolveAgentAsync(agentIdOrAlias, ct);
        var chatClientAgent = await _agentFactory.CreateAgentAsync(agent, ct);

        // Microsoft's MapAGUI handles all event conversion
        return Results.Extensions.AGUI(chatClientAgent, input);
    }
}
```

### Custom Approval Events

Extend AG-UI with Umbraco-specific approval events:

```csharp
public record ApprovalRequestedEvent : AiStreamEvent
{
    [JsonPropertyName("type")]
    public string Type => "approval_requested";

    [JsonPropertyName("approval_id")]
    public required Guid ApprovalId { get; init; }

    [JsonPropertyName("tool_id")]
    public required string ToolId { get; init; }

    [JsonPropertyName("tool_name")]
    public required string ToolName { get; init; }

    [JsonPropertyName("parameters")]
    public required object Parameters { get; init; }

    [JsonPropertyName("parameters_summary")]
    public string? ParametersSummary { get; init; }
}
```

## Frontend Integration

### TypeScript Event Types

**File:** `src/Umbraco.Ai.Web.StaticAssets/Client/src/api/events.ts`

```typescript
/**
 * AG-UI event types matching the specification.
 * Uses snake_case per AG-UI spec.
 */

interface BaseEvent {
  type: string;
  timestamp?: number;
}

// Lifecycle events
export interface RunStartedEvent extends BaseEvent {
  type: 'run_started';
  run_id?: string;
}

export interface RunFinishedEvent extends BaseEvent {
  type: 'run_finished';
  run_id?: string;
}

export interface RunErrorEvent extends BaseEvent {
  type: 'run_error';
  message?: string;
}

// Text message events
export interface TextMessageStartEvent extends BaseEvent {
  type: 'text_message_start';
  message_id: string;
  role?: string;
}

export interface TextMessageContentEvent extends BaseEvent {
  type: 'text_message_content';
  message_id: string;
  delta?: string;
}

export interface TextMessageEndEvent extends BaseEvent {
  type: 'text_message_end';
  message_id: string;
}

// Tool events (Agents only)
export interface ToolCallStartEvent extends BaseEvent {
  type: 'tool_call_start';
  tool_call_id: string;
  tool_call_name: string;
  parent_message_id?: string;
}

export interface ToolCallArgsEvent extends BaseEvent {
  type: 'tool_call_args';
  tool_call_id: string;
  delta?: string;
}

export interface ToolCallEndEvent extends BaseEvent {
  type: 'tool_call_end';
  tool_call_id: string;
}

// Approval events (Agents only, custom)
export interface ApprovalRequestedEvent extends BaseEvent {
  type: 'approval_requested';
  approval_id: string;
  tool_id: string;
  tool_name: string;
  parameters: unknown;
  parameters_summary?: string;
}

// State events (Agents only)
export interface StateSnapshotEvent extends BaseEvent {
  type: 'state_snapshot';
  snapshot: unknown;
}

export interface StateDeltaEvent extends BaseEvent {
  type: 'state_delta';
  delta: Array<{ op: string; path: string; value?: unknown }>;
}

// Messages snapshot (for syncing conversation history)
export interface MessagesSnapshotEvent extends BaseEvent {
  type: 'messages_snapshot';
  messages: Array<{
    role: string;
    id?: string;
    content?: string;
    tool_calls?: Array<{ id: string; function: { name: string; arguments: string } }>;
  }>;
}

// Union types - Core subset vs full Agents
export type CoreStreamEvent =
  | RunStartedEvent
  | RunFinishedEvent
  | RunErrorEvent
  | TextMessageStartEvent
  | TextMessageContentEvent
  | TextMessageEndEvent;

export type AgentStreamEvent =
  | CoreStreamEvent
  | ToolCallStartEvent
  | ToolCallArgsEvent
  | ToolCallEndEvent
  | StateSnapshotEvent
  | StateDeltaEvent
  | MessagesSnapshotEvent
  | ApprovalRequestedEvent;

// Type guards
export const isRunStarted = (e: BaseEvent): e is RunStartedEvent => e.type === 'run_started';
export const isRunFinished = (e: BaseEvent): e is RunFinishedEvent => e.type === 'run_finished';
export const isRunError = (e: BaseEvent): e is RunErrorEvent => e.type === 'run_error';
export const isTextMessageContent = (e: BaseEvent): e is TextMessageContentEvent =>
  e.type === 'text_message_content';
export const isToolCallStart = (e: BaseEvent): e is ToolCallStartEvent =>
  e.type === 'tool_call_start';
export const isApprovalRequested = (e: BaseEvent): e is ApprovalRequestedEvent =>
  e.type === 'approval_requested';
```

## Implementation Phases

### Phase 1: Core AG-UI Events

1. Create `AiStreamEvent` base and derived types in `Umbraco.Ai.Web`
2. Update `StreamChatController` to emit AG-UI events
3. Update frontend TypeScript types
4. Update frontend SSE parsing
5. Remove legacy `ChatStreamChunkModel` and `[DONE]` marker

### Phase 2: Agents Foundation (separate project)

1. Add Microsoft.Agents.AI packages to `Umbraco.Ai.Agents`
2. Create `UmbracoAgentFactory`
3. Implement AG-UI endpoint using `MapAGUI()`
4. Add custom approval events

### Phase 3: Frontend Agents Support

1. Handle tool events in UI
2. Implement approval widget
3. Session management

## Migration Path

### For Developers Using Core

1. Update frontend to handle AG-UI events (same structure as Agents will use)
2. No backend package changes needed - Core uses lightweight custom types

### For Developers Adopting Agents

1. Add `Umbraco.Ai.Agents` package
2. Frontend already understands event structure from Core
3. Add handlers for tool/approval events

## Files Summary

### Core (Modify)

- `src/Umbraco.Ai.Web/Api/Common/Events/` (new folder)
  - `AiStreamEvent.cs` (includes all event types)
- `src/Umbraco.Ai.Web/Api/Management/Chat/Controllers/StreamChatController.cs`
- `src/Umbraco.Ai.Web.StaticAssets/Client/src/api/events.ts`

### Agents (New - Future)

- `src/Umbraco.Ai.Agents/Services/UmbracoAgentFactory.cs`
- `src/Umbraco.Ai.Agents/Api/Controllers/AgentChatController.cs`
- `src/Umbraco.Ai.Agents/Api/Events/ApprovalRequestedEvent.cs`

## Benefits of This Approach

1. **Consistency** - Same event envelope for Core and Agents
2. **Lightweight Core** - No external AG-UI dependency in Core package
3. **Full Power in Agents** - Microsoft Agent Framework for tools/sessions
4. **Easy Transition** - Developers learn one pattern, add capabilities incrementally
5. **AG-UI Spec Compliant** - Uses `snake_case` per specification

## Future Extensions

### Umbraco-Specific Tools

Register Umbraco tools via `ChatClientAgent`:

```csharp
public async Task<ChatClientAgent> CreateAgentAsync(AiAgent agent, CancellationToken ct)
{
    var chatClient = await _chatClientFactory.CreateClientAsync(agent.ProfileId, ct);
    var profile = await _profileService.GetAsync(agent.ProfileId, ct);

    // Define Umbraco tools
    var tools = new List<AITool>
    {
        AIFunctionFactory.Create(SearchContent),
        AIFunctionFactory.Create(GetContent),
        AIFunctionFactory.Create(UpdateContent),  // Requires approval
        AIFunctionFactory.Create(PublishContent), // Requires approval
    };

    return new ChatClientAgent(
        chatClient,
        instructions: profile?.SystemPrompt,
        name: agent.Name,
        tools: tools,
        loggerFactory: _loggerFactory);
}

[Description("Search for content in Umbraco")]
private async Task<IEnumerable<ContentSearchResult>> SearchContent(
    [Description("Search query")] string query,
    [Description("Content type alias to filter by")] string? contentType = null)
{
    // Implementation using Umbraco's Examine/search APIs
}
```

### Context Providers (RAG)

Use Microsoft's `AIContextProvider` for retrieval-augmented generation:

```csharp
var agentOptions = new ChatClientAgentOptions
{
    ChatOptions = new ChatOptions { Instructions = profile.SystemPrompt },
    AIContextProviderFactory = _ => new UmbracoContentContextProvider(_contentService)
};

return new ChatClientAgent(chatClient, agentOptions, _loggerFactory);
```

### OpenTelemetry Tracing

Leverage built-in telemetry:

```csharp
// Wrap agent with OpenTelemetry tracing
var agent = new ChatClientAgent(chatClient, ...);
var tracedAgent = agent.UseOpenTelemetry();

endpoints.MapAGUI("/umbraco/ai/api/v1/agents/chat", tracedAgent);
```

## References

**AG-UI Protocol:**
- Documentation: https://docs.ag-ui.com
- Events: https://docs.ag-ui.com/concepts/events
- State: https://docs.ag-ui.com/concepts/state
- Tools: https://docs.ag-ui.com/concepts/tools

**.NET Implementations:**
- Microsoft Agent Framework: https://github.com/microsoft/agent-framework
- Microsoft AG-UI Support: https://github.com/microsoft/agent-framework/issues/1774
- Community .NET SDK (alternative): https://github.com/ag-ui-protocol/ag-ui/pull/38

**Internal:**
- Umbraco.Ai Agents Design: `docs/internal/umbraco-ai-agents-design.md`
