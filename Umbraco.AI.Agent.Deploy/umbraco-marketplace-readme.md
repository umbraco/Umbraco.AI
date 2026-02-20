## Umbraco.AI.Agent.Deploy

Umbraco Deploy support for AI agents - deploy agents with tool permissions, user group configurations, and scoping rules across environments.

### Features

- **Agent Deployment** - Deploy AI agents with instructions and tool configurations
- **Profile Dependencies** - Automatic resolution of agent-to-profile relationships
- **Tool Permissions** - Deploy direct tool IDs and scope-based permissions
- **User Group Permissions** - Deploy per-group tool permission overrides with validation
- **Scope Deployment** - Deploy scoping rules for sections and entity types
- **Surface Configuration** - Deploy surface IDs (backoffice, frontend, custom)
- **Dependency Validation** - Ensures all user groups exist in target environment
- **Disk-Based Artifacts** - Git-friendly JSON artifacts for GitOps workflows

### Requirements

- Umbraco CMS 17.0.0+
- Umbraco.AI 1.0.0+
- Umbraco.AI.Agent 1.0.0+
- Umbraco.AI.Deploy 1.0.0+
- Umbraco Deploy 17.0.0+
- .NET 10.0

### Installation

```bash
dotnet add package Umbraco.AI.Agent.Deploy
```

Requires both `Umbraco.AI.Deploy` and `Umbraco.AI.Agent` to be installed.

### Tool Permission Model

Agents have fine-grained tool access control:
- **AllowedToolIds**: Specific tool IDs ["content-get", "media-upload"]
- **AllowedToolScopeIds**: Scope-based access ["content-read", "media-write"]
- **UserGroupPermissions**: Per-group overrides with tool ID lists
