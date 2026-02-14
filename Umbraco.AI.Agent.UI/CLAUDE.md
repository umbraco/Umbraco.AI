# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

> **Note:** This is the Umbraco.AI.Agent.UI package. See the [root CLAUDE.md](../CLAUDE.md) for shared coding standards, build commands, and repository-wide conventions that apply to all packages.

## Package Overview

`Umbraco.AI.Agent.UI` is a **reusable frontend library** that provides shared chat UI infrastructure for AI agent interactions. It was extracted from `Umbraco.AI.Agent.Copilot` to enable multiple chat interfaces (copilot sidebar, workspace chat, custom integrations) to share common components and services.

### What This Package Contains

- **Chat Components** - Reusable chat UI elements (message display, input, status indicators)
- **Tool Renderer System** - Framework for rendering tool execution results
- **Frontend Tool Execution** - Client-side tool execution infrastructure
- **Approval System** - HITL (Human-in-the-Loop) approval workflows
- **Run Controller** - Orchestrates agent runs with streaming responses
- **Context Contracts** - Standard context APIs for chat and entity integration
- **Interrupt Handlers** - Handles tool execution and HITL interrupts
- **Localization** - Base localization strings for chat UI

### What This Package Does NOT Contain

- No backend C# code
- No copilot-specific UI (sidebar, header button)
- No AG-UI transport layer (provided by consumers)
- No Umbraco Composer

### Consumer Packages

This package is consumed by:

- **Umbraco.AI.Agent.Copilot** - Copilot sidebar UI
- **Future packages** - Workspace chat, custom chat integrations

## Build Commands

```bash
# Build frontend assets (from repository root)
npm run build:agent-ui

# Watch frontend during development
npm run watch:agent-ui

# Build .NET solution (minimal - just static assets)
dotnet build Umbraco.AI.Agent.UI/Umbraco.AI.Agent.UI.slnx
```

## Project Structure

```
Umbraco.AI.Agent.UI/
├── src/
│   └── Umbraco.AI.Agent.UI/
│       ├── Client/
│       │   ├── src/
│       │   │   ├── chat/                    # Main chat module
│       │   │   │   ├── components/          # Reusable UI components
│       │   │   │   │   ├── chat.element.ts           # Main chat container
│       │   │   │   │   ├── input.element.ts          # Chat input field
│       │   │   │   │   ├── message.element.ts        # Message display
│       │   │   │   │   ├── tool-renderer.element.ts  # Tool result renderer
│       │   │   │   │   ├── agent-status.element.ts   # Agent status indicator
│       │   │   │   │   ├── approval-base.element.ts  # Base approval element
│       │   │   │   │   ├── hitl-approval.element.ts  # HITL approval UI
│       │   │   │   │   └── approval/                 # Approval elements
│       │   │   │   ├── services/            # Core services
│       │   │   │   │   ├── run.controller.ts               # Orchestrates agent runs
│       │   │   │   │   ├── tool-renderer.manager.ts        # Manages tool renderers
│       │   │   │   │   ├── frontend-tool.manager.ts        # Manages frontend tools
│       │   │   │   │   ├── frontend-tool.executor.ts       # Executes frontend tools
│       │   │   │   │   ├── interrupt-handler.registry.ts   # Registers handlers
│       │   │   │   │   ├── hitl.context.ts                 # HITL context
│       │   │   │   │   └── handlers/                       # Interrupt handlers
│       │   │   │   ├── extensions/          # Extension types
│       │   │   │   │   ├── uai-agent-tool-renderer.extension.ts
│       │   │   │   │   ├── uai-agent-frontend-tool.extension.ts
│       │   │   │   │   └── uai-agent-approval-element.extension.ts
│       │   │   │   ├── context.ts           # Chat context API
│       │   │   │   ├── entity-context.ts    # Entity context API
│       │   │   │   ├── types/               # TypeScript types
│       │   │   │   └── utils/               # Utility functions
│       │   │   ├── lang/                    # Localization
│       │   │   ├── manifests.ts             # Extension manifests
│       │   │   ├── exports.ts               # Public API exports
│       │   │   └── index.ts                 # Package entry point
│       │   ├── public/
│       │   │   └── umbraco-package.json     # Umbraco package manifest
│       │   ├── package.json
│       │   ├── tsconfig.json
│       │   └── vite.config.ts
│       └── Umbraco.AI.Agent.UI.csproj
├── Directory.Build.props
├── Umbraco.AI.Agent.UI.slnx
├── changelog.config.json
├── version.json
├── README.md
└── CLAUDE.md
```

