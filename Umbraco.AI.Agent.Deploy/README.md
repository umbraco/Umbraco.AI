# Umbraco.AI.Agent.Deploy

Umbraco Deploy support for Umbraco.AI.Agent - enables deployment of AI agents with tool permissions and user group configurations across environments.

## Features

- **Agent Deployment** - Deploy AI agents with instructions and configurations
- **Profile Dependencies** - Automatic resolution of agent-to-profile relationships
- **Tool Permissions** - Deploy AllowedToolIds and AllowedToolScopeIds
- **User Group Permissions** - Deploy per-group tool permission overrides
- **Context and Guardrail Dependencies** - Referenced contexts and guardrails are declared as dependencies
- **Scope Deployment** - Deploy agent scoping rules for sections and entity types
- **Surface Configuration** - Deploy surface IDs (backoffice, frontend)
- **Disk-Based Artifacts** - Git-friendly JSON artifacts for version control

## Monorepo Context

This package is part of the [Umbraco.AI monorepo](../README.md). For local development, see the monorepo setup instructions in the root README.

## Installation

```bash
dotnet add package Umbraco.AI.Agent.Deploy
```

**Prerequisites:**
- `Umbraco.AI.Deploy` (Core Deploy support)
- `Umbraco.AI.Agent` (Agent management)

## Requirements

- Umbraco CMS 17.0.0+
- Umbraco.AI 1.0.0+
- Umbraco.AI.Agent 1.0.0+
- Umbraco.AI.Deploy 1.0.0+
- Umbraco Deploy 17.0.0+
- .NET 10.0

## Supported Entities

| Entity | UDI Type | Description |
| ------ | -------- | ----------- |
| AIAgent | `umbraco-ai-agent` | AI agents with tool permissions |

## How It Works

### Artifact Properties

AIAgent artifacts include:
- **Basic Properties**: Alias, Name, Description, Instructions
- **Profile Dependency**: Optional ProfileUdi reference
- **Context References**: Array of context IDs in the standard agent config (each declared as a dependency)
- **Guardrail References**: Array of guardrail IDs (each declared as a dependency)
- **Surface IDs**: Array of surface identifiers (backoffice, frontend)
- **Scope**: JSON-serialized scope rules (sections, entity types)
- **Tool Permissions**:
  - AllowedToolIds: Specific tool IDs allowed for the agent
  - AllowedToolScopeIds: Tool scope IDs (grants access to all tools in scope)
- **User Group Permissions**: Per-group tool permission overrides
- **Settings**: IsActive flag

### Deployment Process

Agents are processed in a single pass (**Pass 3**), after connections (pass 1) and profiles,
contexts and guardrails (pass 2) are in place.

Example:
```
Source Environment:
  Agent "Content Assistant" → Profile "GPT-4", Context "Brand Voice"

Target Environment:
  Pass 2: Profile and context are created/updated
  Pass 3: Resolves Profile UDI → ProfileId and creates/updates the agent
```

## Tool Permission Model

Agents have fine-grained tool access control:

**Direct Tool Access** (`AllowedToolIds`):
```json
["umbraco-content-get", "umbraco-media-upload"]
```

**Scope-Based Access** (`AllowedToolScopeIds`):
```json
["content-read", "media-write"]
```

A tool is enabled if:
- It appears in AllowedToolIds, OR
- Its scope appears in AllowedToolScopeIds

**Per-User-Group Overrides** (`UserGroupPermissions`):
```json
{
  "editor-guid": { "AllowedToolIds": ["read", "write"] },
  "admin-guid": { "AllowedToolIds": ["read", "write", "delete"] }
}
```

## Related Packages

- **[Umbraco.AI.Deploy](../Umbraco.AI.Deploy)** - Core Deploy support
- **[Umbraco.AI.Prompt.Deploy](../Umbraco.AI.Prompt.Deploy)** - Deploy support for AI prompts
- **[Umbraco.AI.Agent](../Umbraco.AI.Agent)** - Agent management

## Documentation

- **[CLAUDE.md](CLAUDE.md)** - Development guide and architecture details
- **[Root CLAUDE.md](../CLAUDE.md)** - Shared coding standards
- **[Contributing Guide](../CONTRIBUTING.md)** - How to contribute

## License

This project is licensed under the MIT License. See [LICENSE.md](../LICENSE.md) for details.
