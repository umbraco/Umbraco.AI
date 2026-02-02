---
description: >-
  Model representing an AI agent.
---

# AIAgent

Represents a configured AI agent with instructions and settings.

## Namespace

```csharp
using Umbraco.AI.Agent.Core.Agents;
```

## Definition

{% code title="AIAgent" %}
```csharp
public class AIAgent : IAIVersionableEntity
{
    public Guid Id { get; internal set; }
    public required string Alias { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public Guid? ProfileId { get; set; }
    public IReadOnlyList<Guid> ContextIds { get; set; } = [];
    public IReadOnlyList<string> ScopeIds { get; set; } = [];
    public string? Instructions { get; set; }
    public bool IsActive { get; set; } = true;

    // Audit properties
    public DateTime DateCreated { get; init; } = DateTime.UtcNow;
    public DateTime DateModified { get; set; } = DateTime.UtcNow;
    public Guid? CreatedByUserId { get; init; }
    public Guid? ModifiedByUserId { get; set; }

    // Versioning
    public int Version { get; internal set; } = 1;
}
```
{% endcode %}

## Properties

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `Guid` | Unique identifier |
| `Alias` | `string` | Unique alias for code references (required) |
| `Name` | `string` | Display name (required) |
| `Description` | `string?` | Optional description |
| `ProfileId` | `Guid?` | Associated AI profile (null uses default) |
| `ContextIds` | `IReadOnlyList<Guid>` | AI Contexts to inject |
| `ScopeIds` | `IReadOnlyList<string>` | Scope IDs for categorization |
| `Instructions` | `string?` | Agent system prompt |
| `IsActive` | `bool` | Whether agent is available |
| `DateCreated` | `DateTime` | When created |
| `DateModified` | `DateTime` | When last modified |
| `Version` | `int` | Current version number |

## Example

{% code title="Example" %}
```csharp
var agent = new AIAgent
{
    Alias = "content-assistant",
    Name = "Content Assistant",
    Description = "Helps users write and improve content",
    ProfileId = chatProfileId,
    ContextIds = [brandVoiceContextId],
    ScopeIds = ["copilot"],
    Instructions = @"You are a helpful content assistant.

Your role is to help users write and improve content for the website.

Guidelines:
- Be concise and helpful
- Maintain the brand voice
- Ask clarifying questions when needed",
    IsActive = true
};

var saved = await _agentService.SaveAgentAsync(agent);
```
{% endcode %}

## Related

* [IAIAgentService](ai-agent-service.md) - Agent service
* [Agent Concepts](../concepts.md) - Concepts overview
* [Scopes](../scopes.md) - Agent categorization
