# Umbraco.Ai Connections UI Implementation Plan

## Overview

Implement the Connections management UI for Umbraco.Ai in the Settings section, following Commerce organizational patterns with CommandStore for change tracking and CMS repository patterns for API integration.

**Scope:** Base infrastructure + Connections UI only (Profiles deferred)
**Prefix Convention:** `uai:` for entity types, `UmbracoAi.` for aliases
**Menu Location:** New "AI" menu group in Settings sidebar
**API Client:** Already generated and ready to use

---

## Directory Structure

```
src/Umbraco.Ai.Web.StaticAssets/Client/src/
├── api/                              # Generated from OpenAPI (existing)
├── core/                             # Shared infrastructure (NEW)
│   ├── command/
│   │   ├── command.base.ts
│   │   ├── command.store.ts
│   │   ├── implement/
│   │   │   ├── partial-update.command.ts   # Generic partial update
│   │   │   └── index.ts
│   │   └── index.ts
│   └── index.ts
├── section/                          # AI section in Settings (NEW)
│   ├── menu/
│   │   └── manifests.ts
│   ├── sidebar/
│   │   └── manifests.ts
│   └── manifests.ts
├── connection/                       # Connection feature (singular) (NEW)
│   ├── collection/
│   │   ├── action/
│   │   │   └── manifests.ts
│   │   ├── views/
│   │   │   └── table/
│   │   │       └── connection-table-collection-view.element.ts
│   │   └── manifests.ts
│   ├── entity-action/
│   │   └── manifests.ts
│   ├── menu/
│   │   └── manifests.ts
│   ├── repository/
│   │   ├── detail/
│   │   │   ├── connection-detail.server.data-source.ts
│   │   │   ├── connection-detail.store.ts
│   │   │   ├── connection-detail.repository.ts
│   │   │   └── index.ts
│   │   ├── collection/
│   │   │   ├── connection-collection.repository.ts
│   │   │   └── index.ts
│   │   └── manifests.ts
│   ├── workspace/
│   │   ├── connection-root/
│   │   │   └── manifests.ts
│   │   ├── connection/
│   │   │   ├── connection-workspace.context.ts
│   │   │   ├── connection-workspace-editor.element.ts  # Header/footer (name+alias)
│   │   │   ├── views/
│   │   │   │   └── connection-details-workspace-view.element.ts  # Body content
│   │   │   └── manifests.ts
│   │   └── manifests.ts
│   ├── constants.ts
│   ├── types.ts
│   ├── type-mapper.ts
│   └── manifests.ts
├── entrypoints/                      # Existing
└── bundle.manifests.ts               # Update to include new manifests
```

---

## Phase 1: Core Infrastructure

### 1.1 Command Base Classes

**File:** `src/core/command/command.base.ts`
```typescript
export interface UaiCommand {
    correlationId?: string;
    execute(receiver: unknown): void;
}

export abstract class UaiCommandBase<TReceiver> implements UaiCommand {
    correlationId?: string;

    constructor(correlationId?: string) {
        this.correlationId = correlationId;
    }

    abstract execute(receiver: TReceiver): void;
}
```

**File:** `src/core/command/command.store.ts`
```typescript
import type { UaiCommand } from "./command.base.js";

export class UaiCommandStore {
    #muted = false;
    #commands: UaiCommand[] = [];

    add(command: UaiCommand) {
        if (this.#muted) return;
        // Replace command with same correlationId or append
        this.#commands = [
            ...this.#commands.filter(x => !command.correlationId || x.correlationId !== command.correlationId),
            command,
        ];
    }

    getAll(): UaiCommand[] {
        return this.#muted ? [] : [...this.#commands];
    }

    mute() { this.#muted = true; }
    unmute() { this.#muted = false; }
    clear() { this.#commands = []; }
    reset() { this.clear(); this.unmute(); }
}
```

### 1.2 Partial Update Command (Generic)

**File:** `src/core/command/implement/partial-update.command.ts`
```typescript
import { UaiCommandBase } from "../command.base.js";

export class UaiPartialUpdateCommand<TReceiver> extends UaiCommandBase<TReceiver> {
    #partial: Partial<TReceiver>;

    constructor(partial: Partial<TReceiver>, correlationId?: string) {
        super(correlationId);
        this.#partial = partial;
    }

    execute(receiver: TReceiver) {
        Object.keys(this.#partial)
            .filter(key => this.#partial[key as keyof TReceiver] !== undefined)
            .forEach(key => {
                receiver[key as keyof TReceiver] = this.#partial[key as keyof TReceiver]!;
            });
    }
}
```

### 1.3 Index Exports

**File:** `src/core/command/implement/index.ts`
```typescript
export * from "./partial-update.command.js";
```

**File:** `src/core/command/index.ts`
```typescript
export * from "./command.base.js";
export * from "./command.store.js";
export * from "./implement/index.js";
```

**File:** `src/core/index.ts`
```typescript
export * from "./command/index.js";
```

> **Note:** We use the built-in `uui-input-lock` from `@umbraco-ui/uui` instead of creating a custom component.
> It supports `locked`, `lock-change` event, and `auto-width` properties.

---

## Phase 2: Connection Feature

