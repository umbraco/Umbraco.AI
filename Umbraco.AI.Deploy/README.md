# Umbraco.AI.Deploy

Umbraco Deploy support for Umbraco.AI - enables deployment of AI connections, profiles, and contexts across environments using disk-based artifacts.

## Features

- **Connection Deployment** - Deploy AI provider connections with configuration references
- **Profile Deployment** - Deploy AI profiles with model configurations and settings
- **Context Deployment** - Deploy AI contexts with resource definitions (future)
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
| AIContext | `umbraco-ai-context` | AI contexts (future) |

## Configuration

Configure deployment behavior in `appsettings.json`:

```json
{
  "Umbraco": {
    "AI": {
      "Deploy": {
        "Connections": {
          "IgnoreEncrypted": true,
          "IgnoreSensitive": true,
          "IgnoreSettings": []
        }
      }
    }
  }
}
```

### Settings Filtering

Control which connection settings are deployed:

- **IgnoreEncrypted** - Block encrypted values (`ENC:...`), allow `$` config references
- **IgnoreSensitive** - Block all sensitive fields (marked with `[AIField(IsSensitive = true)]`)
- **IgnoreSettings** - Block specific field names (highest precedence)

**Example: Using Configuration References**

```csharp
var connection = new AIConnection
{
    ProviderId = "openai",
    Settings = new OpenAIProviderSettings
    {
        ApiKey = "$OpenAI:ApiKey"  // Resolved from appsettings.json
    }
};
```

The `$OpenAI:ApiKey` reference is preserved during deployment and resolved in the target environment.

## How It Works

### Artifact Generation

When you save an entity (connection, profile, context), Deploy automatically:

1. Creates a JSON artifact in your project's deploy folder
2. Serializes entity properties and relationships
3. Adds dependency references (e.g., profile → connection)
4. Filters sensitive settings based on configuration

### Deployment Process

When deploying to another environment, Deploy:

1. **Pass 2**: Creates/updates base entities
2. **Pass 4**: Resolves dependencies and updates references
3. Validates all dependencies exist
4. Resolves configuration references from target environment

## Related Packages

- **[Umbraco.AI.Deploy.Prompt](../Umbraco.AI.Deploy.Prompt)** - Deploy support for AI prompts
- **[Umbraco.AI.Deploy.Agent](../Umbraco.AI.Deploy.Agent)** - Deploy support for AI agents

## Documentation

- **[CLAUDE.md](CLAUDE.md)** - Development guide and architecture details
- **[Root CLAUDE.md](../CLAUDE.md)** - Shared coding standards
- **[Contributing Guide](../CONTRIBUTING.md)** - How to contribute

## License

This project is licensed under the MIT License. See [LICENSE.md](../LICENSE.md) for details.
