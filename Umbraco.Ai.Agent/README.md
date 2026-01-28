# Umbraco.Ai.Agent

An agent management add-on for Umbraco.Ai that provides storage, execution, and management of AI agents with full [AG-UI protocol](https://docs.ag-ui.com/) support.

## Features

- **Agent Management** - Store and manage AI agent definitions with instructions and configuration
- **Agent Execution** - Run agents with real-time SSE streaming via the AG-UI protocol
- **Profile Integration** - Link agents to Umbraco.Ai profiles for model configuration
- **Context Injection** - Attach context sources to agents for RAG scenarios
- **Backoffice UI** - Agent management interface integrated into Umbraco
- **Management API** - RESTful API for agent CRUD operations and execution

> **Note:** For the Copilot chat UI (sidebar, tool execution, HITL approval), install [`Umbraco.Ai.Agent.Copilot`](../Umbraco.Ai.Agent.Copilot/README.md) alongside this package.

## Monorepo Context

This package is part of the [Umbraco.Ai monorepo](../README.md). For local development, see the monorepo setup instructions in the root README.

## Installation

```bash
# Agent only (management + APIs)
dotnet add package Umbraco.Ai.Agent

# Agent + Copilot (includes chat UI)
dotnet add package Umbraco.Ai.Agent
dotnet add package Umbraco.Ai.Agent.Copilot
```

This meta-package includes all required components. For more control, install individual packages:

| Package | Description |
|---------|-------------|
| `Umbraco.Ai.Agent.Core` | Domain models and service interfaces |
| `Umbraco.Ai.Agent.Web` | Management API controllers |
| `Umbraco.Ai.Agent.Web.StaticAssets` | Backoffice UI components |
| `Umbraco.Ai.Agent.Persistence` | EF Core persistence |
| `Umbraco.Ai.Agent.Persistence.SqlServer` | SQL Server migrations |
| `Umbraco.Ai.Agent.Persistence.Sqlite` | SQLite migrations |
| `Umbraco.Ai.Agui` | AG-UI protocol SDK |

## Requirements

- Umbraco CMS 17.0.0+
- Umbraco.Ai 1.0.0+
- .NET 10.0

## Agent Model

An `AiAgent` represents a stored agent definition:

| Property | Description |
|----------|-------------|
| `Id` | Unique identifier (GUID) |
| `Alias` | URL-safe unique identifier |
| `Name` | Display name |
| `Description` | Optional description |
| `ProfileId` | Umbraco.Ai profile for model configuration |
| `ContextIds` | Context sources for RAG injection |
| `Instructions` | System instructions defining agent behavior |
| `IsActive` | Whether the agent is available for use |

## Management API

All endpoints are under `/umbraco/ai/management/api/v1/agents/`:

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/` | List all agents (paged) |
| GET | `/{idOrAlias}` | Get agent by ID or alias |
| POST | `/` | Create agent |
| PUT | `/{idOrAlias}` | Update agent |
| DELETE | `/{idOrAlias}` | Delete agent |
| POST | `/{idOrAlias}/run` | Run agent with AG-UI streaming |

The `{idOrAlias}` parameter accepts either a GUID or a string alias.

### Running Agents

The `/run` endpoint accepts an AG-UI `RunRequest` and returns a Server-Sent Events stream:

```http
POST /umbraco/ai/management/api/v1/agents/my-agent/run
Content-Type: application/json

{
  "threadId": "thread-123",
  "runId": "run-456",
  "messages": [
    { "role": "user", "content": "Hello!" }
  ]
}
```

Response: `text/event-stream` with AG-UI events.

## Umbraco.Ai.Agui

The `Umbraco.Ai.Agui` package is a standalone AG-UI protocol SDK that provides:

- **Event Types** - All AG-UI event models (lifecycle, messages, tools, state)
- **SSE Streaming** - `AguiEventStreamResult` for ASP.NET Core streaming
- **Models** - `AguiRunRequest`, `AguiMessage`, `AguiTool`, etc.

This package can be used independently of the Agent add-on to build custom AG-UI endpoints.

## Architecture

Built on the [Microsoft Agent Framework (MAF)](https://github.com/microsoft/Agents-for-net) for agent execution, with a custom AG-UI layer that provides:

- Umbraco authorization integration
- Frontend tool handling with client-side execution
- Custom context item injection

## Documentation

- **[CLAUDE.md](CLAUDE.md)** - Development guide, architecture, and technical details for this package
- **[Root CLAUDE.md](../CLAUDE.md)** - Shared coding standards and conventions
- **[Contributing Guide](../CONTRIBUTING.md)** - How to contribute to the monorepo

## License

This project is licensed under the MIT License. See [LICENSE.md](../LICENSE.md) for details.