### 2.1 Constants

**File:** `src/connection/constants.ts`
```typescript
export const UaiConnectionConstants = {
    EntityType: {
        Root: "uai:connection-root",
        Entity: "uai:connection",
    },
    Icon: {
        Root: "icon-plug",
        Entity: "icon-plug",
    },
    Workspace: {
        Root: "UmbracoAi.Workspace.ConnectionRoot",
        Entity: "UmbracoAi.Workspace.Connection",
    },
    Store: {
        Detail: "UmbracoAi.Store.Connection.Detail",
    },
    Repository: {
        Detail: "UmbracoAi.Repository.Connection.Detail",
        Collection: "UmbracoAi.Repository.Connection.Collection",
    },
    Collection: "UmbracoAi.Collection.Connection",
};
```

### 2.2 Types

**File:** `src/connection/types.ts`
```typescript
// View model for workspace editing
export interface UaiConnectionDetailModel {
    unique: string;
    entityType: string;
    alias: string;
    name: string;
    providerId: string;
    settings: Record<string, unknown> | null;
    isActive: boolean;
}

// Collection item model
export interface UaiConnectionItemModel {
    unique: string;
    entityType: string;
    name: string;
    providerId: string;
    isActive: boolean;
}
```

### 2.3 Type Mapper

**File:** `src/connection/type-mapper.ts`
```typescript
import type { ConnectionResponseModel } from "../api/types.gen.js";
import { UaiConnectionConstants } from "./constants.js";
import type { UaiConnectionDetailModel, UaiConnectionItemModel } from "./types.js";

export const UaiConnectionTypeMapper = {
    toDetailModel(response: ConnectionResponseModel): UaiConnectionDetailModel {
        return {
            unique: response.id,
            entityType: UaiConnectionConstants.EntityType.Entity,
            alias: response.alias,
            name: response.name,
            providerId: response.providerId,
            settings: response.settings ?? null,
            isActive: response.isActive,
        };
    },

    toItemModel(response: ConnectionResponseModel): UaiConnectionItemModel {
        return {
            unique: response.id,
            entityType: UaiConnectionConstants.EntityType.Entity,
            name: response.name,
            providerId: response.providerId,
            isActive: response.isActive,
        };
    },

    toCreateRequest(model: UaiConnectionDetailModel) {
        return {
            alias: model.alias,
            name: model.name,
            providerId: model.providerId,
            settings: model.settings,
            isActive: model.isActive,
        };
    },

    toUpdateRequest(model: UaiConnectionDetailModel) {
        return {
            name: model.name,
            settings: model.settings,
            isActive: model.isActive,
        };
    },
};
```

### 2.4 Server Data Source (implements UmbDetailDataSource)

**File:** `src/connection/repository/detail/connection-detail.server.data-source.ts`
```typescript
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import type { UmbDetailDataSource } from "@umbraco-cms/backoffice/repository";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { UmbId } from "@umbraco-cms/backoffice/id";
import { ConnectionService } from "../../../api/sdk.gen.js";
import { UaiConnectionConstants } from "../../constants.js";
import { UaiConnectionTypeMapper } from "../../type-mapper.js";
import type { UaiConnectionDetailModel } from "../../types.js";

export class UaiConnectionDetailServerDataSource implements UmbDetailDataSource<UaiConnectionDetailModel> {
    #host: UmbControllerHost;

    constructor(host: UmbControllerHost) {
        this.#host = host;
    }

    async createScaffold(preset: Partial<UaiConnectionDetailModel> = {}) {
        const data: UaiConnectionDetailModel = {
            entityType: UaiConnectionConstants.EntityType.Entity,
            unique: UmbId.new(),
            alias: "",
            name: "",
            providerId: "",
            settings: null,
            isActive: true,
            ...preset,
        };
        return { data };
    }

    async read(unique: string) {
        if (!unique) throw new Error("Unique is missing");

        const { data, error } = await tryExecute(
            this.#host,
            ConnectionService.getConnectionById({ path: { id: unique } })
        );

        if (error || !data) {
            return { error };
        }

        return { data: UaiConnectionTypeMapper.toDetailModel(data) };
    }

    async create(model: UaiConnectionDetailModel, _parentUnique: string | null) {
        if (!model) throw new Error("Model is missing");

        const { data, error } = await tryExecute(
            this.#host,
            ConnectionService.createConnection({
                body: UaiConnectionTypeMapper.toCreateRequest(model),
            })
        );

        if (data) {
            return this.read(data.id);
        }

        return { error };
    }

    async update(model: UaiConnectionDetailModel) {
        if (!model.unique) throw new Error("Unique is missing");

        const { error } = await tryExecute(
            this.#host,
            ConnectionService.updateConnection({
                path: { id: model.unique },
                body: UaiConnectionTypeMapper.toUpdateRequest(model),
            })
        );

        if (!error) {
            return this.read(model.unique);
        }

        return { error };
    }

    async delete(unique: string) {
        if (!unique) throw new Error("Unique is missing");

        return tryExecute(
            this.#host,
            ConnectionService.deleteConnection({ path: { id: unique } })
        );
    }
}
```

