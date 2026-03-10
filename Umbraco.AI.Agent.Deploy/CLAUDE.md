# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

> **Note:** This is the Umbraco.AI.Agent.Deploy package. See the [root CLAUDE.md](../CLAUDE.md) for shared coding standards, build commands, and repository-wide conventions that apply to all packages.

## Build Commands

```bash
# Build the solution
dotnet build Umbraco.AI.Agent.Deploy.slnx

# Run tests
dotnet test Umbraco.AI.Agent.Deploy.slnx
```

## Architecture Overview

Umbraco.AI.Agent.Deploy provides Umbraco Deploy integration for AI agents. It enables deployment of both standard and orchestrated agents with their type-specific configurations, scoping rules, and profile dependencies.

### Project Structure

Single-project structure (consistent with Deploy pattern):

| Project                                 | Purpose                                  |
| --------------------------------------- | ---------------------------------------- |
| `Umbraco.AI.Agent.Deploy`               | Service connector, artifact, notification handlers |
| `Umbraco.AI.Agent.Deploy.Tests.Unit`    | Unit tests                               |

### Key Components

**Artifact** - `AIAgentArtifact`:
- Basic properties (Alias, Name, Description)
- `AgentType` string ("Standard" or "Orchestrated")
- `Config` serialized as JSON string (polymorphic based on agent type)
- Optional ProfileUdi for profile dependency
- SurfaceIds array (backoffice, frontend, custom)
- Scope as JsonElement (serialized AIAgentScope)
- IsActive flag

**Service Connector** - `UmbracoAIAgentServiceConnector`:
- Extends `UmbracoAIProfileDependentEntityServiceConnectorBase`
- Handles agent-to-profile dependency resolution
- Serializes `IAIAgentConfig` polymorphically based on agent type
- Deserializes config back using `AIAgentType` to determine concrete type
- Pass 3 handles both agent creation and profile resolution

**Notification Handlers**:
- `AIAgentSavedDeployRefresherNotificationAsyncHandler` - Writes artifacts on save
- `AIAgentDeletedDeployRefresherNotificationAsyncHandler` - Deletes artifacts on delete

### Config Serialization (Polymorphic)

The agent configuration is serialized/deserialized based on agent type:

```csharp
// Export: Serialize config to JSON
string? configJson = null;
if (entity.Config is not null)
{
    configJson = JsonSerializer.Serialize(entity.Config, entity.Config.GetType(), JsonOptions);
}

// Import: Deserialize based on agent type
IAIAgentConfig? config = agentType switch
{
    AIAgentType.Standard => JsonSerializer.Deserialize<AIStandardAgentConfig>(json, JsonOptions),
    AIAgentType.Orchestrated => JsonSerializer.Deserialize<AIOrchestratedAgentConfig>(json, JsonOptions),
    _ => null,
};
```

**Standard config** (`AIStandardAgentConfig`):
- ContextIds, Instructions, AllowedToolIds, AllowedToolScopeIds, UserGroupPermissions

**Orchestrated config** (`AIOrchestratedAgentConfig`):
- WorkflowId, Settings (JsonElement)

### Deployment Workflow

**Pass 3: Create/Update Agent with Profile Resolution**
```csharp
private async Task Pass3Async(...)
{
    // Resolve profile
    var profileId = await ResolveProfileIdAsync(artifact.ProfileUdi, ct);

    // Parse agent type
    var agentType = Enum.TryParse<AIAgentType>(artifact.AgentType, ignoreCase: true, out var parsed)
        ? parsed
        : AIAgentType.Standard;

    // Deserialize type-specific config
    IAIAgentConfig? config = DeserializeConfig(agentType, artifact.Config);

    // Create or update agent
    var agent = state.Entity ?? new AIAgent
    {
        Id = artifact.Udi.Guid,
        Alias = artifact.Alias!,
        Name = artifact.Name,
        AgentType = agentType,
    };

    agent.Config = config;
    agent.ProfileId = profileId;
    // ... set other properties

    state.Entity = await agentService.SaveAgentAsync(agent, ct);
}
```

### Scope Serialization

AIAgentScope is serialized to JSON for artifact storage:

```csharp
// To artifact
artifact.Scope = entity.Scope != null
    ? JsonSerializer.SerializeToElement(entity.Scope)
    : null;

// From artifact
var scope = artifact.Scope.HasValue
    ? JsonSerializer.Deserialize<AIAgentScope>(artifact.Scope.Value)
    : null;
```

**AIAgentScope Structure:**
```csharp
public class AIAgentScope
{
    public List<AIAgentScopeRule> AllowRules { get; set; }
    public List<AIAgentScopeRule> DenyRules { get; set; }
}

public class AIAgentScopeRule
{
    public List<string> Sections { get; set; }       // "content", "media"
    public List<string> EntityTypes { get; set; }    // "document", "media"
}
```

**Note:** AIAgentScopeRule uses `Sections` and `EntityTypes`, different from AIPromptScopeRule which uses `ContentTypeAliases`.

## Target Framework

- .NET 10.0 (`net10.0`)
- Uses Central Package Management (`Directory.Packages.props`)
- Nullable reference types enabled

## Dependencies

- Umbraco CMS 17.x
- Umbraco.AI 1.x
- Umbraco.AI.Agent 1.x
- Umbraco.AI.Deploy 1.x
- Umbraco Deploy 17.x

## Testing Focus

Unit tests cover:
- Artifact creation with all properties populated (both standard and orchestrated)
- Artifact creation with minimal/optional properties (null cases)
- AgentType serialization/deserialization
- Polymorphic config serialization (standard vs orchestrated)
- Profile dependency tracking (ProfileUdi added to dependencies)
- Scope serialization/deserialization (Sections/EntityTypes)
- SurfaceIds array

## Key Patterns

### Polymorphic Config Handling

```csharp
// Determine concrete type for deserialization
private static IAIAgentConfig? DeserializeConfig(AIAgentType agentType, string? json)
{
    if (string.IsNullOrWhiteSpace(json))
    {
        return agentType switch
        {
            AIAgentType.Standard => new AIStandardAgentConfig(),
            AIAgentType.Orchestrated => new AIOrchestratedAgentConfig(),
            _ => null,
        };
    }

    return agentType switch
    {
        AIAgentType.Standard => JsonSerializer.Deserialize<AIStandardAgentConfig>(json, JsonOptions),
        AIAgentType.Orchestrated => JsonSerializer.Deserialize<AIOrchestratedAgentConfig>(json, JsonOptions),
        _ => null,
    };
}
```

### Important Deployment Considerations

- **AgentType is immutable**: Once an agent is created with a type, it cannot be changed
- **Orchestrated agents require workflow code**: The deployment transfers the workflow ID and settings, but the workflow implementation must be registered in the target environment
- **Standard agent config includes tool permissions**: AllowedToolIds, AllowedToolScopeIds, and UserGroupPermissions are all part of the serialized config

## Related Documentation

- **[Umbraco.AI.Deploy CLAUDE.md](../Umbraco.AI.Deploy/CLAUDE.md)** - Base connector patterns
- **[Umbraco.AI.Agent CLAUDE.md](../Umbraco.AI.Agent/CLAUDE.md)** - Agent domain model
