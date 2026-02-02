# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with the Umbraco.AI.Agui project.

> **Note:** This is the AG-UI protocol SDK for Umbraco.AI. See the [Agent CLAUDE.md](../../CLAUDE.md) and [root CLAUDE.md](../../../CLAUDE.md) for shared coding standards and repository-wide conventions.

## ⚠️ CRITICAL: AG-UI Protocol Compliance

**This project implements the [AG-UI (Agent-User Interaction) protocol](https://docs.ag-ui.com/introduction).** ALL changes to this project MUST strictly comply with the AG-UI specification.

### What is AG-UI?

AG-UI is an open, lightweight, event-based protocol that standardizes communication between AI agents and user-facing applications. It provides a bidirectional connection layer specifically designed for agentic systems.

**Reference:** https://docs.ag-ui.com/introduction

### Core AG-UI Principles

1. **Event-based Communication** - All agent-to-UI communication uses discrete asynchronous events, never synchronous request/response patterns
2. **Streaming by Default** - Support long-running operations with intermediate state streaming via Server-Sent Events (SSE)
3. **Bidirectional Flow** - Enable both agent→UI and UI→agent communication for steering and interrupts
4. **Type Safety** - Events must have well-defined schemas matching the AG-UI specification
5. **Nondeterministic UI Control** - Support adaptive UI patterns for unpredictable agent behavior

### Required Patterns ✅

When working with `Umbraco.AI.Agui`, you MUST:

- ✅ **Use event-based patterns** - All new functionality must implement `IAguiEvent`
- ✅ **Support streaming** - Use `IAsyncEnumerable<IAguiEvent>` and `AguiEventEmitter`
- ✅ **Follow AG-UI naming conventions** - Event names must match AG-UI spec (e.g., `RunStartedEvent`, `TextMessageChunkEvent`)
- ✅ **Maintain schema compatibility** - Event properties must align with AG-UI event schemas
- ✅ **Support SSE serialization** - Events must serialize correctly via `AguiEventSerializer`
- ✅ **Enable bidirectional flow** - Support both agent→UI events and UI→agent requests

### Forbidden Patterns ❌

- ❌ **DON'T** introduce synchronous request/response APIs
- ❌ **DON'T** create custom event types that conflict with AG-UI standard events
- ❌ **DON'T** break event schema compatibility with AG-UI spec
- ❌ **DON'T** use blocking or synchronous patterns in streaming contexts
- ❌ **DON'T** bypass `AguiEventEmitter` for event emission
- ❌ **DON'T** modify core event types without validating against AG-UI spec

### AG-UI Event Categories

All events in this project must belong to one of these AG-UI-defined categories:

| Category | Purpose | Examples |
|----------|---------|----------|
| **Lifecycle** | Run and step lifecycle management | `RunStartedEvent`, `RunFinishedEvent`, `RunErrorEvent`, `StepStartedEvent`, `StepFinishedEvent` |
| **Messages** | Text message streaming | `TextMessageStartEvent`, `TextMessageChunkEvent`, `TextMessageContentEvent`, `TextMessageEndEvent` |
| **Tools** | Tool/function call lifecycle | `ToolCallStartEvent`, `ToolCallArgsEvent`, `ToolCallChunkEvent`, `ToolCallEndEvent`, `ToolCallResultEvent` |
| **State** | State snapshots and deltas | `StateSnapshotEvent`, `StateDeltaEvent`, `MessagesSnapshotEvent` |
| **Activity** | Activity tracking | `ActivitySnapshotEvent`, `ActivityDeltaEvent` |
| **Special** | Custom and raw events | `CustomEvent`, `RawEvent` |

### Validation Checklist

Before committing changes to `Umbraco.AI.Agui`, verify:

- [ ] All new events implement `IAguiEvent` and extend `BaseAguiEvent`
- [ ] Event names follow AG-UI naming conventions (check against spec)
- [ ] Event schemas match AG-UI specification (property names, types, structures)
- [ ] Streaming patterns use `IAsyncEnumerable<IAguiEvent>`
- [ ] No blocking or synchronous APIs introduced
- [ ] SSE serialization tested with `AguiEventSerializer`
- [ ] Events are placed in correct category folder (`Events/Lifecycle/`, `Events/Messages/`, etc.)
- [ ] XML documentation references AG-UI spec where applicable
- [ ] Unit tests added for new events (verify serialization, schema compliance)
- [ ] Reviewed AG-UI docs: https://docs.ag-ui.com/introduction

## Project Overview

**Umbraco.AI.Agui** is the AG-UI protocol SDK for Umbraco.AI. It provides:

- Event types for all AG-UI protocol events
- Models for AG-UI data structures (messages, tools, context, run requests)
- SSE streaming infrastructure (`AguiEventEmitter`, `AguiEventSerializer`)
- ASP.NET Core integration for streaming responses

This project is a **pure SDK** - it contains no business logic, UI, or database access. It defines the protocol contract.

## Project Structure

```
Umbraco.AI.Agui/
├── Events/                      # AG-UI event implementations
│   ├── IAguiEvent.cs           # Base event interface
│   ├── BaseAguiEvent.cs        # Base event class
│   ├── Lifecycle/              # Run and step lifecycle events
│   │   ├── RunStartedEvent.cs
│   │   ├── RunFinishedEvent.cs
│   │   ├── RunErrorEvent.cs
│   │   ├── StepStartedEvent.cs
│   │   └── StepFinishedEvent.cs
│   ├── Messages/               # Text message streaming events
│   │   ├── TextMessageStartEvent.cs
│   │   ├── TextMessageChunkEvent.cs
│   │   ├── TextMessageContentEvent.cs
│   │   └── TextMessageEndEvent.cs
│   ├── Tools/                  # Tool call lifecycle events
│   │   ├── ToolCallStartEvent.cs
│   │   ├── ToolCallArgsEvent.cs
│   │   ├── ToolCallChunkEvent.cs
│   │   ├── ToolCallEndEvent.cs
│   │   └── ToolCallResultEvent.cs
│   ├── State/                  # State management events
│   │   ├── StateSnapshotEvent.cs
│   │   ├── StateDeltaEvent.cs
│   │   └── MessagesSnapshotEvent.cs
│   ├── Activity/               # Activity tracking events
│   │   ├── ActivitySnapshotEvent.cs
│   │   └── ActivityDeltaEvent.cs
│   └── Special/                # Custom and raw events
│       ├── CustomEvent.cs
│       └── RawEvent.cs
├── Models/                      # AG-UI data models
│   ├── AguiMessage.cs          # Message representation
│   ├── AguiMessageRole.cs      # Message roles (user, agent, tool)
│   ├── AguiTool.cs             # Tool definition
│   ├── AguiToolCall.cs         # Tool call representation
│   ├── AguiFunctionCall.cs     # Function call details
│   ├── AguiContextItem.cs      # Context item
│   ├── AguiRunRequest.cs       # Run request model
│   ├── AguiRunOutcome.cs       # Run outcome (success, error, interrupt)
│   ├── AguiInterruptInfo.cs    # Interrupt metadata
│   └── AguiResumeInfo.cs       # Resume metadata
├── Streaming/                   # SSE streaming infrastructure
│   ├── AguiEventEmitter.cs     # Event emission and streaming
│   ├── AguiEventSerializer.cs  # SSE serialization
│   ├── AguiStreamResult.cs     # ASP.NET Core result type
│   ├── AguiEventStreamResult.cs # IAsyncEnumerable result wrapper
│   └── AguiStreamOptions.cs    # Stream configuration
├── Configuration/               # DI registration
│   └── ServiceCollectionExtensions.cs
├── AguiConstants.cs            # Protocol constants (event types, content types)
└── Umbraco.AI.Agui.csproj
```

## Build Commands

This project is part of the `Umbraco.AI.Agent.sln` solution.

```bash
# Build from Agent solution root
cd Umbraco.AI.Agent
dotnet build Umbraco.AI.Agent.sln

# Build this project directly
dotnet build src/Umbraco.AI.Agui/Umbraco.AI.Agui.csproj

# Run tests
dotnet test tests/Umbraco.AI.Agent.Tests.Unit/Umbraco.AI.Agent.Tests.Unit.csproj --filter Category=Agui
```

## Testing Requirements

All changes to `Umbraco.AI.Agui` MUST include unit tests:

```csharp
// Example: Testing event serialization
[Fact]
public void RunStartedEvent_SerializesToAguiFormat()
{
    var evt = new RunStartedEvent
    {
        RunId = "test-run-123",
        Timestamp = DateTimeOffset.UtcNow
    };

    var serializer = new AguiEventSerializer();
    var result = serializer.Serialize(evt);

    Assert.StartsWith("event: run.started", result);
    Assert.Contains("\"runId\":\"test-run-123\"", result);
}
```

Tests should verify:
- Event serialization to SSE format
- Event schema compliance
- Stream emission patterns
- Model validation

## Key Namespaces

- `Umbraco.AI.Agui.Events` - All AG-UI event types
- `Umbraco.AI.Agui.Models` - AG-UI data models
- `Umbraco.AI.Agui.Streaming` - SSE streaming infrastructure
- `Umbraco.AI.Agui.Configuration` - DI registration

## Usage Example

```csharp
// Controller returning AG-UI event stream
[HttpPost("run")]
public async Task<IActionResult> RunAgent(
    [FromBody] AguiRunRequest request,
    CancellationToken ct)
{
    var emitter = new AguiEventEmitter();

    // Emit run started
    await emitter.EmitAsync(new RunStartedEvent
    {
        RunId = request.RunId,
        Timestamp = DateTimeOffset.UtcNow
    });

    // Emit text message chunks
    await emitter.EmitAsync(new TextMessageStartEvent
    {
        MessageId = "msg-1",
        Role = AguiMessageRole.Agent
    });

    await emitter.EmitAsync(new TextMessageChunkEvent
    {
        MessageId = "msg-1",
        Chunk = "Hello, "
    });

    await emitter.EmitAsync(new TextMessageChunkEvent
    {
        MessageId = "msg-1",
        Chunk = "world!"
    });

    await emitter.EmitAsync(new TextMessageEndEvent
    {
        MessageId = "msg-1"
    });

    // Emit run finished
    await emitter.EmitAsync(new RunFinishedEvent
    {
        RunId = request.RunId,
        Outcome = AguiRunOutcome.Success,
        Timestamp = DateTimeOffset.UtcNow
    });

    // Return as SSE stream
    return new AguiStreamResult(emitter.GetEvents());
}
```

## Adding New Events

When adding a new event type, follow these steps:

1. **Verify AG-UI Compliance** - Confirm the event exists in the AG-UI spec
2. **Choose Correct Category** - Place in appropriate `Events/` subfolder
3. **Implement IAguiEvent** - Extend `BaseAguiEvent` for consistency
4. **Match Schema** - Ensure properties match AG-UI spec exactly
5. **Add Constant** - Add event type to `AguiConstants.EventTypes`
6. **Write Tests** - Test serialization and schema compliance
7. **Document** - Add XML documentation with AG-UI spec reference

```csharp
// Example: Adding a new lifecycle event
namespace Umbraco.AI.Agui.Events.Lifecycle;

/// <summary>
/// Emitted when a run is paused by the agent.
/// </summary>
/// <remarks>
/// AG-UI Specification: https://docs.ag-ui.com/events/lifecycle#run-paused
/// </remarks>
public class RunPausedEvent : BaseAguiEvent
{
    public override string Type => AguiConstants.EventTypes.RunPaused;

    /// <summary>
    /// The unique identifier for this run.
    /// </summary>
    public required string RunId { get; init; }

    /// <summary>
    /// Optional reason for pausing.
    /// </summary>
    public string? Reason { get; init; }
}
```

## Common Pitfalls

### ❌ Wrong: Custom event not in AG-UI spec
```csharp
// DON'T create custom events outside AG-UI spec
public class CustomProgressEvent : BaseAguiEvent
{
    public override string Type => "custom.progress"; // ❌ Not in AG-UI spec
}
```

### ✅ Right: Use CustomEvent for extensions
```csharp
// DO use CustomEvent for vendor-specific extensions
var evt = new CustomEvent
{
    Name = "umbraco.progress",
    Data = new { percentage = 75 }
};
```

### ❌ Wrong: Synchronous patterns
```csharp
// DON'T use synchronous patterns
public AguiMessage GetNextMessage() // ❌ Synchronous
{
    return _messages.Dequeue();
}
```

### ✅ Right: Streaming patterns
```csharp
// DO use async streaming
public async IAsyncEnumerable<IAguiEvent> StreamMessages()
{
    await foreach (var message in _messageSource)
    {
        yield return new TextMessageChunkEvent
        {
            MessageId = message.Id,
            Chunk = message.Content
        };
    }
}
```

## Dependencies

- **Microsoft.AspNetCore.App** - ASP.NET Core framework for SSE streaming
- **.NET 10.0** (`net10.0`) - Target framework

## Target Framework

- .NET 10.0 (`net10.0`)
- Nullable reference types enabled
- Uses Central Package Management (`Directory.Packages.props`)

## AG-UI Specification Sections

When working on specific areas, reference these AG-UI spec sections:

- **Events**: https://docs.ag-ui.com/events
- **Lifecycle Events**: https://docs.ag-ui.com/events/lifecycle
- **Message Events**: https://docs.ag-ui.com/events/messages
- **Tool Events**: https://docs.ag-ui.com/events/tools
- **State Events**: https://docs.ag-ui.com/events/state
- **Streaming**: https://docs.ag-ui.com/streaming
- **Bidirectional Flow**: https://docs.ag-ui.com/bidirectional

## Questions?

If you're unsure whether a change complies with AG-UI:

1. Read the AG-UI specification: https://docs.ag-ui.com/introduction
2. Check existing event implementations for patterns
3. Ask maintainers before implementing non-standard patterns
4. When in doubt, use `CustomEvent` for extensions

**Remember:** AG-UI compliance is non-negotiable. This ensures interoperability with AG-UI compliant frontends and tools.
