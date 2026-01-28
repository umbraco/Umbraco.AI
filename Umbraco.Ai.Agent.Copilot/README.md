# Umbraco.Ai.Agent.Copilot

Copilot chat UI for Umbraco AI Agent. This package provides the backoffice sidebar interface for interacting with AI agents through a chat-based interface.

## Overview

`Umbraco.Ai.Agent.Copilot` is the execution layer for AI agents, providing:

- **Copilot Sidebar** - Chat interface that appears in the Umbraco backoffice
- **Tool Execution System** - Framework for frontend-executable tools
- **HITL Approval System** - Human-in-the-loop approval workflows
- **AG-UI Integration** - Streaming protocol for real-time AI responses

## Installation

This package requires `Umbraco.Ai.Agent` to be installed first.

```bash
# Install both packages
dotnet add package Umbraco.Ai.Agent
dotnet add package Umbraco.Ai.Agent.Copilot
```

## Dependencies

| Package | Description |
|---------|-------------|
| `Umbraco.Ai.Agent` | Agent definition management (required) |
| `Umbraco.Ai` | Core AI infrastructure (transitive) |

## Architecture

This is a **frontend-only package** containing:

- TypeScript/Lit web components for the copilot UI
- Tool extension system for registering custom tools
- Approval element system for HITL workflows
- AG-UI client for streaming communication

```
Umbraco.Ai.Agent (Backend APIs, Agent Management UI)
         │
         ▼
Umbraco.Ai.Agent.Copilot (Chat UI, Tool Execution)
```

## Usage Scenarios

### Agent + Copilot (Full Experience)

Install both packages for the complete AI agent experience with chat interface:

```xml
<PackageReference Include="Umbraco.Ai.Agent" Version="1.0.0" />
<PackageReference Include="Umbraco.Ai.Agent.Copilot" Version="1.0.0" />
```

### Agent Only (Automation)

Install only the Agent package for programmatic agent usage without the chat UI:

```xml
<PackageReference Include="Umbraco.Ai.Agent" Version="1.0.0" />
```

## Frontend Development

### Building

```bash
# From repository root
npm run build:copilot

# Watch mode
npm run watch:copilot
```

### Package Structure

```
Umbraco.Ai.Agent.Copilot/
├── src/
│   └── Umbraco.Ai.Agent.Copilot/
│       └── Client/
│           ├── src/
│           │   ├── copilot/          # Chat UI components
│           │   │   ├── components/   # Sidebar, chat, input elements
│           │   │   ├── services/     # Tool manager, executor
│           │   │   ├── transport/    # AG-UI client
│           │   │   ├── tools/        # Tool extension system
│           │   │   └── approval/     # HITL approval system
│           │   └── lang/             # Localization
│           └── public/
│               └── umbraco-package.json
├── Umbraco.Ai.Agent.Copilot.sln
└── Directory.Build.props
```

## Documentation

For detailed documentation, see the [Umbraco.Ai documentation](../docs/public/add-ons/agent-copilot/README.md).

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE.md) file for details.
