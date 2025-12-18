# AG-UI Event Protocol Integration Plan

## Overview

This plan introduces AG-UI (Agent User Interface Protocol) event patterns into Umbraco.Ai's streaming APIs. By adopting AG-UI conventions now, we create consistency between the existing Management API and future Agent API, enabling a natural progression as agentic features are added.

## Background

### What is AG-UI?

AG-UI is an open, event-based protocol that standardizes communication between AI agents and user-facing applications. Key concepts:

- **Event-driven streaming** - All communication via typed events (not raw text)
- **Lifecycle events** - Explicit run start/end, message start/end boundaries
- **Tool events** - Standardized tool call/result flow
- **Interrupts** - Human-in-the-loop approval patterns
- **Shared state** - Bidirectional context synchronization

Documentation: https://docs.ag-ui.com

### .NET SDK Options

There are two emerging .NET implementations for AG-UI:

#### Option A: AG-UI Community SDK (PR #38)

**Source:** https://github.com/ag-ui-protocol/ag-ui/pull/38

The community SDK provides:
- **All event types** as C# records with proper JSON polymorphic serialization
- **`IAGUIAgent` interface** - Simple contract for agent implementations
- **`ChatClientAgent`** - Full implementation wrapping M.E.AI's `IChatClient`
- **ASP.NET integration** - `MapAgentEndpoint()` route builder extension
- **Frontend/backend tool support** - Distinguishes tools executed by UI vs agent

**Relevance to Umbraco.Ai:**
- Built directly on M.E.AI's `IChatClient` - exact match for our architecture
- `ChatClientAgent` handles all streaming → AG-UI event conversion
- No additional abstraction layers

**Event naming:** `SCREAMING_CASE` (e.g., `TEXT_MESSAGE_CONTENT`)

#### Option B: Microsoft Agent Framework AG-UI

**Source:** https://github.com/microsoft/agent-framework/issues/1774

Microsoft is adding official AG-UI support via two packages:
- **`Microsoft.Agents.AI.AGUI`** - Client for consuming AG-UI servers
- **`Microsoft.Agents.AI.Hosting.AGUI.AspNetCore`** - Server-side ASP.NET Core integration

**Key Components:**
- **`AGUIAgent`** - Client implementation that communicates with AG-UI servers
- **`AGUIAgentThread`** - Conversation thread with persistent ID
- **`MapAGUIAgent()`** - ASP.NET Core endpoint extension

**Relevance to Umbraco.Ai:**
- Official Microsoft support (likely long-term investment)
- Built on M.E.AI, but via `AIAgent` abstraction (not `IChatClient` directly)
- `AIAgent` wraps `IChatClient`, adding conversation management and tool orchestration
- May become the standard .NET approach for agents
- Adds another abstraction layer between us and `IChatClient`

**Event naming:** `snake_case` (e.g., `text_message_content`)

#### Comparison

**Important:** Both SDKs are built on M.E.AI (`Microsoft.Extensions.AI`). The difference is the abstraction level, not the foundation.