## Key Concepts

### Context Contracts

Two standard context APIs for integrating chat UI:

#### UaiChatContextApi

Core chat context required by all consumers:

```typescript
export interface UaiChatContextApi extends UmbContextMinimal {
    // Agent run management
    startRun(request: AGUIRunRequestModel): Promise<void>;
    stopRun(): void;
    regenerateLastMessage(): void;

    // State observables
    messages$: Observable<AGUIMessageClientModel[]>;
    isRunning$: Observable<boolean>;
    agentStatus$: Observable<string>;
}
```

Consumers MUST implement this context to use the chat components.

#### UaiEntityContextApi

Optional entity context for entity-aware chat (e.g., chat about a specific content item):

```typescript
export interface UaiEntityContextApi extends UmbContextMinimal {
    // Entity information
    entityType$: Observable<string>;
    entityId$: Observable<string>;
    entityData$: Observable<unknown>;
}
```

Consumers can optionally implement this for entity-specific features.

### Tool Renderer System

Tool renderers display the results of tool executions:

```typescript
// Register a tool renderer
const manifest: ManifestUaiAgentToolRenderer = {
    type: "uaiAgentToolRenderer",
    alias: "MyToolRenderer",
    name: "My Tool Result Renderer",
    forToolName: "my_tool", // Tool this renders
    element: () => import("./my-tool-renderer.element.js"),
};
```

**Rendering Flow:**

1. Agent executes a tool (backend or frontend)
2. Tool result is streamed to the UI
3. `UaiToolRendererManager` finds the matching renderer by tool name
4. Renderer displays the result in the chat

### Frontend Tool System

Frontend tools execute in the browser:

```typescript
// Register a frontend tool
const manifest: ManifestUaiAgentFrontendTool = {
    type: "uaiAgentFrontendTool",
    alias: "MyFrontendTool",
    name: "My Frontend Tool",
    meta: {
        toolName: "my_tool",
        description: "Does something in the browser",
        parameters: {
            type: "object",
            properties: {
                input: { type: "string" },
            },
        },
    },
    api: () => import("./my-tool.api.js"), // Execution logic
};
```

**Execution Flow:**

1. Agent requests a frontend tool
2. `UaiToolExecutionHandler` intercepts the request
3. `UaiFrontendToolExecutor` calls the tool's API
4. Result is sent back to the agent
5. Agent continues with the result

### HITL Approval System

Human-in-the-loop approval for agent actions:

```typescript
// Register an approval element
const manifest: ManifestUaiAgentApprovalElement = {
    type: "uaiAgentApprovalElement",
    alias: "MyApprovalElement",
    name: "My Approval Handler",
    forToolName: "my_tool", // Tool requiring approval
    element: () => import("./my-approval.element.js"),
};
```

**Approval Flow:**

1. Agent requests a tool that requires approval
2. `UaiHitlInterruptHandler` detects HITL interrupt
3. Matching approval element is rendered
4. User approves/rejects the action
5. Result is sent back to the agent

### Run Controller

`UaiRunController` orchestrates agent runs:

```typescript
const controller = new UaiRunController(this, {
    transport: myTransport,              // AG-UI transport layer
    frontendToolManager: toolManager,    // Optional: for frontend tools
    interruptHandlers: [                 // Optional: custom handlers
        new CustomInterruptHandler(),
    ],
});

// Start a run
await controller.start({
    agent: { name: "my-agent" },
    messages: [{ role: "user", content: "Hello" }],
});

// Observe state
controller.messages$.subscribe(messages => {
    console.log("Messages:", messages);
});
```

