# Copilot UI Split Plan

## Context

The `Umbraco.AI.Agent.Copilot` package currently contains two concerns bundled together:

1. **Shared chat UI infrastructure** -- message rendering, tool status, input handling, run lifecycle, interrupt system, HITL approval
2. **Copilot-specific surface** -- sidebar shell, header app toggle, entity context, section detection, frontend tool execution

We want to introduce a **central chat** workspace alongside the copilot drawer. Both surfaces need the shared chat infrastructure but have different layouts, context models, and tool capabilities.

This plan extracts the shared layer into a new `@umbraco-ai/agent-ui` package, slims down the copilot to its surface-specific code, splits the tool manifest into rendering and execution concerns, and prepares the structure for the central chat.

### Design Decisions

These were established through architectural discussion and should be treated as settled:

- **Scopes**: `copilot` (contextual drawer) and `chat` (central workspace). Agents opt into one or both via `ScopeIds[]`.
- **Frontend tools are copilot-only**. The central chat uses server-side tools exclusively. No browser-executed tools in the chat surface.
- **Manifest split**: The current `ManifestUaiAgentTool` (which uses `api` presence as a discriminator) splits into `ManifestUaiAgentToolRenderer` (shared rendering) and `ManifestUaiAgentFrontendTool` (copilot execution).
- **The agent package stays clean**. Shared chat UI goes into a new `@umbraco-ai/agent-ui`, not into `@umbraco-ai/agent`. The agent package is about agent infrastructure, not chat rendering.
- **Embed (visitor-facing) is a future concern**. It doesn't use the backoffice UI stack at all. This split doesn't need to account for it -- it's purely additive later.
- **All packages are pre-stable (alpha)**. There is no backward compatibility contract. No deprecation periods, no bridge code, no legacy observers. Clean breaks only.

---

## Target Package Structure

### npm packages

```
@umbraco-ai/core            (unchanged)
@umbraco-ai/agent           (unchanged)
@umbraco-ai/agent-ui        (NEW - shared chat UI components)
@umbraco-ai/copilot         (SLIMMED - copilot surface only, rename from agent-copilot)
@umbraco-ai/chat            (NEW - central chat surface)
@umbraco-ai/prompt          (unchanged)
```

### NuGet packages

```
Umbraco.AI.Agent            (unchanged)
Umbraco.AI.Agent.UI         (NEW - static assets for shared chat UI)
Umbraco.AI.Agent.Copilot    (SLIMMED)
Umbraco.AI.Agent.Chat       (NEW - chat scope + static assets)
```

### Dependency chain

```
@umbraco-ai/core
  └─ @umbraco-ai/agent
       └─ @umbraco-ai/agent-ui
            ├─ @umbraco-ai/copilot
            └─ @umbraco-ai/chat
```

---

## Phase 1: Extract, Split, Restructure

Since all packages are alpha, phases 1-3 from the original plan collapse into a single pass. No deprecation bridge, no temporary coexistence of old and new types.

**Goal**: Create `@umbraco-ai/agent-ui`, define the new manifest types there, move shared code from copilot, update copilot to be surface-only, delete the old `ManifestUaiAgentTool`.

### 1a: New manifest types

Defined in `@umbraco-ai/agent-ui`:

```typescript
// ManifestUaiAgentToolRenderer -- shared, for rendering tool status/results in any chat surface
interface ManifestUaiAgentToolRenderer extends ManifestElement<UaiAgentToolElement> {
    type: "uaiAgentToolRenderer";
    kind?: "default";
    meta: {
        toolName: string;
        label?: string;
        icon?: string;
        approval?: UaiAgentToolApprovalConfig;
    };
}
```

Defined in `@umbraco-ai/copilot`:

```typescript
// ManifestUaiAgentFrontendTool -- copilot-only, for browser-executable tools
interface ManifestUaiAgentFrontendTool extends ManifestApi<UaiAgentToolApi> {
    type: "uaiAgentFrontendTool";
    meta: {
        toolName: string;
        description: string;     // required -- LLM needs this
        parameters: Record<string, unknown>;  // required -- LLM needs this
        /** Tool scope for permission grouping (e.g., 'entity-write', 'navigation') */
        scope?: string;
        /** Whether the tool performs destructive operations */
        isDestructive?: boolean;
    };
}
```

Shared types (in `@umbraco-ai/agent-ui`):

```typescript
UaiAgentToolStatus        // "pending" | "streaming" | "awaiting_approval" | ...
UaiAgentToolElementProps   // { args, status, result }
UaiAgentToolElement        // UmbControllerHostElement & UaiAgentToolElementProps
UaiAgentToolApprovalConfig // true | { elementAlias?, config? }
UaiAgentToolApi            // { execute(args): Promise<unknown> } -- type stays shared, usage is copilot-only
```

The old `ManifestUaiAgentTool` type and its `"uaiAgentTool"` extension type registration are deleted entirely. No bridge, no fallback.

### 1b: Tool registration migration

Each existing `ManifestUaiAgentTool` registration becomes one or two new registrations:

| Current tool | Has `api`? | Has `element`? | Becomes |
|---|---|---|---|
| `getCurrentTime` | Yes | No | 1x `uaiAgentFrontendTool` |
| `getPageInfo` | Yes | No | 1x `uaiAgentFrontendTool` |
| `showWeather` | Yes | Yes | 1x `uaiAgentFrontendTool` + 1x `uaiAgentToolRenderer` |
| `confirmAction` | Yes | No | 1x `uaiAgentFrontendTool` + 1x `uaiAgentToolRenderer` (for approval config) |
| `set_property_value` | Yes | No | 1x `uaiAgentFrontendTool` + 1x `uaiAgentToolRenderer` (for approval config) |
| `search_umbraco` | No | Yes | 1x `uaiAgentToolRenderer` |

**Rule**: If a tool has approval config or custom UI, it needs a `uaiAgentToolRenderer`. If a tool executes in the browser, it needs a `uaiAgentFrontendTool`. A tool may need both, one, or (for backend tools with no custom UI) neither.

### 1c: Scaffold `@umbraco-ai/agent-ui`

