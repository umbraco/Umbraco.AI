# AGUI Implementation Refactoring Plan

## Executive Summary

Refactor the AGUI implementation by creating a shared `Umbraco.Ai.Agui` protocol library that can be used by both `Umbraco.Ai.Web` (generic chat) and `Umbraco.Ai.Agent.Web` (agent orchestration). This provides full AG-UI protocol compliance, plugin architecture extensibility, tool support, conversation persistence, and human-in-the-loop capabilities.

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                      Umbraco.Ai.Agui (NEW)                      │
│                    (Shared AG-UI Protocol)                       │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐ │
│  │ AG-UI Events    │  │ AG-UI Models    │  │ SSE Streaming   │ │
│  │ (17 event types)│  │ (protocol DTOs) │  │ Infrastructure  │ │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘ │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐ │
│  │ JSON Patch      │  │ Event Emitter   │  │ Middleware      │ │
│  │ (state deltas)  │  │ & Serializer    │  │ Abstractions    │ │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
                    ▲                       ▲
                    │                       │
        ┌───────────┴───────────┐  ┌───────┴───────────────┐
        │                       │  │                       │
┌───────┴───────────────┐  ┌────┴──┴───────────────────────┐
│   Umbraco.Ai.Web      │  │   Umbraco.Ai.Agent.Web        │
│   (Generic Chat)      │  │   (Agent Orchestration)       │
│                       │  │                               │
│ • StreamChatController│  │ • Agent Resolution            │
│   (enhanced w/ AG-UI) │  │ • Agent Middleware Pipeline   │
│ • Profile-based chat  │  │ • Tools System                │
│ • Simple streaming    │  │ • Conversation Persistence    │
└───────────────────────┘  │ • Human-in-the-loop           │
                           └───────────────────────────────┘
