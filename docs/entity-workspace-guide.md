# Umbraco Backoffice Entity Workspace Guide

A comprehensive guide for implementing entity management UI in the Umbraco backoffice, based on CMS patterns (webhook) and Commerce patterns (CommandStore).

## Overview

This guide covers the key elements needed to create a fully functional entity workspace in Umbraco's backoffice, including:
- Repository layer for API integration
- Workspace context for state management
- Collection views for listing entities
- Menu integration in the Settings section
- Manifest registration system

---

## Directory Structure

A typical entity workspace follows this structure:

```
src/
├── {entity}/                          # Feature folder (singular: connection, webhook)
│   ├── collection/
│   │   ├── action/
│   │   │   └── manifests.ts           # Create button action
│   │   ├── repository/
│   │   │   ├── {entity}-collection.repository.ts
│   │   │   ├── constants.ts
│   │   │   └── index.ts
│   │   ├── views/
│   │   │   └── table/
│   │   │       └── {entity}-table-collection-view.element.ts
│   │   ├── constants.ts
│   │   └── manifests.ts
│   ├── menu/
│   │   └── manifests.ts               # Menu item registration
│   ├── repository/
│   │   ├── detail/
│   │   │   ├── {entity}-detail.server.data-source.ts
│   │   │   ├── {entity}-detail.store.ts
│   │   │   ├── {entity}-detail.repository.ts
│   │   │   ├── constants.ts
│   │   │   └── index.ts
│   │   └── manifests.ts
│   ├── workspace/
│   │   ├── {entity}/
│   │   │   ├── {entity}-workspace.context.ts
│   │   │   ├── {entity}-workspace-editor.element.ts
│   │   │   ├── views/
│   │   │   │   └── {entity}-details-workspace-view.element.ts
│   │   │   ├── constants.ts
│   │   │   └── manifests.ts
│   │   ├── {entity}-root/
│   │   │   └── manifests.ts           # Root workspace with collection
│   │   └── manifests.ts
│   ├── constants.ts                   # Central constants
│   ├── entity.ts                      # Entity type exports
│   ├── types.ts                       # TypeScript interfaces
│   ├── type-mapper.ts                 # API <-> Model mapping
│   ├── paths.ts                       # URL path patterns
│   └── manifests.ts                   # Aggregates all manifests
├── section/                           # Section integration (if new menu group)
│   ├── menu/
│   │   └── manifests.ts
│   ├── sidebar/
│   │   └── manifests.ts
│   └── manifests.ts
└── bundle.manifests.ts                # Root manifest aggregation
```

---

## 1. Constants & Entity Types

### Entity Type Definition

**File:** `{entity}/entity.ts`

```typescript
export const UMB_CONNECTION_WORKSPACE_ALIAS = 'UmbracoAi.Workspace.Connection';
export const UMB_CONNECTION_ENTITY_TYPE = 'uai:connection';
export const UMB_CONNECTION_ROOT_ENTITY_TYPE = 'uai:connection-root';

export type UmbConnectionEntityType = typeof UMB_CONNECTION_ENTITY_TYPE;
```

### Centralized Constants

**File:** `{entity}/constants.ts`

```typescript
export const EntityConstants = {
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

**Naming Conventions:**
- Entity types: Use prefixed format like `uai:connection` or CMS standard `webhook`
- Aliases: Use dot notation like `UmbracoAi.Workspace.Connection` or `Umb.Workspace.Webhook`

---

## 2. Type Definitions

### View Models

**File:** `{entity}/types.ts`

```typescript
// Detail model for workspace editing
export interface EntityDetailModel {
    unique: string;           // GUID identifier
    entityType: string;       // Must match EntityType.Entity constant
    alias: string;            // URL-safe identifier
    name: string;             // Display name
    // ... entity-specific properties
}

// Collection item model (lighter weight for lists)
export interface EntityItemModel {
    unique: string;
    entityType: string;
    name: string;
    // ... subset of properties needed for display
}
```

### Type Mapper

**File:** `{entity}/type-mapper.ts`

Maps between API response models and internal view models:

```typescript
import type { ApiResponseModel } from "../api/types.gen.js";
import { EntityConstants } from "./constants.js";
import type { EntityDetailModel, EntityItemModel } from "./types.js";

