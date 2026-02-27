# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

> **Note:** This is the Umbraco.AI.Agent.Deploy package. See the [root CLAUDE.md](../CLAUDE.md) for shared coding standards, build commands, and repository-wide conventions that apply to all packages.

## Build Commands

```bash
# Build the solution
dotnet build Umbraco.AI.Agent.Deploy.sln

# Run tests
dotnet test Umbraco.AI.Agent.Deploy.sln
```

## Architecture Overview

Umbraco.AI.Agent.Deploy provides Umbraco Deploy integration for AI agents. It enables deployment of agents with their tool permissions, user group configurations, and scoping rules.

### Project Structure

Single-project structure (consistent with Deploy pattern):

| Project                                 | Purpose                                  |
| --------------------------------------- | ---------------------------------------- |
| `Umbraco.AI.Agent.Deploy`               | Service connector, artifact, notification handlers |
| `Umbraco.AI.Agent.Deploy.Tests.Unit`    | Unit tests                               |

### Key Components

**Artifact** - `AIAgentArtifact`:
- Basic properties (Alias, Name, Description, Instructions)
- Optional ProfileUdi for profile dependency
- ContextIds array (future feature)
- SurfaceIds array (backoffice, frontend, custom)
- Scope as JsonElement (serialized AIAgentScope)
- AllowedToolIds array (specific tool permissions)
- AllowedToolScopeIds array (scope-based permissions)
- UserGroupPermissions as JsonElement (per-group overrides)
- IsActive flag

**Service Connector** - `UmbracoAIAgentServiceConnector`:
- Extends `UmbracoAIProfileDependentEntityServiceConnectorBase`
- Handles agent-to-profile dependency resolution
- Adds user group dependencies with `ArtifactDependencyMode.Exist`
- Serializes/deserializes AIAgentScope and UserGroupPermissions

**Notification Handlers**:
- `AIAgentSavedDeployRefresherNotificationAsyncHandler` - Writes artifacts on save
- `AIAgentDeletedDeployRefresherNotificationAsyncHandler` - Deletes artifacts on delete

### Base Class Benefits

Uses `UmbracoAIProfileDependentEntityServiceConnectorBase<AIAgentArtifact, AIAgent>`:
- Automatic Pass 2/4 pattern for profile resolution
- `AddProfileDependency(Guid?, ArtifactDependencyCollection)` helper
- `ResolveProfileIdAsync(GuidUdi?, CancellationToken)` helper
- Reduces code duplication with Prompt connector

### User Group Dependency Handling

Agent UserGroupPermissions require validation:

```csharp
public override Task<AIAgentArtifact?> GetArtifactAsync(...)
{
    var dependencies = new ArtifactDependencyCollection();

    // Add profile dependency
    var profileUdi = AddProfileDependency(entity.ProfileId, dependencies);

    // Add user group dependencies (ensure user groups exist)
    if (entity.UserGroupPermissions?.Count > 0)
    {
        foreach (var userGroupId in entity.UserGroupPermissions.Keys)
        {
            var userGroupUdi = new GuidUdi("user-group", userGroupId);
            dependencies.Add(new ArtifactDependency(
                userGroupUdi,
                false,
                ArtifactDependencyMode.Exist));
        }
    }

    // ... create artifact
}
```

**Why `ArtifactDependencyMode.Exist`?**
- User groups must exist in target environment
- Deployment fails if any user group is missing
- Provides clear error message
- Prevents broken agent configurations

### Deployment Workflow

**Pass 2: Create/Update Agent**
```csharp
private async Task Pass2Async(...)
{
    var agent = new AIAgent
    {
        Alias = artifact.Alias,
        Name = artifact.Name,
        Description = artifact.Description,
        ProfileId = null,  // Resolved in Pass 4
        ContextIds = artifact.ContextIds.ToList(),
        SurfaceIds = artifact.SurfaceIds.ToList(),
        Scope = DeserializeScope(artifact.Scope),
        AllowedToolIds = artifact.AllowedToolIds.ToList(),
        AllowedToolScopeIds = artifact.AllowedToolScopeIds.ToList(),
        UserGroupPermissions = DeserializeUserGroupPermissions(artifact.UserGroupPermissions),
        Instructions = artifact.Instructions,
        IsActive = artifact.IsActive
    };

    await _agentService.SaveAgentAsync(agent, ct);
}
```

**Pass 4: Resolve Profile Dependency**
```csharp
private async Task Pass4Async(...)
{
    // Use base class helper
    var profileId = await ResolveProfileIdAsync(artifact.ProfileUdi, ct);

    agent.ProfileId = profileId;
    await _agentService.SaveAgentAsync(agent, ct);
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

### UserGroupPermissions Serialization

```csharp
// To artifact
artifact.UserGroupPermissions = entity.UserGroupPermissions != null
    ? JsonSerializer.SerializeToElement(entity.UserGroupPermissions)
    : null;

// From artifact
var permissions = artifact.UserGroupPermissions.HasValue
    ? JsonSerializer.Deserialize<Dictionary<Guid, AIAgentUserGroupPermissions>>(
        artifact.UserGroupPermissions.Value)
    : new Dictionary<Guid, AIAgentUserGroupPermissions>();
```

**AIAgentUserGroupPermissions Structure:**
```csharp
public class AIAgentUserGroupPermissions
{
    public List<string> AllowedToolIds { get; set; }
}
```

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
- Artifact creation with all properties populated
- Artifact creation with minimal/optional properties (null cases)
- Profile dependency tracking (ProfileUdi added to dependencies)
- User group dependency tracking (all user group GUIDs in dependencies)
- Scope serialization/deserialization (Sections/EntityTypes)
- UserGroupPermissions serialization/deserialization
- AllowedToolIds and AllowedToolScopeIds arrays
- SurfaceIds array

## Key Patterns

### User Group Dependencies

```csharp
// Validate user groups exist in target environment
if (entity.UserGroupPermissions?.Count > 0)
{
    foreach (var userGroupId in entity.UserGroupPermissions.Keys)
    {
        var userGroupUdi = new GuidUdi("user-group", userGroupId);
        dependencies.Add(new ArtifactDependency(
            userGroupUdi,
            false,
            ArtifactDependencyMode.Exist));
    }
}
```

### Tool Permission Arrays

```csharp
// Simple arrays, no special handling needed
artifact.AllowedToolIds = entity.AllowedToolIds.ToList();
artifact.AllowedToolScopeIds = entity.AllowedToolScopeIds.ToList();
```

## Related Documentation

- **[Umbraco.AI.Deploy CLAUDE.md](../Umbraco.AI.Deploy/CLAUDE.md)** - Base connector patterns
- **[Umbraco.AI.Agent CLAUDE.md](../Umbraco.AI.Agent/CLAUDE.md)** - Agent domain model