```

---

## Current State Issues

| Issue | Location | Impact |
|-------|----------|--------|
| Stateless operation | `AgentStreamResult.cs:71` - `_agent.GetNewThread()` | No conversation continuity |
| No tool support | `UmbracoAgentFactory.cs:41` - `tools: null` | Agents cannot use tools |
| Limited events | `AgentStreamResult.cs` | Only 5 of 17 AG-UI events |
| Single agent source | `AgentResolver.cs` | Only database agents |
| No state sync | Missing | No STATE_SNAPSHOT/DELTA |
| Duplicate potential | AG-UI models in Agent.Web only | Can't reuse for generic chat |

---

## Requirements

- **Resolution**: Mixed (database + code) + Plugin-based agents
- **Features**: Tool calling, State sync, Conversation persistence, Human-in-the-loop
- **Extensibility**: Full plugin architecture
- **Reusability**: Shared AG-UI protocol for both chat and agent endpoints

---

## Project Structure

### New Project: Umbraco.Ai.Agui

**Location**: `D:\Work\Umbraco\Umbraco.Ai\Umbraco.Ai\src\Umbraco.Ai.Agui`

```
Umbraco.Ai.Agui/
├── Events/
│   ├── AgentEventType.cs              # Enum of all 17 AG-UI event types
│   ├── IAgentEvent.cs                 # Base event interface
│   ├── BaseAgentEvent.cs              # Abstract base with timestamp
│   ├── Lifecycle/
│   │   ├── RunStartedEvent.cs
│   │   ├── RunFinishedEvent.cs
│   │   ├── RunErrorEvent.cs
│   │   ├── StepStartedEvent.cs
│   │   └── StepFinishedEvent.cs
│   ├── Messages/
│   │   ├── TextMessageStartEvent.cs
│   │   ├── TextMessageContentEvent.cs
│   │   └── TextMessageEndEvent.cs
│   ├── Tools/
│   │   ├── ToolCallStartEvent.cs
│   │   ├── ToolCallArgsEvent.cs
│   │   ├── ToolCallEndEvent.cs
│   │   └── ToolCallResultEvent.cs
│   └── State/
│       ├── StateSnapshotEvent.cs
│       ├── StateDeltaEvent.cs
│       └── MessagesSnapshotEvent.cs
├── Models/
│   ├── AguiRunRequest.cs              # AG-UI protocol request
│   ├── AguiMessage.cs                 # Message with role, content, tool calls
│   ├── AguiToolCall.cs                # Tool call model
│   ├── AguiFunctionCall.cs            # Function name and arguments
│   ├── AguiTool.cs                    # Tool definition
│   ├── AguiContextItem.cs             # Context item
│   └── AguiRunOutcome.cs              # Success, Interrupt enum
├── State/
│   ├── IJsonPatchService.cs
│   ├── JsonPatchService.cs            # RFC 6902 implementation
│   ├── JsonPatchOperation.cs          # Patch operation model
│   └── StateChangeTracker.cs          # Tracks state for delta computation
├── Streaming/
│   ├── IAguiEventEmitter.cs           # Event emission interface
│   ├── AguiEventEmitter.cs            # Wraps execution with events
│   ├── AguiEventSerializer.cs         # JSON serialization for SSE
│   ├── AguiStreamResult.cs            # Base IResult for SSE streaming
│   └── AguiStreamOptions.cs           # Streaming configuration
├── Middleware/
│   ├── IAguiMiddleware.cs             # Base middleware interface
│   ├── AguiMiddlewareContext.cs       # Middleware context
│   └── AguiMiddlewarePipeline.cs      # Pipeline executor
├── Configuration/
│   └── UmbracoBuilderExtensions.cs    # builder.AddUmbracoAiAgui()
├── Constants.cs
└── Umbraco.Ai.Agui.csproj
```

### Updated: Umbraco.Ai.Web

**Changes**: Enhance `StreamChatController` to use AG-UI events from `Umbraco.Ai.Agui`

```
Umbraco.Ai.Web/
├── Api/Management/Chat/
│   ├── Controllers/
│   │   ├── StreamChatController.cs    # MODIFIED: Use AguiStreamResult
│   │   └── CompleteChatController.cs  # Unchanged
│   └── Models/
│       └── ChatStreamResult.cs        # NEW: Extends AguiStreamResult
```

### Updated: Umbraco.Ai.Agent.Web

**Changes**: Remove duplicate AG-UI models, reference `Umbraco.Ai.Agui`

```
Umbraco.Ai.Agent.Web/
├── Api/Management/Agent/
│   ├── Endpoints/
│   │   ├── AgentEndpoints.cs          # Unchanged
│   │   ├── RunAgentRequest.cs         # REMOVED (use AguiRunRequest)
│   │   └── AgentStreamResult.cs       # MODIFIED: Extends AguiStreamResult
│   ├── Execution/
│   │   ├── IAgentResolver.cs          # Unchanged
│   │   ├── CompositeAgentResolver.cs  # NEW: Multi-source resolution
│   │   ├── DatabaseAgentResolver.cs   # NEW: Wraps IAiAgentService
│   │   ├── CodeAgentResolver.cs       # NEW: Code-registered agents
│   │   ├── IUmbracoAgentFactory.cs    # Unchanged
│   │   └── UmbracoAgentFactory.cs     # MODIFIED: Add tool support
│   ├── Protocol/
│   │   └── AgentEventInterceptor.cs   # NEW: Intercepts tool calls
│   └── Middleware/
│       ├── AgentRunMiddleware.cs      # NEW: Agent-specific middleware
│       └── HumanApprovalMiddleware.cs # NEW: HITL support
```

### New: Umbraco.Ai.Agent.Core (Additions)

```
Umbraco.Ai.Agent.Core/
├── Tools/
│   ├── IAiAgentTool.cs
│   ├── AiAgentToolAttribute.cs
│   ├── AiAgentToolBase.cs
│   ├── AgentToolDefinition.cs
│   ├── AgentToolResult.cs
│   ├── IAgentToolProvider.cs
│   ├── CompositeToolProvider.cs
│   ├── IAgentToolFilter.cs
│   ├── AgentToolScopeProvider.cs
│   └── Collections/
│       ├── AiAgentToolCollectionBuilder.cs
│       └── AiAgentToolCollection.cs
├── Providers/
│   ├── IAiAgentProvider.cs
│   ├── IAgentDefinition.cs
│   ├── AiAgentProviderCollectionBuilder.cs
│   └── AiAgentProviderCollection.cs
├── Conversations/
│   ├── AgentThread.cs
│   ├── AgentMessage.cs
│   ├── AgentRun.cs
│   ├── AgentState.cs
│   ├── IAgentConversationService.cs
│   ├── AgentConversationService.cs
│   ├── IAgentStateService.cs
│   ├── AgentStateService.cs
│   ├── IAgentThreadRepository.cs
│   ├── IAgentMessageRepository.cs
│   ├── IAgentRunRepository.cs
│   └── IAgentStateRepository.cs
└── Plugins/
    ├── IAgentContributor.cs
    ├── AgentContributorCollectionBuilder.cs
    └── AgentContributorCollection.cs