**Features:**

- Streams agent responses via AG-UI protocol
- Automatically registers `UaiToolExecutionHandler` if `frontendToolManager` is provided
- Merges custom interrupt handlers with built-in handlers
- Manages run lifecycle (start, stop, regenerate)
- Provides reactive state via RxJS observables

## Dependencies

### npm Dependencies

```json
{
    "peerDependencies": {
        "@umbraco-ai/agent": "^1.0.0",
        "@umbraco-cms/backoffice": "^17.1.0"
    },
    "dependencies": {
        "rxjs": "^7.8.2"
    }
}
```

**Note:** This package does NOT include `@ag-ui/client` - consumers must provide their own AG-UI transport implementation.

### NuGet Dependencies

None - this is a frontend-only package. The .csproj is a Razor Class Library that only serves static assets.

## Import Patterns

### Importing from Agent Package

```typescript
// Import API client and types from agent package
import { AgentsService } from "@umbraco-ai/agent";
import type { AGUIRunRequestModel, AGUIMessageClientModel } from "@umbraco-ai/agent";
```

### Importing from Agent-UI Package

```typescript
// Import components
import { UaiChatElement } from "@umbraco-ai/agent-ui";

// Import services
import { UaiRunController, UaiFrontendToolManager } from "@umbraco-ai/agent-ui";

// Import types
import type { UaiChatContextApi, ManifestUaiAgentToolRenderer } from "@umbraco-ai/agent-ui";
```

## Build Output

Frontend assets compile to:

- `/App_Plugins/UmbracoAIAgentUI/umbraco-ai-agent-ui.js`

The import map in `umbraco-package.json` registers:

- `@umbraco-ai/agent-ui` -> `/App_Plugins/UmbracoAIAgentUI/umbraco-ai-agent-ui.js`

## Testing

This package is frontend-only. Testing approaches:

1. **Manual testing** - Test via consumer packages (Copilot)
2. **Unit tests** - Use vitest for component/service unit tests (if added)
3. **Integration testing** - Test with demo site that has consumer packages installed

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

### Creating a Consumer Package

To create a new package that uses Agent.UI:

1. **Add npm dependency:**
   ```json
   {
       "peerDependencies": {
           "@umbraco-ai/agent-ui": "^1.0.0"
       }
   }
   ```

2. **Implement `UaiChatContextApi`:**
   ```typescript
   export class MyCustomChatContext extends UmbContextBase<MyHost> implements UaiChatContextApi {
       // Implement required methods and observables
   }
   ```

3. **Provide AG-UI transport:**
   ```typescript
   import { UaiRunController } from "@umbraco-ai/agent-ui";

   const controller = new UaiRunController(this, {
       transport: new MyCustomTransport(),
   });
   ```

4. **Use chat components:**
   ```typescript
   import { UaiChatElement } from "@umbraco-ai/agent-ui";

   // In your template
   html`<uai-chat></uai-chat>`;
   ```

### Adding a Custom Tool Renderer

1. Create renderer element extending `LitElement`
2. Create manifest with `type: 'uaiAgentToolRenderer'`
3. Set `forToolName` to match the tool
4. Register in your consumer's manifests.ts

### Adding a Custom Frontend Tool

1. Create tool API class implementing the tool interface
2. Create manifest with `type: 'uaiAgentFrontendTool'`
3. Provide the tool to `UaiFrontendToolManager` in `UaiRunController`
4. Register in your consumer's manifests.ts

### Debugging Agent-UI

1. Build agent-ui: `npm run build:agent-ui`
2. Run demo site with a consumer package (e.g., Copilot)
3. Open browser dev tools to see console logs
4. Check Network tab for AG-UI streaming requests

## Target Framework

- .NET 10.0 (`net10.0`)
- Uses Central Package Management (`Directory.Packages.props`)
- TypeScript 5.x
- Vite 7.x for bundling
