# Umbraco.AI.Prompt.Deploy

Umbraco Deploy support for Umbraco.AI.Prompt - enables deployment of AI prompt templates across environments using disk-based artifacts.

## Features

- **Prompt Deployment** - Deploy prompt templates with instructions and configurations
- **Profile Dependencies** - Automatic resolution of prompt-to-profile relationships
- **Scope Deployment** - Deploy prompt scoping rules for content types and properties
- **Tag Preservation** - Maintain prompt tags and categorization
- **Context References** - Deploy context ID references (future)
- **Disk-Based Artifacts** - Git-friendly JSON artifacts for version control

## Monorepo Context

This package is part of the [Umbraco.AI monorepo](../README.md). For local development, see the monorepo setup instructions in the root README.

## Installation

```bash
dotnet add package Umbraco.AI.Prompt.Deploy
```

**Prerequisites:**
- `Umbraco.AI.Deploy` (Core Deploy support)
- `Umbraco.AI.Prompt` (Prompt management)

## Requirements

- Umbraco CMS 17.0.0+
- Umbraco.AI 1.0.0+
- Umbraco.AI.Prompt 1.0.0+
- Umbraco.AI.Deploy 1.0.0+
- Umbraco Deploy 17.0.0+
- .NET 10.0

## Supported Entities

| Entity | UDI Type | Description |
| ------ | -------- | ----------- |
| AIPrompt | `umbraco-ai-prompt` | AI prompt templates |

## How It Works

### Artifact Properties

AIPrompt artifacts include:
- **Basic Properties**: Alias, Name, Description, Instructions
- **Profile Dependency**: Optional ProfileUdi reference
- **Context References**: Array of context IDs (future)
- **Tags**: Categorization tags
- **Scope**: JSON-serialized scope rules (content types, properties)
- **Settings**: IsActive, IncludeEntityContext, OptionCount

### Deployment Process

**Pass 2**: Creates/updates prompt with basic properties
**Pass 4**: Resolves ProfileUdi → ProfileId dependency

Example:
```
Source Environment:
  Prompt "Generate Summary" → Profile "GPT-4 Summary"

Target Environment:
  Pass 2: Creates prompt with temporary ProfileId
  Pass 4: Resolves Profile UDI, updates prompt with correct ProfileId
```

## Related Packages

- **[Umbraco.AI.Deploy](../Umbraco.AI.Deploy)** - Core Deploy support
- **[Umbraco.AI.Agent.Deploy](../Umbraco.AI.Agent.Deploy)** - Deploy support for AI agents
- **[Umbraco.AI.Prompt](../Umbraco.AI.Prompt)** - Prompt management

## Documentation

- **[CLAUDE.md](CLAUDE.md)** - Development guide and architecture details
- **[Root CLAUDE.md](../CLAUDE.md)** - Shared coding standards
- **[Contributing Guide](../CONTRIBUTING.md)** - How to contribute

## License

This project is licensed under the MIT License. See [LICENSE.md](../LICENSE.md) for details.
