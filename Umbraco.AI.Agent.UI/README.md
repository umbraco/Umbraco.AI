# Umbraco.AI.Agent.UI

Reusable chat UI infrastructure for Umbraco AI agents. This package provides shared components, services, and contexts that enable multiple chat interfaces to be built on a common foundation.

## Overview

`Umbraco.AI.Agent.UI` is a **frontend library** extracted from `Umbraco.AI.Agent.Copilot` to enable code reuse across different agent chat implementations:

- **Shared Chat Components** - Message display, input field, status indicators
- **Tool Renderer System** - Framework for displaying tool execution results
- **Frontend Tool Execution** - Browser-based tool execution infrastructure
- **HITL Approval System** - Human-in-the-loop approval workflows
- **Run Controller** - Orchestrates agent runs with streaming responses
- **Context Contracts** - Standard APIs for chat and entity integration

## When to Use This Package

**Direct Usage (Library Consumer):**
- You're building a custom chat interface for AI agents
- You want to integrate agent chat into a custom workspace
- You need reusable chat components for a custom implementation

**Indirect Usage (End User):**
- Install `Umbraco.AI.Agent.Copilot` for the ready-to-use copilot sidebar
- This package is automatically included as a dependency

## Installation

This package is typically installed as a dependency of `Umbraco.AI.Agent.Copilot` or other consumer packages. For direct usage:

```bash
# npm (for frontend development)
npm install @umbraco-ai/agent-ui

# NuGet (for including static assets in your package)
dotnet add package Umbraco.AI.Agent.UI
```

## Dependencies

| Package                   | Description                       |
| ------------------------- | --------------------------------- |
| `@umbraco-ai/agent`       | Agent API client and types        |
| `@umbraco-cms/backoffice` | Umbraco backoffice infrastructure |
| `rxjs`                    | Reactive state management         |

**Note:** Consumers must provide their own AG-UI transport implementation.

## Architecture

```
┌─────────────────────────────────────────────────────┐
│            Umbraco.AI.Agent.UI (Reusable)            │
│  ┌──────────────────────────────────────────────────┤
│  │       Shared Chat Infrastructure                 │
│  │    - Components, services, contexts              │
│  └──────────────────────────────────────────────────┤
└─────────────────────────────────────────────────────┘
                          │
                          │ consumed by
                          ▼
┌─────────────────────────────────────────────────────┐
│            Umbraco.AI.Agent.Copilot                  │
│  - Copilot sidebar implementation                    │
│  - AG-UI transport layer                             │
└─────────────────────────────────────────────────────┘
```

## Key Concepts

### Context Contracts

Two standard context APIs enable integration:

**UaiChatContextApi** - Required chat context:
```typescript
export interface UaiChatContextApi extends UmbContextMinimal {
    startRun(request: AGUIRunRequestModel): Promise<void>;
    stopRun(): void;
    messages$: Observable<AGUIMessageClientModel[]>;
    isRunning$: Observable<boolean>;
}
```

**UaiEntityContextApi** - Optional entity context:
```typescript
export interface UaiEntityContextApi extends UmbContextMinimal {
    entityType$: Observable<string>;
    entityId$: Observable<string>;
    entityData$: Observable<unknown>;
}
```

### Tool System

**Tool Renderers** display tool execution results:
```typescript
const manifest: ManifestUaiAgentToolRenderer = {
    type: "uaiAgentToolRenderer",
    alias: "MyRenderer",
    forToolName: "my_tool",
    element: () => import("./renderer.js"),
};
```

**Frontend Tools** execute in the browser:
```typescript
const manifest: ManifestUaiAgentFrontendTool = {
    type: "uaiAgentFrontendTool",
    alias: "MyTool",
    meta: {
        toolName: "my_tool",
        description: "Browser-based tool",
        parameters: { /* JSON Schema */ },
    },
    api: () => import("./tool.api.js"),
};
```

### Run Controller

Orchestrates agent runs with streaming:

```typescript
import { UaiRunController } from "@umbraco-ai/agent-ui";

const controller = new UaiRunController(this, {
    transport: myTransport,              // AG-UI transport
    frontendToolManager: toolManager,    // Optional
    interruptHandlers: [/* custom */],   // Optional
});

await controller.start({
    agent: { name: "my-agent" },
    messages: [{ role: "user", content: "Hello" }],
});
```

## Usage Example

### Building a Custom Chat Interface

```typescript
import { UaiChatElement, UaiRunController } from "@umbraco-ai/agent-ui";

// 1. Implement chat context
class MyCustomChatContext extends UmbContextBase<MyHost> implements UaiChatContextApi {
    private _controller: UaiRunController;

    constructor(host: MyHost) {
        super(host);
        this._controller = new UaiRunController(this, {
            transport: new MyCustomTransport(),
        });
    }

    async startRun(request: AGUIRunRequestModel) {
        return this._controller.start(request);
    }

    stopRun() {
        this._controller.stop();
    }

    get messages$() { return this._controller.messages$; }
    get isRunning$() { return this._controller.isRunning$; }
}

// 2. Use chat component
html`<uai-chat></uai-chat>`;
```

## Consumer Packages

This package is designed to be consumed by:

- **Umbraco.AI.Agent.Copilot** - Copilot sidebar UI (existing)
- **Future workspace chat** - Dedicated chat workspace
- **Custom integrations** - Your own chat implementations

## Frontend Development

### Building

```bash
# From repository root
npm run build:agent-ui

# Watch mode
npm run watch:agent-ui
```

### Package Structure

```
Umbraco.AI.Agent.UI/
└── src/Umbraco.AI.Agent.UI/Client/
    ├── src/
    │   ├── chat/           # Chat infrastructure
    │   ├── lang/           # Localization
    │   ├── manifests.ts    # Extension manifests
    │   └── exports.ts      # Public API
    └── public/
        └── umbraco-package.json
```

## Exported Types

Key types available for consumers:

```typescript
// Context APIs
export type { UaiChatContextApi, UaiEntityContextApi };

// Extension types
export type { ManifestUaiAgentToolRenderer };
export type { ManifestUaiAgentFrontendTool };
export type { ManifestUaiAgentApprovalElement };

// Services
export { UaiRunController };
export { UaiFrontendToolManager };
export { UaiToolRendererManager };

// Components
export { UaiChatElement };
export { UaiMessageElement };
export { UaiInputElement };
```

## Documentation

For detailed documentation, see:
- [CLAUDE.md](CLAUDE.md) - Development guide
- [Root documentation](../CLAUDE.md) - Repository-wide conventions

## License

This project is licensed under the MIT License - see the [LICENSE](../LICENSE.md) file for details.