export const EntityTypeMapper = {
    toDetailModel(response: ApiResponseModel): EntityDetailModel {
        return {
            unique: response.id,
            entityType: EntityConstants.EntityType.Entity,
            alias: response.alias,
            name: response.name,
            // ... map other properties
        };
    },

    toItemModel(response: ApiResponseModel): EntityItemModel {
        return {
            unique: response.id,
            entityType: EntityConstants.EntityType.Entity,
            name: response.name,
        };
    },

    toCreateRequest(model: EntityDetailModel) {
        return {
            alias: model.alias,
            name: model.name,
            // ... only properties needed for creation
        };
    },

    toUpdateRequest(model: EntityDetailModel) {
        return {
            name: model.name,
            // ... only mutable properties (exclude alias/id)
        };
    },
};
```

---

## 3. Repository Layer

The repository layer follows UmbDetailRepositoryBase pattern with three components:

### 3.1 Server Data Source

**File:** `{entity}/repository/detail/{entity}-detail.server.data-source.ts`

Implements `UmbDetailDataSource<TModel>` interface:

```typescript
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import type { UmbDetailDataSource } from "@umbraco-cms/backoffice/repository";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { UmbId } from "@umbraco-cms/backoffice/id";
import { EntityService } from "../../../api/sdk.gen.js";
import { EntityConstants } from "../../constants.js";
import { EntityTypeMapper } from "../../type-mapper.js";
import type { EntityDetailModel } from "../../types.js";

export class EntityDetailServerDataSource implements UmbDetailDataSource<EntityDetailModel> {
    #host: UmbControllerHost;

    constructor(host: UmbControllerHost) {
        this.#host = host;
    }

    async createScaffold(preset: Partial<EntityDetailModel> = {}) {
        const data: EntityDetailModel = {
            entityType: EntityConstants.EntityType.Entity,
            unique: UmbId.new(),
            alias: "",
            name: "",
            ...preset,
        };
        return { data };
    }

    async read(unique: string) {
        if (!unique) throw new Error("Unique is missing");

        const { data, error } = await tryExecute(
            this.#host,
            EntityService.getEntityById({ path: { id: unique } })
        );

        if (error || !data) {
            return { error };
        }

        return { data: EntityTypeMapper.toDetailModel(data) };
    }

    async create(model: EntityDetailModel, _parentUnique: string | null) {
        if (!model) throw new Error("Model is missing");

        const { data, error } = await tryExecute(
            this.#host,
            EntityService.createEntity({
                body: EntityTypeMapper.toCreateRequest(model),
            })
        );

        if (data) {
            return this.read(data.id);
        }

        return { error };
    }

    async update(model: EntityDetailModel) {
        if (!model.unique) throw new Error("Unique is missing");

        const { error } = await tryExecute(
            this.#host,
            EntityService.updateEntity({
                path: { id: model.unique },
                body: EntityTypeMapper.toUpdateRequest(model),
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
            EntityService.deleteEntity({ path: { id: unique } })
        );
    }
}
```

### 3.2 Detail Store

**File:** `{entity}/repository/detail/{entity}-detail.store.ts`

```typescript
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbDetailStoreBase } from "@umbraco-cms/backoffice/store";
import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";
import { EntityConstants } from "../../constants.js";
import type { EntityDetailModel } from "../../types.js";

export class EntityDetailStore extends UmbDetailStoreBase<EntityDetailModel> {
    constructor(host: UmbControllerHost) {
        super(host, EntityConstants.Store.Detail);
    }
}

export const ENTITY_DETAIL_STORE_CONTEXT = new UmbContextToken<EntityDetailStore>(
    EntityConstants.Store.Detail
);

export { EntityDetailStore as api };
```

### 3.3 Detail Repository

**File:** `{entity}/repository/detail/{entity}-detail.repository.ts`

```typescript
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbDetailRepositoryBase } from "@umbraco-cms/backoffice/repository";
import { EntityDetailServerDataSource } from "./{entity}-detail.server.data-source.js";
import { ENTITY_DETAIL_STORE_CONTEXT } from "./{entity}-detail.store.js";
import type { EntityDetailModel } from "../../types.js";

