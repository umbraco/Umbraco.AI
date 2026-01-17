# ADR-0001: Custom AG-UI Implementation

## Status

**Accepted**

## Date

2026-01-10

## Context

Umbraco.Ai.Agent requires an AG-UI (Agent-User Interface) protocol implementation to enable streaming communication between AI agents and frontend clients. AG-UI is an emerging protocol for real-time agent-to-UI communication using Server-Sent Events (SSE).

Microsoft's Agent Framework (MAF) includes an AG-UI implementation, which we evaluated for potential reuse.

### Options Considered

1. **Use Microsoft's AG-UI implementation directly** - Reference MAF types and inherit their AG-UI infrastructure
2. **Fork Microsoft's AG-UI code** - Copy and modify their implementation
3. **Implement our own AG-UI library** - Create `Umbraco.Ai.Agui` as a pure protocol package

## Decision

**We chose Option 3: Implement our own AG-UI library (`Umbraco.Ai.Agui`).**

## Rationale

### 1. Microsoft's Types Are Internal

All AG-UI types in Microsoft's Agent Framework are marked `internal`:

- `RunAgentInput`
- `AGUIServerSentEventsResult`
- `BaseEvent` and all event types
- Helper classes and converters

This prevents any direct reuse via NuGet reference. We cannot inherit, extend, or even reference these types from our code.

**Related Issue:** [microsoft/agent-framework#2988](https://github.com/microsoft/agent-framework/issues/2988) - ".NET: Support dynamic agent resolution in AG-UI endpoints (MapAGUI with factory delegate)"

| Field | Value |
|-------|-------|
| Status | Open |
| Author | mattbrailsford (Umbraco) |
| Assignee | javiercn (Microsoft) |
| Created | 2025-12-20 |
| Reactions | 6 thumbs up |

We filed this issue to request the changes needed for our use case. The issue documents our specific requirements:

1. **Dynamic agent resolution** - `MapAGUI` only accepts a pre-created `AIAgent` instance, but Umbraco needs to resolve agents by ID/alias from a database at request time (e.g., `POST /agents/{agentId}/stream`)
2. **Public types** - `RunAgentInput` and `AGUIServerSentEventsResult` are `internal`, preventing custom endpoint implementations

The issue explicitly states: *"This forces consumers to reimplement AG-UI request/response handling"* - which is exactly what we've done.

While the issue is assigned, it remains unresolved. This validates our decision to implement our own library rather than wait for upstream changes.

### 2. Umbraco Requires Features Microsoft Doesn't Provide

| Feature | Microsoft | Umbraco | Purpose |
|---------|-----------|---------|---------|
| Frontend Tool Interception | No | Yes | Intercepts tool calls to terminate agent loop and delegate execution to frontend |
| HITL Interrupts | No | Yes | Human-in-the-loop workflow with `AguiInterruptInfo` and `AguiResumeInfo` |
| Chunk Events | No | Yes | `TextMessageChunkEvent`, `ToolCallChunkEvent` - simpler than START/CONTENT/END triplets |
| Activity Events | No | Yes | `ActivitySnapshotEvent`, `ActivityDeltaEvent` for frontend-only UI state |
| Messages Snapshot | No | Yes | `MessagesSnapshotEvent` for full message history synchronization |
| Custom/Raw Events | No | Yes | `CustomEvent`, `RawEvent` for extensibility |
| Outcome Enum | No | Yes | Clear run result indication (Success, Error, Interrupt) |

These features are core to Umbraco's agent architecture and cannot be added without control over the implementation.

### 3. Clean Architectural Separation

Our implementation maintains clear boundaries:

```
Umbraco.Ai.Agui (Pure Protocol Package)
├── Events/* (IAguiEvent, BaseAguiEvent, all event types)
├── Models/* (AguiMessage, AguiTool, AguiRunRequest, etc.)
└── Streaming/* (AguiEventEmitter, AguiStreamResult, serialization)
         │
         ▼ (references)
Umbraco.Ai.Agent.Core (Agent + MAF Integration)
├── Chat/FrontendToolFunction.cs
└── Agui/AguiStreamingService.cs
         │
         ▼ (references)
Umbraco.Ai.Agent.Web (HTTP Layer)
└── Controllers/RunAgentController.cs
```

`Umbraco.Ai.Agui` has no dependency on Microsoft Agent Framework, making it a pure protocol implementation that could theoretically be used outside the agent context.

### 4. We Can Still Adopt Microsoft's Patterns

While we can't reuse their types, we adopted valuable architectural patterns:

| Pattern | Description | Adopted |
|---------|-------------|---------|
| Direct streaming | No `Task.Run()`, preserves `AsyncLocal` context | Yes |
| Builder pattern | Centralized event emission via `AguiEventEmitter` | Yes |
| Thin controller | Controller as routing layer, logic in services | Yes |

## Consequences

### Positive

- **Full control** over AG-UI event types and serialization
- **Extensibility** for Umbraco-specific features (interrupts, chunk events)
- **No dependency** on Microsoft's internal type decisions
- **Clean separation** between protocol and agent implementation
- **Testability** through well-defined interfaces

### Negative

- **Maintenance burden** - We own the AG-UI implementation
- **Potential divergence** - If AG-UI spec evolves, we must update manually
- **No upstream fixes** - Bug fixes in Microsoft's implementation won't flow to us

### Risks Mitigated

- If Microsoft makes types public ([#2988](https://github.com/microsoft/agent-framework/issues/2988)), we can evaluate migration
- AG-UI is a relatively stable protocol; major changes are unlikely
- Our implementation follows the spec, so compatibility is maintained

## Future Migration Path

If `microsoft/agent-framework#2988` is resolved and types become public:

1. **Evaluate** whether Microsoft's types meet our needs
2. **Consider** using their base types while extending with our features
3. **Contribute** upstream - patterns like frontend tool interception and HITL interrupts could benefit the broader ecosystem

**Decision:** Wait for issue resolution before committing to any migration. Current implementation is stable and meets all requirements.

## References

- [AG-UI Protocol Specification](https://docs.ag-ui.com/)
- [AG-UI Interrupts Draft Spec](https://docs.ag-ui.com/drafts/interrupts)
- [microsoft/agent-framework#2988](https://github.com/microsoft/agent-framework/issues/2988) - Dynamic agent resolution & public types request
- Internal: `docs/internal/plans/entity-adapter-architecture.md`