### 2.5 Detail Store (extends UmbDetailStoreBase)

**File:** `src/connection/repository/detail/connection-detail.store.ts`
```typescript
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbDetailStoreBase } from "@umbraco-cms/backoffice/store";
import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";
import { UaiConnectionConstants } from "../../constants.js";
import type { UaiConnectionDetailModel } from "../../types.js";

export class UaiConnectionDetailStore extends UmbDetailStoreBase<UaiConnectionDetailModel> {
    constructor(host: UmbControllerHost) {
        super(host, UaiConnectionConstants.Store.Detail);
    }
}

export const UAI_CONNECTION_DETAIL_STORE_CONTEXT = new UmbContextToken<UaiConnectionDetailStore>(
    UaiConnectionConstants.Store.Detail
);

export { UaiConnectionDetailStore as api };
```

### 2.6 Detail Repository (extends UmbDetailRepositoryBase)

**File:** `src/connection/repository/detail/connection-detail.repository.ts`
```typescript
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbDetailRepositoryBase } from "@umbraco-cms/backoffice/repository";
import { UaiConnectionDetailServerDataSource } from "./connection-detail.server.data-source.js";
import { UAI_CONNECTION_DETAIL_STORE_CONTEXT } from "./connection-detail.store.js";
import type { UaiConnectionDetailModel } from "../../types.js";

export class UaiConnectionDetailRepository extends UmbDetailRepositoryBase<UaiConnectionDetailModel> {
    constructor(host: UmbControllerHost) {
        super(host, UaiConnectionDetailServerDataSource, UAI_CONNECTION_DETAIL_STORE_CONTEXT);
    }

    override async create(model: UaiConnectionDetailModel) {
        return super.create(model, null);
    }
}

export { UaiConnectionDetailRepository as api };
```

### 2.7 Collection Repository (for table view)

**File:** `src/connection/repository/collection/connection-collection.repository.ts`
```typescript
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbRepositoryBase } from "@umbraco-cms/backoffice/repository";
import type { UmbCollectionRepository } from "@umbraco-cms/backoffice/collection";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { ConnectionService } from "../../../api/sdk.gen.js";
import { UaiConnectionTypeMapper } from "../../type-mapper.js";

export class UaiConnectionCollectionRepository extends UmbRepositoryBase implements UmbCollectionRepository {
    constructor(host: UmbControllerHost) {
        super(host);
    }

    async requestCollection() {
        const { data, error } = await tryExecute(
            this,
            ConnectionService.getAllConnections({ query: { skip: 0, take: 1000 } })
        );

        if (data) {
            const items = data.items.map(UaiConnectionTypeMapper.toItemModel);
            return { data: { items, total: data.total } };
        }

        return { error };
    }
}

export { UaiConnectionCollectionRepository as api };
```

### 2.8 Index Exports

**File:** `src/connection/repository/detail/index.ts`
```typescript
export * from "./connection-detail.server.data-source.js";
export * from "./connection-detail.store.js";
export * from "./connection-detail.repository.js";
```

**File:** `src/connection/repository/collection/index.ts`
```typescript
export * from "./connection-collection.repository.js";
```

### 2.9 Repository Manifests

**File:** `src/connection/repository/manifests.ts`
```typescript
import type { ManifestRepository, ManifestStore } from "@umbraco-cms/backoffice/extension-registry";
import { UaiConnectionConstants } from "../constants.js";

export const manifests: Array<ManifestRepository | ManifestStore> = [
    {
        type: "store",
        alias: UaiConnectionConstants.Store.Detail,
        name: "Connection Detail Store",
        api: () => import("./detail/connection-detail.store.js"),
    },
    {
        type: "repository",
        alias: UaiConnectionConstants.Repository.Detail,
        name: "Connection Detail Repository",
        api: () => import("./detail/connection-detail.repository.js"),
    },
    {
        type: "repository",
        alias: UaiConnectionConstants.Repository.Collection,
        name: "Connection Collection Repository",
        api: () => import("./collection/connection-collection.repository.js"),
    },
];
```

---

## Phase 3: Workspace Implementation

### 3.1 Workspace Context (with CommandStore and mute/unmute)

Uses `UmbDetailRepositoryBase` for CRUD operations plus Commerce-style CommandStore for optimistic UI.

