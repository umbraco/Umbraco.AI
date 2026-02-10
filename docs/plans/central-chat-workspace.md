# Phase 2: Central Chat Workspace

## Context

Phase 1 (merged to dev) extracted shared chat UI infrastructure from `@umbraco-ai/agent-copilot` into `@umbraco-ai/agent-ui`. This created the shared layer that both the copilot sidebar and a central chat workspace can consume.

This plan creates the central chat workspace -- a full-page AI chat experience accessible from the Umbraco backoffice, alongside the existing copilot drawer.

### Prerequisites (from Phase 1)

These are already in place on dev:

- `@umbraco-ai/agent-ui` package with shared chat components, services, and context contracts
- `UaiChatContextApi` interface and `UAI_CHAT_CONTEXT` token
- `UaiRunController` with config injection (optional `frontendToolManager`)
- `UaiToolRendererManager`, `UaiHitlContext`, interrupt handlers
- `<uai-chat>` shared chat element consuming `UAI_CHAT_CONTEXT`

### Design Decisions

- **The chat workspace is a new section** in the Umbraco backoffice, not a workspace under Settings. It needs its own top-level navigation entry because it's a primary user-facing feature, not a configuration area.
- **No frontend tools initially**. The chat starts with server-side tools only. The `UaiRunController` is created without a `frontendToolManager`. When/if chat gains frontend tools later, the infrastructure in agent-ui already supports it -- just add a `UaiFrontendToolManager` to the config.
- **No entity context initially**. The chat doesn't wrap any workspace entity state. `UAI_ENTITY_CONTEXT` is not provided. Entity-scoped tools (like `set_property_value`) don't resolve in the chat because they're only registered in the copilot package.
- **Persistent conversation**. Unlike the copilot (which resets on navigation), the chat workspace maintains its conversation state while the user navigates within the chat section.
- **Minimal backend**. The NuGet package contains only a scope definition class and frontend static assets. No controllers, no services, no persistence.
- **Scope filtering**. Agents opt into the chat scope via `ScopeIds[]` on the agent entity. The chat only shows agents that include `"chat"` in their scope IDs.

---

## Target Structure

### NuGet Package

```
Umbraco.AI.Agent.Chat/
├── src/
│   └── Umbraco.AI.Agent.Chat/
│       ├── Scope/
│       │   └── ChatAgentScope.cs
│       ├── Client/
│       │   ├── package.json              # @umbraco-ai/chat
│       │   ├── vite.config.ts
│       │   ├── tsconfig.json
│       │   ├── tsconfig.api.json
│       │   ├── api-extractor.json
│       │   ├── public/
│       │   │   └── umbraco-package.json
│       │   └── src/
│       │       ├── index.ts
│       │       ├── exports.ts
│       │       ├── app.ts
│       │       ├── manifests.ts
│       │       ├── chat/
│       │       │   ├── chat.context.ts
│       │       │   ├── types.ts
│       │       │   ├── components/
│       │       │   │   ├── chat-section.element.ts
│       │       │   │   └── manifests.ts
│       │       │   └── repository/
│       │       │       └── chat-agent.repository.ts
│       │       └── lang/
│       │           ├── en.ts
│       │           └── manifests.ts
│       └── Umbraco.AI.Agent.Chat.csproj
├── Umbraco.AI.Agent.Chat.slnx
├── Directory.Build.props
├── version.json
└── changelog.config.json
```

### npm Package

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

### Dependency Chain

```
@umbraco-ai/core
  └─ @umbraco-ai/agent
       └─ @umbraco-ai/agent-ui
            ├─ @umbraco-ai/agent-copilot  (existing)
            └─ @umbraco-ai/chat           (NEW)
```

---

## Implementation Steps

### Step 1: Backend -- Chat Scope

Create the C# scope definition. This is auto-discovered by Umbraco's type loader.

**`Scope/ChatAgentScope.cs`**:

```csharp
using Umbraco.AI.Agent.Core.Scopes;

namespace Umbraco.AI.Agent.Chat.Scope;

[AIAgentScope(ScopeId, Icon = "icon-chat")]
public class ChatAgentScope : AIAgentScopeBase
{
    public const string ScopeId = "chat";
}
```

**`Umbraco.AI.Agent.Chat.csproj`** -- Razor Class Library following the copilot pattern:

```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">
    <PropertyGroup>
        <Title>Umbraco AI Agent Chat</Title>
        <Description>Central chat workspace for Umbraco AI Agent</Description>
        <StaticWebAssetBasePath>App_Plugins/UmbracoAIAgentChat</StaticWebAssetBasePath>
        <AddRazorSupportForMvc>false</AddRazorSupportForMvc>
    </PropertyGroup>

    <ItemGroup Condition="'$(UseProjectReferences)' == 'true'">
        <ProjectReference Include="..\..\..\Umbraco.AI.Agent.UI\src\Umbraco.AI.Agent.UI\Umbraco.AI.Agent.UI.csproj" />
    </ItemGroup>

    <ItemGroup Condition="'$(UseProjectReferences)' != 'true'">
        <PackageReference Include="Umbraco.AI.Agent.UI" />
    </ItemGroup>
</Project>
```

### Step 2: Frontend -- Chat Context

The chat context implements `UaiChatContextApi` and is simpler than the copilot context:

- No panel state (the chat workspace is always visible)
- No entity context (no `UAI_ENTITY_CONTEXT` provided)
- No frontend tools initially (no `UaiFrontendToolManager`)
- Provides `UAI_CHAT_CONTEXT` for shared `<uai-chat>` to consume

**`chat/chat.context.ts`**:

```typescript
import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";
import {
    UaiRunController,
    UaiToolRendererManager,
    UaiHitlContext,
    UAI_CHAT_CONTEXT,
    UaiHitlInterruptHandler,
    UaiDefaultInterruptHandler,
    type UaiChatContextApi,
    type UaiAgentItem,
} from "@umbraco-ai/agent-ui";
import type { Observable } from "rxjs";
import type {
    UaiChatMessage,
    UaiAgentState,
    UaiInterruptInfo,
} from "@umbraco-ai/agent-ui";
import type { PendingApproval } from "@umbraco-ai/agent-ui";
import { UaiChatAgentRepository } from "./repository/chat-agent.repository.js";

export const UAI_CHAT_WORKSPACE_CONTEXT = new UmbContextToken<UaiChatContext>(
    "UaiChatContext",
);

export class UaiChatContext extends UmbControllerBase implements UaiChatContextApi {
    #runController: UaiRunController;
    #hitlContext: UaiHitlContext;
    #toolRendererManager: UaiToolRendererManager;
    #agentRepository: UaiChatAgentRepository;

    // Required by UmbContextMinimal
    override getHostElement(): Element {
        return this._host.getHostElement();
    }

    // UaiChatContextApi implementation -- delegate to run controller + services
    get messages$(): Observable<UaiChatMessage[]> { return this.#runController.messages$; }
    get streamingContent$(): Observable<string> { return this.#runController.streamingContent$; }
    get agentState$(): Observable<UaiAgentState | undefined> { return this.#runController.agentState$; }
    get isRunning$(): Observable<boolean> { return this.#runController.isRunning$; }
    get hitlInterrupt$(): Observable<UaiInterruptInfo | undefined> { return this.#hitlContext.interrupt$; }
    get pendingApproval$(): Observable<PendingApproval | undefined> { return this.#hitlContext.pendingApproval$; }
    get agents(): Observable<UaiAgentItem[]> { return this.#agentRepository.agentItems$; }
    get selectedAgent(): Observable<UaiAgentItem | undefined> { return this.#runController.selectedAgent$; }
    get toolRendererManager(): UaiToolRendererManager { return this.#toolRendererManager; }

    constructor(host: UmbControllerHost) {
        super(host);

        this.#toolRendererManager = new UaiToolRendererManager(host);
        this.#hitlContext = new UaiHitlContext(host);
        this.#agentRepository = new UaiChatAgentRepository(host);

        this.#runController = new UaiRunController(host, this.#hitlContext, {
            toolRendererManager: this.#toolRendererManager,
            // No frontendToolManager -- chat starts with server-side tools only.
            // Adding frontend tools later requires only:
            //   frontendToolManager: new UaiFrontendToolManager(host),
            interruptHandlers: [
                new UaiHitlInterruptHandler(this),
                new UaiDefaultInterruptHandler(),
            ],
        });

        // Provide the shared chat context so <uai-chat> can consume it
        this.provideContext(UAI_CHAT_CONTEXT, this);
    }

    async sendUserMessage(content: string): Promise<void> {
        // No entity context serialization -- just send the message
        this.#runController.sendUserMessage(content, []);
    }

    abortRun(): void {
        this.#runController.abortRun();
    }

    regenerateLastMessage(): void {
        this.#runController.regenerateLastMessage();
    }

    selectAgent(agentId: string | undefined): void {
        this.#runController.selectAgent(agentId);
    }

    respondToHitl(response: string): void {
        this.#hitlContext.respond(response);
    }

    async initialize(): Promise<void> {
        await this.#agentRepository.initialize();
    }
}
```