Create the new npm workspace and NuGet project following existing patterns.

**Directory structure**:

```
Umbraco.AI.Agent.UI/
├── src/
│   └── Umbraco.AI.Agent.UI/
│       ├── Client/
│       │   ├── package.json           # @umbraco-ai/agent-ui
│       │   ├── vite.config.ts
│       │   ├── tsconfig.json
│       │   └── src/
│       │       ├── index.ts
│       │       ├── exports.ts
│       │       ├── app.ts
│       │       ├── manifests.ts
│       │       └── chat/              # Main module
│       └── Umbraco.AI.Agent.UI.csproj  # StaticAssets project
├── Umbraco.AI.Agent.UI.sln
└── CLAUDE.md
```

**package.json**:

```json
{
  "name": "@umbraco-ai/agent-ui",
  "version": "0.0.0",
  "peerDependencies": {
    "@umbraco-ai/agent": "workspace:*",
    "@umbraco-cms/backoffice": "^17.1.0"
  },
  "dependencies": {
    "rxjs": "^7.8.2"
  }
}
```

**Root package.json workspace update**:

```json
{
  "workspaces": [
    "Umbraco.AI/src/Umbraco.AI.Web.StaticAssets/Client",
    "Umbraco.AI.Prompt/src/Umbraco.AI.Prompt.Web.StaticAssets/Client",
    "Umbraco.AI.Agent/src/Umbraco.AI.Agent.Web.StaticAssets/Client",
    "Umbraco.AI.Agent.UI/src/Umbraco.AI.Agent.UI/Client",
    "Umbraco.AI.Agent.Copilot/src/Umbraco.AI.Agent.Copilot/Client"
  ]
}
```

**Build order**: core -> prompt -> agent -> **agent-ui** -> copilot

### 1d: Move shared components from copilot to agent-ui

#### Components

| Source (copilot) | Destination (agent-ui) | New tag name | Notes |
|---|---|---|---|
| `components/chat/chat.element.ts` | `chat/components/chat.element.ts` | `<uai-chat>` | Remove `UAI_COPILOT_CONTEXT` dependency -- consume `UAI_CHAT_CONTEXT` interface instead |
| `components/chat/message.element.ts` | `chat/components/message.element.ts` | `<uai-chat-message>` | Pure rendering, no context dependency |
| `components/chat/input.element.ts` | `chat/components/input.element.ts` | `<uai-chat-input>` | Remove agent selector coupling -- accept agents as prop |
| `components/chat/agent-status.element.ts` | `chat/components/agent-status.element.ts` | `<uai-agent-status>` | Already context-free |
| `components/chat/tool-renderer.element.ts` | `chat/components/tool-renderer.element.ts` | `<uai-tool-renderer>` | Use `UaiToolRendererManager` instead of `UaiToolManager` |
| `components/chat/approval-base.element.ts` | `chat/components/approval-base.element.ts` | `<uai-approval-base>` | Already generic |
| `components/chat/hitl-approval.element.ts` | `chat/components/hitl-approval.element.ts` | `<uai-hitl-approval>` | Drop copilot prefix |
| `components/chat/message-copy-button.element.ts` | `chat/components/message-copy-button.element.ts` | `<uai-message-copy-button>` | Already generic |
| `components/chat/message-regenerate-button.element.ts` | `chat/components/message-regenerate-button.element.ts` | `<uai-message-regenerate-button>` | Already generic |
| `tools/tool-status.element.ts` | `chat/components/tool-status.element.ts` | `<uai-agent-tool-status>` | Already generic |

#### Services

| Source (copilot) | Destination (agent-ui) | New name | Notes |
|---|---|---|---|
| `services/tool.manager.ts` | `chat/services/tool-renderer.manager.ts` | `UaiToolRendererManager` | **Only the rendering half**: manifest lookup, element resolution, element caching. Remove `frontendTools$`, `getApi()`, `isFrontendTool()`. Observe `"uaiAgentToolRenderer"` only. |
| `services/copilot-run.controller.ts` | `chat/services/run.controller.ts` | `UaiRunController` | Refactored to accept tool execution as optional injection (see 1e). |
| `hitl.context.ts` | `chat/services/hitl.context.ts` | `UaiHitlContext` | Move as-is. Used by both surfaces. |
| `interrupts/interrupt-handler.registry.ts` | `chat/services/interrupt-handler.registry.ts` | `UaiInterruptHandlerRegistry` | Already generic |
| `interrupts/types.ts` | `chat/services/interrupt.types.ts` | (types) | Already generic |
| `interrupts/handlers/hitl-interrupt.handler.ts` | `chat/services/handlers/hitl-interrupt.handler.ts` | `UaiHitlInterruptHandler` | Used by both surfaces |
| `interrupts/handlers/default-interrupt.handler.ts` | `chat/services/handlers/default-interrupt.handler.ts` | `UaiDefaultInterruptHandler` | Used by both surfaces |

**Note**: `UaiToolExecutionHandler` stays in copilot. It's the handler that invokes `UaiFrontendToolExecutor` for browser-side tool execution -- copilot-only.

#### Types

| Source (copilot) | Destination (agent-ui) | Notes |
|---|---|---|
| `tools/uai-agent-tool.extension.ts` (shared types only) | `chat/types/tool.types.ts` | `UaiAgentToolStatus`, `UaiAgentToolElementProps`, `UaiAgentToolElement`, `UaiAgentToolApprovalConfig`, `UaiAgentToolApi` |
| `approval/uai-agent-approval-element.extension.ts` | `chat/extensions/uai-agent-approval-element.extension.ts` | Approval manifest type |
| `types.ts` (re-exports from agent) | `chat/types/index.ts` | `UaiChatMessage`, `UaiToolCallInfo`, `UaiToolCallStatus`, `UaiInterruptInfo`, etc. |
| `utils/json.ts` | `chat/utils/json.ts` | `safeParseJson` |

#### Approval elements

| Source (copilot) | Destination (agent-ui) | Notes |
|---|---|---|
| `approval/elements/default.element.ts` | `chat/components/approval/default.element.ts` | Default approve/deny buttons |
| `approval/manifests.ts` | `chat/manifests/approval.manifests.ts` | Default approval manifest registration |

