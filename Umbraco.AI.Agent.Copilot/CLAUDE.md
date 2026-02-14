# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

> **Note:** This is the Umbraco.AI.Agent.Copilot add-on package. See the [root CLAUDE.md](../CLAUDE.md) for shared coding standards, build commands, and repository-wide conventions that apply to all packages.

## Package Overview

`Umbraco.AI.Agent.Copilot` is a **frontend-only package** that provides the copilot chat UI for Umbraco AI agents. It builds on top of `Umbraco.AI.Agent.UI` (the reusable chat infrastructure) and provides the copilot-specific implementation.

### What This Package Contains

- Copilot sidebar container (chat interface in backoffice sidebar)
- Header button (quick access from backoffice header)
- AG-UI transport layer (streaming protocol implementation)
- Copilot context (integrates with `UaiChatContextApi` from Agent.UI)
- Entity context integration (connects copilot with entity-aware features)
- Copilot-specific tool implementations
- Localization for copilot features

### What This Package Does NOT Contain

- No C# code
- No backend APIs (uses `Umbraco.AI.Agent` APIs)
- No database access
- No Umbraco Composer
- No shared chat components (provided by `Umbraco.AI.Agent.UI`)

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
│       │   │   │   ├── components/       # Copilot-specific components
│       │   │   │   │   ├── sidebar/      # Sidebar container
│       │   │   │   │   └── header-app/   # Header button
│       │   │   │   ├── transport/        # AG-UI transport implementation
│       │   │   │   │   └── uai-http-agent.ts
│       │   │   │   ├── copilot.context.ts # Implements UaiChatContextApi
│       │   │   │   ├── tools/            # Copilot-specific tools
│       │   │   │   │   ├── entity/       # Entity-aware tools
│       │   │   │   │   ├── umbraco/      # Umbraco integration tools
│       │   │   │   │   └── examples/     # Example tools
│       │   │   │   └── frontend-tool.repository.ts # Tool registry
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
├── changelog.config.json
├── version.json
├── README.md
└── CLAUDE.md
```

## Key Concepts

### Copilot Context

The copilot implements `UaiChatContextApi` from Agent.UI:

```typescript
// Located in src/copilot/copilot.context.ts
export class UaiCopilotContext extends UmbContextBase<UmbElement> implements UaiChatContextApi {
    private _runController: UaiRunController;

    constructor(host: UmbElement) {
        super(host);

        // Initialize run controller with AG-UI transport
        this._runController = new UaiRunController(this, {
            transport: new UaiHttpAgent(/* config */),
            frontendToolManager: new UaiFrontendToolManager(this),
        });
    }

