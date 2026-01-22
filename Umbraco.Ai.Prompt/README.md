# Umbraco.Ai.Prompt

A prompt template management add-on for Umbraco.Ai that provides storage, organization, and management of AI prompt templates.

## Features

- **Prompt Management** - Store and manage AI prompt templates with full CRUD operations
- **Profile Integration** - Link prompts to Umbraco.Ai profiles for consistent configuration
- **Tag Organization** - Organize prompts using tags for easy categorization
- **Backoffice UI** - Full management interface integrated into Umbraco
- **Management API** - RESTful API for prompt CRUD operations
- **Alias Support** - Access prompts by GUID or URL-safe alias

## Monorepo Context

This package is part of the [Umbraco.Ai monorepo](../README.md). For local development, see the monorepo setup instructions in the root README.

## Installation

```bash
dotnet add package Umbraco.Ai.Prompt
```

This meta-package includes all required components. For more control, install individual packages:

| Package | Description |
|---------|-------------|
| `Umbraco.Ai.Prompt.Core` | Domain models and service interfaces |
| `Umbraco.Ai.Prompt.Web` | Management API controllers |
| `Umbraco.Ai.Prompt.Web.StaticAssets` | Backoffice UI components |
| `Umbraco.Ai.Prompt.Persistence` | EF Core persistence |
| `Umbraco.Ai.Prompt.Persistence.SqlServer` | SQL Server migrations |
| `Umbraco.Ai.Prompt.Persistence.Sqlite` | SQLite migrations |

## Requirements

- Umbraco CMS 17.0.0+
- Umbraco.Ai 17.0.0+
- .NET 10.0

## Prompt Model

A `Prompt` represents a stored prompt template:

| Property | Description |
|----------|-------------|
| `Id` | Unique identifier (GUID) |
| `Alias` | URL-safe unique identifier |
| `Name` | Display name |
| `Description` | Optional description |
| `Content` | The prompt template text |
| `ProfileId` | Optional link to Umbraco.Ai profile |
| `Tags` | Categorization tags |
| `IsActive` | Whether the prompt is available for use |

## Management API

All endpoints are under `/umbraco/ai/management/api/v1/prompts/`:

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/` | List all prompts (paged) |
| GET | `/{idOrAlias}` | Get prompt by ID or alias |
| GET | `/profile/{profileId}` | Get prompts by profile |
| POST | `/` | Create prompt |
| PUT | `/{idOrAlias}` | Update prompt |
| DELETE | `/{idOrAlias}` | Delete prompt |

The `{idOrAlias}` parameter accepts either a GUID or a string alias.

## Documentation

- **[CLAUDE.md](CLAUDE.md)** - Development guide, architecture, and technical details for this package
- **[Root CLAUDE.md](../CLAUDE.md)** - Shared coding standards and conventions
- **[Contributing Guide](../CONTRIBUTING.md)** - How to contribute to the monorepo

## License

This project is licensed under the MIT License. See [LICENSE.md](../LICENSE.md) for details.
