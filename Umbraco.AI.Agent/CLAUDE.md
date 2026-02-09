# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

> **Note:** This is the Umbraco.AI.Agent add-on package. See the [root CLAUDE.md](../CLAUDE.md) for shared coding standards, build commands, and repository-wide conventions that apply to all packages.

## Build Commands

```bash
# Build the solution
dotnet build Umbraco.AI.Agent.sln

# Build frontend assets (from Client directory)
cd src/Umbraco.AI.Agent.Web.StaticAssets/Client
npm install
npm run build

# Watch frontend during development
npm run watch
```

## Testing

```bash
# Run all tests
dotnet test Umbraco.AI.Agent.sln

# Run specific test project
dotnet test tests/Umbraco.AI.Agent.Tests.Unit/Umbraco.AI.Agent.Tests.Unit.csproj
```

## Architecture Overview

Umbraco.AI.Agent is an agent management plugin for Umbraco.AI. It provides storage, organization, and management of AI agent definitions with full backoffice UI integration.

### Project Structure

| Project                                  | Purpose                                                     |
| ---------------------------------------- | ----------------------------------------------------------- |
| `Umbraco.AI.Agent.Core`                  | Core domain models, services, and repository interfaces     |
| `Umbraco.AI.Agent.Persistence`           | EF Core DbContext, entities, and repository implementations |
| `Umbraco.AI.Agent.Persistence.SqlServer` | SQL Server migrations                                       |
| `Umbraco.AI.Agent.Persistence.Sqlite`    | SQLite migrations                                           |
| `Umbraco.AI.Agent.Web`                   | Management API controllers, models, and mapping             |
| `Umbraco.AI.Agent.Web.StaticAssets`      | TypeScript/Lit frontend components                          |
| `Umbraco.AI.Agent.Startup`               | Umbraco Composer for auto-discovery and DI registration     |
| `Umbraco.AI.Agent`                       | Meta-package that bundles all components                    |

### Key Services

- `IAIAgentService` - Primary interface for agent CRUD operations and tool permission validation
  - `GetAllowedToolIdsAsync(agent)` - Returns all tool IDs allowed for an agent (both direct and scope-based)
  - `IsToolEnabledAsync(agent, toolId)` - Checks if a specific tool is allowed for an agent
- `IAIAgentRepository` - Repository interface for agent persistence

### Domain Model

The `AIAgent` entity represents a stored agent definition:

- `Id` - Unique identifier
- `Alias` - URL-safe unique identifier
- `Name` - Display name
- `Description` - Optional description
- `Content` - The agent definition text
- `ProfileId` - Optional link to Umbraco.AI profile (soft FK)
- `Tags` - Categorization tags
- `IsActive` - Active status
- `AllowedToolIds` - List of specific tool IDs allowed for this agent
- `AllowedToolScopeIds` - List of tool scope IDs allowed for this agent (grants access to all tools in those scopes)
- `DateCreated` / `DateModified` - Timestamps

### Agent Tool Permissions

Agents can have fine-grained control over which tools they can access through two mechanisms:

**1. Direct Tool Access** (`AllowedToolIds`)
- Explicitly grant access to specific tools by their ID
- Example: `["umbraco-content-get", "umbraco-media-upload"]`

**2. Scope-Based Access** (`AllowedToolScopeIds`)
- Grant access to all tools within a scope
- Scopes represent categories of operations (e.g., "content-read", "media-write")
- Example: `["content-read", "search"]` grants access to all tools in those scopes

**Permission Resolution:**
- A tool is enabled if it appears in `AllowedToolIds` OR if its scope appears in `AllowedToolScopeIds`
- Case-insensitive comparison for both tool IDs and scope IDs
- Permission checks are performed via `IAIAgentService.IsToolEnabledAsync(agent, toolId)`

**Usage Example:**
```csharp
// Check if a specific tool is allowed for an agent
bool canUseContentGet = await agentService.IsToolEnabledAsync(agent, "umbraco-content-get");

// Get all enabled tool IDs for an agent
IReadOnlyList<string> allowedTools = await agentService.GetAllowedToolIdsAsync(agent);
```

### Management API

Endpoints are under `/umbraco/ai/management/api/v1/agents/`:

| Method | Endpoint                      | Description                 |
| ------ | ----------------------------- | --------------------------- |
| GET    | `/agents`                     | Get all agents (paged)      |
| GET    | `/agents/{agentIdOrAlias}`    | Get agent by ID or alias    |
| GET    | `/agents/profile/{profileId}` | Get agents by profile       |
| POST   | `/agents`                     | Create agent                |
| PUT    | `/agents/{agentIdOrAlias}`    | Update agent by ID or alias |
| DELETE | `/agents/{agentIdOrAlias}`    | Delete agent by ID or alias |

The `{agentIdOrAlias}` parameter accepts either a GUID (e.g., `550e8400-e29b-41d4-a716-446655440000`) or a string alias (e.g., `my-agent-alias`). This pattern matches Umbraco.AI's `IdOrAlias` convention.

The API shares the same Swagger group (`ai-management`) as Umbraco.AI.

## Database Migrations

Migrations use the `UmbracoAIAgent_` prefix.

```bash
# SQL Server
dotnet ef migrations add UmbracoAIAgent_<MigrationName> \
  -p src/Umbraco.AI.Agent.Persistence.SqlServer \
  -c UmbracoAIAgentDbContext \
  --output-dir Migrations

# SQLite
dotnet ef migrations add UmbracoAIAgent_<MigrationName> \
  -p src/Umbraco.AI.Agent.Persistence.Sqlite \
  -c UmbracoAIAgentDbContext \
  --output-dir Migrations
```

## Project Organization

### Core Principles (Feature-Sliced Architecture)

1. **Feature folders are flat** - All files for a feature live at the folder root
2. **Interfaces and implementations live side-by-side**
3. **Shared code lives at the project root level** (Models/, Configuration/)

### Web Layer (Layer-Based Organization)

Web follows Umbraco CMS Management API conventions:

- `Controllers/` - API endpoints
- `Models/` - Request/response DTOs
- `Mapping/` - UmbracoMapper definitions

## Key Namespaces

- `Umbraco.AI.Agent.Core.Agents` - Agent domain model and services
- `Umbraco.AI.Agent.Persistence` - EF Core persistence
- `Umbraco.AI.Agent.Web.Api.Management.Agent` - API controllers and models
- `Umbraco.AI.Agent.Extensions` - DI extension methods

## Configuration

```json
{
    "Umbraco": {
        "AI": {
            "Agent": {
                // Future configuration options
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
- Entity Framework Core 10.x
