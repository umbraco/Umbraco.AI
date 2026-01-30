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

There are two approaches for exposing AG-UI endpoints, each with different OpenAPI integration patterns.

#### Option A: Minimal API with MapAGUI() (Simpler)

Use Microsoft's `MapAGUI()` extension directly with minimal API OpenAPI extensions.

**File:** `src/Umbraco.Ai.Agents/Api/Endpoints/AgentEndpoints.cs`

```csharp
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;

namespace Umbraco.Ai.Agents.Api.Endpoints;

public static class AgentEndpoints
{
    public static IEndpointRouteBuilder MapAgentEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/umbraco/ai/agents/api/v1")
            .WithTags("Umbraco AI Agents")
            .RequireAuthorization();

        group.MapPost("{agentIdOrAlias}/chat/stream", StreamChat)
            .WithOpenApi(ConfigureAgUiOpenApi)
            .Produces(StatusCodes.Status200OK, contentType: "text/event-stream")
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static async Task<IResult> StreamChat(
        string agentIdOrAlias,
        AgentChatRequestModel request,
        UmbracoAgentFactory agentFactory,
        IAgentService agentService,
        CancellationToken ct)
    {
        var agent = await agentService.GetByIdOrAliasAsync(agentIdOrAlias, ct);
        if (agent is null)
            return Results.NotFound();

        var chatClientAgent = await agentFactory.CreateAgentAsync(agent, ct);
        var input = MapToRunAgentInput(request);

        // Microsoft's AGUI result handles all SSE streaming
        return Results.Extensions.AGUI(chatClientAgent, input);
    }

    private static OpenApiOperation ConfigureAgUiOpenApi(OpenApiOperation operation)
    {
        operation.Summary = "Stream an AI agent chat session";
        operation.Description = """
            Initiates a streaming chat session with an Umbraco AI agent.

            **AG-UI Protocol**: This endpoint streams Server-Sent Events (SSE)
            following the AG-UI protocol. Each event is a JSON object with a
            `type` discriminator.

            **Event Flow**:
            1. `run_started` - Stream begins
            2. `text_message_start` - New message from assistant
            3. `text_message_content` (repeated) - Message content chunks
            4. `tool_call_*` events if tools are invoked
            5. `approval_requested` if tool requires user approval
            6. `text_message_end` - Message complete
            7. `run_finished` or `run_error` - Stream ends
            """;

        operation.Extensions["x-ag-ui-events"] = AgUiOpenApiExtensions.BuildEventsArray();
        return operation;
    }

    private static RunAgentInput MapToRunAgentInput(AgentChatRequestModel request)
    {
        return new RunAgentInput
        {
            Messages = request.Messages
                .Select(m => new ChatMessage { Role = m.Role, Content = m.Content })
                .ToList(),
            ThreadId = request.ThreadId
        };
    }
}
```

**Registration in Composer:**

```csharp
public class UmbracoAiAgentsComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        // Register services...

        builder.Services.Configure<UmbracoPipelineOptions>(options =>
        {
            options.AddFilter(new UmbracoPipelineFilter("UmbracoAiAgents")
            {
                Endpoints = app => app.MapAgentEndpoints()
            });
        });
    }
}
```

#### Option B: Controller-Based (More Control)

Use a controller that manually streams AG-UI events. This gives full control over OpenAPI attributes but requires implementing the SSE streaming ourselves.

**File:** `src/Umbraco.Ai.Agents/Api/Controllers/AgentChatController.cs`