export class EntityDetailRepository extends UmbDetailRepositoryBase<EntityDetailModel> {
    constructor(host: UmbControllerHost) {
        super(host, EntityDetailServerDataSource, ENTITY_DETAIL_STORE_CONTEXT);
    }

    override async create(model: EntityDetailModel) {
        return super.create(model, null);  // null parent for root-level entities
    }
}

export { EntityDetailRepository as api };
```

### 3.4 Collection Repository

**File:** `{entity}/repository/collection/{entity}-collection.repository.ts`

```typescript
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbRepositoryBase } from "@umbraco-cms/backoffice/repository";
import type { UmbCollectionRepository } from "@umbraco-cms/backoffice/collection";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { EntityService } from "../../../api/sdk.gen.js";
import { EntityTypeMapper } from "../../type-mapper.js";

export class EntityCollectionRepository extends UmbRepositoryBase implements UmbCollectionRepository {
    constructor(host: UmbControllerHost) {
        super(host);
    }

    async requestCollection() {
        const { data, error } = await tryExecute(
            this,
            EntityService.getAllEntities({ query: { skip: 0, take: 1000 } })
        );

        if (data) {
            const items = data.items.map(EntityTypeMapper.toItemModel);
            return { data: { items, total: data.total } };
        }

        return { error };
    }
}

export { EntityCollectionRepository as api };
```

### 3.5 Repository Manifests

**File:** `{entity}/repository/manifests.ts`

```typescript
import type { ManifestRepository, ManifestStore } from "@umbraco-cms/backoffice/extension-registry";
import { EntityConstants } from "../constants.js";

export const manifests: Array<ManifestRepository | ManifestStore> = [
    {
        type: "store",
        alias: EntityConstants.Store.Detail,
        name: "Entity Detail Store",
        api: () => import("./detail/{entity}-detail.store.js"),
    },
    {
        type: "repository",
        alias: EntityConstants.Repository.Detail,
        name: "Entity Detail Repository",
        api: () => import("./detail/{entity}-detail.repository.js"),
    },
    {
        type: "repository",
        alias: EntityConstants.Repository.Collection,
        name: "Entity Collection Repository",
        api: () => import("./collection/{entity}-collection.repository.js"),
    },
];
```

---

## 4. Workspace Context

The workspace context manages entity state and provides methods for UI components.

### 4.1 Basic Pattern (CMS Style)

**File:** `{entity}/workspace/{entity}/{entity}-workspace.context.ts`

```typescript
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import type { UmbRoutableWorkspaceContext, UmbSubmittableWorkspaceContext } from "@umbraco-cms/backoffice/workspace";
import { UmbSubmittableWorkspaceContextBase, UmbWorkspaceRouteManager } from "@umbraco-cms/backoffice/workspace";
import { UmbObjectState, UmbBasicState } from "@umbraco-cms/backoffice/observable-api";
import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";
import { EntityConstants } from "../../constants.js";
import { EntityDetailRepository } from "../../repository/detail/{entity}-detail.repository.js";
import type { EntityDetailModel } from "../../types.js";
import EntityWorkspaceEditorElement from "./{entity}-workspace-editor.element.js";

export const ENTITY_WORKSPACE_CONTEXT = new UmbContextToken<
    UmbSubmittableWorkspaceContext,
    EntityWorkspaceContext
>(
    "UmbWorkspaceContext",
    undefined,
    (context): context is EntityWorkspaceContext =>
        context.getEntityType() === EntityConstants.EntityType.Entity
);