**File:** `src/connection/workspace/connection/connection-workspace.context.ts`
```typescript
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import type { UmbRoutableWorkspaceContext, UmbSubmittableWorkspaceContext } from "@umbraco-cms/backoffice/workspace";
import { UmbSubmittableWorkspaceContextBase, UmbWorkspaceRouteManager } from "@umbraco-cms/backoffice/workspace";
import { UmbObjectState, UmbBasicState } from "@umbraco-cms/backoffice/observable-api";
import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";
import { UaiConnectionConstants } from "../../constants.js";
import { UaiConnectionDetailRepository } from "../../repository/detail/connection-detail.repository.js";
import type { UaiConnectionDetailModel } from "../../types.js";
import { UaiCommandStore } from "../../../core/command/command.store.js";
import type { UaiCommandBase } from "../../../core/command/command.base.js";
import UaiConnectionWorkspaceEditorElement from "./connection-workspace-editor.element.js";

export const UAI_CONNECTION_WORKSPACE_CONTEXT = new UmbContextToken<
    UmbSubmittableWorkspaceContext,
    UaiConnectionWorkspaceContext
>(
    "UmbWorkspaceContext",
    undefined,
    (context): context is UaiConnectionWorkspaceContext =>
        context.getEntityType() === UaiConnectionConstants.EntityType.Entity
);

export class UaiConnectionWorkspaceContext
    extends UmbSubmittableWorkspaceContextBase<UaiConnectionDetailModel>
    implements UmbSubmittableWorkspaceContext, UmbRoutableWorkspaceContext
{
    public readonly routes = new UmbWorkspaceRouteManager(this);

    readonly #repository = new UaiConnectionDetailRepository(this);
    readonly #commandStore = new UaiCommandStore();

    #unique = new UmbBasicState<string | undefined>(undefined);
    readonly unique = this.#unique.asObservable();

    #model = new UmbObjectState<UaiConnectionDetailModel | undefined>(undefined);
    readonly model = this.#model.asObservable();

    constructor(host: UmbControllerHost) {
        super(host, UaiConnectionConstants.Workspace.Entity);

        this.routes.setRoutes([
            {
                path: "create",
                component: UaiConnectionWorkspaceEditorElement,
                setup: async () => {
                    await this.create();
                },
            },
            {
                path: ":unique",
                component: UaiConnectionWorkspaceEditorElement,
                setup: async (_component, info) => {
                    await this.load(info.match.params.unique);
                },
            },
        ]);
    }

    protected resetState(): void {
        super.resetState();
        this.#unique.setValue(undefined);
        this.#model.setValue(undefined);
        this.#commandStore.reset();
    }

    async create() {
        this.resetState();
        const { data } = await this.#repository.createScaffold();
        if (data) {
            this.#unique.setValue(data.unique);
            this.#model.setValue(data);
            this.setIsNew(true);
        }
    }

    async load(unique: string) {
        this.resetState();
        const { asObservable } = await this.#repository.requestByUnique(unique);
        if (asObservable) {
            this.observe(asObservable(), (model) => {
                if (model) {
                    this.#unique.setValue(model.unique);
                    // Clone and replay commands on fresh server data
                    const newModel = structuredClone(model);
                    this.#commandStore.getAll().forEach((cmd) => cmd.execute(newModel));
                    this.#model.setValue(newModel);
                    this.setIsNew(false);
                }
            });
        }
    }

    handleCommand(command: UaiCommandBase<UaiConnectionDetailModel>) {
        const currentValue = this.#model.getValue();
        if (currentValue) {
            const newValue = structuredClone(currentValue);
            command.execute(newValue);
            this.#model.setValue(newValue);
            this.#commandStore.add(command);
        }
    }

    getData(): UaiConnectionDetailModel | undefined {
        return this.#model.getValue();
    }

    getUnique(): string | undefined {
        return this.#unique.getValue();
    }

    getEntityType(): string {
        return UaiConnectionConstants.EntityType.Entity;
    }

    async submit() {
        if (!this.#model.value) return;

        // Mute command store during save to prevent interference
        this.#commandStore.mute();

        const { data, error } = this.getIsNew()
            ? await this.#repository.create(this.#model.value)
            : await this.#repository.save(this.#model.value);

        if (!error && data) {
            this.#unique.setValue(data.unique);
            this.#commandStore.reset();  // Reset clears and unmutes
            this.setIsNew(false);
        } else {
            this.#commandStore.unmute();  // Unmute on error to allow further edits
        }
    }
}

export { UaiConnectionWorkspaceContext as api };
```

### 3.2 Workspace Editor Element (Header/Footer Only)

The editor element handles ONLY the header (name + alias with lock) and footer (breadcrumbs).
The body content is handled by workspace views (see 3.4).