```csharp
[UmbracoAiAgentsApiRoute("agents")]
[ApiExplorerSettings(GroupName = "Umbraco AI Agents")]
public class AgentChatController : UmbracoAuthorizedApiController
{
    private readonly UmbracoAgentFactory _agentFactory;
    private readonly IAgentService _agentService;

    /// <summary>
    /// Stream an AI agent chat session.
    /// </summary>
    [HttpPost("{agentIdOrAlias}/chat/stream")]
    [Produces("text/event-stream")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [AgUiEndpoint] // Custom attribute for OpenAPI filter
    public async Task StreamChat(
        string agentIdOrAlias,
        [FromBody] AgentChatRequestModel request,
        CancellationToken ct)
    {
        var agent = await _agentService.GetByIdOrAliasAsync(agentIdOrAlias, ct);
        if (agent is null)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";

        var chatClientAgent = await _agentFactory.CreateAgentAsync(agent, ct);
        var messages = MapToChatMessages(request.Messages);

        // Stream AG-UI events from ChatClientAgent
        await foreach (var update in chatClientAgent.RunStreamingAsync(messages, ct))
        {
            var events = update.ToAgUiEvents(); // Convert to AG-UI events
            foreach (var evt in events)
            {
                await WriteEventAsync(evt, ct);
            }
        }
    }

    private async Task WriteEventAsync(object evt, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(evt, _jsonOptions);
        await Response.WriteAsync($"data: {json}\n\n", ct);
        await Response.Body.FlushAsync(ct);
    }
}
```

#### Comparison

| Aspect | Option A: Minimal API | Option B: Controller |
|--------|----------------------|---------------------|
| AG-UI streaming | Handled by `Results.Extensions.AGUI()` | Manual implementation required |
| OpenAPI | Via `.WithOpenApi()` fluent API | Via attributes + filters |
| Umbraco conventions | Uses `UmbracoPipelineFilter` | Uses standard controller routing |
| Customization | Limited to what Microsoft exposes | Full control over event stream |
| Maintenance | Microsoft maintains streaming logic | We maintain streaming logic |

**Recommendation**: Use **Option A** (Minimal API) unless you need fine-grained control over the event stream. The OpenAPI documentation is achieved through the fluent `.WithOpenApi()` API rather than controller attributes.

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

## OpenAPI Documentation (Agents Package)

SSE endpoints require special handling in OpenAPI/Swashbuckle since OpenAPI doesn't natively describe event schemas within streams. This section documents how to expose AG-UI endpoints for discoverability.

### Challenges

- OpenAPI supports `text/event-stream` content type but not event schemas within streams
- Event types (run_started, text_message_content, etc.) aren't expressible in standard OpenAPI
- Need to document events as schemas even though they're streamed, not returned directly

### Approach

The implementation differs based on the endpoint approach chosen above:

| Approach | OpenAPI Integration |
|----------|---------------------|
| **Option A: Minimal API** | `.WithOpenApi()` fluent API + Document Filter for schemas |
| **Option B: Controller** | `[AgUiEndpoint]` attribute + Operation Filter |

Both approaches share:
1. **Event schema models** - For OpenAPI schema generation
2. **Document filter** - Registers schemas and adds `x-ag-ui-protocol` extension
3. **Extension builder** - Creates `x-ag-ui-events` array

### Implementation

#### 1. AG-UI OpenAPI Extensions Helper

**File:** `src/Umbraco.Ai.Agents/Api/OpenApi/AgUiOpenApiExtensions.cs`

```csharp
using Microsoft.OpenApi.Any;

namespace Umbraco.Ai.Agents.Api.OpenApi;

/// <summary>
/// Helper methods for building AG-UI OpenAPI extensions.
/// </summary>
public static class AgUiOpenApiExtensions
{
    private static readonly string[] EventTypes =
    [
        "run_started",
        "run_finished",
        "run_error",
        "text_message_start",
        "text_message_content",
        "text_message_end",
        "tool_call_start",
        "tool_call_args",
        "tool_call_end",
        "approval_requested"
    ];

    /// <summary>
    /// Builds the x-ag-ui-events extension array with schema references.
    /// </summary>
    public static OpenApiArray BuildEventsArray()
    {
        var array = new OpenApiArray();

        foreach (var eventType in EventTypes)
        {
            var schemaName = ToPascalCase(eventType) + "Event";
            array.Add(new OpenApiObject
            {
                ["type"] = new OpenApiString(eventType),
                ["$ref"] = new OpenApiString($"#/components/schemas/{schemaName}")
            });
        }

        return array;
    }

    /// <summary>
    /// Builds the x-ag-ui-protocol document extension.
    /// </summary>
    public static OpenApiObject BuildProtocolExtension()
    {
        return new OpenApiObject
        {
            ["version"] = new OpenApiString("1.0"),
            ["documentation"] = new OpenApiString("https://docs.ag-ui.com"),
            ["events"] = new OpenApiArray(EventTypes.Select(e => new OpenApiString(e)))
        };
    }

    private static string ToPascalCase(string snakeCase)
    {
        return string.Concat(
            snakeCase.Split('_')
                .Select(word => char.ToUpper(word[0]) + word[1..])
        );
    }
}
```

