# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

> **Note:** This is the Umbraco.AI.Prompt.Deploy package. See the [root CLAUDE.md](../CLAUDE.md) for shared coding standards, build commands, and repository-wide conventions that apply to all packages.

## Build Commands

```bash
# Build the solution
dotnet build Umbraco.AI.Prompt.Deploy.slnx

# Run tests
dotnet test Umbraco.AI.Prompt.Deploy.slnx
```

## Architecture Overview

Umbraco.AI.Prompt.Deploy provides Umbraco Deploy integration for AI prompt templates. It enables deployment of prompts with their scoping rules, profile dependencies, and configurations.

### Project Structure

Single-project structure (consistent with Deploy pattern):

| Project                                  | Purpose                                  |
| ---------------------------------------- | ---------------------------------------- |
| `Umbraco.AI.Prompt.Deploy`               | Service connector, artifact, notification handlers |
| `Umbraco.AI.Prompt.Deploy.Tests.Unit`    | Unit tests                               |

### Key Components

**Artifact** - `AIPromptArtifact`:
- Basic properties (Alias, Name, Description, Instructions)
- Optional ProfileUdi for profile dependency
- ContextIds and GuardrailIds arrays (each ID declared as a `Match` dependency)
- Tags array for categorization
- Scope as JsonElement (serialized AIPromptScope)
- Settings (IsActive, IncludeEntityContext, OptionCount)

**Service Connector** - `UmbracoAIPromptServiceConnector`:
- Extends `UmbracoAIProfileDependentEntityServiceConnectorBase`
- Handles prompt-to-profile dependency resolution
- Serializes/deserializes AIPromptScope to/from JSON

**Notification Handlers**:
- `AIPromptSavedDeployRefresherNotificationAsyncHandler` - Writes artifacts on save
- `AIPromptDeletedDeployRefresherNotificationAsyncHandler` - Deletes artifacts on delete

### Base Class Benefits

Uses `UmbracoAIProfileDependentEntityServiceConnectorBase<AIPromptArtifact, AIPrompt>`:
- Single Pass 3 (after profiles, contexts and guardrails in pass 2)
- `AddProfileDependency(Guid?, ArtifactDependencyCollection)` helper
- `ResolveProfileIdAsync(GuidUdi?, CancellationToken)` helper
- Reduces code duplication with Agent connector

### Deployment Workflow

**Pass 3: Resolve Profile and Create/Update Prompt**
```csharp
private async Task Pass3Async(...)
{
    // Use base class helper
    var profileId = await ResolveProfileIdAsync(artifact.ProfileUdi, ct);

    var prompt = state.Entity ?? new AIPrompt { Id = artifact.Udi.Guid, /* ... */ };

    prompt.ProfileId = profileId;
    prompt.ContextIds = artifact.ContextIds.ToList();
    prompt.GuardrailIds = artifact.GuardrailIds.ToList();
    // ... set other properties

    state.Entity = await promptService.SavePromptAsync(prompt, ct);
}
```

### Scope Serialization

AIPromptScope is serialized to JSON for artifact storage:

```csharp
// To artifact
artifact.Scope = entity.Scope != null
    ? JsonSerializer.SerializeToElement(entity.Scope)
    : null;

// From artifact
var scope = artifact.Scope.HasValue
    ? JsonSerializer.Deserialize<AIPromptScope>(artifact.Scope.Value)
    : null;
```

**AIPromptScope Structure:**
```csharp
public class AIPromptScope
{
    public List<AIPromptScopeRule> AllowRules { get; set; }
    public List<AIPromptScopeRule> DenyRules { get; set; }
}

public class AIPromptScopeRule
{
    public List<string> ContentTypeAliases { get; set; }
    public List<string> PropertyAliases { get; set; }
    public List<string> PropertyEditorAliases { get; set; }
}
```

## Target Framework

- .NET 10.0 (`net10.0`)
- Uses Central Package Management (`Directory.Packages.props`)
- Nullable reference types enabled

## Dependencies

- Umbraco CMS 17.x
- Umbraco.AI 1.x
- Umbraco.AI.Prompt 1.x
- Umbraco.AI.Deploy 1.x
- Umbraco Deploy 17.x

## Testing Focus

Unit tests cover:
- Artifact creation with all properties populated
- Artifact creation with minimal/optional properties (null cases)
- Profile dependency tracking (ProfileUdi added to dependencies)
- Context dependency tracking (each ContextId added to dependencies)
- Scope serialization/deserialization
- ContextIds preservation
- Tag arrays

## Key Patterns

### Profile Dependency Pattern

```csharp
public override Task<AIPromptArtifact?> GetArtifactAsync(...)
{
    var dependencies = new ArtifactDependencyCollection();

    // Use base class helper
    var profileUdi = AddProfileDependency(entity.ProfileId, dependencies);

    var artifact = new AIPromptArtifact(udi, dependencies)
    {
        ProfileUdi = profileUdi,
        // ... other properties
    };
}
```

### Optional Properties Handling

```csharp
// Handle null scope
artifact.Scope = entity.Scope != null
    ? JsonSerializer.SerializeToElement(entity.Scope)
    : null;

// Handle empty collections
artifact.ContextIds = entity.ContextIds.ToList();  // Empty list if none
artifact.Tags = entity.Tags.ToList();              // Empty list if none
```

## Related Documentation

- **[Umbraco.AI.Deploy CLAUDE.md](../Umbraco.AI.Deploy/CLAUDE.md)** - Base connector patterns
- **[Umbraco.AI.Prompt CLAUDE.md](../Umbraco.AI.Prompt/CLAUDE.md)** - Prompt domain model