#### Tool kind

| Source (copilot) | Destination (agent-ui) | Notes |
|---|---|---|
| `tools/default/default.tool.kind.ts` | `chat/manifests/tool-renderer-kind.manifests.ts` | Default kind for `uaiAgentToolRenderer` (maps to `<uai-agent-tool-status>`) |

### 1e: Run controller refactoring

The `UaiCopilotRunController` currently couples frontend tool execution into the run lifecycle. For the shared layer, the run controller accepts tool execution as optional injection.

```typescript
// In @umbraco-ai/agent-ui
export interface UaiRunControllerConfig {
    /** Tool renderer manager for manifest/element lookup */
    toolRendererManager: UaiToolRendererManager;
    /** Optional frontend tool provider -- copilot injects this, chat does not */
    frontendToolProvider?: {
        /** Tools to send in the AG-UI request (includes scope/isDestructive metadata) */
        frontendTools: UaiFrontendTool[];
        frontendTools$: Observable<UaiFrontendTool[]>;
    };
    /** Interrupt handlers to register */
    interruptHandlers: UaiInterruptHandler[];
}

export class UaiRunController extends UmbControllerBase {
    constructor(
        host: UmbControllerHost,
        hitlContext: UaiHitlContext,
        config: UaiRunControllerConfig,
    ) { ... }
}
```

- **Copilot** creates the run controller with `frontendToolProvider` set to `UaiFrontendToolManager` and registers `UaiToolExecutionHandler` in the interrupt handlers array.
- **Chat** creates the run controller with `frontendToolProvider` unset (no frontend tools sent in AG-UI request) and only registers the HITL + default handlers.

The run controller's `sendMessage()` reads `frontendToolProvider.frontendTools` (or empty array if not set) and passes them to the AG-UI client. No if/else branching needed -- just absence of data.

### 1f: Shared chat context interface

Currently `<uai-copilot-chat>` consumes `UAI_COPILOT_CONTEXT`. The shared `<uai-chat>` should consume a generic context interface.

```typescript
// In @umbraco-ai/agent-ui
export interface UaiChatContextApi {
    readonly messages$: Observable<UaiChatMessage[]>;
    readonly streamingContent$: Observable<string>;
    readonly agentState$: Observable<UaiAgentState | undefined>;
    readonly isRunning$: Observable<boolean>;
    readonly hitlInterrupt$: Observable<UaiInterruptInfo | undefined>;
    readonly pendingApproval$: Observable<...>;
    readonly agents: Observable<{ id: string; name: string; alias: string }[]>;
    readonly selectedAgent: Observable<{ id: string; name: string; alias: string } | undefined>;
    readonly toolRendererManager: UaiToolRendererManager;

    sendUserMessage(content: string): Promise<void>;
    abortRun(): void;
    regenerateLastMessage(): void;
    selectAgent(agentId: string | undefined): void;
    respondToHitl(response: string): void;
}

export const UAI_CHAT_CONTEXT = new UmbContextToken<UaiChatContextApi>("UaiChatContext");
```

Both `UaiCopilotContext` and the future `UaiChatContext` implement this interface. Shared chat components consume `UAI_CHAT_CONTEXT`. Each surface provides its own implementation.

### 1g: Slim down copilot

Delete everything that moved. The copilot becomes a thin surface shell.

**What stays in copilot**:

```
@umbraco-ai/copilot
├── copilot/
│   ├── copilot.context.ts              Copilot facade (implements UaiChatContextApi)
│   │                                    - Panel state (open/close/toggle)
│   │                                    - Entity context serialization for sendUserMessage
│   │                                    - Creates UaiRunController with FrontendToolManager
│   │                                    - Provides UAI_CHAT_CONTEXT + UAI_COPILOT_CONTEXT
│   │
│   ├── components/
│   │   ├── sidebar/
│   │   │   ├── copilot-sidebar.element.ts    Layout shell (450px fixed panel)
│   │   │   ├── entry-point.ts                Creates context, appends sidebar
│   │   │   └── manifests.ts
│   │   ├── header-app/
│   │   │   ├── copilot-header-app.element.ts  Toggle button in header
│   │   │   ├── copilot-section.condition.ts   Section visibility condition
│   │   │   └── manifests.ts
│   │   └── entity-selector/
│   │       └── entity-selector.element.ts     Entity context display/selection
│   │
│   ├── services/
│   │   ├── frontend-tool.manager.ts     NEW - extracted from UaiToolManager
│   │   │                                 - Observes "uaiAgentFrontendTool" extensions
│   │   │                                 - Provides frontendTools$ and frontendTools for AG-UI
│   │   │                                 - Provides getApi() for tool execution
│   │   │                                 - Provides isFrontendTool()
│   │   └── frontend-tool.executor.ts    Browser-side tool execution
│   │
│   ├── interrupts/
│   │   └── handlers/
│   │       └── tool-execution.handler.ts  Frontend tool execution handler (copilot-only)
│   │
│   ├── repository/
│   │   └── copilot-agent.repository.ts   Filters agents by scope="copilot"
│   │
│   ├── tools/
│   │   ├── uai-agent-frontend-tool.extension.ts   ManifestUaiAgentFrontendTool type
│   │   ├── frontend-tool.repository.ts   Implements UaiFrontendToolRepositoryApi for tool picker
│   │   ├── examples/                     Example frontend tools + renderers
│   │   ├── entity/                       Entity tools (set_property_value)
│   │   └── umbraco/                      Backend tool renderers (search_umbraco)
│   │
│   ├── section-detector.ts              URL-based section detection
│   └── types.ts                         UaiCopilotAgentItem (copilot-specific)
```

**What gets deleted from copilot** (now in `@umbraco-ai/agent-ui`):