```

### New: Umbraco.Ai.Agent.Persistence (Additions)

```
Umbraco.Ai.Agent.Persistence/
├── Conversations/
│   ├── AgentThreadEntity.cs
│   ├── AgentMessageEntity.cs
│   ├── AgentRunEntity.cs
│   ├── AgentStateEntity.cs
│   ├── EfCoreAgentThreadRepository.cs
│   ├── EfCoreAgentMessageRepository.cs
│   ├── EfCoreAgentRunRepository.cs
│   └── EfCoreAgentStateRepository.cs
└── UmbracoAiAgentDbContext.cs         # MODIFIED: Add new DbSets
```

---

## Implementation Phases

### Phase 1: Create Umbraco.Ai.Agui Project

**Goal**: Establish the shared AG-UI protocol library.

#### 1.1 Project Setup

Create new project in `Umbraco.Ai` solution:

```xml
<!-- Umbraco.Ai.Agui.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>Umbraco.Ai.Agui</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="SystemTextJson.JsonDiffPatch" />
  </ItemGroup>
</Project>
```

#### 1.2 AG-UI Event Types

```csharp
// Events/AgentEventType.cs
namespace Umbraco.Ai.Agui.Events;

public enum AgentEventType
{
    // Lifecycle
    RunStarted,
    RunFinished,
    RunError,
    StepStarted,
    StepFinished,

    // Text Messages
    TextMessageStart,
    TextMessageContent,
    TextMessageEnd,

    // Tool Calls
    ToolCallStart,
    ToolCallArgs,
    ToolCallEnd,
    ToolCallResult,

    // State
    StateSnapshot,
    StateDelta,
    MessagesSnapshot,

    // Special
    Raw,
    Custom
}
```

```csharp
// Events/IAgentEvent.cs
namespace Umbraco.Ai.Agui.Events;

public interface IAgentEvent
{
    AgentEventType Type { get; }
    long Timestamp { get; }
}

// Events/BaseAgentEvent.cs
public abstract record BaseAgentEvent : IAgentEvent
{
    public abstract AgentEventType Type { get; }
    public long Timestamp { get; init; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}
```

#### 1.3 Event Implementations

```csharp
// Events/Lifecycle/RunStartedEvent.cs
namespace Umbraco.Ai.Agui.Events.Lifecycle;

public sealed record RunStartedEvent : BaseAgentEvent
{
    public override AgentEventType Type => AgentEventType.RunStarted;
    public required string ThreadId { get; init; }
    public required string RunId { get; init; }
}

// Events/Lifecycle/RunFinishedEvent.cs
public sealed record RunFinishedEvent : BaseAgentEvent
{
    public override AgentEventType Type => AgentEventType.RunFinished;
    public required string ThreadId { get; init; }
    public required string RunId { get; init; }
    public AguiRunOutcome Outcome { get; init; } = AguiRunOutcome.Success;
    public AguiInterruptInfo? Interrupt { get; init; }
}

// Events/Lifecycle/RunErrorEvent.cs
public sealed record RunErrorEvent : BaseAgentEvent
{
    public override AgentEventType Type => AgentEventType.RunError;
    public required string Code { get; init; }
    public required string Message { get; init; }
}

// Events/Messages/TextMessageContentEvent.cs
public sealed record TextMessageContentEvent : BaseAgentEvent
{
    public override AgentEventType Type => AgentEventType.TextMessageContent;
    public required string MessageId { get; init; }
    public required string Delta { get; init; }
}

// Events/Tools/ToolCallStartEvent.cs
public sealed record ToolCallStartEvent : BaseAgentEvent
{
    public override AgentEventType Type => AgentEventType.ToolCallStart;
    public required string ToolCallId { get; init; }
    public required string ToolName { get; init; }
    public string? ParentMessageId { get; init; }
}

// Events/State/StateDeltaEvent.cs
public sealed record StateDeltaEvent : BaseAgentEvent
{
    public override AgentEventType Type => AgentEventType.StateDelta;
    public required IReadOnlyList<JsonPatchOperation> Delta { get; init; }
}
```

#### 1.4 AG-UI Models

```csharp
// Models/AguiRunRequest.cs
namespace Umbraco.Ai.Agui.Models;

public sealed class AguiRunRequest
{
    [JsonPropertyName("threadId")]
    public string ThreadId { get; set; } = string.Empty;

    [JsonPropertyName("runId")]
    public string RunId { get; set; } = string.Empty;

