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

## Phase 1: Manifest Type Split

**Goal**: Define the two new extension types without changing runtime behavior yet. The old `ManifestUaiAgentTool` type remains temporarily as a bridge.

### New types

Create in `@umbraco-ai/agent-ui` (initially can be authored in copilot, moved in phase 2):

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

// ManifestUaiAgentFrontendTool -- copilot-only, for browser-executable tools
interface ManifestUaiAgentFrontendTool extends ManifestApi<UaiAgentToolApi> {
    type: "uaiAgentFrontendTool";
    meta: {
        toolName: string;
        description: string;     // required -- LLM needs this
        parameters: Record<string, unknown>;  // required -- LLM needs this
    };
}
```

### Shared types (stay as-is, move to agent-ui)

```typescript
UaiAgentToolStatus        // "pending" | "streaming" | "awaiting_approval" | ...
UaiAgentToolElementProps   // { args, status, result }
UaiAgentToolElement        // UmbControllerHostElement & UaiAgentToolElementProps
UaiAgentToolApprovalConfig // true | { elementAlias?, config? }
UaiAgentToolApi            // { execute(args): Promise<unknown> } -- type stays shared, usage is copilot-only
```

### Migration of existing tool registrations

Each existing `ManifestUaiAgentTool` registration becomes one or two new registrations:

| Current tool | Has `api`? | Has `element`? | Becomes |
|---|---|---|---|
| `getCurrentTime` | Yes | No | 1x `uaiAgentFrontendTool` |
| `getPageInfo` | Yes | No | 1x `uaiAgentFrontendTool` |
| `showWeather` | Yes | Yes | 1x `uaiAgentFrontendTool` + 1x `uaiAgentToolRenderer` |
| `confirmAction` | Yes | No | 1x `uaiAgentFrontendTool` (approval config moves to renderer) |
| `set_property_value` | Yes | No | 1x `uaiAgentFrontendTool` + 1x `uaiAgentToolRenderer` (for approval) |
| `search_umbraco` | No | Yes | 1x `uaiAgentToolRenderer` |

**Rule**: If a tool has approval config, it needs a `uaiAgentToolRenderer` registration (approval lives on the renderer, not the frontend tool). If a tool is execute-only with no custom UI and no approval, it only needs a `uaiAgentFrontendTool`.

### Deprecation of `ManifestUaiAgentTool`

Keep the old type temporarily with a `@deprecated` JSDoc tag. The `ToolManager` (before split) can handle both old and new types during the transition:

```typescript
// In ToolRendererManager (shared):
// Observe both "uaiAgentToolRenderer" AND legacy "uaiAgentTool" for element resolution