#### 2. AG-UI Event Schema Models

**File:** `src/Umbraco.Ai.Agents/Api/OpenApi/AgUiEventSchemas.cs`

```csharp
using System.Text.Json.Serialization;

namespace Umbraco.Ai.Agents.Api.OpenApi;

/// <summary>
/// OpenAPI schema models for AG-UI events.
/// These are documentation-only models used to generate OpenAPI schemas.
/// </summary>
public static class AgUiEventSchemas
{
    /// <summary>
    /// Base event with type discriminator.
    /// </summary>
    public record BaseEvent
    {
        /// <summary>Event type discriminator.</summary>
        /// <example>text_message_content</example>
        [JsonPropertyName("type")]
        public string Type { get; init; } = default!;

        /// <summary>Unix timestamp in milliseconds.</summary>
        /// <example>1703001234567</example>
        [JsonPropertyName("timestamp")]
        public long? Timestamp { get; init; }
    }

    /// <summary>Emitted when an agent run starts.</summary>
    public record RunStartedEvent : BaseEvent
    {
        /// <summary>Unique identifier for this run.</summary>
        /// <example>550e8400-e29b-41d4-a716-446655440000</example>
        [JsonPropertyName("run_id")]
        public string? RunId { get; init; }
    }

    /// <summary>Emitted when an agent run completes successfully.</summary>
    public record RunFinishedEvent : BaseEvent
    {
        /// <summary>Unique identifier for this run.</summary>
        [JsonPropertyName("run_id")]
        public string? RunId { get; init; }
    }

    /// <summary>Emitted when an agent run encounters an error.</summary>
    public record RunErrorEvent : BaseEvent
    {
        /// <summary>Error message describing what went wrong.</summary>
        /// <example>Model rate limit exceeded</example>
        [JsonPropertyName("message")]
        public string? Message { get; init; }
    }

    /// <summary>Emitted when the assistant starts a new message.</summary>
    public record TextMessageStartEvent : BaseEvent
    {
        /// <summary>Unique identifier for this message.</summary>
        [JsonPropertyName("message_id")]
        public string MessageId { get; init; } = default!;

        /// <summary>Role of the message author.</summary>
        /// <example>assistant</example>
        [JsonPropertyName("role")]
        public string? Role { get; init; }
    }

    /// <summary>Emitted for each chunk of message content.</summary>
    public record TextMessageContentEvent : BaseEvent
    {
        /// <summary>Message this content belongs to.</summary>
        [JsonPropertyName("message_id")]
        public string MessageId { get; init; } = default!;

        /// <summary>The text content delta.</summary>
        /// <example>Hello, how can I</example>
        [JsonPropertyName("delta")]
        public string? Delta { get; init; }
    }

    /// <summary>Emitted when a message is complete.</summary>
    public record TextMessageEndEvent : BaseEvent
    {
        /// <summary>Message that has completed.</summary>
        [JsonPropertyName("message_id")]
        public string MessageId { get; init; } = default!;
    }

    /// <summary>Emitted when the agent starts a tool call.</summary>
    public record ToolCallStartEvent : BaseEvent
    {
        /// <summary>Unique identifier for this tool call.</summary>
        [JsonPropertyName("tool_call_id")]
        public string ToolCallId { get; init; } = default!;

        /// <summary>Name of the tool being called.</summary>
        /// <example>SearchContent</example>
        [JsonPropertyName("tool_call_name")]
        public string ToolCallName { get; init; } = default!;

        /// <summary>Message that initiated this tool call.</summary>
        [JsonPropertyName("parent_message_id")]
        public string? ParentMessageId { get; init; }
    }

    /// <summary>Emitted for tool call argument chunks.</summary>
    public record ToolCallArgsEvent : BaseEvent
    {
        /// <summary>Tool call this argument belongs to.</summary>
        [JsonPropertyName("tool_call_id")]
        public string ToolCallId { get; init; } = default!;

        /// <summary>JSON argument delta.</summary>
        /// <example>{"query": "home</example>
        [JsonPropertyName("delta")]
        public string? Delta { get; init; }
    }

    /// <summary>Emitted when a tool call completes.</summary>
    public record ToolCallEndEvent : BaseEvent
    {
        /// <summary>Tool call that has completed.</summary>
        [JsonPropertyName("tool_call_id")]
        public string ToolCallId { get; init; } = default!;
    }

    /// <summary>Emitted when a tool requires user approval (Umbraco-specific).</summary>
    public record ApprovalRequestedEvent : BaseEvent
    {
        /// <summary>Unique identifier for this approval request.</summary>
        [JsonPropertyName("approval_id")]
        public Guid ApprovalId { get; init; }

        /// <summary>Tool requesting approval.</summary>
        /// <example>PublishContent</example>
        [JsonPropertyName("tool_id")]
        public string ToolId { get; init; } = default!;

        /// <summary>Human-readable tool name.</summary>
        /// <example>Publish Content</example>
        [JsonPropertyName("tool_name")]
        public string ToolName { get; init; } = default!;

        /// <summary>Tool parameters requiring approval.</summary>
        [JsonPropertyName("parameters")]
        public object Parameters { get; init; } = default!;

        /// <summary>Human-readable summary of what will happen.</summary>
        /// <example>Publish "Home Page" and 3 child pages</example>
        [JsonPropertyName("parameters_summary")]
        public string? ParametersSummary { get; init; }
    }
}
```