```
DELETED from copilot:
├── components/chat/chat.element.ts
├── components/chat/message.element.ts
├── components/chat/input.element.ts
├── components/chat/agent-status.element.ts
├── components/chat/tool-renderer.element.ts
├── components/chat/approval-base.element.ts
├── components/chat/hitl-approval.element.ts
├── components/chat/message-copy-button.element.ts
├── components/chat/message-regenerate-button.element.ts
├── services/copilot-run.controller.ts         (replaced by UaiRunController from agent-ui)
├── services/tool.manager.ts                   (split into ToolRendererManager + FrontendToolManager)
├── hitl.context.ts
├── interrupts/interrupt-handler.registry.ts
├── interrupts/types.ts
├── interrupts/handlers/hitl-interrupt.handler.ts
├── interrupts/handlers/default-interrupt.handler.ts
├── tools/uai-agent-tool.extension.ts          (deleted -- replaced by new types)
├── tools/tool-status.element.ts
├── tools/default/default.tool.kind.ts
├── approval/uai-agent-approval-element.extension.ts
├── approval/elements/default.element.ts
├── approval/manifests.ts
├── utils/json.ts
```

### 1h: FrontendToolManager (new, copilot-only)

```typescript
import { type UaiFrontendTool } from "@umbraco-ai/agent";

// Extracted from UaiToolManager -- only the execution/LLM concerns
// Produces UaiFrontendTool[] (extends AGUITool with scope + isDestructive metadata)
export class UaiFrontendToolManager extends UmbControllerBase {
    #apiCache: Map<string, UaiAgentToolApi> = new Map();
    #frontendTools = new BehaviorSubject<UaiFrontendTool[]>([]);

    readonly frontendTools$ = this.#frontendTools.asObservable();
    get frontendTools(): UaiFrontendTool[] { return [...this.#frontendTools.value]; }

    constructor(host: UmbControllerHost) {
        super(host);
        // Observe "uaiAgentFrontendTool" extensions only
        this.observe(umbExtensionsRegistry.byType("uaiAgentFrontendTool"), (manifests) => {
            this.#frontendTools.next(manifests.map(m => ({
                name: m.meta.toolName,
                description: m.meta.description,
                parameters: m.meta.parameters,
                // Permission metadata -- forwarded to backend via UaiAgentClient
                scope: m.meta.scope,
                isDestructive: m.meta.isDestructive ?? false,
            })));
        });
    }

    isFrontendTool(toolName: string): boolean { ... }
    async getApi(toolName: string): Promise<UaiAgentToolApi> { ... }
}
```

### 1i: Copilot's updated dependencies

```json
{
  "name": "@umbraco-ai/copilot",
  "peerDependencies": {
    "@umbraco-ai/core": "workspace:*",
    "@umbraco-ai/agent": "workspace:*",
    "@umbraco-ai/agent-ui": "workspace:*",
    "@umbraco-cms/backoffice": "^17.1.0"
  }
}
```

### 1j: Public exports from agent-ui

```typescript
// @umbraco-ai/agent-ui exports

// Components
export { UaiChatElement } from "./chat/components/chat.element.js";
export { UaiChatMessageElement } from "./chat/components/message.element.js";
export { UaiChatInputElement } from "./chat/components/input.element.js";
export { UaiAgentStatusElement } from "./chat/components/agent-status.element.js";
export { UaiToolRendererElement } from "./chat/components/tool-renderer.element.js";
export { UaiAgentToolStatusElement } from "./chat/components/tool-status.element.js";

// Services
export { UaiToolRendererManager } from "./chat/services/tool-renderer.manager.js";
export { UaiRunController, type UaiRunControllerConfig } from "./chat/services/run.controller.js";
export { UaiHitlContext, UAI_HITL_CONTEXT } from "./chat/services/hitl.context.js";
export { UaiInterruptHandlerRegistry } from "./chat/services/interrupt-handler.registry.js";

// Context
export { UAI_CHAT_CONTEXT, type UaiChatContextApi } from "./chat/context.js";

// Extension types (for tool/approval authors)
export type { ManifestUaiAgentToolRenderer } from "./chat/extensions/uai-agent-tool-renderer.extension.js";
export type { ManifestUaiAgentApprovalElement } from "./chat/extensions/uai-agent-approval-element.extension.js";
export type { UaiAgentToolStatus, UaiAgentToolElementProps, UaiAgentToolElement } from "./chat/types/tool.types.js";
export type { UaiAgentToolApprovalConfig } from "./chat/types/tool.types.js";

// Types (re-exported from agent for convenience)
export type { UaiChatMessage, UaiToolCallInfo, UaiInterruptInfo, UaiAgentState } from "./chat/types/index.js";

// Utils
export { safeParseJson } from "./chat/utils/json.js";
```

---

## Phase 2: Create Central Chat Package

**Goal**: Add the `chat` scope and central chat workspace surface.

### 2a: Backend -- Chat scope

Create `Umbraco.AI.Agent.Chat/` following the Copilot pattern:

```csharp
// Umbraco.AI.Agent.Chat/src/Umbraco.AI.Agent.Chat/Scope/ChatAgentScope.cs
[AIAgentScope(ScopeId, Icon = "icon-chat")]
public class ChatAgentScope : AIAgentScopeBase
{
    public const string ScopeId = "chat";
}
```

Minimal NuGet package: scope definition + static assets host. No controllers, no services -- it's just a scope and a frontend.

### 2b: Frontend -- Chat workspace

```
@umbraco-ai/chat
├── src/
│   ├── index.ts
│   ├── exports.ts
│   ├── app.ts
│   ├── manifests.ts
│   └── chat/
│       ├── chat.context.ts              Chat facade (implements UaiChatContextApi)
│       │                                 - No panel state (always visible)
│       │                                 - No entity context injection
│       │                                 - Creates UaiRunController WITHOUT FrontendToolManager
│       │                                 - Provides UAI_CHAT_CONTEXT
│       │
│       ├── components/
│       │   ├── chat-workspace.element.ts  Full-page workspace layout
│       │   │                               - Renders <uai-chat> from agent-ui
│       │   │                               - Full-width, no sidebar constraints
│       │   │                               - Persistent conversation (no reset on nav)
│       │   └── manifests.ts               workspace + workspaceView manifests
│       │
│       ├── repository/
│       │   └── chat-agent.repository.ts   Filters agents by scope="chat"
│       │
│       └── types.ts
```

