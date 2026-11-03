# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

> **Note:** This is the Umbraco.AI.Deploy package. See the [root CLAUDE.md](../CLAUDE.md) for shared coding standards, build commands, and repository-wide conventions that apply to all packages.

## Build Commands

```bash
# Build the solution
dotnet build Umbraco.AI.Deploy.slnx

# Run tests
dotnet test Umbraco.AI.Deploy.slnx
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
- `AIContextArtifact` - Context with resources
- `AIGuardrailArtifact` - Guardrail with rules
- `AISettingsArtifact` - Global AI settings (default profiles)

**Service Connectors** - Transform entities to/from artifacts:
- `UmbracoAIConnectionServiceConnector` - Connection deployment
- `UmbracoAIProfileServiceConnector` - Profile deployment with dependency resolution
- `UmbracoAIContextServiceConnector` - Context deployment
- `UmbracoAIGuardrailServiceConnector` - Guardrail deployment
- `UmbracoAISettingsServiceConnector` - Settings deployment with default-profile dependencies

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
6. Writes artifact to disk (`umbraco/Deploy/Revision/*.uda`, committed to source control)

**2. Deployment (Target Environment)**

Each entity type is processed in a single pass (`ProcessPasses`), ordered so an entity's
dependencies are already saved when it runs:
- **Pass 1**: Connection
- **Pass 2**: Profile, Context, Guardrail
- **Pass 3**: Settings, plus Prompt and Agent (via `UmbracoAIProfileDependentEntityServiceConnectorBase`)

Example: AIProfile deployment
- Pass 2: Resolves ConnectionUdi → ConnectionId and creates/updates the profile in one step

Any ID an artifact references (profile, connection, context, guardrail) must be declared as an
artifact dependency in `GetArtifactAsync`, or Deploy can transfer the entity without it.

### Sensitive Settings

Rule lives in `UmbracoAISensitiveSettingsFilter`, applied on both export **and** import (so an
artifact written by an older version can't carry a secret into another environment):

- Sensitivity comes from the schema (`[AIField(IsSensitive = true)]`), matched case-insensitively.
  Connections use the provider settings schema; profiles, guardrail rules and context resources use
  their own schemas. A connection whose provider is unknown treats every string as sensitive.
- Sensitive fields keep only `$` configuration references (`$$` is a literal, not a reference),
  unless listed in `IncludeSensitiveSettings`.
- `ENC:` values are never deployed (each environment has its own Data Protection keys).
- `IgnoreSettings` drops a field whatever its value and wins over `IncludeSensitiveSettings`.
- On import the target keeps its own value for a sensitive field the artifact doesn't carry. A new
  connection missing a *required* secret gets `MissingSecretPlaceholder` and is imported inactive
  (and stays inactive on redeploy while the placeholder is in place).
- `IgnoreEncrypted` / `IgnoreSensitive` are obsolete no-ops.

**Configuration Reference Pattern:**

```csharp
// Source environment (appsettings.json) — must be under an allow-listed prefix
{
  "Umbraco": { "AI": { "Secrets": { "OpenAIApiKey": "sk-abc123..." } } }
}

// Connection settings
{
  "ApiKey": "$Umbraco:AI:Secrets:OpenAIApiKey"  // Reference, not actual value
}

// Target environment resolves from its own config
// (resolution is default-deny: only Umbraco:AI:Secrets / Umbraco:AI:Variables
//  resolve unless AIOptions.AllowedConfigurationKeyPrefixes is extended)
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

- **Match** - Entity must exist and match exactly (used for all Umbraco.AI entity references)
- **Exist** - Entity must exist (not currently used)

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
          "IgnoreSettings": ["specificFieldName"],
          "IncludeSensitiveSettings": []
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

- Umbraco CMS 18.x
- Umbraco.AI 18.x
- Umbraco Deploy 18.x

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
    protected override int[] ProcessPasses => [2]; // single pass: 1, 2 or 3 depending on dependencies

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
- Sensitive settings filtering (export and import, IgnoreSettings, IncludeSensitiveSettings)
- Optional property handling (null cases)

## Deployment Strategy

**Disk-Based (Not UI Transfer):**
- Entities deployed via git-committed artifacts
- No "Queue for Transfer" UI
- Use `RegisterDiskEntityType`, not `RegisterTransferEntityType`
- Register disk entity types once, in the package's `IAsyncComponent` (e.g. `UmbracoAIDeployComponent`), not in notification handler constructors
- Configuration managed through backoffice, deployed through git

**Why disk-based?**
- AI configuration is infrastructure
- Version control for AI setup
- GitOps workflow support
- Consistent across environments