#### 2. Swashbuckle Document Filter

**File:** `src/Umbraco.Ai.Agents/Api/OpenApi/AgUiDocumentFilter.cs`

```csharp
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Umbraco.Ai.Agents.Api.OpenApi;

/// <summary>
/// Adds AG-UI event schemas to the OpenAPI document.
/// </summary>
public class AgUiDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        // Register all AG-UI event schemas
        context.SchemaGenerator.GenerateSchema(
            typeof(AgUiEventSchemas.RunStartedEvent), context.SchemaRepository);
        context.SchemaGenerator.GenerateSchema(
            typeof(AgUiEventSchemas.RunFinishedEvent), context.SchemaRepository);
        context.SchemaGenerator.GenerateSchema(
            typeof(AgUiEventSchemas.RunErrorEvent), context.SchemaRepository);
        context.SchemaGenerator.GenerateSchema(
            typeof(AgUiEventSchemas.TextMessageStartEvent), context.SchemaRepository);
        context.SchemaGenerator.GenerateSchema(
            typeof(AgUiEventSchemas.TextMessageContentEvent), context.SchemaRepository);
        context.SchemaGenerator.GenerateSchema(
            typeof(AgUiEventSchemas.TextMessageEndEvent), context.SchemaRepository);
        context.SchemaGenerator.GenerateSchema(
            typeof(AgUiEventSchemas.ToolCallStartEvent), context.SchemaRepository);
        context.SchemaGenerator.GenerateSchema(
            typeof(AgUiEventSchemas.ToolCallArgsEvent), context.SchemaRepository);
        context.SchemaGenerator.GenerateSchema(
            typeof(AgUiEventSchemas.ToolCallEndEvent), context.SchemaRepository);
        context.SchemaGenerator.GenerateSchema(
            typeof(AgUiEventSchemas.ApprovalRequestedEvent), context.SchemaRepository);

        // Add AG-UI info to document extensions
        swaggerDoc.Extensions["x-ag-ui-protocol"] = new OpenApiObject
        {
            ["version"] = new OpenApiString("1.0"),
            ["documentation"] = new OpenApiString("https://docs.ag-ui.com"),
            ["events"] = new OpenApiArray
            {
                new OpenApiString("run_started"),
                new OpenApiString("run_finished"),
                new OpenApiString("run_error"),
                new OpenApiString("text_message_start"),
                new OpenApiString("text_message_content"),
                new OpenApiString("text_message_end"),
                new OpenApiString("tool_call_start"),
                new OpenApiString("tool_call_args"),
                new OpenApiString("tool_call_end"),
                new OpenApiString("approval_requested"),
            }
        };
    }
}
```