// In FrontendToolManager (copilot):
// Observe both "uaiAgentFrontendTool" AND legacy "uaiAgentTool" (with api) for tool extraction
```

Remove the old type after all registrations are migrated.

### Files to create/modify

| Action | File | Notes |
|---|---|---|
| Create | `copilot/tools/uai-agent-tool-renderer.extension.ts` | `ManifestUaiAgentToolRenderer` type + global declaration |
| Create | `copilot/tools/uai-agent-frontend-tool.extension.ts` | `ManifestUaiAgentFrontendTool` type + global declaration |
| Modify | `copilot/tools/uai-agent-tool.extension.ts` | Add `@deprecated` to `ManifestUaiAgentTool` |
| Modify | `copilot/tools/examples/manifests.ts` | Split each tool into renderer + frontend-tool registrations |
| Modify | `copilot/tools/entity/manifests.ts` | Split `set_property_value` |
| Modify | `copilot/tools/umbraco/manifests.ts` | Change to `uaiAgentToolRenderer` type |
| Modify | `copilot/tools/exports.ts` | Export new types |

---

## Phase 2: Create `@umbraco-ai/agent-ui` Package

**Goal**: Establish the shared chat UI package and move components + services from copilot.

### 2a: Scaffold the package

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

### 2b: Move shared components

Move from `@umbraco-ai/agent-copilot` `copilot/` to `@umbraco-ai/agent-ui` `chat/`:

#### Components

| Source (copilot) | Destination (agent-ui) | New tag name | Notes |
|---|---|---|---|
| `components/chat/chat.element.ts` | `chat/components/chat.element.ts` | `<uai-chat>` | Drop copilot prefix. Remove `UAI_COPILOT_CONTEXT` dependency -- accept props/context interface instead |
| `components/chat/message.element.ts` | `chat/components/message.element.ts` | `<uai-chat-message>` | Pure rendering, no context dependency |
| `components/chat/input.element.ts` | `chat/components/input.element.ts` | `<uai-chat-input>` | Remove agent selector coupling -- accept agents as prop |
| `components/chat/agent-status.element.ts` | `chat/components/agent-status.element.ts` | `<uai-agent-status>` | Already context-free |
| `components/chat/tool-renderer.element.ts` | `chat/components/tool-renderer.element.ts` | `<uai-tool-renderer>` | Change to use `ToolRendererManager` instead of `UaiToolManager` |
| `components/chat/approval-base.element.ts` | `chat/components/approval-base.element.ts` | `<uai-approval-base>` | Already generic |
| `components/chat/hitl-approval.element.ts` | `chat/components/hitl-approval.element.ts` | `<uai-hitl-approval>` | Drop copilot prefix |
| `components/chat/message-copy-button.element.ts` | `chat/components/message-copy-button.element.ts` | `<uai-message-copy-button>` | Already generic |
| `components/chat/message-regenerate-button.element.ts` | `chat/components/message-regenerate-button.element.ts` | `<uai-message-regenerate-button>` | Already generic |
| `tools/tool-status.element.ts` | `chat/components/tool-status.element.ts` | `<uai-agent-tool-status>` | Already generic |

#### Services

| Source (copilot) | Destination (agent-ui) | New name | Notes |
|---|---|---|---|
| `services/tool.manager.ts` | `chat/services/tool-renderer.manager.ts` | `UaiToolRendererManager` | **Only the rendering half**: manifest lookup, element resolution, element caching. Remove `frontendTools$`, `getApi()`, `isFrontendTool()`. Observe `"uaiAgentToolRenderer"` (+ legacy `"uaiAgentTool"` during transition) |
| `services/copilot-run.controller.ts` | `chat/services/run.controller.ts` | `UaiRunController` | Extract base class or refactor to accept tool manager and tool executor as constructor params. The copilot version passes `FrontendToolManager` + `FrontendToolExecutor`. The chat version passes only `ToolRendererManager` (no frontend tools). See details below. |
| `hitl.context.ts` | `chat/services/hitl.context.ts` | `UaiHitlContext` | Move as-is. Used by both surfaces for HITL approval rendering. |
| `interrupts/interrupt-handler.registry.ts` | `chat/services/interrupt-handler.registry.ts` | `UaiInterruptHandlerRegistry` | Already generic |
| `interrupts/types.ts` | `chat/services/interrupt.types.ts` | (types) | Already generic |
| `interrupts/handlers/hitl-interrupt.handler.ts` | `chat/services/handlers/hitl-interrupt.handler.ts` | `UaiHitlInterruptHandler` | Used by both surfaces |
| `interrupts/handlers/default-interrupt.handler.ts` | `chat/services/handlers/default-interrupt.handler.ts` | `UaiDefaultInterruptHandler` | Used by both surfaces |

**Note**: `UaiToolExecutionHandler` stays in copilot. It's the handler that invokes `UaiFrontendToolExecutor` for browser-side tool execution, which is copilot-only.

#### Types

| Source (copilot) | Destination (agent-ui) | Notes |
|---|---|---|
| `tools/uai-agent-tool-renderer.extension.ts` | `chat/extensions/uai-agent-tool-renderer.extension.ts` | Created in phase 1 |
| `tools/uai-agent-tool.extension.ts` (shared types only) | `chat/types/tool.types.ts` | `UaiAgentToolStatus`, `UaiAgentToolElementProps`, `UaiAgentToolElement`, `UaiAgentToolApprovalConfig` |
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

### 2c: Run controller refactoring

The `UaiCopilotRunController` currently couples frontend tool execution into the run lifecycle. For the shared layer, we need a run controller that both surfaces can use.

**Approach**: Make the run controller configurable via constructor injection.

```typescript
// In @umbraco-ai/agent-ui
export interface UaiRunControllerConfig {
    /** Tool renderer manager for manifest/element lookup */
    toolRendererManager: UaiToolRendererManager;
    /** Optional frontend tool provider -- copilot injects this, chat does not */
    frontendToolProvider?: {
        /** Tools to send in the AG-UI request */
        frontendTools: AGUITool[];
        frontendTools$: Observable<AGUITool[]>;
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

- **Copilot** creates the run controller with `frontendToolProvider` set to `FrontendToolManager` and registers `UaiToolExecutionHandler` in the interrupt handlers array.
- **Chat** creates the run controller with `frontendToolProvider` unset (no frontend tools sent in AG-UI request) and only registers the HITL + default handlers.

The run controller's `sendMessage()` reads `frontendToolProvider.frontendTools` (or empty array if not set) and passes them to the AG-UI client. No if/else branching needed -- just absence of data.

### 2d: Decouple chat component from copilot context

Currently `<uai-copilot-chat>` consumes `UAI_COPILOT_CONTEXT`. The shared `<uai-chat>` should consume a more generic context interface.

**Approach**: Define a chat context interface in agent-ui.

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

Both `UaiCopilotContext` and the new `UaiChatContext` implement this interface. The shared chat components consume `UAI_CHAT_CONTEXT`. Each surface provides its own implementation.

### 2e: Public exports from agent-ui

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

## Phase 3: Slim Down Copilot

**Goal**: Remove everything that moved to `@umbraco-ai/agent-ui`. The copilot becomes a thin surface shell.

### What stays in copilot

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
│   │   ├── frontend-tool.manager.ts     NEW - extracts frontend tool concerns from UaiToolManager
│   │   │                                 - Observes "uaiAgentFrontendTool" extensions
│   │   │                                 - Provides frontendTools$ and frontendTools for AG-UI
│   │   │                                 - Provides getApi() for tool execution
│   │   │                                 - Provides isFrontendTool()
│   │   └── frontend-tool.executor.ts    Browser-side tool execution (moved reference stays)
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
│   │   ├── examples/                     Example frontend tools + renderers
│   │   ├── entity/                       Entity tools (set_property_value)
│   │   └── umbraco/                      Backend tool renderers (search_umbraco)
│   │
│   ├── section-detector.ts              URL-based section detection
│   └── types.ts                         UaiCopilotAgentItem (copilot-specific)
```

### What gets deleted from copilot (moved to agent-ui)

```
DELETED from copilot (now in @umbraco-ai/agent-ui):
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
├── tools/uai-agent-tool.extension.ts          (deprecated, removed)
├── tools/tool-status.element.ts
├── tools/default/default.tool.kind.ts
├── approval/uai-agent-approval-element.extension.ts
├── approval/elements/default.element.ts
├── approval/manifests.ts
├── utils/json.ts
```

### Copilot's updated dependencies

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

### FrontendToolManager (new, copilot-only)

```typescript
// Extracted from UaiToolManager -- only the execution/LLM concerns
export class UaiFrontendToolManager extends UmbControllerBase {
    #apiCache: Map<string, UaiAgentToolApi> = new Map();
    #frontendTools = new BehaviorSubject<AGUITool[]>([]);

    readonly frontendTools$ = this.#frontendTools.asObservable();
    get frontendTools(): AGUITool[] { return [...this.#frontendTools.value]; }

    constructor(host: UmbControllerHost) {
        super(host);
        // Observe "uaiAgentFrontendTool" extensions
        this.observe(umbExtensionsRegistry.byType("uaiAgentFrontendTool"), (manifests) => {
            this.#frontendTools.next(manifests.map(m => ({
                name: m.meta.toolName,
                description: m.meta.description,
                parameters: m.meta.parameters,
            })));
        });
    }

