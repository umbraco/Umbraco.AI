# Umbraco.AI.Deploy

Umbraco Deploy support for Umbraco.AI - enables deployment of AI connections, profiles, and contexts across environments using disk-based artifacts.

## Features

- **Connection Deployment** - Deploy AI provider connections with configuration references
- **Profile Deployment** - Deploy AI profiles with model configurations and settings
- **Context Deployment** - Deploy AI contexts with resource definitions
- **Guardrail Deployment** - Deploy AI guardrails and their rules
- **Settings Deployment** - Deploy global AI settings (default profiles)
- **Dependency Resolution** - Automatic resolution of cross-entity dependencies
- **Configuration References** - Support for `$` config references to avoid storing secrets
- **Disk-Based Artifacts** - Git-friendly JSON artifacts for version control
- **Multi-Pass Processing** - Intelligent dependency resolution during deployment

## Monorepo Context

This package is part of the [Umbraco.AI monorepo](../README.md). For local development, see the monorepo setup instructions in the root README.

## Installation

```bash
dotnet add package Umbraco.AI.Deploy
```

## Requirements

- Umbraco CMS 17.0.0+
- Umbraco.AI 1.0.0+
- Umbraco Deploy 17.0.0+
- .NET 10.0

## Supported Entities

| Entity | UDI Type | Description |
| ------ | -------- | ----------- |
| AIConnection | `umbraco-ai-connection` | AI provider connections |
| AIProfile | `umbraco-ai-profile` | AI model profiles |
| AIContext | `umbraco-ai-context` | AI contexts |
| AIGuardrail | `umbraco-ai-guardrail` | AI guardrails |
| AISettings | `umbraco-ai-settings` | Global AI settings |

## Configuration

Configure deployment behavior in `appsettings.json`:

```json
{
  "Umbraco": {
    "AI": {
      "Deploy": {
        "Connections": {
          "IgnoreSettings": [],
          "IncludeSensitiveSettings": []
        }
      }
    }
  }
}
```

### Sensitive Settings

Sensitive settings (fields a provider marks `[AIField(IsSensitive = true)]`, such as API keys and client secrets) are never written to a deploy artifact, or read from one, as a literal value:

- **Configuration references are deployed.** A `$Umbraco:AI:Secrets:...` reference only names a setting, so it's safe to commit, and each environment resolves it from its own configuration. This is the recommended way to deploy credentials.
- **Encrypted values are never deployed.** Each environment has its own encryption keys, so an encrypted value can't be read anywhere else.
- **The target environment keeps its own values.** A deploy never clears a secret already set on the target.
- **A connection that arrives without a required secret is imported inactive.** Set the value on that environment, then activate the connection.

The same rule applies to sensitive fields in profile capability settings, guardrail rule configuration and context resource settings.

Two settings fine-tune what's deployed (both matched case-insensitively):

- **IgnoreSettings** - Connection setting names never to deploy, whatever their value (configuration references included). Takes precedence over `IncludeSensitiveSettings`.
- **IncludeSensitiveSettings** - Sensitive connection setting names whose literal values may be deployed. **These values are written in plain text to the artifact on disk, and so to source control.** Prefer a configuration reference.

`IgnoreEncrypted` and `IgnoreSensitive` are obsolete and have no effect.

**Example: Using Configuration References**

```csharp
var connection = new AIConnection
{
    ProviderId = "openai",
    Settings = new OpenAIProviderSettings
    {
        ApiKey = "$Umbraco:AI:Secrets:OpenAIApiKey"  // Resolved from appsettings.json
    }
};
```

The `$Umbraco:AI:Secrets:OpenAIApiKey` reference is preserved during deployment and resolved in the target environment. Resolution is default-deny — only keys under an allow-listed prefix (default `Umbraco:AI:Secrets` / `Umbraco:AI:Variables`) resolve, and secret keys only into `IsSensitive` fields.

## How It Works

### Artifact Generation

When you save an entity (connection, profile, context), Deploy automatically:

1. Creates a JSON artifact in your project's deploy folder
2. Serializes entity properties and relationships
3. Adds dependency references (e.g., profile → connection, profile → contexts and guardrails)
4. Filters sensitive settings based on configuration

### Deployment Process

When deploying to another environment, Deploy:

1. Validates all dependencies exist
2. Processes each entity type in a single pass, ordered so dependencies are saved first:
   - **Pass 1**: Connections
   - **Pass 2**: Profiles, contexts, guardrails
   - **Pass 3**: Settings, prompts, agents (from the add-on Deploy packages)
3. Resolves configuration references from target environment

## Related Packages

- **[Umbraco.AI.Prompt.Deploy](../Umbraco.AI.Prompt.Deploy)** - Deploy support for AI prompts
- **[Umbraco.AI.Agent.Deploy](../Umbraco.AI.Agent.Deploy)** - Deploy support for AI agents

## Documentation

- **[CLAUDE.md](CLAUDE.md)** - Development guide and architecture details
- **[Root CLAUDE.md](../CLAUDE.md)** - Shared coding standards
- **[Contributing Guide](../CONTRIBUTING.md)** - How to contribute

## License

This project is licensed under the MIT License. See [LICENSE.md](../LICENSE.md) for details.