**package.json**:

```json
{
  "name": "@umbraco-ai/chat",
  "version": "0.0.0",
  "peerDependencies": {
    "@umbraco-ai/agent": "workspace:*",
    "@umbraco-ai/agent-ui": "workspace:*",
    "@umbraco-cms/backoffice": "^17.1.0"
  }
}
```

### 2c: Chat context implementation

```typescript
export class UaiChatContext extends UmbControllerBase implements UaiChatContextApi {
    #agentRepository: UaiChatAgentRepository;
    #runController: UaiRunController;
    #hitlContext: UaiHitlContext;
    // ... (no entity adapter, no panel state)

    constructor(host: UmbControllerHost) {
        super(host);
        const toolRendererManager = new UaiToolRendererManager(host);
        this.#hitlContext = new UaiHitlContext(host);
        this.#runController = new UaiRunController(host, this.#hitlContext, {
            toolRendererManager,
            // NO frontendToolProvider -- chat uses server-side tools only
            interruptHandlers: [
                new UaiHitlInterruptHandler(this),
                new UaiDefaultInterruptHandler(),
            ],
        });
        this.#agentRepository = new UaiChatAgentRepository(host);
        this.provideContext(UAI_CHAT_CONTEXT, this);
    }

    async sendUserMessage(content: string): Promise<void> {
        // No entity context serialization -- just send the message
        this.#runController.sendUserMessage(content, []);
    }
}
```

### 2d: Workspace registration

Register as a section or workspace in the Umbraco backoffice. The chat workspace appears as a dedicated section or within the existing AI settings area.

---

## Phase Execution

| Phase | Description | Ship as |
|---|---|---|
| **1** | Create agent-ui, manifest split, move shared code, slim copilot, delete old types | Single PR |
| **2** | Add central chat surface + chat scope | Separate PR |

Phase 1 is the structural refactoring. Phase 2 is purely additive new functionality. Both are breaking relative to the current alpha, but since there's no stability contract, that's fine.

---

## Tool Registration Examples

### Frontend tool with custom UI and approval (copilot-only execution, shared rendering)

```typescript
import type { ManifestUaiAgentToolRenderer } from "@umbraco-ai/agent-ui";
import type { ManifestUaiAgentFrontendTool } from "@umbraco-ai/copilot";

// Renderer -- registered globally, works in both copilot and chat
const renderer: ManifestUaiAgentToolRenderer = {
    type: "uaiAgentToolRenderer",
    alias: "My.AgentToolRenderer.Example",
    meta: {
        toolName: "my_tool",
        icon: "icon-search",
        approval: true,
    },
    element: () => import("./my-tool.element.js"),
};

// Frontend tool -- registered only in copilot context
const frontendTool: ManifestUaiAgentFrontendTool = {
    type: "uaiAgentFrontendTool",
    alias: "My.AgentFrontendTool.Example",
    meta: {
        toolName: "my_tool",
        description: "Does something",
        parameters: { type: "object", properties: { query: { type: "string" } } },
        scope: "search",             // permission grouping
        isDestructive: false,         // safe operation
    },
    api: () => import("./my-tool.api.js"),
};
```

### Backend tool with custom UI (shared rendering, server execution)

```typescript
import type { ManifestUaiAgentToolRenderer } from "@umbraco-ai/agent-ui";

const renderer: ManifestUaiAgentToolRenderer = {
    type: "uaiAgentToolRenderer",
    alias: "My.AgentToolRenderer.Search",
    meta: { toolName: "search_content", icon: "icon-search" },
    element: () => import("./search-results.element.js"),
};
// No ManifestUaiAgentFrontendTool -- execution is server-side
```

### Frontend tool with no custom UI (copilot-only, default status indicator)

```typescript
import type { ManifestUaiAgentFrontendTool } from "@umbraco-ai/copilot";

const frontendTool: ManifestUaiAgentFrontendTool = {
    type: "uaiAgentFrontendTool",
    alias: "My.AgentFrontendTool.GetTime",
    meta: {
        toolName: "get_current_time",
        description: "Returns the current date and time",
        parameters: { type: "object", properties: {} },
    },
    api: () => import("./get-time.api.js"),
};
// No renderer needed -- default tool status indicator is used automatically
```

---

## Localization

Chat components currently use keys prefixed with `uaiCopilot_`. Since we're alpha:

- Rename to `uaiChat_` in agent-ui
- No backward-compatible aliases needed
- Copilot-specific keys (sidebar title, section labels) stay as `uaiCopilot_`
- Chat-specific keys use `uaiChat_` prefix

---

## API Changes from dev (merged 2026-02-09)

Several features landed on dev that affect the plan. This section documents what changed and how the plan adapts.

### Tool Permissions System

A full tool permissions system was added. This affects both manifest types and the transport layer.