**File:** `src/connection/workspace/connection/connection-workspace-editor.element.ts`
```typescript
import { css, html, customElement, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import type { UUIInputElement, UUIInputEvent, UUIInputLockElement } from "@umbraco-cms/backoffice/external/uui";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import type { UaiConnectionDetailModel } from "../../types.js";
import { UaiPartialUpdateCommand } from "../../../core/command/implement/partial-update.command.js";
import { UaiConnectionConstants } from "../../constants.js";
import { UAI_CONNECTION_WORKSPACE_CONTEXT } from "./connection-workspace.context.js";

@customElement("uai-connection-workspace-editor")
export class UaiConnectionWorkspaceEditorElement extends UmbLitElement {
    #workspaceContext?: typeof UAI_CONNECTION_WORKSPACE_CONTEXT.TYPE;

    @state()
    _model?: UaiConnectionDetailModel;

    @state()
    _isNew?: boolean;

    @state()
    _aliasLocked = true;

    constructor() {
        super();
        this.consumeContext(UAI_CONNECTION_WORKSPACE_CONTEXT, (context) => {
            this.#workspaceContext = context;
            this.#observeWorkspace();
        });
    }

    #observeWorkspace() {
        if (!this.#workspaceContext) return;
        this.observe(this.#workspaceContext.model, (model) => this._model = model);
        this.observe(this.#workspaceContext.isNew, (isNew) => {
            this._isNew = isNew;
            if (isNew) {
                // Focus name input for new entities
                (this.shadowRoot?.querySelector("#name") as HTMLElement)?.focus();
            }
        });
    }

    #onNameChange(event: UUIInputEvent) {
        event.stopPropagation();
        const target = event.composedPath()[0] as UUIInputElement;
        const name = target.value.toString();

        // If alias is locked, generate alias from name
        if (this._aliasLocked && this._isNew) {
            const alias = this.#generateAlias(name);
            this.#workspaceContext?.handleCommand(new UaiPartialUpdateCommand({ name, alias } as Partial<UaiConnectionDetailModel>));
        } else {
            this.#workspaceContext?.handleCommand(new UaiPartialUpdateCommand({ name } as Partial<UaiConnectionDetailModel>));
        }
    }

    #onAliasChange(event: UUIInputEvent) {
        event.stopPropagation();
        const target = event.composedPath()[0] as UUIInputElement;
        this.#workspaceContext?.handleCommand(new UaiPartialUpdateCommand({ alias: target.value.toString() } as Partial<UaiConnectionDetailModel>));
    }

    #onToggleAliasLock() {
        this._aliasLocked = !this._aliasLocked;
    }

    #generateAlias(name: string): string {
        return name
            .toLowerCase()
            .replace(/[^a-z0-9]+/g, "-")
            .replace(/^-|-$/g, "");
    }

    render() {
        if (!this._model) return;
        return html`
            <umb-workspace-editor alias=${UaiConnectionConstants.Workspace.Entity}>
                <div id="header" slot="header">
                    <uui-button
                        href="section/settings/workspace/${UaiConnectionConstants.Workspace.Root}/collection"
                        label=${this.localize.term("general_backToOverview")}
                        compact>
                        <uui-icon name="icon-arrow-left"></uui-icon>
                    </uui-button>
                    <uui-input
                        id="name"
                        .value=${this._model?.name ?? ""}
                        @input="${this.#onNameChange}"
                        label="name"
                        placeholder=${this.localize.term("placeholders_entername")}>
                        <uui-input-lock
                            id="alias"
                            name="alias"
                            slot="append"
                            label=${this.localize.term("placeholders_enterAlias")}
                            placeholder=${this.localize.term("placeholders_enterAlias")}
                            .value=${this._model?.alias ?? ""}
                            ?auto-width=${!!this._model?.name}
                            ?locked=${this._aliasLocked}
                            ?readonly=${this._aliasLocked || !this._isNew}
                            @input=${this.#onAliasChange}
                            @lock-change=${this.#onToggleAliasLock}>
                        </uui-input-lock>
                    </uui-input>
                </div>
                ${!this._isNew && this._model
                    ? html`<umb-workspace-entity-action-menu slot="action-menu"></umb-workspace-entity-action-menu>`
                    : ""}
                <div slot="footer-info" id="footer">
                    <a href="section/settings">Settings</a> /
                    <a href="section/settings/workspace/${UaiConnectionConstants.Workspace.Root}/collection">Connections</a> /
                    ${this._model?.name ?? this.localize.term("general_untitled")}
                </div>
            </umb-workspace-editor>
        `;
    }

    static styles = [
        UmbTextStyles,
        css`
            :host {
                display: block;
                width: 100%;
                height: 100%;
            }

            #header {
                display: flex;
                flex: 1 1 auto;
            }

            #footer {
                padding: 0 var(--uui-size-layout-1);
            }

            #name {
                width: 100%;
                flex: 1 1 auto;
                align-items: center;
            }
        `,
    ];
}

export default UaiConnectionWorkspaceEditorElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-connection-workspace-editor": UaiConnectionWorkspaceEditorElement;
    }
}
```

### 3.3 Workspace Details View (Body Content)

The workspace view handles the body content (provider picker, settings, active toggle).
This is registered as a workspace view via manifests.

