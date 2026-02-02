---
description: >-
  Core concepts for Agent Runtime.
---

# Agent Concepts

## What is an Agent?

An agent is a configured AI assistant that can:

- **Follow instructions** - Custom system prompts defining behavior
- **Stream responses** - Real-time text generation via SSE
- **Call tools** - Execute functions during conversation
- **Maintain context** - Include brand voice and guidelines

## Agent Properties

| Property | Description |
|----------|-------------|
| `Alias` | Unique identifier for code references |
| `Name` | Display name in the backoffice |
| `Description` | Optional description |
| `Instructions` | System prompt defining agent behavior |
| `ProfileId` | Associated AI profile (or uses default) |
| `ContextIds` | AI Contexts to inject |
| `ScopeIds` | Scopes for categorization (e.g., "copilot") |
| `IsActive` | Whether the agent is available |

## AG-UI Protocol

Agents communicate using the AG-UI (Agent UI) protocol, a standardized event format for streaming AI interactions.

### Event Categories

```
┌─────────────────────────────────────────────────────────────┐
│                    AG-UI Event Flow                         │
│                                                             │
│  run_started                                                │
│       │                                                     │
│       ├──► text_message_start ──► content* ──► end          │
│       │                                                     │
│       ├──► tool_call_start ──► args* ──► end                │
│       │         │                                           │
│       │         └──► tool_call_result                       │
│       │                                                     │
│       └──► run_finished / run_error                         │
└─────────────────────────────────────────────────────────────┘
```

### Lifecycle Events

| Event | Description |
|-------|-------------|
| `run_started` | Agent run has begun |
| `run_finished` | Agent run completed successfully |
| `run_error` | Agent run failed with error |

### Text Message Events

| Event | Description |
|-------|-------------|
| `text_message_start` | Beginning of a text message |
| `text_message_content` | Text content chunk |
| `text_message_end` | End of a text message |

### Tool Events

| Event | Description |
|-------|-------------|
| `tool_call_start` | Tool call initiated |
| `tool_call_args` | Tool argument chunk |
| `tool_call_end` | Tool call complete |
| `tool_call_result` | Tool execution result |

### State Events

| Event | Description |
|-------|-------------|
| `state_snapshot` | Complete state update |
| `state_delta` | Incremental state change |

## Agent vs Prompt

| Aspect | Prompt | Agent |
|--------|--------|-------|
| **Execution** | Single request/response | Streaming conversation |
| **Protocol** | Simple HTTP | SSE with AG-UI events |
| **Tools** | No tool support | Frontend tool definitions |
| **Use Case** | One-shot generation | Interactive assistance |
| **Complexity** | Simple | More complex |

## How Agents Work

When you run an agent:

1. **Agent is loaded** - Configuration and instructions retrieved
2. **Context assembled** - Contexts and instructions combined
3. **Messages prepared** - User messages formatted
4. **Streaming begins** - SSE connection established
5. **Events emitted** - AG-UI events sent as generated
6. **Tools handled** - Frontend tools executed and results returned
7. **Run completes** - Final event sent

## Frontend Tools

Agents can define tools that execute in the browser:

{% code title="Tool Definition" %}
```json
{
  "name": "insert_content",
  "description": "Insert content at the cursor position",
  "parameters": {
    "type": "object",
    "properties": {
      "content": {
        "type": "string",
        "description": "The content to insert"
      }
    },
    "required": ["content"]
  }
}
```
{% endcode %}

When the agent calls this tool, the frontend receives the event and executes the action.

## Version History

Every change to an agent creates a new version:

- View the complete history of changes
- Compare any two versions
- Rollback to a previous version
- Track who made each change

## Best Practices

### Instruction Design

1. **Be specific** about the agent's role and capabilities
2. **Define boundaries** - what the agent should and shouldn't do
3. **Provide examples** of expected interactions
4. **Include guardrails** for safety

### Tool Design

1. **Single responsibility** - one action per tool
2. **Clear descriptions** - help the model understand usage
3. **Proper schemas** - validate parameters
4. **Meaningful names** - action-oriented naming

### Performance

1. **Associate profiles** - don't rely on defaults
2. **Minimize contexts** - only include necessary content
3. **Handle errors** - implement reconnection logic

## Related

* [Instructions](instructions.md) - Configuring agent behavior
* [Streaming](streaming.md) - SSE event handling
* [Scopes](scopes.md) - Categorizing agents
* [Frontend Tools](frontend-tools.md) - Defining tools