#### 3. Operation Filter for SSE Endpoints

**File:** `src/Umbraco.Ai.Agents/Api/OpenApi/AgUiOperationFilter.cs`

```csharp
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Umbraco.Ai.Agents.Api.OpenApi;

/// <summary>
/// Adds AG-UI metadata to streaming endpoints.
/// </summary>
public class AgUiOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Check if this is an AG-UI streaming endpoint
        var hasAgUiAttribute = context.MethodInfo
            .GetCustomAttributes(typeof(AgUiEndpointAttribute), true)
            .Any();

        if (!hasAgUiAttribute)
            return;

        // Add AG-UI extension with event schema references
        operation.Extensions["x-ag-ui-events"] = new OpenApiArray
        {
            new OpenApiObject
            {
                ["type"] = new OpenApiString("run_started"),
                ["$ref"] = new OpenApiString("#/components/schemas/RunStartedEvent")
            },
            new OpenApiObject
            {
                ["type"] = new OpenApiString("run_finished"),
                ["$ref"] = new OpenApiString("#/components/schemas/RunFinishedEvent")
            },
            new OpenApiObject
            {
                ["type"] = new OpenApiString("run_error"),
                ["$ref"] = new OpenApiString("#/components/schemas/RunErrorEvent")
            },
            new OpenApiObject
            {
                ["type"] = new OpenApiString("text_message_start"),
                ["$ref"] = new OpenApiString("#/components/schemas/TextMessageStartEvent")
            },
            new OpenApiObject
            {
                ["type"] = new OpenApiString("text_message_content"),
                ["$ref"] = new OpenApiString("#/components/schemas/TextMessageContentEvent")
            },
            new OpenApiObject
            {
                ["type"] = new OpenApiString("text_message_end"),
                ["$ref"] = new OpenApiString("#/components/schemas/TextMessageEndEvent")
            },
            new OpenApiObject
            {
                ["type"] = new OpenApiString("tool_call_start"),
                ["$ref"] = new OpenApiString("#/components/schemas/ToolCallStartEvent")
            },
            new OpenApiObject
            {
                ["type"] = new OpenApiString("tool_call_args"),
                ["$ref"] = new OpenApiString("#/components/schemas/ToolCallArgsEvent")
            },
            new OpenApiObject
            {
                ["type"] = new OpenApiString("tool_call_end"),
                ["$ref"] = new OpenApiString("#/components/schemas/ToolCallEndEvent")
            },
            new OpenApiObject
            {
                ["type"] = new OpenApiString("approval_requested"),
                ["$ref"] = new OpenApiString("#/components/schemas/ApprovalRequestedEvent")
            },
        };

        // Enhance description
        operation.Description = $"""
            {operation.Description}

            **AG-UI Protocol**: This endpoint streams Server-Sent Events (SSE) following the AG-UI protocol.
            Each event is a JSON object with a `type` discriminator. See the `x-ag-ui-events` extension
            for event schemas.

            **Event Flow**:
            1. `run_started` - Stream begins
            2. `text_message_start` - New message from assistant
            3. `text_message_content` (repeated) - Message content chunks
            4. `tool_call_*` events if tools are invoked
            5. `approval_requested` if tool requires user approval
            6. `text_message_end` - Message complete
            7. `run_finished` or `run_error` - Stream ends
            """;
    }
}

/// <summary>
/// Marks an endpoint as an AG-UI streaming endpoint for OpenAPI documentation.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class AgUiEndpointAttribute : Attribute { }
```

#### 4. Request/Response Models

**File:** `src/Umbraco.Ai.Agents/Api/Models/AgentChatRequestModel.cs`