    isFrontendTool(toolName: string): boolean { ... }
    async getApi(toolName: string): Promise<UaiAgentToolApi> { ... }
}
```

---

## Phase 4: Create Central Chat Package

**Goal**: Add the `chat` scope and central chat workspace surface.

### 4a: Backend -- Chat scope

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

### 4b: Frontend -- Chat workspace

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

### 4c: Chat context implementation

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

### 4d: Workspace registration

Register as a section or workspace in the Umbraco backoffice. The chat workspace appears as a dedicated section or within the existing AI settings area.

---

## Phase Execution Order

Each phase leaves the system in a working state:

| Phase | Description | Breaking? | Ship independently? |
|---|---|---|---|
| **1** | Define new manifest types, deprecate old | No -- old type still works | Yes |
| **2** | Create agent-ui, move shared code, update copilot imports | Yes (internal) -- copilot's import paths change | Yes (ship with phase 3) |
| **3** | Slim copilot, migrate tool registrations to new types | Yes (public API) -- tool authors update manifests | Ship with phase 2 |
| **4** | Add central chat surface | No -- purely additive | Yes |

**Recommended approach**: Phases 1-3 as one PR (the manifest split and extraction are tightly coupled). Phase 4 as a separate PR.

---

## Migration Guide for Tool Authors

### Before (single manifest)

```typescript
const myTool: ManifestUaiAgentTool = {
    type: "uaiAgentTool",
    alias: "My.AgentTool.Example",
    meta: {
        toolName: "my_tool",
        description: "Does something",
        parameters: { type: "object", properties: { query: { type: "string" } } },
        icon: "icon-search",
        approval: true,
    },
    api: () => import("./my-tool.api.js"),
    element: () => import("./my-tool.element.js"),
};
```

### After (two manifests)

```typescript
import type { ManifestUaiAgentToolRenderer } from "@umbraco-ai/agent-ui";
import type { ManifestUaiAgentFrontendTool } from "@umbraco-ai/copilot";

