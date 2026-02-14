# Umbraco.AI.Agent.Copilot

Copilot chat UI for Umbraco AI Agent. This package provides the backoffice sidebar interface for interacting with AI agents through a chat-based interface.

## Overview

`Umbraco.AI.Agent.Copilot` is a **frontend-only package** that provides the copilot chat UI for AI agents. It builds on top of `Umbraco.AI.Agent.UI` (the reusable chat infrastructure) and adds:

- **Copilot Sidebar** - Chat interface that appears in the Umbraco backoffice
- **Header Button** - Quick access to the copilot from the backoffice header
- **AG-UI Transport** - Streaming protocol implementation for real-time AI responses
- **Copilot-Specific Features** - Context awareness and entity integration

## Installation

This package requires `Umbraco.AI.Agent` to be installed first.

```bash
# Install both packages
dotnet add package Umbraco.AI.Agent
dotnet add package Umbraco.AI.Agent.Copilot
```

## Dependencies

| Package                | Description                            |
| ---------------------- | -------------------------------------- |
| `Umbraco.AI.Agent`     | Agent definition management (required) |
| `Umbraco.AI.Agent.UI`  | Reusable chat infrastructure (required)|
| `Umbraco.AI`           | Core AI infrastructure (transitive)    |

## Architecture

This is a **frontend-only package** that consumes `Umbraco.AI.Agent.UI` for shared chat infrastructure and adds copilot-specific features:

```
Umbraco.AI.Agent (Backend APIs, Agent Management)
         │
         ├── Umbraco.AI.Agent.UI (Reusable Chat Infrastructure)
         │           │
         │           ├── Chat components, services, contexts
         │           ├── Tool renderer system
         │           ├── Frontend tool execution
         │           └── HITL approval system
         │           │
         │           ▼
         └── Umbraco.AI.Agent.Copilot (Copilot-Specific UI)
                     │
                     ├── Copilot sidebar container
                     ├── Header button
                     ├── AG-UI transport layer
                     └── Entity context integration
```

## Usage Scenarios

### Agent + Copilot (Full Experience)

Install both packages for the complete AI agent experience with chat interface:

```xml
<PackageReference Include="Umbraco.AI.Agent" Version="1.0.0" />
<PackageReference Include="Umbraco.AI.Agent.Copilot" Version="1.0.0" />
```

### Agent Only (Automation)

Install only the Agent package for programmatic agent usage without the chat UI:

```xml
<PackageReference Include="Umbraco.AI.Agent" Version="1.0.0" />
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
Umbraco.AI.Agent.Copilot/
├── src/
│   └── Umbraco.AI.Agent.Copilot/
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
├── Umbraco.AI.Agent.Copilot.sln
└── Directory.Build.props
```

## Documentation

For detailed documentation, see the [Umbraco.AI documentation](../docs/public/add-ons/agent-copilot/README.md).

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE.md) file for details.