```csharp
using System.ComponentModel.DataAnnotations;

namespace Umbraco.Ai.Agents.Api.Models;

/// <summary>
/// Request model for AG-UI agent chat endpoint.
/// </summary>
public class AgentChatRequestModel
{
    /// <summary>
    /// Conversation messages in AG-UI format.
    /// </summary>
    [Required]
    public required IReadOnlyList<AgentChatMessage> Messages { get; init; }

    /// <summary>
    /// Optional thread ID for conversation continuity.
    /// </summary>
    public string? ThreadId { get; init; }

    /// <summary>
    /// Optional run configuration.
    /// </summary>
    public AgentRunOptions? Options { get; init; }
}

/// <summary>
/// A message in the conversation.
/// </summary>
public class AgentChatMessage
{
    /// <summary>
    /// Role of the message author.
    /// </summary>
    /// <example>user</example>
    [Required]
    public required string Role { get; init; }

    /// <summary>
    /// Message content.
    /// </summary>
    /// <example>What content do we have about products?</example>
    public string? Content { get; init; }

    /// <summary>
    /// Optional message ID for correlation.
    /// </summary>
    public string? Id { get; init; }
}

/// <summary>
/// Optional configuration for the agent run.
/// </summary>
public class AgentRunOptions
{
    /// <summary>
    /// Whether to allow tool execution without approval.
    /// </summary>
    public bool? AutoApproveSafeTools { get; init; }

    /// <summary>
    /// Maximum number of tool call iterations.
    /// </summary>
    public int? MaxIterations { get; init; }
}
```

#### 5. Updated Controller with OpenAPI Attributes

**File:** `src/Umbraco.Ai.Agents/Api/Controllers/AgentChatController.cs`

```csharp
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Agents.Api.Models;
using Umbraco.Ai.Agents.Api.OpenApi;

namespace Umbraco.Ai.Agents.Api.Controllers;

[UmbracoAiVersionedManagementApiRoute("agents")]
[ApiExplorerSettings(GroupName = "Umbraco AI Agents")]
public class AgentChatController : AgentControllerBase
{
    private readonly UmbracoAgentFactory _agentFactory;

    public AgentChatController(UmbracoAgentFactory agentFactory)
    {
        _agentFactory = agentFactory;
    }

    /// <summary>
    /// Stream an AI agent chat session.
    /// </summary>
    /// <remarks>
    /// Initiates a streaming chat session with an Umbraco AI agent. The response
    /// is a Server-Sent Events (SSE) stream following the AG-UI protocol.
    ///
    /// The agent can search content, execute tools, and may request approval
    /// for destructive operations like publishing or deleting content.
    /// </remarks>
    /// <param name="agentIdOrAlias">Agent ID (GUID) or alias</param>
    /// <param name="request">Chat request with conversation messages</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>SSE stream of AG-UI events</returns>
    [HttpPost("{agentIdOrAlias}/chat/stream")]
    [Produces("text/event-stream")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [AgUiEndpoint]
    public async Task<IResult> StreamChat(
        string agentIdOrAlias,
        [FromBody] AgentChatRequestModel request,
        CancellationToken ct)
    {
        var agent = await ResolveAgentAsync(agentIdOrAlias, ct);
        if (agent is null)
            return Results.NotFound();

        var chatClientAgent = await _agentFactory.CreateAgentAsync(agent, ct);

        // Convert to Microsoft's RunAgentInput
        var input = MapToRunAgentInput(request);

        // Microsoft's MapAGUI handles all event conversion
        return Results.Extensions.AGUI(chatClientAgent, input);
    }

    private static RunAgentInput MapToRunAgentInput(AgentChatRequestModel request)
    {
        // Map from our model to Microsoft's expected input
        return new RunAgentInput
        {
            Messages = request.Messages.Select(m => new ChatMessage
            {
                Role = m.Role,
                Content = m.Content
            }).ToList(),
            ThreadId = request.ThreadId
        };
    }
}
```

#### 6. Register OpenAPI Filters

**File:** `src/Umbraco.Ai.Agents/Configuration/AgentsSwaggerConfiguration.cs`