const myToolRenderer: ManifestUaiAgentToolRenderer = {
    type: "uaiAgentToolRenderer",
    alias: "My.AgentToolRenderer.Example",
    meta: {
        toolName: "my_tool",
        icon: "icon-search",
        approval: true,
    },
    element: () => import("./my-tool.element.js"),
};

const myFrontendTool: ManifestUaiAgentFrontendTool = {
    type: "uaiAgentFrontendTool",
    alias: "My.AgentFrontendTool.Example",
    meta: {
        toolName: "my_tool",
        description: "Does something",
        parameters: { type: "object", properties: { query: { type: "string" } } },
    },
    api: () => import("./my-tool.api.js"),
};
```

### Backend-only tools (no change in pattern)

```typescript
const searchRenderer: ManifestUaiAgentToolRenderer = {
    type: "uaiAgentToolRenderer",
    alias: "My.AgentToolRenderer.Search",
    meta: { toolName: "search_content", icon: "icon-search" },
    element: () => import("./search-results.element.js"),
};
// No ManifestUaiAgentFrontendTool needed -- execution is server-side
```

---

## Risks and Considerations

### Package naming

The copilot npm package is currently `@umbraco-ai/agent-copilot`. This plan uses `@umbraco-ai/copilot` for brevity. Decide whether to rename (breaking for npm consumers) or keep the longer name. The NuGet package name `Umbraco.AI.Agent.Copilot` stays regardless.

### Component tag name changes

Renaming `<uai-copilot-chat>` to `<uai-chat>` etc. is a breaking change for anyone targeting these elements in CSS or JS. Since these are internal (not documented as public API), the risk is low.

### Localization keys

Chat components use localization keys prefixed with `uaiCopilot_`. When moving to agent-ui, these should be renamed to `uaiChat_` or kept with backward-compatible aliases.

### Build order in CI

Adding `@umbraco-ai/agent-ui` between `@umbraco-ai/agent` and `@umbraco-ai/copilot` in the build chain. Update `npm run build` script ordering.

### Test coverage

The copilot currently has no frontend tests. This split is an opportunity to add them, but shouldn't block the refactoring.