**New backend concepts:**
- `AIAgent` now has `AllowedToolIds`, `AllowedToolScopeIds`, and `UserGroupPermissions` -- agents control which tools they can use
- `AIAgentToolHelper` resolves effective tool permissions (agent defaults + user group overrides - denials)
- `AIToolScopeCollection` / `IAIToolScope` -- backend tool scopes for permission grouping (e.g., "content-read", "content-write")
- `AIFrontendTool` record (C#) -- wraps `AGUITool` with `Scope` and `IsDestructive` metadata

**New frontend concepts:**
- `ManifestUaiAgentTool.meta` gained `scope?: string` and `isDestructive?: boolean` fields
- `UaiFrontendTool` type (in `@umbraco-ai/agent` transport) extends AG-UI `Tool` with `scope` and `isDestructive`
- `UaiToolManager` now produces `UaiFrontendTool[]` instead of `AGUITool[]`
- `UaiAgentClient.sendMessage()` splits `UaiFrontendTool[]` into AG-UI tools + `toolMetadata` in `forwardedProps`
- `RunAgentController` recombines tools with metadata on the server, then filters by agent permissions
- `UaiFrontendToolRepository` in copilot -- exposes frontend tool metadata for the backoffice tool picker
- `UaiFrontendToolRepositoryApi` / `UaiFrontendToolData` in `@umbraco-ai/core` -- interface for frontend tool discovery

**Impact on the plan:**

1. **`ManifestUaiAgentFrontendTool` meta needs `scope` and `isDestructive`**. These fields were added to the old `ManifestUaiAgentTool.meta` and must carry over to the new frontend tool manifest:

    ```typescript
    interface ManifestUaiAgentFrontendTool extends ManifestApi<UaiAgentToolApi> {
        type: "uaiAgentFrontendTool";
        meta: {
            toolName: string;
            description: string;
            parameters: Record<string, unknown>;
            scope?: string;           // NEW
            isDestructive?: boolean;   // NEW
        };
    }
    ```

2. **`FrontendToolManager` must produce `UaiFrontendTool[]`** (not plain `AGUITool[]`). It reads `scope` and `isDestructive` from the manifest and includes them in the tool objects passed to the AG-UI client.

3. **`UaiFrontendToolRepository` stays in copilot**. It queries the extension registry for frontend tool manifests to feed the backoffice tool picker. After the split, it queries `"uaiAgentFrontendTool"` extensions instead of `"uaiAgentTool"`.

4. **`UaiFrontendToolRepositoryApi` stays in `@umbraco-ai/core`**. It's the interface contract -- any package providing frontend tools implements it. This is correct and doesn't change.

5. **`ManifestUaiAgentToolRenderer` does NOT need `scope` or `isDestructive`**. These are execution/permission concerns, not rendering concerns. The renderer only needs `toolName`, `label`, `icon`, and `approval`.

6. **The transport layer (`UaiAgentClient`) already handles the split correctly**. It accepts `UaiFrontendTool[]`, splits into AG-UI tools + metadata, and sends metadata via `forwardedProps`. No changes needed -- the `FrontendToolManager` just needs to produce the right type.

### Workspace Validation

All workspaces (Connection, Profile, Agent, Prompt) gained client-side validation with alias uniqueness checking. This is purely additive and doesn't affect the plan. The agent workspace editor's validation will stay in `@umbraco-ai/agent`, not in the copilot or chat packages.

### Alias Existence Endpoints

New `AliasExists` controllers added across Core, Agent, and Prompt. Backend-only -- no impact on the split.

---

## Azure Pipeline Configuration

The split introduces two new products (`Umbraco.AI.Agent.UI`, `Umbraco.AI.Agent.Chat`) and changes the build level of the existing Copilot. This section documents all CI/CD changes needed.

### Build level impact

The NuGet dependency chain after the split:

```
Level 0: Umbraco.AI
Level 1: Providers, Prompt, Agent
Level 2: Agent.UI (depends on Agent)
Level 3: Agent.Copilot, Agent.Chat (depend on Agent.UI)  ← NEW LEVEL
```

Currently Copilot is Level 2 (depends on Agent). After the split, Copilot and Chat both depend on Agent.UI, which pushes them to Level 3. The pipeline currently only supports Levels 0-2, so a new `level3Products` parameter and `PackLevel3` job are required.

**Why Copilot/Chat must NuGet-depend on Agent.UI**: Both packages need the shared UI static assets (App_Plugins) to be automatically installed. Without the NuGet dependency, users would have to manually install `Umbraco.AI.Agent.UI` alongside Copilot or Chat.

### `azure-pipelines.yml` parameter changes

```yaml
parameters:
    - name: level1Products
      type: object
      default:
          # Providers (no frontend)
          - name: Umbraco.AI.OpenAI
            changeVar: OpenaiChanged
            hasNpm: false
          - name: Umbraco.AI.Anthropic
            changeVar: AnthropicChanged
            hasNpm: false
          - name: Umbraco.AI.Amazon
            changeVar: AmazonChanged
            hasNpm: false
          - name: Umbraco.AI.Google
            changeVar: GoogleChanged
            hasNpm: false
          - name: Umbraco.AI.MicrosoftFoundry
            changeVar: MicrosoftfoundryChanged
            hasNpm: false
          # Add-ons
          - name: Umbraco.AI.Prompt
            changeVar: PromptChanged
            hasNpm: false
          - name: Umbraco.AI.Agent
            changeVar: AgentChanged
            hasNpm: true
    - name: level2Products                   # CHANGED -- Copilot removed, Agent.UI added
      type: object
      default:
          - name: Umbraco.AI.Agent.UI
            changeVar: AgentuiChanged
            hasNpm: true
    - name: level3Products                   # NEW
      type: object
      default:
          - name: Umbraco.AI.Agent.Copilot
            changeVar: AgentCopilotChanged
            hasNpm: true
          - name: Umbraco.AI.Agent.Chat
            changeVar: AgentChatChanged
            hasNpm: true
```

### New `PackLevel3` job

A new pack job following the same pattern as `PackLevel2`, but downloading Core, Level 1, and Level 2 packages as local NuGet feeds:

```yaml
- job: PackLevel3
  displayName: Pack Level 3
  dependsOn:
      - DetectChanges
      - PackCore
      - PackLevel1
      - PackLevel2
  condition: |
      and(
        not(failed()),
        not(canceled()),
        eq(dependencies.DetectChanges.outputs['detect.Level3Changed'], 'true')
      )
  strategy:
      matrix:
          ${{ each product in parameters.level3Products }}:
              ${{ replace(replace(product.name, 'Umbraco.AI.', ''), '.', '_') }}:
                  product: ${{ product.name }}
                  changeVar: ${{ product.changeVar }}
      maxParallel: 10
  variables:
      CoreChanged: $[ dependencies.DetectChanges.outputs['detect.CoreChanged'] ]
      ${{ each product in parameters.level1Products }}:
          ${{ product.changeVar }}: $[ dependencies.DetectChanges.outputs['detect.${{ product.changeVar }}'] ]
      ${{ each product in parameters.level2Products }}:
          ${{ product.changeVar }}: $[ dependencies.DetectChanges.outputs['detect.${{ product.changeVar }}'] ]
      ${{ each product in parameters.level3Products }}:
          ${{ product.changeVar }}: $[ dependencies.DetectChanges.outputs['detect.${{ product.changeVar }}'] ]
  steps:
      - checkout: self
        fetchDepth: 0
      - template: .azure-pipelines/templates/check-should-pack.yml
      - template: .azure-pipelines/templates/setup-pack-job.yml
        parameters:
            downloadCore: true
            downloadLevel1: true
            downloadLevel2: true          # NEW parameter
            level1Products: ${{ parameters.level1Products }}
            level2Products: ${{ parameters.level2Products }}
      - template: .azure-pipelines/templates/pack-product.yml
        parameters:
            product: $(product)
            useProjectReferences: ${{ eq(variables['Build.SourceBranchName'], 'dev') }}
            condition: eq(variables['check.shouldPack'], 'true')
      - template: .azure-pipelines/templates/upload-packages.yml
        parameters:
            product: $(product)
            condition: eq(variables['check.shouldPack'], 'true')
```

### Template changes

**`setup-pack-job.yml`** -- Add `downloadLevel2` parameter:

```yaml
parameters:
    - name: downloadLevel2
      type: boolean
      default: false
    - name: level2Products
      type: object
      default: []

# Add after existing Level 1 download:
- ${{ if eq(parameters.downloadLevel2, true) }}:
      - ${{ each product in parameters.level2Products }}:
            - task: DownloadPipelineArtifact@2
              displayName: Download ${{ product.name }} packages (local feed)
              condition: |
                  and(
                    ${{ parameters.condition }},
                    eq(variables['${{ product.changeVar }}'], 'true')
                  )
              inputs:
                  artifact: ${{ product.name }}-packages
                  path: $(Build.SourcesDirectory)/artifacts/nupkg
```

### `CollectPackages` job updates

Add `PackLevel3` to `dependsOn`, add Level 3 change variables, and add Level 3 download steps:

```yaml
- job: CollectPackages
  dependsOn:
      - DetectChanges
      - PackCore
      - PackLevel1
      - PackLevel2
      - PackLevel3          # NEW

  variables:
      # ... existing Core + Level 1 + Level 2 variables ...
      ${{ each product in parameters.level3Products }}:
          ${{ product.changeVar }}: $[ dependencies.DetectChanges.outputs['detect.${{ product.changeVar }}'] ]

  steps:
      # ... existing Core + Level 1 + Level 2 downloads ...

      # Download Level 3 NuGet packages
      - ${{ each product in parameters.level3Products }}:
            - task: DownloadPipelineArtifact@2
              displayName: Download ${{ product.name }} NuGet packages
              condition: eq(variables.${{ product.changeVar }}, 'true')
              inputs:
                  artifact: "${{ product.name }}-packages"
                  targetPath: "$(Pipeline.Workspace)/nuget"

      # Download Level 3 npm packages
      - ${{ each product in parameters.level3Products }}:
            - ${{ if eq(product.hasNpm, true) }}:
                  - task: DownloadPipelineArtifact@2
                    displayName: Download ${{ product.name }} npm packages
                    condition: eq(variables.${{ product.changeVar }}, 'true')
                    inputs:
                        artifact: "${{ product.name }}-npm-packages"
                        targetPath: "$(Pipeline.Workspace)/npm"
```

The `buildManifest` step in CollectPackages also needs `LEVEL3_JSON`:

```yaml
env:
    LEVEL1_JSON: ${{ convertToJson(parameters.level1Products) }}
    LEVEL2_JSON: ${{ convertToJson(parameters.level2Products) }}
    LEVEL3_JSON: ${{ convertToJson(parameters.level3Products) }}
```

And the PowerShell script extended to iterate `$level3`:

```powershell
$level3 = $env:LEVEL3_JSON | ConvertFrom-Json
foreach ($p in $level3) {
    $changeVar = [string]$p.changeVar
    $changed = [Environment]::GetEnvironmentVariable($changeVar)
    if (-not $changed) {
        $changed = [Environment]::GetEnvironmentVariable($changeVar.ToUpper())
    }
    if ($changed -eq "true") {
        Add-VersionByProduct $p.name
    }
}
```

### SBOM generation updates

Add Level 3 to the GenerateSBOM stage:

```yaml
# Level 3 products (matrix)
- job: GenerateSBOM_Level3
  displayName: Generate SBOM - Level 3
  pool:
      vmImage: "ubuntu-latest"
  strategy:
      matrix:
          ${{ each product in parameters.level3Products }}:
              ${{ replace(replace(product.name, 'Umbraco.AI.', ''), '.', '_') }}:
                  product: ${{ product.name }}
      maxParallel: 10
  steps:
      - checkout: self
        fetchDepth: 1
      - task: NodeTool@0
        displayName: Setup Node.js
        inputs:
            versionSpec: $(NODE_VERSION)
      - template: .azure-pipelines/templates/generate-sbom.yml
        parameters:
            product: $(product)
```

`CollectSBOMs` adds `GenerateSBOM_Level3` to `dependsOn` and downloads Level 3 SBOMs:

```yaml
- job: CollectSBOMs
  dependsOn:
      - GenerateSBOM_Core
      - GenerateSBOM_Level1
      - GenerateSBOM_Level2
      - GenerateSBOM_Level3      # NEW
  steps:
      # ... existing downloads ...
      - ${{ each product in parameters.level3Products }}:
            - task: DownloadPipelineArtifact@2
              displayName: Download ${{ product.name }} SBOM
              inputs:
                  artifact: "${{ product.name }}-sbom"
                  targetPath: "$(Pipeline.Workspace)/sboms"
```

### `detect-changes.ps1` updates

The script auto-discovers products and computes build levels from `.csproj` dependencies. No changes needed to the detection logic itself -- it already handles arbitrary dependency depths. It outputs:

- Per-product variables: `CoreChanged`, `AgentChanged`, `AgentuiChanged`, `AgentCopilotChanged`, `AgentChatChanged`
- Per-level variables: `Level0Changed`, `Level1Changed`, `Level2Changed`, `Level3Changed`

The script's `Get-VariableName` function converts product keys to variable names:
- `"agent.ui"` → `"AgentuiChanged"` (dots are collapsed in the PascalCase conversion)
- `"agent.chat"` → `"AgentchatChanged"`
- `"agent.copilot"` → `"AgentcopilotChanged"` (unchanged from current)

Verify the `changeVar` values in the pipeline parameters match what the script produces. The current `AgentCopilotChanged` works because `Get-ProductKey("Umbraco.AI.Agent.Copilot")` returns `"agent.copilot"` and `Get-VariableName` converts that to `"AgentcopilotChanged"` -- but the parameter says `AgentCopilotChanged` (capital C). This existing inconsistency should be checked; if the detect script is case-insensitive, this works, but if not, align the casing.

### npm workspace and build order

**Root `package.json`** -- add new workspaces and build scripts:

```json
{
    "workspaces": [
        "Umbraco.AI/src/Umbraco.AI.Web.StaticAssets/Client",
        "Umbraco.AI.Prompt/src/Umbraco.AI.Prompt.Web.StaticAssets/Client",
        "Umbraco.AI.Agent/src/Umbraco.AI.Agent.Web.StaticAssets/Client",
        "Umbraco.AI.Agent.UI/src/Umbraco.AI.Agent.UI/Client",
        "Umbraco.AI.Agent.Copilot/src/Umbraco.AI.Agent.Copilot/Client",
        "Umbraco.AI.Agent.Chat/src/Umbraco.AI.Agent.Chat/Client"
    ],
    "scripts": {
        "build": "npm run build:core && npm run build:prompt && npm run build:agent && npm run build:agent-ui && npm run build:copilot && npm run build:chat",
        "build:core": "npm run build -w @umbraco-ai/core",
        "build:prompt": "npm run build -w @umbraco-ai/prompt",
        "build:agent": "npm run build -w @umbraco-ai/agent",
        "build:agent-ui": "npm run build -w @umbraco-ai/agent-ui",
        "build:copilot": "npm run build -w @umbraco-ai/copilot",
        "build:chat": "npm run build -w @umbraco-ai/chat",
        "watch": "concurrently --names \"core,prompt,agent,agent-ui,copilot,chat\" -c \"blue,green,yellow,cyan,magenta,red\" \"npm run watch:core\" \"npm run watch:prompt\" \"npm run watch:agent\" \"npm run watch:agent-ui\" \"npm run watch:copilot\" \"npm run watch:chat\"",
        "watch:core": "npm run watch -w @umbraco-ai/core",
        "watch:prompt": "npm run watch -w @umbraco-ai/prompt",
        "watch:agent": "npm run watch -w @umbraco-ai/agent",
        "watch:agent-ui": "npm run watch -w @umbraco-ai/agent-ui",
        "watch:copilot": "npm run build -w @umbraco-ai/copilot",
        "watch:chat": "npm run watch -w @umbraco-ai/chat"
    }
}
```

The sequential build order (`&&`) ensures npm dependencies are available: `core → prompt → agent → agent-ui → copilot → chat`.

### New product scaffolding files

Each new product needs:

| File | Product | Purpose |
|---|---|---|
| `Umbraco.AI.Agent.UI/changelog.config.json` | Agent.UI | Commit scopes: `["agent-ui"]` |
| `Umbraco.AI.Agent.Chat/changelog.config.json` | Agent.Chat | Commit scopes: `["chat"]` |
| `Umbraco.AI.Agent.UI/version.json` | Agent.UI | NBGV versioning configuration |
| `Umbraco.AI.Agent.Chat/version.json` | Agent.Chat | NBGV versioning configuration |
| `Umbraco.AI.Agent.UI/Directory.Packages.props` | Agent.UI | NuGet version range for Agent dependency |
| `Umbraco.AI.Agent.Chat/Directory.Packages.props` | Agent.Chat | NuGet version range for Agent dependency |

**changelog.config.json examples**:

```json
// Umbraco.AI.Agent.UI/changelog.config.json
{ "scopes": ["agent-ui"] }

// Umbraco.AI.Agent.Chat/changelog.config.json
{ "scopes": ["chat"] }
```

The commitlint config auto-discovers these -- no changes needed to `commitlint.config.js`.

### `Umbraco.AI.local.sln` update

The local unified solution must include the two new projects. Regenerate with:

```bash
dotnet sln Umbraco.AI.local.sln add Umbraco.AI.Agent.UI/Umbraco.AI.Agent.UI.sln
dotnet sln Umbraco.AI.local.sln add Umbraco.AI.Agent.Chat/Umbraco.AI.Agent.Chat.sln
```

### Summary of pipeline file changes

| File | Change |
|---|---|
| `azure-pipelines.yml` | Add `level3Products` parameter; move Copilot to Level 3; add Agent.UI to Level 2; add Agent.Chat to Level 3; add `PackLevel3` job; add `GenerateSBOM_Level3` job; update `CollectPackages` and `CollectSBOMs` dependencies and downloads |
| `.azure-pipelines/templates/setup-pack-job.yml` | Add `downloadLevel2` + `level2Products` parameters and download steps |
| `package.json` (root) | Add workspaces for Agent.UI and Agent.Chat; add build/watch scripts; update build order |
| `NuGet.CI.config` | No changes needed (existing `Umbraco.AI.*` pattern covers new packages) |
| `.azure-pipelines/scripts/detect-changes.ps1` | No changes needed (auto-discovers products and computes levels) |
| `commitlint.config.js` | No changes needed (auto-discovers scopes from `changelog.config.json` files) |

---

## Risks and Considerations

### Package naming

The copilot npm package is currently `@umbraco-ai/agent-copilot`. This plan uses `@umbraco-ai/copilot`. Since we're alpha, rename now. The NuGet package name `Umbraco.AI.Agent.Copilot` stays regardless.

### Component tag name changes

Renaming `<uai-copilot-chat>` to `<uai-chat>` etc. is a breaking change for anyone targeting these elements in CSS or JS. Alpha -- acceptable.

### Build order in CI

Adding `@umbraco-ai/agent-ui` between `@umbraco-ai/agent` and `@umbraco-ai/copilot` in the build chain. Update `npm run build` script ordering and CI pipeline.

### Test coverage

The copilot currently has no frontend tests. This split is an opportunity to add them, but shouldn't block the refactoring.