export class EntityWorkspaceContext
    extends UmbSubmittableWorkspaceContextBase<EntityDetailModel>
    implements UmbSubmittableWorkspaceContext, UmbRoutableWorkspaceContext
{
    public readonly routes = new UmbWorkspaceRouteManager(this);

    readonly #repository = new EntityDetailRepository(this);

    #unique = new UmbBasicState<string | undefined>(undefined);
    readonly unique = this.#unique.asObservable();

    #model = new UmbObjectState<EntityDetailModel | undefined>(undefined);
    readonly model = this.#model.asObservable();

    constructor(host: UmbControllerHost) {
        super(host, EntityConstants.Workspace.Entity);

        this.routes.setRoutes([
            {
                path: "create",
                component: EntityWorkspaceEditorElement,
                setup: async () => {
                    await this.create();
                },
            },
            {
                path: "edit/:unique",
                component: EntityWorkspaceEditorElement,
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
                    this.#model.setValue(model);
                    this.setIsNew(false);
                }
            });
        }
    }

    // Property update method
    updateProperty<K extends keyof EntityDetailModel>(key: K, value: EntityDetailModel[K]) {
        const current = this.#model.getValue();
        if (current) {
            this.#model.setValue({ ...current, [key]: value });
        }
    }

    getData(): EntityDetailModel | undefined {
        return this.#model.getValue();
    }

    getUnique(): string | undefined {
        return this.#unique.getValue();
    }

    getEntityType(): string {
        return EntityConstants.EntityType.Entity;
    }

    async submit() {
        if (!this.#model.value) return;

        const { error } = this.getIsNew()
            ? await this.#repository.create(this.#model.value)
            : await this.#repository.save(this.#model.value);

        if (!error) {
            this.setIsNew(false);
        }
    }
}

export { EntityWorkspaceContext as api };
```

### 4.2 CommandStore Pattern (Commerce Style)

For optimistic UI with command replay on server refresh:

```typescript
import { CommandStore } from "../../core/command/command.store.js";
import type { CommandBase } from "../../core/command/command.base.js";

export class EntityWorkspaceContext extends UmbSubmittableWorkspaceContextBase<EntityDetailModel> {
    readonly #commandStore = new CommandStore();

    // In load method - replay commands on fresh data
    async load(unique: string) {
        // ... fetch data
        this.observe(asObservable(), (model) => {
            if (model) {
                const newModel = structuredClone(model);
                this.#commandStore.getAll().forEach((cmd) => cmd.execute(newModel));
                this.#model.setValue(newModel);
            }
        });
    }

    // Command-based updates
    handleCommand(command: CommandBase<EntityDetailModel>) {
        const currentValue = this.#model.getValue();
        if (currentValue) {
            const newValue = structuredClone(currentValue);
            command.execute(newValue);
            this.#model.setValue(newValue);
            this.#commandStore.add(command);
        }
    }

    async submit() {
        this.#commandStore.mute();  // Prevent interference during save

        const { error } = this.getIsNew()
            ? await this.#repository.create(this.#model.value!)
            : await this.#repository.save(this.#model.value!);

        if (!error) {
            this.#commandStore.reset();  // Clear and unmute
        } else {
            this.#commandStore.unmute();  // Allow further edits on error
        }
    }
}
```

---

## 5. Workspace Editor Element

The editor element renders the workspace UI (header, body, footer).

**File:** `{entity}/workspace/{entity}/{entity}-workspace-editor.element.ts`

```typescript
import { css, html, customElement, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import type { UUIInputElement, UUIInputEvent } from "@umbraco-cms/backoffice/external/uui";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import type { EntityDetailModel } from "../../types.js";
import { EntityConstants } from "../../constants.js";
import { ENTITY_WORKSPACE_CONTEXT } from "./{entity}-workspace.context.js";

@customElement("entity-workspace-editor")
export class EntityWorkspaceEditorElement extends UmbLitElement {
    #workspaceContext?: typeof ENTITY_WORKSPACE_CONTEXT.TYPE;

    @state()
    _model?: EntityDetailModel;

    @state()
    _isNew?: boolean;

    constructor() {
        super();
        this.consumeContext(ENTITY_WORKSPACE_CONTEXT, (context) => {
            this.#workspaceContext = context;
            this.observe(context.model, (model) => this._model = model);
            this.observe(context.isNew, (isNew) => this._isNew = isNew);
        });
    }

    #onNameChange(event: UUIInputEvent) {
        event.stopPropagation();
        const target = event.composedPath()[0] as UUIInputElement;
        this.#workspaceContext?.updateProperty("name", target.value.toString());
    }

    render() {
        if (!this._model) return;
        return html`
            <umb-workspace-editor alias=${EntityConstants.Workspace.Entity}>
                <div id="header" slot="header">
                    <uui-button
                        href="section/settings/workspace/${EntityConstants.Workspace.Root}/collection"
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
                    </uui-input>
                </div>
                ${!this._isNew
                    ? html`<umb-workspace-entity-action-menu slot="action-menu"></umb-workspace-entity-action-menu>`
                    : ""}
                <div slot="footer-info" id="footer">
                    <a href="section/settings">Settings</a> /
                    <a href="section/settings/workspace/${EntityConstants.Workspace.Root}/collection">Entities</a> /
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
            #name {
                width: 100%;
                flex: 1 1 auto;
            }
        `,
    ];
}