**File:** `src/connection/workspace/connection/views/connection-details-workspace-view.element.ts`
```typescript
import { html, customElement, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import type { UaiConnectionDetailModel } from "../../../types.js";
import { UaiPartialUpdateCommand } from "../../../../core/command/implement/partial-update.command.js";
import { UAI_CONNECTION_WORKSPACE_CONTEXT } from "../connection-workspace.context.js";

@customElement("uai-connection-details-workspace-view")
export class UaiConnectionDetailsWorkspaceViewElement extends UmbLitElement {
    #workspaceContext?: typeof UAI_CONNECTION_WORKSPACE_CONTEXT.TYPE;

    @state()
    _model?: UaiConnectionDetailModel;

    @state()
    _isNew?: boolean;

    constructor() {
        super();
        this.consumeContext(UAI_CONNECTION_WORKSPACE_CONTEXT, (context) => {
            this.#workspaceContext = context;
            this.observe(context.model, (model) => this._model = model);
            this.observe(context.isNew, (isNew) => this._isNew = isNew);
        });
    }

    #onProviderChange(event: Event) {
        event.stopPropagation();
        const target = event.target as HTMLInputElement;
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand({ providerId: target.value } as Partial<UaiConnectionDetailModel>)
        );
    }

    #onActiveChange(event: Event) {
        event.stopPropagation();
        const target = event.target as HTMLInputElement;
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand({ isActive: target.checked } as Partial<UaiConnectionDetailModel>)
        );
    }

    render() {
        if (!this._model) return;
        return html`
            <uui-box headline="Connection Details">
                <umb-property-layout label="Provider" description="AI provider for this connection">
                    <uui-input
                        slot="editor"
                        .value=${this._model?.providerId ?? ""}
                        @change=${this.#onProviderChange}
                        placeholder="e.g., openai"
                        ?disabled=${!this._isNew}></uui-input>
                </umb-property-layout>

                <umb-property-layout label="Active" description="Enable or disable this connection">
                    <uui-toggle
                        slot="editor"
                        .checked=${this._model?.isActive ?? true}
                        @change=${this.#onActiveChange}></uui-toggle>
                </umb-property-layout>
            </uui-box>
        `;
    }

    static styles = [UmbTextStyles];
}

export default UaiConnectionDetailsWorkspaceViewElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-connection-details-workspace-view": UaiConnectionDetailsWorkspaceViewElement;
    }
}
```

### 3.4 Workspace Manifests

**File:** `src/connection/workspace/connection/manifests.ts`
```typescript
import type { ManifestWorkspace, ManifestWorkspaceAction, ManifestWorkspaceView } from "@umbraco-cms/backoffice/extension-registry";
import { UmbSubmitWorkspaceAction } from "@umbraco-cms/backoffice/workspace";
import { UaiConnectionConstants } from "../../constants.js";

export const manifests: Array<ManifestWorkspace | ManifestWorkspaceAction | ManifestWorkspaceView> = [
    {
        type: "workspace",
        kind: "routable",
        alias: UaiConnectionConstants.Workspace.Entity,
        name: "Connection Workspace",
        api: () => import("./connection-workspace.context.js"),
        meta: {
            entityType: UaiConnectionConstants.EntityType.Entity,
        },
    },
    {
        type: "workspaceView",
        alias: "UmbracoAi.WorkspaceView.Connection.Details",
        name: "Connection Details Workspace View",
        element: () => import("./views/connection-details-workspace-view.element.js"),
        weight: 100,
        meta: {
            label: "Details",
            pathname: "details",
            icon: "icon-settings",
        },
        conditions: [
            {
                alias: "Umb.Condition.WorkspaceAlias",
                match: UaiConnectionConstants.Workspace.Entity,
            },
        ],
    },
    {
        type: "workspaceAction",
        kind: "default",
        alias: "UmbracoAi.WorkspaceAction.Connection.Save",
        name: "Save Connection",
        api: UmbSubmitWorkspaceAction,
        meta: {
            label: "Save",
            look: "primary",
            color: "positive",
        },
        conditions: [
            {
                alias: "Umb.Condition.WorkspaceAlias",
                match: UaiConnectionConstants.Workspace.Entity,
            },
        ],
    },
];
```

---

## Phase 4: Collection View

### 4.1 Collection Table View

**File:** `src/connection/collection/views/table/connection-table-collection-view.element.ts`
```typescript
import { html, customElement, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import type { UmbTableColumn, UmbTableItem } from "@umbraco-cms/backoffice/components";
import { UMB_COLLECTION_CONTEXT } from "@umbraco-cms/backoffice/collection";
import type { UaiConnectionItemModel } from "../../../types.js";
import { UaiConnectionConstants } from "../../../constants.js";

@customElement("uai-connection-table-collection-view")
export class UaiConnectionTableCollectionViewElement extends UmbLitElement {
    @state() private _items: UmbTableItem[] = [];

    private _columns: UmbTableColumn[] = [
        { name: "Name", alias: "name" },
        { name: "Provider", alias: "provider" },
        { name: "Status", alias: "status" },
    ];

    constructor() {
        super();
        this.consumeContext(UMB_COLLECTION_CONTEXT, (ctx) => {
            this.observe(ctx.items, (items) => this._createTableItems(items as UaiConnectionItemModel[]));
        });
    }

    _createTableItems(items: UaiConnectionItemModel[]) {
        this._items = items.map((item) => ({
            id: item.unique,
            icon: UaiConnectionConstants.Icon.Entity,
            data: [
                {
                    columnAlias: "name",
                    value: html`<a href="/section/settings/workspace/${UaiConnectionConstants.Workspace.Entity}/edit/${item.unique}">${item.name}</a>`,
                },
                { columnAlias: "provider", value: item.providerId },
                {
                    columnAlias: "status",
                    value: html`<uui-tag color=${item.isActive ? "positive" : "danger"}>
                        ${item.isActive ? "Active" : "Inactive"}
                    </uui-tag>`,
                },
            ],
        }));
    }

    render() {
        return html`<umb-table .columns=${this._columns} .items=${this._items}></umb-table>`;
    }
}

export default UaiConnectionTableCollectionViewElement;
```

### 4.2 Collection Manifests

**File:** `src/connection/collection/manifests.ts`
```typescript
import type { ManifestCollection, ManifestCollectionView } from "@umbraco-cms/backoffice/extension-registry";
import { UaiConnectionConstants } from "../constants.js";

export const manifests: Array<ManifestCollection | ManifestCollectionView> = [
    {
        type: "collection",
        alias: UaiConnectionConstants.Collection,
        name: "Connection Collection",
        meta: {
            repositoryAlias: UaiConnectionConstants.Repository.Collection,
        },
    },
    {
        type: "collectionView",
        alias: "UmbracoAi.CollectionView.Connection.Table",
        name: "Connection Table View",
        element: () => import("./views/table/connection-table-collection-view.element.js"),
        meta: {
            label: "Table",
            icon: "icon-list",
            pathName: "table",
        },
        conditions: [
            { alias: "Umb.Condition.CollectionAlias", match: UaiConnectionConstants.Collection },
        ],
    },
];
```

### 4.3 Collection Action (Create Button)

**File:** `src/connection/collection/action/manifests.ts`
```typescript
import type { ManifestCollectionAction } from "@umbraco-cms/backoffice/extension-registry";
import { UaiConnectionConstants } from "../../constants.js";

export const manifests: ManifestCollectionAction[] = [
    {
        type: "collectionAction",
        kind: "button",
        alias: "UmbracoAi.CollectionAction.Connection.Create",
        name: "Create Connection",
        meta: {
            label: "Create",
            href: `/section/settings/workspace/${UaiConnectionConstants.Workspace.Entity}/create`,
        },
        conditions: [
            { alias: "Umb.Condition.CollectionAlias", match: UaiConnectionConstants.Collection },
        ],
    },
];
```

---

## Phase 5: Settings Section Integration

### 5.1 AI Menu in Settings

**File:** `src/section/menu/manifests.ts`
```typescript
import type { ManifestMenu } from "@umbraco-cms/backoffice/extension-registry";

export const manifests: ManifestMenu[] = [
    {
        type: "menu",
        alias: "UmbracoAi.Menu.Settings",
        name: "AI Settings Menu",
    },
];
```

### 5.2 Sidebar App

**File:** `src/section/sidebar/manifests.ts`
```typescript
import type { ManifestSectionSidebarApp } from "@umbraco-cms/backoffice/extension-registry";

export const manifests: ManifestSectionSidebarApp[] = [
    {
        type: "sectionSidebarApp",
        kind: "menuWithEntityActions",
        alias: "UmbracoAi.SectionSidebarApp.AiMenu",
        name: "AI Section Sidebar",
        weight: 100,
        meta: {
            label: "AI",
            menu: "UmbracoAi.Menu.Settings",
        },
        conditions: [
            { alias: "Umb.Condition.SectionAlias", match: "Umb.Section.Settings" },
        ],
    },
];
```

### 5.3 Connections Menu Item

**File:** `src/connection/menu/manifests.ts`
```typescript
import type { ManifestMenuItem } from "@umbraco-cms/backoffice/extension-registry";
import { UaiConnectionConstants } from "../constants.js";

export const manifests: ManifestMenuItem[] = [
    {
        type: "menuItem",
        alias: "UmbracoAi.MenuItem.Connections",
        name: "Connections Menu Item",
        weight: 100,
        meta: {
            label: "Connections",
            icon: UaiConnectionConstants.Icon.Root,
            entityType: UaiConnectionConstants.EntityType.Root,
            menus: ["UmbracoAi.Menu.Settings"],
        },
    },
];
```

### 5.4 Root Workspace (Collection Container)

**File:** `src/connection/workspace/connection-root/manifests.ts`
```typescript
import type { ManifestWorkspace, ManifestWorkspaceView } from "@umbraco-cms/backoffice/extension-registry";
import { UaiConnectionConstants } from "../../constants.js";

export const manifests: Array<ManifestWorkspace | ManifestWorkspaceView> = [
    {
        type: "workspace",
        kind: "default",
        alias: UaiConnectionConstants.Workspace.Root,
        name: "Connections Root Workspace",
        meta: {
            entityType: UaiConnectionConstants.EntityType.Root,
            headline: "Connections",
        },
    },
    {
        type: "workspaceView",
        kind: "collection",
        alias: "UmbracoAi.WorkspaceView.ConnectionRoot.Collection",
        name: "Connections Collection View",
        meta: {
            label: "Connections",
            pathname: "collection",
            icon: "icon-list",
            collectionAlias: UaiConnectionConstants.Collection,
        },
        conditions: [
            { alias: "Umb.Condition.WorkspaceAlias", match: UaiConnectionConstants.Workspace.Root },
        ],
    },
];
```

---

## Phase 6: Manifest Aggregation

### 6.1 Workspace Manifests

**File:** `src/connection/workspace/manifests.ts`
```typescript
import { manifests as connectionRootManifests } from "./connection-root/manifests.js";
import { manifests as connectionManifests } from "./connection/manifests.js";

export const manifests = [...connectionRootManifests, ...connectionManifests];
```

### 6.2 Connection Manifests

**File:** `src/connection/manifests.ts`
```typescript
import { manifests as collectionManifests } from "./collection/manifests.js";
import { manifests as collectionActionManifests } from "./collection/action/manifests.js";
import { manifests as menuManifests } from "./menu/manifests.js";
import { manifests as repositoryManifests } from "./repository/manifests.js";
import { manifests as workspaceManifests } from "./workspace/manifests.js";

export const manifests = [
    ...collectionManifests,
    ...collectionActionManifests,
    ...menuManifests,
    ...repositoryManifests,
    ...workspaceManifests,
];
```

### 6.3 Section Manifests

**File:** `src/section/manifests.ts`
```typescript
import { manifests as menuManifests } from "./menu/manifests.js";
import { manifests as sidebarManifests } from "./sidebar/manifests.js";

export const manifests = [...menuManifests, ...sidebarManifests];
```

### 6.4 Bundle Manifests (Root)

**File:** `src/bundle.manifests.ts`
```typescript
import { manifests as entrypointManifests } from "./entrypoints/manifest.js";
import { manifests as sectionManifests } from "./section/manifests.js";
import { manifests as connectionManifests } from "./connection/manifests.js";

export const manifests = [
    ...entrypointManifests,
    ...sectionManifests,
    ...connectionManifests,
];
```

---

## Implementation Order

### Step 1: Prerequisites
1. API client already generated in `src/api/`
2. Verify generated types exist

### Step 2: Core Infrastructure
1. Create `src/core/command/command.base.ts`
2. Create `src/core/command/command.store.ts`
3. Create `src/core/command/implement/partial-update.command.ts`
4. Create `src/core/command/implement/index.ts`
5. Create `src/core/command/index.ts`
6. Create `src/core/index.ts`

> **Note:** No custom input-lock component needed - use built-in `uui-input-lock` from UUI.

### Step 3: Connection Foundation
1. Create `src/connection/constants.ts`
2. Create `src/connection/types.ts`
3. Create `src/connection/type-mapper.ts`

### Step 4: Repository Layer
1. Create `src/connection/repository/detail/connection-detail.server.data-source.ts`
2. Create `src/connection/repository/detail/connection-detail.store.ts`
3. Create `src/connection/repository/detail/connection-detail.repository.ts`
4. Create `src/connection/repository/detail/index.ts`
5. Create `src/connection/repository/collection/connection-collection.repository.ts`
6. Create `src/connection/repository/collection/index.ts`
7. Create `src/connection/repository/manifests.ts`

### Step 5: Workspace
1. Create `src/connection/workspace/connection/connection-workspace.context.ts`
2. Create `src/connection/workspace/connection/connection-workspace-editor.element.ts`
3. Create `src/connection/workspace/connection/views/connection-details-workspace-view.element.ts`
4. Create `src/connection/workspace/connection/manifests.ts`
5. Create `src/connection/workspace/connection-root/manifests.ts`
6. Create `src/connection/workspace/manifests.ts`

### Step 6: Collection
1. Create `src/connection/collection/views/table/connection-table-collection-view.element.ts`
2. Create `src/connection/collection/action/manifests.ts`
3. Create `src/connection/collection/manifests.ts`

### Step 7: Section Integration
1. Create `src/section/menu/manifests.ts`
2. Create `src/section/sidebar/manifests.ts`
3. Create `src/section/manifests.ts`
4. Create `src/connection/menu/manifests.ts`

### Step 8: Final Assembly
1. Create `src/connection/manifests.ts`
2. Update `src/bundle.manifests.ts`
3. Build and test: `npm run build`

---

## Key Reference Files

**Commerce (CommandStore pattern):**
- `D:\Work\Umbraco\Umbraco.Commerce\Umbraco.Commerce.vLatest\src\Umbraco.Commerce.Cms.Web.StaticAssets\Client\src\core\patterns\command\command.store.ts`
- `D:\Work\Umbraco\Umbraco.Commerce\Umbraco.Commerce.vLatest\src\Umbraco.Commerce.Cms.Web.StaticAssets\Client\src\location\constants.ts`

**CMS (Repository/API pattern):**
- `D:\Work\Umbraco\Umbraco.CMS\Umbraco.CMS\src\Umbraco.Web.UI.Client\src\packages\webhook\repository\detail\webhook-detail.server.data-source.ts`
- `D:\Work\Umbraco\Umbraco.CMS\Umbraco.CMS\src\Umbraco.Web.UI.Client\src\packages\webhook\workspace\webhook-workspace.context.ts`

**Umbraco.Ai Backend:**
- `src\Umbraco.Ai.Web\Api\Management\Connection\Controllers\*`
- `src\Umbraco.Ai.Web\Api\Management\Connection\Models\*`

---

## Testing Checklist

- [ ] API client generates successfully
- [ ] AI menu appears in Settings sidebar
- [ ] Connections menu item appears under AI
- [ ] Clicking Connections shows collection view
- [ ] Collection displays existing connections
- [ ] "Create" button navigates to workspace
- [ ] Workspace form accepts input
- [ ] Save creates new connection
- [ ] Edit route loads existing connection
- [ ] Changes persist after save
- [ ] CommandStore replays on data refresh

---

## Future Enhancements (Out of Scope)

- Provider picker dropdown (requires Provider repository)
- Dynamic settings form (requires SettingDefinition schema)
- Connection test button
- Delete entity action
- Profiles UI (separate implementation)