    [JsonPropertyName("messages")]
    public IEnumerable<AguiMessage> Messages { get; set; } = [];

    [JsonPropertyName("state")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public JsonElement State { get; set; }

    [JsonPropertyName("tools")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public IEnumerable<AguiTool>? Tools { get; set; }

    [JsonPropertyName("context")]
    public AguiContextItem[] Context { get; set; } = [];

    [JsonPropertyName("forwardedProps")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public JsonElement ForwardedProperties { get; set; }
}

// Models/AguiMessage.cs
public sealed class AguiMessage
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("toolCalls")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public IEnumerable<AguiToolCall>? ToolCalls { get; set; }

    [JsonPropertyName("toolCallId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? ToolCallId { get; set; }
}
```

#### 1.5 SSE Streaming Infrastructure

```csharp
// Streaming/AguiStreamResult.cs
namespace Umbraco.Ai.Agui.Streaming;

public abstract class AguiStreamResult : IResult
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    protected string ThreadId { get; }
    protected string RunId { get; }

    protected AguiStreamResult(string threadId, string runId)
    {
        ThreadId = threadId;
        RunId = runId;
    }

    public async Task ExecuteAsync(HttpContext httpContext)
    {
        httpContext.Response.ContentType = "text/event-stream";
        httpContext.Response.Headers.CacheControl = "no-cache,no-store";
        httpContext.Response.Headers["X-Accel-Buffering"] = "no";

        var cancellationToken = httpContext.RequestAborted;

        try
        {
            await foreach (var agentEvent in StreamEventsAsync(httpContext, cancellationToken))
            {
                await WriteEventAsync(httpContext.Response, agentEvent, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Client disconnected
        }
        catch (Exception ex)
        {
            await WriteEventAsync(httpContext.Response, new RunErrorEvent
            {
                Code = "StreamingError",
                Message = ex.Message
            }, CancellationToken.None);
        }

        await httpContext.Response.Body.FlushAsync(cancellationToken);
    }

    protected abstract IAsyncEnumerable<IAgentEvent> StreamEventsAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken);

    protected static async Task WriteEventAsync(
        HttpResponse response,
        IAgentEvent agentEvent,
        CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(agentEvent, agentEvent.GetType(), JsonOptions);
        await response.WriteAsync($"data: {json}\n\n", cancellationToken);
        await response.Body.FlushAsync(cancellationToken);
    }
}
```

#### 1.6 JSON Patch Service

```csharp
// State/JsonPatchOperation.cs
namespace Umbraco.Ai.Agui.State;

public sealed class JsonPatchOperation
{
    [JsonPropertyName("op")]
    public string Op { get; set; } = string.Empty;

    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public JsonElement? Value { get; set; }

    [JsonPropertyName("from")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? From { get; set; }
}

// State/IJsonPatchService.cs
public interface IJsonPatchService
{
    IReadOnlyList<JsonPatchOperation> CreateDiff(JsonElement before, JsonElement after);
    JsonElement ApplyPatch(JsonElement document, IEnumerable<JsonPatchOperation> operations);
}
```

#### 1.7 DI Registration

```csharp
// Configuration/UmbracoBuilderExtensions.cs
namespace Umbraco.Ai.Agui.Configuration;

public static class UmbracoBuilderExtensions
{
    public static IUmbracoBuilder AddUmbracoAiAgui(this IUmbracoBuilder builder)
    {
        builder.Services.AddSingleton<IJsonPatchService, JsonPatchService>();
        builder.Services.AddSingleton<AguiEventSerializer>();

        return builder;
    }
}
```

---

### Phase 2: Integrate Agui into Umbraco.Ai.Web

**Goal**: Enhance the generic chat endpoint with AG-UI events.

#### 2.1 Update Project Reference

```xml
<!-- Umbraco.Ai.Web.csproj -->
<ItemGroup>
  <ProjectReference Include="..\Umbraco.Ai.Agui\Umbraco.Ai.Agui.csproj" />
</ItemGroup>
```

#### 2.2 Create Chat Stream Result

```csharp
// Api/Management/Chat/Models/ChatAguiStreamResult.cs
namespace Umbraco.Ai.Web.Api.Management.Chat.Models;

internal sealed class ChatAguiStreamResult : AguiStreamResult
{
    private readonly IChatClient _chatClient;
    private readonly IList<ChatMessage> _messages;

    public ChatAguiStreamResult(
        IChatClient chatClient,
        IList<ChatMessage> messages,
        string threadId,
        string runId)
        : base(threadId, runId)
    {
        _chatClient = chatClient;
        _messages = messages;
    }

    protected override async IAsyncEnumerable<IAgentEvent> StreamEventsAsync(
        HttpContext httpContext,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return new RunStartedEvent { ThreadId = ThreadId, RunId = RunId };

        var messageId = Guid.NewGuid().ToString();
        yield return new TextMessageStartEvent { MessageId = messageId, Role = "assistant" };

        await foreach (var update in _chatClient.GetStreamingResponseAsync(_messages, cancellationToken: cancellationToken))
        {
            if (update.Text is { Length: > 0 } text)
            {
                yield return new TextMessageContentEvent { MessageId = messageId, Delta = text };
            }
        }

        yield return new TextMessageEndEvent { MessageId = messageId };
        yield return new RunFinishedEvent { ThreadId = ThreadId, RunId = RunId };
    }
}
```

#### 2.3 Update StreamChatController

Modify to optionally use AG-UI format based on Accept header or query param.

---

### Phase 3: Migrate Umbraco.Ai.Agent.Web to Use Agui

**Goal**: Remove duplicate models, use shared protocol library.

#### 3.1 Update Project Reference

```xml
<!-- Umbraco.Ai.Agent.Web.csproj -->
<ItemGroup>
  <ProjectReference Include="..\..\..\Umbraco.Ai\src\Umbraco.Ai.Agui\Umbraco.Ai.Agui.csproj" />
</ItemGroup>
```

#### 3.2 Remove Duplicate Models

Delete from `Umbraco.Ai.Agent.Web/Api/Management/Agent/Endpoints/`:
- `RunAgentRequest.cs` (use `AguiRunRequest`)

Update imports throughout to use `Umbraco.Ai.Agui.Models`.

#### 3.3 Refactor AgentStreamResult

```csharp
// Api/Management/Agent/Endpoints/AgentStreamResult.cs
namespace Umbraco.Ai.Agent.Web.Api.Management.Agent.Endpoints;

internal sealed class AgentStreamResult : AguiStreamResult
{
    private readonly AIAgent _agent;
    private readonly IList<ChatMessage> _messages;
    private readonly AgentRunOptions? _options;
    private readonly IAgentConversationService? _conversationService;

    public AgentStreamResult(
        AIAgent agent,
        IList<ChatMessage> messages,
        AgentRunOptions? options,
        string threadId,
        string runId,
        IAgentConversationService? conversationService = null)
        : base(threadId, runId)
    {
        _agent = agent;
        _messages = messages;
        _options = options;
        _conversationService = conversationService;
    }

    protected override async IAsyncEnumerable<IAgentEvent> StreamEventsAsync(
        HttpContext httpContext,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return new RunStartedEvent { ThreadId = ThreadId, RunId = RunId };

        var messageId = Guid.NewGuid().ToString();
        yield return new TextMessageStartEvent { MessageId = messageId, Role = "assistant" };

        // TODO: Use persistent thread from conversation service
        var thread = _agent.GetNewThread();
        var updates = _agent.RunStreamingAsync(_messages, thread, _options, cancellationToken);

        await foreach (var update in updates.AsChatResponseUpdatesAsync().WithCancellation(cancellationToken))
        {
            if (update.Text is { Length: > 0 } text)
            {
                yield return new TextMessageContentEvent { MessageId = messageId, Delta = text };
            }

            // TODO: Handle tool calls and emit TOOL_CALL_* events
        }

        yield return new TextMessageEndEvent { MessageId = messageId };
        yield return new RunFinishedEvent { ThreadId = ThreadId, RunId = RunId };
    }
}
```

---

### Phase 4: Tool System

**Goal**: Enable agents to use tools with streaming support.

#### 4.1 Tool Interface

```csharp
// Umbraco.Ai.Agent.Core/Tools/IAiAgentTool.cs
namespace Umbraco.Ai.Agent.Core.Tools;

public interface IAiAgentTool
{
    string Id { get; }
    string Name { get; }
    string Description { get; }
    string Category { get; }
    bool IsDestructive { get; }
    IReadOnlyList<string> Tags { get; }

    AIFunction CreateFunction(IServiceProvider serviceProvider);
}
```

#### 4.2 Tool Collection Builder

```csharp
// Umbraco.Ai.Agent.Core/Tools/Collections/AiAgentToolCollectionBuilder.cs
public class AiAgentToolCollectionBuilder
    : LazyCollectionBuilderBase<AiAgentToolCollectionBuilder, AiAgentToolCollection, IAiAgentTool>
{
    protected override AiAgentToolCollectionBuilder This => this;
}
```

#### 4.3 Update UmbracoAgentFactory

```csharp
// Inject IAgentToolScopeProvider and create tools
public async Task<ChatClientAgent> CreateAgentAsync(AiAgent agent, CancellationToken ct = default)
{
    // ... existing profile/client resolution ...

    var scopedTools = _toolScopeProvider.GetToolsForAgent(agent);
    var aiTools = scopedTools
        .Select(t => t.CreateFunction(_serviceProvider))
        .ToList();

    return new ChatClientAgent(
        chatClient,
        instructions: agent.Instructions,
        name: agent.Name,
        description: agent.Description,
        tools: aiTools.Count > 0 ? aiTools : null,
        loggerFactory: _loggerFactory);
}
```

---

### Phase 5: Conversation Persistence

**Goal**: Persistent threads, messages, runs, and state.

#### 5.1 Database Schema

```sql
CREATE TABLE UmbracoAiAgentThread (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    AgentId UNIQUEIDENTIFIER NOT NULL,
    UserId NVARCHAR(255),
    Title NVARCHAR(500),
    Status INT NOT NULL DEFAULT 0,
    DateCreated DATETIME2 NOT NULL,
    DateLastActivity DATETIME2 NOT NULL,
    DateArchived DATETIME2,
    MessageCount INT NOT NULL DEFAULT 0,
    TotalTokensUsed INT NOT NULL DEFAULT 0,

    INDEX IX_AgentId (AgentId),
    INDEX IX_UserId (UserId),
    FOREIGN KEY (AgentId) REFERENCES UmbracoAiAgent(Id) ON DELETE CASCADE
);

CREATE TABLE UmbracoAiAgentMessage (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    ThreadId UNIQUEIDENTIFIER NOT NULL,
    RunId UNIQUEIDENTIFIER,
    Role NVARCHAR(50) NOT NULL,
    Content NVARCHAR(MAX),
    ToolCalls NVARCHAR(MAX),
    ToolCallId NVARCHAR(255),
    TokenCount INT,
    DateCreated DATETIME2 NOT NULL,
    SequenceNumber INT NOT NULL,

    INDEX IX_ThreadId_Sequence (ThreadId, SequenceNumber),
    FOREIGN KEY (ThreadId) REFERENCES UmbracoAiAgentThread(Id) ON DELETE CASCADE
);

CREATE TABLE UmbracoAiAgentRun (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    ThreadId UNIQUEIDENTIFIER NOT NULL,
    Status INT NOT NULL DEFAULT 0,
    DateStarted DATETIME2 NOT NULL,
    DateCompleted DATETIME2,
    InputTokens INT,
    OutputTokens INT,
    ErrorCode NVARCHAR(100),
    ErrorMessage NVARCHAR(MAX),

    INDEX IX_ThreadId (ThreadId),
    FOREIGN KEY (ThreadId) REFERENCES UmbracoAiAgentThread(Id) ON DELETE CASCADE
);

CREATE TABLE UmbracoAiAgentState (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    ThreadId UNIQUEIDENTIFIER NOT NULL UNIQUE,
    StateJson NVARCHAR(MAX) NOT NULL,
    Version INT NOT NULL DEFAULT 1,
    DateModified DATETIME2 NOT NULL,

    FOREIGN KEY (ThreadId) REFERENCES UmbracoAiAgentThread(Id) ON DELETE CASCADE
);
```

#### 5.2 Conversation Service

```csharp
// Umbraco.Ai.Agent.Core/Conversations/IAgentConversationService.cs
public interface IAgentConversationService
{
    Task<AgentThread> GetOrCreateThreadAsync(string threadId, Guid agentId, string? userId = null, CancellationToken ct = default);
    Task<IList<ChatMessage>> BuildChatHistoryAsync(Guid threadId, int? maxMessages = null, CancellationToken ct = default);
    Task AddMessagesAsync(Guid threadId, IEnumerable<AgentMessage> messages, CancellationToken ct = default);
    Task<AgentRun> StartRunAsync(Guid threadId, CancellationToken ct = default);
    Task CompleteRunAsync(Guid runId, int? inputTokens = null, int? outputTokens = null, CancellationToken ct = default);
}
```

---

### Phase 6: Agent Provider System

**Goal**: Support database, code-registered, and plugin-contributed agents.

#### 6.1 Provider Interface

```csharp
// Umbraco.Ai.Agent.Core/Providers/IAiAgentProvider.cs
public interface IAiAgentProvider
{
    int Priority { get; }
    string ProviderId { get; }

    Task<AiAgent?> GetAsync(Guid id, CancellationToken ct = default);
    Task<AiAgent?> GetByAliasAsync(string alias, CancellationToken ct = default);
    Task<IEnumerable<AiAgent>> GetAllAsync(CancellationToken ct = default);
}
```

#### 6.2 Composite Resolver

```csharp
// Umbraco.Ai.Agent.Web/Api/Management/Agent/Execution/CompositeAgentResolver.cs
internal sealed class CompositeAgentResolver : IAgentResolver
{
    private readonly AiAgentProviderCollection _providers;
    private readonly IUmbracoAgentFactory _factory;

    public async Task<ChatClientAgent?> ResolveAgentAsync(string agentIdOrAlias, CancellationToken ct = default)
    {
        foreach (var provider in _providers.OrderBy(p => p.Priority))
        {
            var agent = Guid.TryParse(agentIdOrAlias, out var id)
                ? await provider.GetAsync(id, ct)
                : await provider.GetByAliasAsync(agentIdOrAlias, ct);

            if (agent is { IsActive: true })
            {
                return await _factory.CreateAgentAsync(agent, ct);
            }
        }

        return null;
    }
}
```

---

### Phase 7: Middleware Pipeline

**Goal**: Extensible execution hooks with human-in-the-loop support.

#### 7.1 Agent-Specific Middleware

```csharp
// Umbraco.Ai.Agent.Web/Api/Management/Agent/Middleware/IAgentRunMiddleware.cs
public interface IAgentRunMiddleware
{
    int Order { get; }

    IAsyncEnumerable<IAgentEvent> InvokeAsync(
        AgentExecutionContext context,
        AgentRunDelegate next,
        CancellationToken cancellationToken = default);
}
```

#### 7.2 Human Approval Middleware

```csharp
// Umbraco.Ai.Agent.Web/Api/Management/Agent/Middleware/HumanApprovalMiddleware.cs
public class HumanApprovalMiddleware : IAgentRunMiddleware
{
    public int Order => 100;

    public async IAsyncEnumerable<IAgentEvent> InvokeAsync(
        AgentExecutionContext context,
        AgentRunDelegate next,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var evt in next(context, cancellationToken))
        {
            // Intercept destructive tool calls for approval
            if (evt is ToolCallStartEvent toolCall && IsDestructive(toolCall.ToolName))
            {
                yield return new RunFinishedEvent
                {
                    ThreadId = context.ThreadId,
                    RunId = context.RunId,
                    Outcome = AguiRunOutcome.Interrupt,
                    Interrupt = new AguiInterruptInfo
                    {
                        Id = Guid.NewGuid().ToString(),
                        Reason = "human_approval",
                        Payload = new { tool = toolCall.ToolName }
                    }
                };
                yield break;
            }

            yield return evt;
        }
    }
}
```

---

### Phase 8: Plugin Architecture

**Goal**: Enable external packages to contribute agents, tools, and middleware.

#### 8.1 Builder Extensions

```csharp
// Umbraco.Ai.Agent.Core/Configuration/AgentBuilderExtensions.cs
public static class AgentBuilderExtensions
{
    public static AiAgentProviderCollectionBuilder AiAgentProviders(this IUmbracoBuilder builder)
        => builder.WithCollectionBuilder<AiAgentProviderCollectionBuilder>();

    public static AiAgentToolCollectionBuilder AiAgentTools(this IUmbracoBuilder builder)
        => builder.WithCollectionBuilder<AiAgentToolCollectionBuilder>();

    public static AgentRunMiddlewareCollectionBuilder AiAgentRunMiddleware(this IUmbracoBuilder builder)
        => builder.WithCollectionBuilder<AgentRunMiddlewareCollectionBuilder>();

    public static AgentDefinitionCollectionBuilder AiAgentDefinitions(this IUmbracoBuilder builder)
        => builder.WithCollectionBuilder<AgentDefinitionCollectionBuilder>();
}
```

#### 8.2 Plugin Example

```csharp
// External package example
[ComposeAfter(typeof(UmbracoAiAgentComposer))]
public class MyPluginComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.AiAgentTools()
            .Add<MyCustomTool>();

        builder.AiAgentRunMiddleware()
            .Append<MyLoggingMiddleware>();

        builder.AiAgentDefinitions()
            .Add<MySupportAgentDefinition>();
    }
}
```

---

## Dependency Graph

```
Umbraco.Ai.Agui (NEW - shared protocol)
    ↑
    ├── Umbraco.Ai.Web (generic chat)
    │
    └── Umbraco.Ai.Agent.Web (agent orchestration)
            ↑
            └── Umbraco.Ai.Agent.Core (domain logic)
                    ↑
                    └── Umbraco.Ai.Agent.Persistence (EF Core)
```

---

## Execution Order

1. **Phase 1**: Create `Umbraco.Ai.Agui` (foundation)
2. **Phase 2**: Integrate into `Umbraco.Ai.Web` (optional, can be deferred)
3. **Phase 3**: Migrate `Umbraco.Ai.Agent.Web` to use Agui
4. **Phase 5**: Conversation persistence
5. **Phase 4**: Tool system
6. **Phase 7**: Middleware pipeline
7. **Phase 6**: Agent provider system
8. **Phase 8**: Plugin architecture

---

## Configuration Schema

```json
{
  "Umbraco": {
    "Ai": {
      "Agent": {
        "ThreadLifecycle": {
          "InactivityArchiveThreshold": "7.00:00:00",
          "ArchiveRetentionPeriod": "30.00:00:00",
          "AutoArchiveInactive": true,
          "AutoDeleteExpired": true
        },
        "MessageHistory": {
          "MaxMessagesPerThread": 100,
          "MaxTokensForContext": 32000,
          "PreserveSystemMessages": true
        },
        "StateManagement": {
          "PersistState": true,
          "UseInMemoryCache": true,
          "CacheExpiration": "00:30:00"
        }
      }
    }
  }
}
```

---

## Critical Files Summary

| File | Project | Changes |
|------|---------|---------|
| `Umbraco.Ai.Agui.csproj` | Umbraco.Ai | NEW - shared protocol library |
| `Events/*.cs` | Umbraco.Ai.Agui | NEW - 17 AG-UI event types |
| `Models/*.cs` | Umbraco.Ai.Agui | NEW - AG-UI protocol models |
| `AguiStreamResult.cs` | Umbraco.Ai.Agui | NEW - base SSE streaming |
| `StreamChatController.cs` | Umbraco.Ai.Web | MODIFY - use Agui events |
| `AgentStreamResult.cs` | Umbraco.Ai.Agent.Web | MODIFY - extend AguiStreamResult |
| `RunAgentRequest.cs` | Umbraco.Ai.Agent.Web | DELETE - use AguiRunRequest |
| `UmbracoAgentFactory.cs` | Umbraco.Ai.Agent.Web | MODIFY - add tool support |
| `AgentResolver.cs` | Umbraco.Ai.Agent.Web | MODIFY - composite pattern |
| `UmbracoAiAgentDbContext.cs` | Umbraco.Ai.Agent.Persistence | MODIFY - add new DbSets |

---

## AG-UI Event Types Reference

| Event | Type String | Category |
|-------|-------------|----------|
| `RunStartedEvent` | `run_started` | Lifecycle |
| `RunFinishedEvent` | `run_finished` | Lifecycle |
| `RunErrorEvent` | `run_error` | Lifecycle |
| `StepStartedEvent` | `step_started` | Lifecycle |
| `StepFinishedEvent` | `step_finished` | Lifecycle |
| `TextMessageStartEvent` | `text_message_start` | Messages |
| `TextMessageContentEvent` | `text_message_content` | Messages |
| `TextMessageEndEvent` | `text_message_end` | Messages |
| `ToolCallStartEvent` | `tool_call_start` | Tools |
| `ToolCallArgsEvent` | `tool_call_args` | Tools |
| `ToolCallEndEvent` | `tool_call_end` | Tools |
| `ToolCallResultEvent` | `tool_call_result` | Tools |
| `StateSnapshotEvent` | `state_snapshot` | State |
| `StateDeltaEvent` | `state_delta` | State |
| `MessagesSnapshotEvent` | `messages_snapshot` | State |
| `RawEvent` | `raw` | Special |
| `CustomEvent` | `custom` | Special |

---

## References

- [AG-UI Protocol Documentation](https://docs.ag-ui.com)
- [Microsoft Agent Framework](https://learn.microsoft.com/en-us/agent-framework/)
- [JSON Patch RFC 6902](https://tools.ietf.org/html/rfc6902)
