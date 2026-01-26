---
description: >-
  Service for managing and running agents.
---

# IAiAgentService

Service for agent CRUD operations and streaming execution.

## Namespace

```csharp
using Umbraco.Ai.Agent.Core.Agents;
```

## Interface

{% code title="IAiAgentService" %}
```csharp
public interface IAiAgentService
{
    Task<AiAgent?> GetAgentAsync(Guid id, CancellationToken cancellationToken = default);

    Task<AiAgent?> GetAgentByAliasAsync(string alias, CancellationToken cancellationToken = default);

    Task<IEnumerable<AiAgent>> GetAgentsAsync(CancellationToken cancellationToken = default);

    Task<PagedModel<AiAgent>> GetAgentsPagedAsync(
        int skip = 0,
        int take = 100,
        string? filter = null,
        Guid? profileId = null,
        CancellationToken cancellationToken = default);

    Task<AiAgent> SaveAgentAsync(AiAgent agent, CancellationToken cancellationToken = default);

    Task<bool> DeleteAgentAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> AgentAliasExistsAsync(string alias, Guid? excludeId = null, CancellationToken cancellationToken = default);

    IAsyncEnumerable<IAguiEvent> StreamAgentAsync(
        Guid agentId,
        AiAgentRunRequest request,
        IEnumerable<AiFrontendToolDefinition>? frontendToolDefinitions = null,
        CancellationToken cancellationToken = default);
}
```
{% endcode %}

## Methods

### GetAgentAsync

Gets an agent by its unique identifier.

| Parameter | Type | Description |
|-----------|------|-------------|
| `id` | `Guid` | The agent ID |
| `cancellationToken` | `CancellationToken` | Cancellation token |

**Returns**: The agent if found, otherwise `null`.

### GetAgentByAliasAsync

Gets an agent by its alias.

| Parameter | Type | Description |
|-----------|------|-------------|
| `alias` | `string` | The agent alias |
| `cancellationToken` | `CancellationToken` | Cancellation token |

**Returns**: The agent if found, otherwise `null`.

### GetAgentsAsync

Gets all agents.

**Returns**: All agents in the system.

### GetAgentsPagedAsync

Gets agents with pagination and filtering.

| Parameter | Type | Description |
|-----------|------|-------------|
| `skip` | `int` | Items to skip |
| `take` | `int` | Items to take |
| `filter` | `string?` | Filter by name |
| `profileId` | `Guid?` | Filter by profile |
| `cancellationToken` | `CancellationToken` | Cancellation token |

**Returns**: Paged result with items and total count.

### SaveAgentAsync

Creates or updates an agent.

| Parameter | Type | Description |
|-----------|------|-------------|
| `agent` | `AiAgent` | The agent to save |
| `cancellationToken` | `CancellationToken` | Cancellation token |

**Returns**: The saved agent with ID and version.

### DeleteAgentAsync

Deletes an agent by ID.

| Parameter | Type | Description |
|-----------|------|-------------|
| `id` | `Guid` | The agent ID |
| `cancellationToken` | `CancellationToken` | Cancellation token |

**Returns**: `true` if deleted, `false` if not found.

### AgentAliasExistsAsync

Checks if an alias is in use.

| Parameter | Type | Description |
|-----------|------|-------------|
| `alias` | `string` | The alias to check |
| `excludeId` | `Guid?` | Optional ID to exclude |
| `cancellationToken` | `CancellationToken` | Cancellation token |

**Returns**: `true` if alias exists.

### StreamAgentAsync

Runs an agent and streams AG-UI events.

| Parameter | Type | Description |
|-----------|------|-------------|
| `agentId` | `Guid` | The agent ID |
| `request` | `AiAgentRunRequest` | Run parameters |
| `frontendToolDefinitions` | `IEnumerable<AiFrontendToolDefinition>?` | Frontend tools |
| `cancellationToken` | `CancellationToken` | Cancellation token |

**Returns**: Async enumerable of AG-UI events.

{% code title="Example" %}
```csharp
await foreach (var evt in _agentService.StreamAgentAsync(
    agentId,
    new AiAgentRunRequest
    {
        Messages = new List<AiAgentMessage>
        {
            new AiAgentMessage { Role = "user", Content = "Hello!" }
        }
    }))
{
    switch (evt)
    {
        case AguiTextMessageContentEvent textEvt:
            Console.Write(textEvt.Content);
            break;
        case AguiRunFinishedEvent:
            Console.WriteLine("\nDone!");
            break;
    }
}
```
{% endcode %}

## Related Models

### AiAgentRunRequest

{% code title="AiAgentRunRequest" %}
```csharp
public class AiAgentRunRequest
{
    public IReadOnlyList<AiAgentMessage> Messages { get; set; } = Array.Empty<AiAgentMessage>();
}
```
{% endcode %}

### AiAgentMessage

{% code title="AiAgentMessage" %}
```csharp
public class AiAgentMessage
{
    public string Role { get; set; } = "user";
    public string Content { get; set; } = string.Empty;
}
```
{% endcode %}

### AiFrontendToolDefinition

{% code title="AiFrontendToolDefinition" %}
```csharp
public class AiFrontendToolDefinition
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public JsonNode? Parameters { get; set; }
}
```
{% endcode %}

## Related

* [AiAgent Model](ai-agent.md) - Agent model reference
* [Agent Concepts](../concepts.md) - Concepts overview
