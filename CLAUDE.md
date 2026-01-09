# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

> **Note:** This is the Umbraco.Ai.Prompt add-on package. See the [root CLAUDE.md](../CLAUDE.md) for shared coding standards, build commands, and repository-wide conventions that apply to all packages.

## Build Commands

```bash
# Build the solution
dotnet build Umbraco.Ai.Prompt.sln

# Build frontend assets (from Client directory)
cd src/Umbraco.Ai.Prompt.Web.StaticAssets/Client
npm install
npm run build

# Watch frontend during development
npm run watch
```

## Testing

```bash
# Run all tests
dotnet test Umbraco.Ai.Prompt.sln

# Run specific test project
dotnet test tests/Umbraco.Ai.Prompt.Tests.Unit/Umbraco.Ai.Prompt.Tests.Unit.csproj
```

## Architecture Overview

Umbraco.Ai.Prompt is a prompt management plugin for Umbraco.Ai. It provides storage, organization, and management of AI prompt templates with full backoffice UI integration.

### Project Structure

| Project | Purpose |
|---------|---------|
| `Umbraco.Ai.Prompt.Core` | Core domain models, services, and repository interfaces |
| `Umbraco.Ai.Prompt.Persistence` | EF Core DbContext, entities, and repository implementations |
| `Umbraco.Ai.Prompt.Persistence.SqlServer` | SQL Server migrations |
| `Umbraco.Ai.Prompt.Persistence.Sqlite` | SQLite migrations |
| `Umbraco.Ai.Prompt.Web` | Management API controllers, models, and mapping |
| `Umbraco.Ai.Prompt.Web.StaticAssets` | TypeScript/Lit frontend components |
| `Umbraco.Ai.Prompt.Startup` | Umbraco Composer for auto-discovery and DI registration |
| `Umbraco.Ai.Prompt` | Meta-package that bundles all components |

### Key Services

- `IPromptService` - Primary interface for prompt CRUD operations
- `IPromptRepository` - Repository interface for prompt persistence

### Domain Model

The `Prompt` entity represents a stored prompt template:
- `Id` - Unique identifier
- `Alias` - URL-safe unique identifier
- `Name` - Display name
- `Description` - Optional description
- `Content` - The prompt template text
- `ProfileId` - Optional link to Umbraco.Ai profile (soft FK)
- `Tags` - Categorization tags
- `IsActive` - Active status
- `DateCreated` / `DateModified` - Timestamps

### Management API

Endpoints are under `/umbraco/ai/management/api/v1/prompts/`:

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/prompts` | Get all prompts (paged) |
| GET | `/prompts/{promptIdOrAlias}` | Get prompt by ID or alias |
| GET | `/prompts/profile/{profileId}` | Get prompts by profile |
| POST | `/prompts` | Create prompt |
| PUT | `/prompts/{promptIdOrAlias}` | Update prompt by ID or alias |
| DELETE | `/prompts/{promptIdOrAlias}` | Delete prompt by ID or alias |

The `{promptIdOrAlias}` parameter accepts either a GUID (e.g., `550e8400-e29b-41d4-a716-446655440000`) or a string alias (e.g., `my-prompt-alias`). This pattern matches Umbraco.Ai's `IdOrAlias` convention.

The API shares the same Swagger group (`ai-management`) as Umbraco.Ai.

## Database Migrations

Migrations use the `UmbracoAiPrompt_` prefix.

```bash
# SQL Server
dotnet ef migrations add UmbracoAiPrompt_<MigrationName> \
  -p src/Umbraco.Ai.Prompt.Persistence.SqlServer \
  -c UmbracoAiPromptDbContext \
  --output-dir Migrations

# SQLite
dotnet ef migrations add UmbracoAiPrompt_<MigrationName> \
  -p src/Umbraco.Ai.Prompt.Persistence.Sqlite \
  -c UmbracoAiPromptDbContext \
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

- `Umbraco.Ai.Prompt.Core.Prompts` - Prompt domain model and services
- `Umbraco.Ai.Prompt.Persistence` - EF Core persistence
- `Umbraco.Ai.Prompt.Web.Api.Management.Prompt` - API controllers and models
- `Umbraco.Ai.Prompt.Extensions` - DI extension methods

## Configuration

```json
{
  "Umbraco": {
    "Ai": {
      "Prompt": {
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
- Umbraco.Ai 17.x
- Entity Framework Core 10.x