| Aspect | Community SDK (PR #38) | Microsoft Agent Framework |
|--------|------------------------|---------------------------|
| Foundation | M.E.AI | M.E.AI |
| Primary abstraction | `IChatClient` directly | `AIAgent` (wraps `IChatClient`) |
| Abstraction level | Thin - minimal overhead | Thicker - additional concepts |
| Maturity | PR pending | Issue tracking, early |
| Maintenance | Community | Microsoft |
| Event naming | `SCREAMING_CASE` | `snake_case` |
| Package | `AGUIDotnet` | `Microsoft.Agents.AI.*` |

**Recommendation:** Use the **Microsoft Agent Framework** because:
1. **Both SDKs provide `ChatClientAgent`** that wraps `IChatClient` directly - same integration effort
2. Official Microsoft support with long-term investment
3. Umbraco is a Microsoft ecosystem product - consistent tooling for .NET developers
4. Additional features included: OpenTelemetry, memory providers, workflow support
5. Broader ecosystem integration (Copilot Studio, Azure AI, A2A protocol)

### Why Adopt AG-UI Patterns?

1. **Future-proofing** - Agent API will need these patterns; adopt them now
2. **Single vocabulary** - Management API and Agent API speak the same language
3. **Official SDK** - Use maintained types rather than rolling our own
4. **M.E.AI integration** - SDK already integrates with `IChatClient`
5. **Ecosystem alignment** - Compatible with AG-UI tooling and frontends

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

## Implementation Options

### Option A: Microsoft Agent Framework (Recommended)

```xml
<PackageReference Include="Microsoft.Agents.AI" Version="x.x.x" />
<PackageReference Include="Microsoft.Agents.AI.Hosting.AGUI.AspNetCore" Version="x.x.x" />
```

**Advantages:**
- **`ChatClientAgent` wraps `IChatClient` directly** - same pattern as Umbraco.Ai
- Official Microsoft support with long-term investment
- Consistent with Microsoft/.NET ecosystem (natural for Umbraco developers)
- Built-in OpenTelemetry, logging, memory providers
- Broader ecosystem: Copilot Studio, Azure AI, A2A protocol support
- ASP.NET integration via `MapAGUI()` extension

**Disadvantages:**
- Still in active development
- Event naming uses `snake_case` (AG-UI spec prefers this)

### Option B: AG-UI Community SDK

Once PR #38 is merged and package is published:

```xml
<PackageReference Include="AGUIDotnet" Version="1.0.0" />
```

**Advantages:**
- `ChatClientAgent` also wraps `IChatClient` directly
- Simpler, focused solely on AG-UI
- Lighter weight (fewer dependencies)

**Disadvantages:**
- Community maintained (not Microsoft)
- May need to track upstream changes
- Event naming uses `SCREAMING_CASE` (differs from AG-UI spec)

### Option C: Custom Implementation (Fallback)

Implement our own AG-UI-compatible event types. Use this if:
- Neither SDK is available when we need to ship
- SDKs don't meet specific Umbraco requirements
- We need full control over the implementation

**Note:** If using custom implementation, align with one of the SDK's event naming conventions to ease future migration.

## Proposed Design (Microsoft Agent Framework)

### Architecture

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
│     Microsoft.Agents.AI.Hosting.AGUI.AspNetCore                 │
│   MapAGUI() - converts AIAgent streams to AG-UI SSE             │
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

**Key insight:** Microsoft's `ChatClientAgent` wraps `IChatClient` directly, so Umbraco.Ai's existing architecture remains unchanged. We simply wrap our `IChatClient` instances when exposing AG-UI endpoints.

### Microsoft ChatClientAgent

Microsoft's `ChatClientAgent` wraps `IChatClient` directly:

```csharp
public sealed class ChatClientAgent : AIAgent
{
    // Constructor takes IChatClient directly - exactly what Umbraco.Ai uses
    public ChatClientAgent(
        IChatClient chatClient,
        string? instructions = null,
        string? name = null,
        string? description = null,
        IList<AITool>? tools = null,
        ILoggerFactory? loggerFactory = null,
        IServiceProvider? services = null)

    // Exposes the underlying IChatClient
    public IChatClient ChatClient { get; }

    // Streaming implementation converts IChatClient streams to AgentRunResponseUpdate
    public override IAsyncEnumerable<AgentRunResponseUpdate> RunStreamingAsync(
        IEnumerable<ChatMessage> messages,
        AgentThread? thread = null,
        AgentRunOptions? options = null,
        CancellationToken cancellationToken = default)
}
```

### Microsoft AG-UI ASP.NET Integration

```csharp
// Microsoft.Agents.AI.Hosting.AGUI.AspNetCore
public static class AGUIEndpointRouteBuilderExtensions
{
    public static IEndpointConventionBuilder MapAGUI(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        AIAgent aiAgent)
    {
        return endpoints.MapPost(pattern, async (RunAgentInput? input, HttpContext context, ...) =>
        {
            // Converts agent streaming to AG-UI SSE events automatically
            var events = aiAgent.RunStreamingAsync(messages, options: runOptions, ...)
                .AsAGUIEventStreamAsync(...);

            return new AGUIServerSentEventsResult(events);
        });
    }
}
```

## Implementation with Microsoft Agent Framework

### 1. Create Umbraco Agent Factory

**File:** `src/Umbraco.Ai.Web/Api/Agents/UmbracoAgentFactory.cs`

```csharp
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Umbraco.Ai.Core.Chat;
using Umbraco.Ai.Core.Profiles;

namespace Umbraco.Ai.Web.Api.Agents;

/// <summary>
/// Factory for creating ChatClientAgent instances from Umbraco.Ai profiles.
/// </summary>
public class UmbracoAgentFactory
{
    private readonly IAiChatClientFactory _chatClientFactory;
    private readonly IAiProfileService _profileService;
    private readonly ILoggerFactory _loggerFactory;

    public UmbracoAgentFactory(
        IAiChatClientFactory chatClientFactory,
        IAiProfileService profileService,
        ILoggerFactory loggerFactory)
    {
        _chatClientFactory = chatClientFactory;
        _profileService = profileService;
        _loggerFactory = loggerFactory;
    }

    /// <summary>
    /// Creates a ChatClientAgent from an Umbraco.Ai profile.
    /// </summary>
    public async Task<ChatClientAgent> CreateAgentAsync(
        Guid profileId,
        IList<AITool>? tools = null,
        CancellationToken ct = default)
    {
        var profile = await _profileService.GetAsync(profileId, ct)
            ?? throw new InvalidOperationException($"Profile {profileId} not found");

        var chatClient = await _chatClientFactory.CreateClientAsync(profileId, ct);

        // Wrap our IChatClient in Microsoft's ChatClientAgent
        return new ChatClientAgent(
            chatClient,
            instructions: profile.SystemPrompt,
            name: profile.Name,
            description: $"Umbraco AI Agent: {profile.Alias}",
            tools: tools,
            loggerFactory: _loggerFactory);
    }
}
```

### 2. Register Agent Endpoints

**File:** `src/Umbraco.Ai.Web/Api/Agents/AgentEndpointExtensions.cs`

```csharp
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Umbraco.Ai.Core.Chat;
using Umbraco.Ai.Core.Profiles;

namespace Umbraco.Ai.Web.Api.Agents;

public static class AgentEndpointExtensions
{
    public static IEndpointRouteBuilder MapUmbracoAiAgents(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/umbraco/ai/api/v1/agents");

        // Default chat agent using default profile
        group.MapPost("chat", async (
            RunAgentInput input,
            IAiChatService chatService,
            ILoggerFactory loggerFactory,
            HttpContext context,
            CancellationToken ct) =>
        {
            var chatClient = chatService.CreateDefaultClient();
            var agent = new ChatClientAgent(chatClient, loggerFactory: loggerFactory);

            // Use Microsoft's AG-UI result handler
            return Results.Extensions.AGUI(agent, input);
        });

        // Profile-specific agent endpoint
        group.MapPost("{profileIdOrAlias}", async (
            string profileIdOrAlias,
            RunAgentInput input,
            UmbracoAgentFactory agentFactory,
            IAiProfileService profileService,
            HttpContext context,
            CancellationToken ct) =>
        {
            var profileId = await profileService.GetProfileIdAsync(
                new IdOrAlias(profileIdOrAlias), ct);

            var agent = await agentFactory.CreateAgentAsync(profileId, ct);

            return Results.Extensions.AGUI(agent, input);
        });

        return builder;
    }
}
```

### 3. Frontend TypeScript Types

Use types matching Microsoft's `snake_case` convention (AG-UI spec standard):

**File:** `src/Umbraco.Ai.Web.StaticAssets/Client/src/api/events.ts`

```typescript
/**
 * AG-UI event types matching Microsoft Agent Framework.
 * Uses snake_case as per AG-UI specification.
 * See: https://github.com/microsoft/agent-framework
 */

interface BaseEvent {
  type: string;
  timestamp?: number;
}

// Lifecycle events
export interface RunStartedEvent extends BaseEvent {
  type: 'run_started';
  run_id?: string;
  thread_id?: string;
}

export interface RunFinishedEvent extends BaseEvent {
  type: 'run_finished';
  run_id?: string;
  thread_id?: string;
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

// Tool events
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

// State events
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

// Union type
export type AgUiEvent =
  | RunStartedEvent
  | RunFinishedEvent
  | RunErrorEvent
  | TextMessageStartEvent
  | TextMessageContentEvent
  | TextMessageEndEvent
  | ToolCallStartEvent
  | ToolCallArgsEvent
  | ToolCallEndEvent
  | StateSnapshotEvent
  | StateDeltaEvent
  | MessagesSnapshotEvent;

// Type guards
export const isRunStarted = (e: AgUiEvent): e is RunStartedEvent => e.type === 'run_started';
export const isRunFinished = (e: AgUiEvent): e is RunFinishedEvent => e.type === 'run_finished';
export const isRunError = (e: AgUiEvent): e is RunErrorEvent => e.type === 'run_error';
export const isTextMessageContent = (e: AgUiEvent): e is TextMessageContentEvent =>
  e.type === 'text_message_content';
export const isToolCallStart = (e: AgUiEvent): e is ToolCallStartEvent =>
  e.type === 'tool_call_start';
```

## Migration Steps

### Phase 1: Add Microsoft Agent Framework

1. **Add NuGet package references**
   ```xml
   <PackageReference Include="Microsoft.Agents.AI" Version="x.x.x" />
   <PackageReference Include="Microsoft.Agents.AI.Hosting.AGUI.AspNetCore" Version="x.x.x" />
   ```

2. **Create UmbracoAgentFactory**
   - Wrap `IChatClient` from `IAiChatClientFactory` in `ChatClientAgent`
   - Integrate with profile system for instructions/settings

3. **Register AG-UI Endpoints**
   - Use `MapAGUI()` extension method
   - Configure authentication/authorization via Umbraco backoffice security

4. **Update Frontend**
   - Add TypeScript types for AG-UI events (`snake_case`)
   - Update event handlers for new event structure

### Phase 2: Agent Features

1. **Add Umbraco Tools**
   - Content tools (search, create, update, publish)
   - Media tools
   - Navigation tools
   - Register via `ChatClientAgent` tools parameter

2. **Register Tools in Agent Factory**
   ```csharp
   var tools = new List<AITool>
   {
       AIFunctionFactory.Create(SearchContent),
       AIFunctionFactory.Create(GetContent),
       AIFunctionFactory.Create(UpdateContent),
   };

   return new ChatClientAgent(chatClient, tools: tools, ...);
   ```

3. **Frontend Tool UI**
   - Handle `tool_call_*` events
   - Implement approval UI for destructive tools
   - Add state synchronization via `state_*` events

### Phase 3: Advanced Features

1. **Memory/Context Providers**
   - Leverage Microsoft's `AIContextProvider` for RAG
   - Integrate with Umbraco content search

2. **OpenTelemetry Integration**
   - Use Microsoft's built-in `OpenTelemetryAgent` wrapper
   - Configure tracing for agent interactions

3. **Multi-Agent Scenarios**
   - Explore A2A (Agent-to-Agent) protocol support
   - Consider Copilot Studio integration

## SDK Comparison Summary

| Aspect | Microsoft Agent Framework | Custom Implementation |
|--------|---------------------------|----------------------|
| IChatClient wrapping | `ChatClientAgent` included | Must implement |
| ASP.NET integration | `MapAGUI()` included | Must implement |
| Event serialization | Handled automatically | Must configure |
| Tool support | Built-in via M.E.AI | Must implement |
| OpenTelemetry | Built-in | Must implement |
| Memory providers | Built-in | Must implement |
| Maintenance | Microsoft | Self-maintained |

## Recommendation

**Use Microsoft Agent Framework** because:

1. **`ChatClientAgent` wraps `IChatClient` directly** - perfect fit for Umbraco.Ai's architecture
2. **Official Microsoft support** - long-term investment, consistent with .NET ecosystem
3. **Built-in features** - OpenTelemetry, logging, memory providers included
4. **Broader ecosystem** - Copilot Studio, Azure AI, A2A protocol integration
5. **Standard AG-UI** - uses `snake_case` matching the AG-UI specification

**Community SDK alternative** if:
- Microsoft packages are unavailable or unstable
- Lighter weight solution needed (fewer dependencies)
- Specific customization requirements not met by Microsoft

## Future Extensions

### Umbraco-Specific Tools

Register Umbraco tools via `ChatClientAgent`:

```csharp
public async Task<ChatClientAgent> CreateAgentAsync(Guid profileId, CancellationToken ct)
{
    var chatClient = await _chatClientFactory.CreateClientAsync(profileId, ct);
    var profile = await _profileService.GetAsync(profileId, ct);

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
        name: profile?.Name,
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