### Step 3: Frontend -- Chat Agent Repository

Filters agents by scope, mirroring the copilot pattern.

**`chat/repository/chat-agent.repository.ts`**:

```typescript
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import type { Observable } from "rxjs";
import { map } from "rxjs";
import { UaiAgentRepository } from "@umbraco-ai/agent";
import type { UaiChatAgentItem } from "../types.js";

export class UaiChatAgentRepository {
    #agentRepository: UaiAgentRepository;
    #chatAgents$: Observable<UaiChatAgentItem[]>;

    constructor(host: UmbControllerHost) {
        this.#agentRepository = new UaiAgentRepository(host);

        this.#chatAgents$ = this.#agentRepository.agentItems$.pipe(
            map((items) =>
                Array.from(items.values())
                    .filter((agent) => agent.scopeIds.includes("chat"))
                    .map((agent) => ({
                        id: agent.unique,
                        name: agent.name,
                        alias: agent.alias,
                    })),
            ),
        );
    }

    get agentItems$(): Observable<UaiChatAgentItem[]> {
        return this.#chatAgents$;
    }

    async initialize(): Promise<void> {
        await this.#agentRepository.initialize();
    }
}
```

**`chat/types.ts`**:

```typescript
export type { UaiAgentItem as UaiChatAgentItem } from "@umbraco-ai/agent-ui";
```

### Step 4: Frontend -- Chat Section Element

The chat workspace renders as a full section with the shared `<uai-chat>` component.

**`chat/components/chat-section.element.ts`**:

```typescript
import { customElement, css, html } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UaiChatContext } from "../chat.context.js";

@customElement("uai-chat-section")
export class UaiChatSectionElement extends UmbLitElement {
    #chatContext: UaiChatContext;

    constructor() {
        super();
        this.#chatContext = new UaiChatContext(this);
        this.#chatContext.initialize();
    }

    override render() {
        return html`
            <umb-body-layout>
                <div id="chat-container">
                    <uai-chat></uai-chat>
                </div>
            </umb-body-layout>
        `;
    }

    static override styles = css`
        :host {
            display: flex;
            flex-direction: column;
            height: 100%;
        }

        #chat-container {
            display: flex;
            flex-direction: column;
            flex: 1;
            max-width: 900px;
            margin: 0 auto;
            width: 100%;
            padding: var(--uui-size-layout-1);
        }

        uai-chat {
            flex: 1;
            display: flex;
            flex-direction: column;
        }
    `;
}

declare global {
    interface HTMLElementTagNameMap {
        "uai-chat-section": UaiChatSectionElement;
    }
}
```

### Step 5: Frontend -- Manifest Registration

The chat registers as a section in the Umbraco backoffice.

**`chat/components/manifests.ts`**:

```typescript
import type { ManifestSection, ManifestSectionView } from "@umbraco-cms/backoffice/extension-registry";

const chatSection: ManifestSection = {
    type: "section",
    alias: "UmbracoAIAgent.Section.Chat",
    name: "AI Chat",
    meta: {
        label: "#uaiChat_sectionName",
        pathname: "ai-chat",
    },
    conditions: [
        {
            alias: "Umb.Condition.SectionUserPermission",
            match: "UmbracoAIAgent.Section.Chat",
        },
    ],
};

const chatSectionView: ManifestSectionView = {
    type: "sectionView",
    alias: "UmbracoAIAgent.SectionView.Chat",
    name: "AI Chat Section View",
    element: () => import("./chat-section.element.js"),
    meta: {
        label: "#uaiChat_sectionName",
        pathname: "",
        icon: "icon-chat",
    },
    conditions: [
        {
            alias: "Umb.Condition.SectionAlias",
            match: "UmbracoAIAgent.Section.Chat",
        },
    ],
};

export const manifests = [chatSection, chatSectionView];
```

**`manifests.ts`** (root):

```typescript
import type { UmbExtensionManifestKind } from "@umbraco-cms/backoffice/extension-registry";
import { manifests as componentManifests } from "./chat/components/manifests.js";
import { manifests as langManifests } from "./lang/manifests.js";

export const manifests: Array<UmbExtensionManifest | UmbExtensionManifestKind> = [
    ...componentManifests,
    ...langManifests,
];
```

