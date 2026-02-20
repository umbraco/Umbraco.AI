# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

> **Note:** This is the Umbraco.AI.Deploy package. See the [root CLAUDE.md](../CLAUDE.md) for shared coding standards, build commands, and repository-wide conventions that apply to all packages.

## Build Commands

```bash
# Build the solution
dotnet build Umbraco.AI.Deploy.sln

# Run tests
dotnet test Umbraco.AI.Deploy.sln
```

## Architecture Overview

Umbraco.AI.Deploy provides Umbraco Deploy integration for Umbraco.AI entities. It enables deployment of AI connections, profiles, and contexts across environments using disk-based artifacts.

### Project Structure

This package uses a **simplified single-project structure** (different from Core/Prompt/Agent):

| Project                          | Purpose                                         |
| -------------------------------- | ----------------------------------------------- |
| `Umbraco.AI.Deploy`              | Service connectors, artifacts, and notification handlers |
| `Umbraco.AI.Deploy.Tests.Unit`   | Unit tests                                      |

**Why single-project?**
- Deploy packages are integration layers, not domain models
- No database persistence needed (uses Deploy's infrastructure)
- No backoffice UI components
- Simpler structure for focused scope

### Key Components

**Artifacts** - Serializable deployment representations:
- `AIConnectionArtifact` - Connection with filtered settings
- `AIProfileArtifact` - Profile with model configuration
- `AIContextArtifact` - Context with resources (future)

**Service Connectors** - Transform entities to/from artifacts:
- `UmbracoAIConnectionServiceConnector` - Connection deployment
- `UmbracoAIProfileServiceConnector` - Profile deployment with dependency resolution
- `UmbracoAIContextServiceConnector` - Context deployment (future)

**Base Classes**:
- `UmbracoAIEntityServiceConnectorBase<TArtifact, TEntity>` - Common artifact operations
- `UmbracoAIProfileDependentEntityServiceConnectorBase<TArtifact, TEntity>` - Profile dependency handling

**Notification Handlers** - Auto-write artifacts on save/delete:
- `UmbracoAIEntitySavedDeployRefresherNotificationAsyncHandlerBase`
- `UmbracoAIEntityDeletedDeployRefresherNotificationAsyncHandlerBase`

**Value Connectors** - Convert property editor values:
- `UmbracoAIContextPickerValueConnector` - Context picker GUIDs → UDIs

### Deployment Workflow

**1. Artifact Creation (Source Environment)**

When an entity is saved:
1. Notification handler triggered
2. Service connector creates artifact
3. Serializes properties to JSON
4. Adds dependency UDIs
5. Filters sensitive settings
6. Writes artifact to disk (e.g., `~/data/UmbracoAI/AIConnection__abc123.uda`)

**2. Deployment (Target Environment)**

Multi-pass processing:
- **Pass 2**: Create/update entities with basic properties
- **Pass 4**: Resolve dependencies and update references

Example: AIProfile deployment
- Pass 2: Creates profile with temporary ConnectionId
- Pass 4: Resolves ConnectionUdi → ConnectionId, updates profile

### Settings Filtering

Three-layer filtering for AIConnection settings:

**Layer 1: IgnoreSettings (Highest Priority)**
- Specific field names to always block
- Most granular control
- Example: `["ApiKey", "SecretKey"]`

**Layer 2: IgnoreSensitive**
- Blocks fields marked `[AIField(IsSensitive = true)]`
- Blocks all values (even `$` references)
- Use for fields that should never be deployed

**Layer 3: IgnoreEncrypted**
- Blocks encrypted values (`ENC:...`)
- **Allows `$` configuration references**
- Most common setting

**Configuration Reference Pattern:**

```csharp
// Source environment (appsettings.json)
{
  "OpenAI": {
    "ApiKey": "sk-abc123..."
  }
}

// Connection settings
{
  "ApiKey": "$OpenAI:ApiKey"  // Reference, not actual value
}

// Target environment resolves from its own config
```

### UDI Registration

Deploy uses UDI (Umbraco Unique Identifier) for entity references:

```csharp
UdiParser.RegisterUdiType(UmbracoAIConstants.UdiEntityType.Connection, UdiType.GuidUdi);
UdiParser.RegisterUdiType(UmbracoAIConstants.UdiEntityType.Profile, UdiType.GuidUdi);
UdiParser.RegisterUdiType(UmbracoAIConstants.UdiEntityType.Context, UdiType.GuidUdi);
```

### Dependency Modes

When adding dependencies to artifacts:

- **Match** - Entity must exist and match exactly (default for profiles)
- **Exist** - Entity must exist (used for user groups)

```csharp
dependencies.Add(new UmbracoAIArtifactDependency(
    profileUdi,
    ArtifactDependencyMode.Match));
```

## Configuration

```json
{
  "Umbraco": {
    "AI": {
      "Deploy": {
        "Connections": {
          "IgnoreEncrypted": true,
          "IgnoreSensitive": false,
          "IgnoreSettings": ["SpecificFieldName"]
        }
      }
    }
  }
}
```

## Target Framework

- .NET 10.0 (`net10.0`)
- Uses Central Package Management (`Directory.Packages.props`)
- Nullable reference types enabled

## Dependencies

- Umbraco CMS 17.x
- Umbraco.AI 1.x
- Umbraco Deploy 17.x

## Related Packages

- **Umbraco.AI.Prompt.Deploy** - Deploy support for prompts
- **Umbraco.AI.Agent.Deploy** - Deploy support for agents

## Key Patterns

### Service Connector Pattern

All service connectors follow this structure:

```csharp
[UdiDefinition(Constants.UdiEntityType, UdiType.GuidUdi)]
public class EntityServiceConnector : UmbracoAIEntityServiceConnectorBase<TArtifact, TEntity>
{
    public override string UdiEntityType => Constants.UdiEntityType;
    protected override int[] ProcessPasses => new[] { 2, 4 };

    public override Task<TArtifact?> GetArtifactAsync(GuidUdi? udi, TEntity? entity, CancellationToken ct);
    public override Task ProcessAsync(ArtifactDeployState<TArtifact, TEntity> state, IDeployContext context, int pass, CancellationToken ct);
}
```

### Notification Handler Pattern

Handlers automatically manage artifact lifecycle:

```csharp
internal sealed class EntitySavedHandler
    : UmbracoAIEntitySavedDeployRefresherNotificationAsyncHandlerBase<TEntity, TNotification>
{
    public EntitySavedHandler(
        IServiceConnectorFactory serviceConnectorFactory,
        IDiskEntityService diskEntityService,
        ISignatureService signatureService)
        : base(serviceConnectorFactory, diskEntityService, signatureService, UdiEntityType)
    { }

    protected override object GetEntityId(TEntity entity) => entity.Id;
}
```

## Testing

Unit tests focus on:
- Artifact creation with all properties
- Dependency tracking
- Settings filtering (IgnoreSettings, IgnoreSensitive, IgnoreEncrypted)
- Optional property handling (null cases)
- User group dependency validation

## Deployment Strategy

**Disk-Based (Not UI Transfer):**
- Entities deployed via git-committed artifacts
- No "Queue for Transfer" UI
- Use `RegisterDiskEntityType`, not `RegisterTransferEntityType`
- Configuration managed through backoffice, deployed through git

**Why disk-based?**
- AI configuration is infrastructure
- Version control for AI setup
- GitOps workflow support
- Consistent across environments