    // Implement UaiChatContextApi methods
    async startRun(request: AGUIRunRequestModel) {
        return this._runController.start(request);
    }
    // ... other methods
}
```

### AG-UI Transport

The copilot provides the AG-UI transport implementation:

```typescript
// Located in src/copilot/transport/uai-http-agent.ts
export class UaiHttpAgent implements AGUIAgent {
    async run(request: AGUIRunRequest): Promise<AGUIRunResponse> {
        // Streams agent responses from backend API
        // Handles tool execution, HITL interrupts, etc.
    }
}
```

### Tool System

The copilot uses the tool system from Agent.UI:

**Tool Renderers** (defined in copilot-specific manifests):
```typescript
const manifest: ManifestUaiAgentToolRenderer = {
    type: "uaiAgentToolRenderer", // From Agent.UI
    alias: "SearchUmbraco",
    forToolName: "search_umbraco",
    element: () => import("./search-umbraco.element.js"),
};
```

**Frontend Tools** (copilot-specific implementations):
```typescript
const manifest: ManifestUaiAgentFrontendTool = {
    type: "uaiAgentFrontendTool", // From Agent.UI
    alias: "GetPageInfo",
    meta: {
        toolName: "get_page_info",
        description: "Gets info about the current page",
        parameters: { /* schema */ },
    },
    api: () => import("./get-page-info.api.js"),
};
```

### Frontend Tool Repository

The copilot maintains a registry of available frontend tools:

```typescript
// Located in src/copilot/tools/frontend-tool.repository.ts
export class UaiFrontendToolRepository {
    // Registers all copilot tools with UaiFrontendToolManager
    registerTools(manager: UaiFrontendToolManager) {
        // Tools are auto-discovered from extension manifests
    }
}
```

## Dependencies

### npm Dependencies

```json
{
    "peerDependencies": {
        "@umbraco-ai/core": "^1.0.0",
        "@umbraco-ai/agent": "^1.0.0",
        "@umbraco-ai/agent-ui": "^1.0.0",
        "@umbraco-cms/backoffice": "^17.1.0"
    },
    "dependencies": {
        "@ag-ui/client": "^0.0.42"
    }
}
```

**Note:** `rxjs` is provided by `@umbraco-ai/agent-ui`.

### NuGet Dependencies

None - this is a frontend-only package. The .csproj is a Razor Class Library that only serves static assets.

## Import Patterns

### Importing from Agent Package

```typescript
// Import API client and types from agent package
import { AgentsService } from "@umbraco-ai/agent";
import type { AGUIRunRequestModel } from "@umbraco-ai/agent";
```

### Importing from Agent.UI Package

```typescript
// Import shared chat infrastructure
import { UaiRunController, UaiFrontendToolManager } from "@umbraco-ai/agent-ui";
import type { UaiChatContextApi, ManifestUaiAgentToolRenderer } from "@umbraco-ai/agent-ui";
```

### Importing from Core Package

```typescript
// Import core utilities
import { someUtil } from "@umbraco-ai/core";
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

## Relationship with Other Packages

```
┌─────────────────────────────────────────────────────┐
│                 Umbraco.AI.Agent                     │
│  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐ │
│  │    Core      │ │  Persistence │ │     Web      │ │
│  │  (Services)  │ │  (Database)  │ │   (APIs)     │ │
│  └──────────────┘ └──────────────┘ └──────────────┘ │
│  ┌──────────────────────────────────────────────────┤
│  │         Web.StaticAssets (Agent Management UI)   │
│  │    - Agent workspace, collection, editing        │
│  └──────────────────────────────────────────────────┤
└─────────────────────────────────────────────────────┘
                          │
                          │ depends on (npm)
                          ▼
┌─────────────────────────────────────────────────────┐
│            Umbraco.AI.Agent.UI (Reusable)            │
│  ┌──────────────────────────────────────────────────┤
│  │       Shared Chat Infrastructure                 │
│  │    - Components, services, contexts              │
│  └──────────────────────────────────────────────────┤
└─────────────────────────────────────────────────────┘
                          │
                          │ depends on (npm + runtime)
                          ▼
┌─────────────────────────────────────────────────────┐
│            Umbraco.AI.Agent.Copilot                  │
│  ┌──────────────────────────────────────────────────┤
│  │         Copilot-Specific UI                      │
│  │    - Sidebar, header button, AG-UI transport     │
│  └──────────────────────────────────────────────────┤
└─────────────────────────────────────────────────────┘
```

## Common Tasks

### Adding a Copilot-Specific Tool

1. Create tool API class implementing the tool interface
2. Create tool manifest with `type: 'uaiAgentFrontendTool'` (from Agent.UI)
3. Add to `frontend-tool.repository.ts`
4. Register in manifests.ts

### Adding a Tool Renderer

1. Create renderer element extending `LitElement`
2. Create manifest with `type: 'uaiAgentToolRenderer'` (from Agent.UI)
3. Set `forToolName` to match the tool
4. Register in manifests.ts

### Adding HITL Approval

1. Create approval element extending `UmbElementMixin(LitElement)`
2. Create manifest with `type: 'uaiAgentApprovalElement'` (from Agent.UI)
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