export default EntityWorkspaceEditorElement;
```

---

## 6. Workspace Views

Workspace views provide tabbed content areas within the workspace.

**File:** `{entity}/workspace/{entity}/views/{entity}-details-workspace-view.element.ts`

```typescript
import { html, customElement, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import type { EntityDetailModel } from "../../../types.js";
import { ENTITY_WORKSPACE_CONTEXT } from "../{entity}-workspace.context.js";

@customElement("entity-details-workspace-view")
export class EntityDetailsWorkspaceViewElement extends UmbLitElement {
    #workspaceContext?: typeof ENTITY_WORKSPACE_CONTEXT.TYPE;

    @state()
    _model?: EntityDetailModel;

    constructor() {
        super();
        this.consumeContext(ENTITY_WORKSPACE_CONTEXT, (context) => {
            this.#workspaceContext = context;
            this.observe(context.model, (model) => this._model = model);
        });
    }

    #onPropertyChange(property: keyof EntityDetailModel, value: unknown) {
        this.#workspaceContext?.updateProperty(property, value as EntityDetailModel[typeof property]);
    }

    render() {
        if (!this._model) return;
        return html`
            <uui-box headline="Entity Details">
                <umb-property-layout label="Name" description="Display name">
                    <uui-input
                        slot="editor"
                        .value=${this._model.name ?? ""}
                        @change=${(e: Event) => this.#onPropertyChange("name", (e.target as HTMLInputElement).value)}>
                    </uui-input>
                </umb-property-layout>
                <!-- Additional properties -->
            </uui-box>
        `;
    }

    static styles = [UmbTextStyles];
}