**`app.ts`**:

```typescript
import { umbExtensionsRegistry } from "@umbraco-cms/backoffice/extension-registry";
import { manifests } from "./manifests.js";

umbExtensionsRegistry.registerMany(manifests);
```

### Step 6: Frontend -- Localization

**`lang/en.ts`**:

```typescript
import type { UmbLocalizationDictionary } from "@umbraco-cms/backoffice/localization-api";

export default {
    uaiChat: {
        sectionName: "AI Chat",
    },
    uaiAgentScope: {
        chatLabel: "Chat",
        chatDescription: "Enable chat workspace for this agent.",
    },
} as UmbLocalizationDictionary;
```

**`lang/manifests.ts`**:

```typescript
import type { UmbExtensionManifestKind } from "@umbraco-cms/backoffice/extension-registry";

export const manifests: Array<UmbExtensionManifest | UmbExtensionManifestKind> = [
    {
        type: "localization",
        alias: "UAIAgent.Chat.Localization.En",
        weight: -100,
        name: "English",
        meta: {
            culture: "en",
        },
        js: () => import("./en.js"),
    },
];
```

### Step 7: Frontend -- Package Manifest

**`public/umbraco-package.json`**:

```json
{
    "$schema": "https://raw.githubusercontent.com/umbraco/Umbraco.CMS.Backoffice/main/umbraco-package-schema.json",
    "id": "Umbraco.AI.Agent.Chat",
    "name": "Umbraco AI Agent Chat",
    "version": "1.0.0",
    "extensions": [
        {
            "type": "bundle",
            "alias": "Umbraco.AI.Agent.Chat.Bundle",
            "js": "/App_Plugins/UmbracoAIAgentChat/umbraco-ai-agent-chat-manifests.js"
        },
        {
            "type": "backofficeEntryPoint",
            "alias": "Umbraco.AI.Agent.Chat.BackofficeEntryPoint",
            "js": "/App_Plugins/UmbracoAIAgentChat/umbraco-ai-agent-chat-app.js"
        }
    ],
    "importmap": {
        "imports": {
            "@umbraco-ai/chat": "/App_Plugins/UmbracoAIAgentChat/umbraco-ai-agent-chat-app.js"
        }
    }
}
```

### Step 8: Build Configuration

**`vite.config.ts`**:

```typescript
import { defineConfig } from "vite";
import { resolve } from "path";

export default defineConfig({
    build: {
        lib: {
            entry: {
                "umbraco-ai-agent-chat-manifests": resolve(__dirname, "src/manifests.ts"),
                "umbraco-ai-agent-chat-app": resolve(__dirname, "src/app.ts"),
            },
            formats: ["es"],
        },
        outDir: "../wwwroot",
        emptyOutDir: true,
        sourcemap: true,
        rollupOptions: {
            external: [/^@umbraco/, /^@umbraco-ai/],
            output: {
                entryFileNames: "[name].js",
                chunkFileNames: "[name].js",
            },
        },
    },
});
```

**`tsconfig.json`**:

```json
{
    "compilerOptions": {
        "target": "ESNext",
        "module": "ESNext",
        "moduleResolution": "bundler",
        "declaration": true,
        "declarationMap": true,
        "declarationDir": "./types",
        "emitDeclarationOnly": true,
        "outDir": "./types",
        "rootDir": "./src",
        "strict": true,
        "esModuleInterop": true,
        "experimentalDecorators": true,
        "useDefineForClassFields": false,
        "skipLibCheck": true
    },
    "include": ["src/**/*.ts"],
    "exclude": ["node_modules", "dist", "wwwroot"]
}
```

### Step 9: Project Scaffolding Files

**`version.json`**:

```json
{
    "$schema": "https://raw.githubusercontent.com/dotnet/Nerdbank.GitVersioning/master/src/NerdBank.GitVersioning/version.schema.json",
    "version": "1.0.0-alpha1",
    "assemblyVersion": {
        "precision": "build"
    },
    "gitCommitIdShortFixedLength": 7,
    "nugetPackageVersion": {
        "semVer": 2
    },
    "publicReleaseRefSpec": ["^refs/heads/main$", "^refs/heads/hotfix/", "^refs/heads/release/"]
}
```

**`changelog.config.json`**:

```json
{
    "scopes": ["chat"]
}
```

**`Directory.Build.props`**:

```xml
<Project>
    <PropertyGroup>
        <UseProjectReferences Condition="'$(UseProjectReferences)' == ''">true</UseProjectReferences>
    </PropertyGroup>
</Project>
```

**`Umbraco.AI.Agent.Chat.slnx`**:

```xml
<Solution>
  <Project Path="src/Umbraco.AI.Agent.Chat/Umbraco.AI.Agent.Chat.csproj" />
</Solution>
```

### Step 10: Root Configuration Updates

**Root `package.json`** -- add workspace and build scripts:

```diff
  "workspaces": [
      ...existing...
+     "Umbraco.AI.Agent.Chat/src/Umbraco.AI.Agent.Chat/Client"
  ],
  "scripts": {
-     "build": "... && npm run build:copilot",
+     "build": "... && npm run build:copilot && npm run build:chat",
+     "build:chat": "npm run build -w @umbraco-ai/chat",
+     "watch:chat": "npm run watch -w @umbraco-ai/chat",
      ...
  }
```

**Root `Directory.Packages.props`** -- add chat package version:

```diff
+ <PackageVersion Include="Umbraco.AI.Agent.Chat" Version="[1.0.0-alpha1, 1.999.999)" />
```

**`Umbraco.AI.local.sln`** -- add project:

```bash
dotnet sln Umbraco.AI.local.sln add Umbraco.AI.Agent.Chat/src/Umbraco.AI.Agent.Chat/Umbraco.AI.Agent.Chat.csproj
```

### Step 11: Azure Pipeline Updates

The pipeline needs to accommodate Agent.UI at Level 2 and push Copilot + Chat to Level 3.

**`azure-pipelines.yml`** parameter changes:

```yaml
parameters:
    - name: level2Products
      type: object
      default:
          - name: Umbraco.AI.Agent.UI
            changeVar: AgentuiChanged
            hasNpm: true
    - name: level3Products               # NEW
      type: object
      default:
          - name: Umbraco.AI.Agent.Copilot
            changeVar: AgentcopilotChanged
            hasNpm: true
          - name: Umbraco.AI.Agent.Chat
            changeVar: AgentchatChanged
            hasNpm: true
```

Add `PackLevel3` job (depends on PackLevel2), update `CollectPackages`/`CollectSBOMs` to depend on Level 3, and add `GenerateSBOM_Level3` job. See the original plan's "Azure Pipeline Configuration" section for full YAML.

**Note**: The `changeVar` casing must match what `detect-changes.ps1` produces. The script's `Get-VariableName` function collapses dots in PascalCase:
- `"agent.copilot"` → `"AgentcopilotChanged"` (not `AgentCopilotChanged`)
- `"agent.chat"` → `"AgentchatChanged"`
- `"agent.ui"` → `"AgentuiChanged"`

The existing `AgentCopilotChanged` in the pipeline may need to be corrected to `AgentcopilotChanged` to match the detection script. Verify before applying.

---

## Build Order

```
core → prompt → agent → agent-ui → copilot → chat
```

Sequential in npm. In CI, copilot and chat can build in parallel at Level 3 since they don't depend on each other.

---

## What's NOT in Scope

- **Frontend tools in chat** -- future concern, infrastructure supports it
- **Entity context in chat** -- future concern (side-drawer editor)
- **Copilot npm rename** (`agent-copilot` → `copilot`) -- separate task
- **Conversation persistence** -- future concern (backend changes needed)
- **User group permissions for chat section** -- standard Umbraco section permissions apply, no custom work needed
- **Pipeline changes for Agent.UI** -- Level 2 restructuring (Agent.UI) is part of Phase 1 CI follow-up, not Phase 2

---

## Risks

### Section vs Workspace

Registering as a `type: "section"` creates a top-level navigation entry. This requires section permissions to be granted to user groups. If the section should instead be a workspace under Settings (like the AI configuration pages), the manifest type changes to a workspace + sectionSidebarApp pattern. The plan assumes a dedicated section is the right UX choice since chat is a primary feature, not a settings page.

### Manifest Type Availability

The `ManifestSection` and `ManifestSectionView` types need to be verified against the actual `@umbraco-cms/backoffice` exports. If these specific types aren't available or named differently, the manifests may need adjustment. The pattern is based on Umbraco CMS v17 conventions.

### Agent Scope Assignment

For the chat to show any agents, agents must have `"chat"` in their `ScopeIds`. The demo site's seed data and the agent workspace editor need to support multi-scope assignment (copilot + chat). Verify that the agent editor already supports selecting multiple scopes.