```csharp
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.SwaggerGen;
using Umbraco.Ai.Agents.Api.OpenApi;

namespace Umbraco.Ai.Agents.Configuration;

/// <summary>
/// Extension methods for configuring Swagger/OpenAPI for Agents.
/// </summary>
public static class AgentsSwaggerConfiguration
{
    /// <summary>
    /// Adds AG-UI schema documentation to Swagger.
    /// </summary>
    public static SwaggerGenOptions AddAgUiSchemas(this SwaggerGenOptions options)
    {
        options.DocumentFilter<AgUiDocumentFilter>();
        options.OperationFilter<AgUiOperationFilter>();

        return options;
    }
}
```

**Usage in Composer:**

```csharp
// In UmbracoAiAgentsComposer or startup
services.Configure<SwaggerGenOptions>(options =>
{
    options.AddAgUiSchemas();
});
```

### Generated OpenAPI Output

The above configuration produces OpenAPI documentation like:

```yaml
paths:
  /umbraco/ai/management/api/v1/agents/{agentIdOrAlias}/chat/stream:
    post:
      summary: Stream an AI agent chat session.
      description: |
        Initiates a streaming chat session with an Umbraco AI agent...

        **AG-UI Protocol**: This endpoint streams Server-Sent Events (SSE)...
      produces:
        - text/event-stream
      x-ag-ui-events:
        - type: run_started
          $ref: '#/components/schemas/RunStartedEvent'
        - type: text_message_content
          $ref: '#/components/schemas/TextMessageContentEvent'
        # ... etc

components:
  schemas:
    RunStartedEvent:
      type: object
      properties:
        type:
          type: string
          example: run_started
        run_id:
          type: string
          format: uuid
        timestamp:
          type: integer
          format: int64

    TextMessageContentEvent:
      type: object
      properties:
        type:
          type: string
          example: text_message_content
        message_id:
          type: string
        delta:
          type: string
          example: "Hello, how can I"
    # ... all other event schemas

x-ag-ui-protocol:
  version: "1.0"
  documentation: https://docs.ag-ui.com
  events:
    - run_started
    - run_finished
    - run_error
    - text_message_start
    - text_message_content
    - text_message_end
    - tool_call_start
    - tool_call_args
    - tool_call_end
    - approval_requested
```

### Files Summary (OpenAPI)

| File | Purpose |
|------|---------|
| `Api/OpenApi/AgUiOpenApiExtensions.cs` | Helper for building `x-ag-ui-events` and `x-ag-ui-protocol` |
| `Api/OpenApi/AgUiEventSchemas.cs` | Schema models for OpenAPI generation |
| `Api/OpenApi/AgUiDocumentFilter.cs` | Registers event schemas in OpenAPI doc |
| `Api/OpenApi/AgUiOperationFilter.cs` | Adds AG-UI metadata to endpoints (Option B only) |
| `Api/Models/AgentChatRequestModel.cs` | Request model with XML docs |
| `Configuration/AgentsSwaggerConfiguration.cs` | SwaggerGen extension method |

### Integration with MapAGUI()

The key insight is that `Results.Extensions.AGUI()` handles all the SSE streaming and AG-UI event conversion automatically. Our OpenAPI additions are purely for **documentation** - they don't affect runtime behavior.

```
┌─────────────────────────────────────────────────────────────────┐
│  Runtime: Results.Extensions.AGUI(chatClientAgent, input)      │
│  - Microsoft handles SSE streaming                              │
│  - Microsoft handles AG-UI event conversion                     │
│  - We just call it and return the result                        │
└─────────────────────────────────────────────────────────────────┘
                              +
┌─────────────────────────────────────────────────────────────────┐
│  Documentation: .WithOpenApi() + AgUiDocumentFilter             │
│  - We add endpoint metadata via fluent API                      │
│  - Document filter registers event schemas                      │
│  - x-ag-ui-events extension links events to schemas             │
└─────────────────────────────────────────────────────────────────┘
```

This separation means:
1. **If Microsoft changes event format** - Runtime automatically updates, docs need manual update
2. **If we need custom events** (like `approval_requested`) - Add to our schema models and extension helper
3. **OpenAPI consumers** - Can see the event schemas even though SSE doesn't normally document them

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