export default EntityDetailsWorkspaceViewElement;
```

---

## 7. Collection Views

### 7.1 Table Collection View

**File:** `{entity}/collection/views/table/{entity}-table-collection-view.element.ts`

```typescript
import { html, customElement, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import type { UmbTableColumn, UmbTableItem } from "@umbraco-cms/backoffice/components";
import { UMB_COLLECTION_CONTEXT } from "@umbraco-cms/backoffice/collection";
import type { EntityItemModel } from "../../../types.js";
import { EntityConstants } from "../../../constants.js";

@customElement("entity-table-collection-view")
export class EntityTableCollectionViewElement extends UmbLitElement {
    @state() private _items: UmbTableItem[] = [];

    private _columns: UmbTableColumn[] = [
        { name: "Name", alias: "name" },
        { name: "Status", alias: "status" },
    ];

    constructor() {
        super();
        this.consumeContext(UMB_COLLECTION_CONTEXT, (ctx) => {
            this.observe(ctx.items, (items) => this._createTableItems(items as EntityItemModel[]));
        });
    }

    _createTableItems(items: EntityItemModel[]) {
        this._items = items.map((item) => ({
            id: item.unique,
            icon: EntityConstants.Icon.Entity,
            data: [
                {
                    columnAlias: "name",
                    value: html`<a href="/section/settings/workspace/${EntityConstants.Workspace.Entity}/edit/${item.unique}">${item.name}</a>`,
                },
                {
                    columnAlias: "status",
                    value: html`<uui-tag color="positive">Active</uui-tag>`,
                },
            ],
        }));
    }

    render() {
        return html`<umb-table .columns=${this._columns} .items=${this._items}></umb-table>`;
    }
}

export default EntityTableCollectionViewElement;
```

### 7.2 Collection Action (Create Button)

**File:** `{entity}/collection/action/manifests.ts`

```typescript
import type { ManifestCollectionAction } from "@umbraco-cms/backoffice/extension-registry";
import { EntityConstants } from "../../constants.js";

export const manifests: ManifestCollectionAction[] = [
    {
        type: "collectionAction",
        kind: "button",
        alias: "Entity.CollectionAction.Create",
        name: "Create Entity",
        meta: {
            label: "Create",
            href: `/section/settings/workspace/${EntityConstants.Workspace.Entity}/create`,
        },
        conditions: [
            { alias: "Umb.Condition.CollectionAlias", match: EntityConstants.Collection },
        ],
    },
];
```

---

## 8. Menu Integration

### 8.1 Menu Definition (for new menu groups)

**File:** `section/menu/manifests.ts`

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

### 8.2 Sidebar App (menu group in section)

**File:** `section/sidebar/manifests.ts`

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

### 8.3 Menu Item

**File:** `{entity}/menu/manifests.ts`

```typescript
import type { ManifestMenuItem } from "@umbraco-cms/backoffice/extension-registry";
import { EntityConstants } from "../constants.js";

export const manifests: ManifestMenuItem[] = [
    {
        type: "menuItem",
        alias: "Entity.MenuItem",
        name: "Entities Menu Item",
        weight: 100,
        meta: {
            label: "Entities",
            icon: EntityConstants.Icon.Root,
            entityType: EntityConstants.EntityType.Root,
            menus: ["UmbracoAi.Menu.Settings"],  // or "Umb.Menu.AdvancedSettings"
        },
    },
];
```

---

## 9. Root Workspace (Collection Container)

**File:** `{entity}/workspace/{entity}-root/manifests.ts`

```typescript
import type { ManifestWorkspace, ManifestWorkspaceView } from "@umbraco-cms/backoffice/extension-registry";
import { EntityConstants } from "../../constants.js";

export const manifests: Array<ManifestWorkspace | ManifestWorkspaceView> = [
    {
        type: "workspace",
        kind: "default",
        alias: EntityConstants.Workspace.Root,
        name: "Entities Root Workspace",
        meta: {
            entityType: EntityConstants.EntityType.Root,
            headline: "Entities",
        },
    },
    {
        type: "workspaceView",
        kind: "collection",
        alias: "Entity.WorkspaceView.Root.Collection",
        name: "Entities Collection View",
        meta: {
            label: "Entities",
            pathname: "collection",
            icon: "icon-list",
            collectionAlias: EntityConstants.Collection,
        },
        conditions: [
            { alias: "Umb.Condition.WorkspaceAlias", match: EntityConstants.Workspace.Root },
        ],
    },
];
```

---

## 10. Workspace Manifests

**File:** `{entity}/workspace/{entity}/manifests.ts`

```typescript
import type { ManifestWorkspace, ManifestWorkspaceAction, ManifestWorkspaceView } from "@umbraco-cms/backoffice/extension-registry";
import { UmbSubmitWorkspaceAction } from "@umbraco-cms/backoffice/workspace";
import { EntityConstants } from "../../constants.js";

export const manifests: Array<ManifestWorkspace | ManifestWorkspaceAction | ManifestWorkspaceView> = [
    {
        type: "workspace",
        kind: "routable",
        alias: EntityConstants.Workspace.Entity,
        name: "Entity Workspace",
        api: () => import("./{entity}-workspace.context.js"),
        meta: {
            entityType: EntityConstants.EntityType.Entity,
        },
    },
    {
        type: "workspaceView",
        alias: "Entity.WorkspaceView.Details",
        name: "Entity Details Workspace View",
        element: () => import("./views/{entity}-details-workspace-view.element.js"),
        weight: 100,
        meta: {
            label: "Details",
            pathname: "details",
            icon: "icon-settings",
        },
        conditions: [
            { alias: "Umb.Condition.WorkspaceAlias", match: EntityConstants.Workspace.Entity },
        ],
    },
    {
        type: "workspaceAction",
        kind: "default",
        alias: "Entity.WorkspaceAction.Save",
        name: "Save Entity",
        api: UmbSubmitWorkspaceAction,
        meta: {
            label: "Save",
            look: "primary",
            color: "positive",
        },
        conditions: [
            { alias: "Umb.Condition.WorkspaceAlias", match: EntityConstants.Workspace.Entity },
        ],
    },
];
```

---

## 11. Manifest Aggregation

### Feature Manifests

**File:** `{entity}/manifests.ts`

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

### Bundle Manifests (Root)

**File:** `bundle.manifests.ts`

```typescript
import { manifests as entrypointManifests } from "./entrypoints/manifest.js";
import { manifests as sectionManifests } from "./section/manifests.js";
import { manifests as entityManifests } from "./{entity}/manifests.js";

export const manifests = [
    ...entrypointManifests,
    ...sectionManifests,
    ...entityManifests,
];
```

---

## 12. Implementation Checklist

### Core Infrastructure
- [ ] Define entity types and constants
- [ ] Create TypeScript interfaces (types.ts)
- [ ] Create type mapper (API <-> Model)

### Repository Layer
- [ ] Server data source (CRUD operations)
- [ ] Detail store
- [ ] Detail repository
- [ ] Collection repository
- [ ] Repository manifests

### Workspace
- [ ] Workspace context (state management)
- [ ] Workspace editor element (UI)
- [ ] Workspace views (tabbed content)
- [ ] Workspace manifests

### Collection
- [ ] Table collection view
- [ ] Collection action (Create button)
- [ ] Collection manifests

### Section Integration
- [ ] Menu definition (if new group)
- [ ] Sidebar app (if new group)
- [ ] Menu item
- [ ] Root workspace with collection view

### Final Assembly
- [ ] Feature manifests aggregation
- [ ] Bundle manifests update
- [ ] Build and test

---

## Key Patterns Summary

| Pattern | Purpose | Key Base Class |
|---------|---------|----------------|
| Data Source | API communication | `UmbDetailDataSource<T>` |
| Store | Client-side cache | `UmbDetailStoreBase<T>` |
| Repository | Coordinates data source + store | `UmbDetailRepositoryBase<T>` |
| Workspace Context | Entity state management | `UmbSubmittableWorkspaceContextBase<T>` |
| CommandStore | Optimistic UI with replay | Custom (Commerce pattern) |
| Collection Repository | List data fetching | `UmbCollectionRepository` |

---

## Common Imports Reference

```typescript
// Controller/Context
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";

// Repository
import type { UmbDetailDataSource } from "@umbraco-cms/backoffice/repository";
import { UmbDetailRepositoryBase, UmbRepositoryBase } from "@umbraco-cms/backoffice/repository";
import { tryExecute } from "@umbraco-cms/backoffice/resources";

// Store
import { UmbDetailStoreBase } from "@umbraco-cms/backoffice/store";

// Workspace
import type { UmbRoutableWorkspaceContext, UmbSubmittableWorkspaceContext } from "@umbraco-cms/backoffice/workspace";
import { UmbSubmittableWorkspaceContextBase, UmbWorkspaceRouteManager, UmbSubmitWorkspaceAction } from "@umbraco-cms/backoffice/workspace";

// Observable
import { UmbObjectState, UmbBasicState } from "@umbraco-cms/backoffice/observable-api";

// Lit Elements
import { css, html, customElement, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";

// Collection
import type { UmbCollectionRepository } from "@umbraco-cms/backoffice/collection";
import { UMB_COLLECTION_CONTEXT } from "@umbraco-cms/backoffice/collection";

// Components
import type { UmbTableColumn, UmbTableItem } from "@umbraco-cms/backoffice/components";

// ID Generation
import { UmbId } from "@umbraco-cms/backoffice/id";
```
