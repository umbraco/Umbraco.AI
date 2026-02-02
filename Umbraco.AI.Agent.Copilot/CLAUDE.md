# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

> **Note:** This is the Umbraco.AI.Agent.Copilot add-on package. See the [root CLAUDE.md](../CLAUDE.md) for shared coding standards, build commands, and repository-wide conventions that apply to all packages.

## Package Overview

`Umbraco.AI.Agent.Copilot` is a **frontend-only package** that provides the copilot chat UI for Umbraco AI agents. It has no backend code - all backend functionality comes from `Umbraco.AI.Agent`.

### What This Package Contains

- Copilot sidebar UI (chat interface in backoffice)
- Tool extension system (frontend tool registration and execution)
- Approval element system (HITL approval workflows)
- AG-UI client integration (streaming protocol)
- Localization for copilot features

### What This Package Does NOT Contain

- No C# code
- No backend APIs (uses `Umbraco.AI.Agent` APIs)
- No database access
- No Umbraco Composer

## Build Commands

```bash
# Build frontend assets (from repository root)
npm run build:copilot

# Watch frontend during development
npm run watch:copilot

# Build .NET solution (minimal - just static assets)
dotnet build Umbraco.AI.Agent.Copilot/Umbraco.AI.Agent.Copilot.sln
```

## Project Structure

```
Umbraco.AI.Agent.Copilot/
├── src/
│   └── Umbraco.AI.Agent.Copilot/
│       ├── Client/
│       │   ├── src/
│       │   │   ├── copilot/              # Main copilot module
│       │   │   │   ├── components/       # UI components
│       │   │   │   │   ├── chat/         # Chat UI elements
│       │   │   │   │   ├── sidebar/      # Sidebar container
│       │   │   │   │   └── header-app/   # Header button
│       │   │   │   ├── services/         # Services
│       │   │   │   │   ├── tool.manager.ts
│       │   │   │   │   └── frontend-tool.executor.ts
│       │   │   │   ├── transport/        # AG-UI integration
│       │   │   │   ├── tools/            # Tool extension system
│       │   │   │   ├── approval/         # HITL approval system
│       │   │   │   └── interrupts/       # Interrupt handlers
│       │   │   ├── lang/                 # Localization
│       │   │   ├── manifests.ts          # Extension manifests
│       │   │   └── index.ts              # Package exports
│       │   ├── public/
│       │   │   └── umbraco-package.json  # Umbraco package manifest
│       │   ├── package.json
│       │   ├── tsconfig.json
│       │   └── vite.config.ts
│       └── Umbraco.AI.Agent.Copilot.csproj
├── Directory.Build.props
├── Umbraco.AI.Agent.Copilot.sln
├── README.md
└── CLAUDE.md
```

## Key Concepts

### Tool Extension System

Tools are registered via the Umbraco extension registry:

```typescript
// Register a frontend tool
const manifest: ManifestUaiAgentTool = {
  type: 'uaiAgentTool',
  alias: 'MyTool',
  name: 'My Custom Tool',
  meta: {
    toolName: 'my_tool',
    description: 'Does something useful',
    parameters: {
      type: 'object',
      properties: {
        input: { type: 'string' }
      }
    }
  },
  api: () => import('./my-tool.api.js'),      // Execution logic
  element: () => import('./my-tool.element.js') // Custom UI (optional)
};
```

### Approval Element System

HITL approval elements for agent actions:

```typescript
// Register an approval element
const manifest: ManifestUaiAgentApprovalElement = {
  type: 'uaiAgentApprovalElement',
  alias: 'MyApproval',
  name: 'My Approval Handler',
  forToolName: 'my_tool',           // Tool this approves
  element: () => import('./my-approval.element.js')
};
```

### AG-UI Transport

The copilot uses AG-UI (Agent UI) protocol for streaming communication:

```typescript
// Located in src/copilot/transport/
// - uai-http-agent.ts - HTTP transport implementation
// - Handles streaming responses from agent API
```

## Dependencies

### npm Dependencies

```json
{
  "peerDependencies": {
    "@umbraco-ai/core": "^17.0.0",
    "@umbraco-ai/agent": "^17.0.0",
    "@umbraco-cms/backoffice": "^17.1.0"
  },
  "dependencies": {
    "@ag-ui/client": "^0.0.42",
    "rxjs": "^7.8.2"
  }
}
```

### NuGet Dependencies

None - this is a frontend-only package. The .csproj is a Razor Class Library that only serves static assets.

## Import Patterns

### Importing from Agent Package

```typescript
// Import API client and types from agent package
import { AgentsService } from '@umbraco-ai/agent';
import type { AguiRunRequestModel } from '@umbraco-ai/agent';
```

### Importing from Core Package

```typescript
// Import core utilities
import { someUtil } from '@umbraco-ai/core';
```

## Build Output

Frontend assets compile to:
- `/App_Plugins/UmbracoAIAgentCopilot/umbraco-ai-agent-copilot.js`

The import map in `umbraco-package.json` registers:
- `@umbraco-ai/agent-copilot` -> `/App_Plugins/UmbracoAIAgentCopilot/umbraco-ai-agent-copilot.js`

## Testing

This package is frontend-only. Testing approaches:

1. **Manual testing** - Run demo site and test copilot UI
2. **Unit tests** - Use vitest for component/service unit tests (if added)
3. **Integration testing** - Test with demo site that has both Agent and Copilot installed

## Relationship with Agent Package

```
┌─────────────────────────────────────────────────────┐
│                 Umbraco.AI.Agent                     │
│  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐ │
│  │    Core      │ │  Persistence │ │     Web      │ │
│  │  (Services)  │ │  (Database)  │ │   (APIs)     │ │
│  └──────────────┘ └──────────────┘ └──────────────┘ │
│  ┌──────────────────────────────────────────────────┤
│  │         Web.StaticAssets (Agent UI)              │
│  │    - Agent workspace, collection, editing        │
│  └──────────────────────────────────────────────────┤
└─────────────────────────────────────────────────────┘
                          │
                          │ depends on (npm + runtime)
                          ▼
┌─────────────────────────────────────────────────────┐
│            Umbraco.AI.Agent.Copilot                  │
│  ┌──────────────────────────────────────────────────┤
│  │         Web.StaticAssets (Copilot UI)            │
│  │    - Chat sidebar, tool execution, HITL          │
│  └──────────────────────────────────────────────────┤
└─────────────────────────────────────────────────────┘
```

## Common Tasks

### Adding a New Tool

1. Create tool API class implementing `UaiAgentToolApi`
2. Create tool manifest with `type: 'uaiAgentTool'`
3. Optionally create custom element for tool UI
4. Register in manifests.ts

### Adding HITL Approval

1. Create approval element extending `UaiAgentApprovalElement`
2. Create manifest with `type: 'uaiAgentApprovalElement'`
3. Set `forToolName` to match the tool requiring approval
4. Register in manifests.ts

### Debugging Copilot

1. Build copilot: `npm run build:copilot`
2. Run demo site: `dotnet run --project demo/Umbraco.AI.DemoSite`
3. Open browser dev tools to see console logs
4. Check Network tab for AG-UI streaming requests

## Target Framework

- .NET 10.0 (`net10.0`)
- Uses Central Package Management (`Directory.Packages.props`)
- TypeScript 5.x
- Vite 7.x for bundling
