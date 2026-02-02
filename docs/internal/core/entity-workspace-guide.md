# Umbraco Backoffice Entity Workspace Guide

A comprehensive guide for implementing entity management UI in the Umbraco backoffice, based on the actual patterns used in Umbraco.AI (connection workspace).

## Overview

This guide covers the key elements needed to create a fully functional entity workspace in Umbraco's backoffice, including:
- Repository layer for API integration
- Workspace context for state management with CommandStore pattern
- Collection views for listing entities
- Menu integration in the Settings section
- Manifest registration system
- Type-safe URL path patterns

---

## Directory Structure

A typical entity workspace follows this structure:

```
src/
├── {entity}/                              # Feature folder (singular: connection, profile)
│   ├── collection/
│   │   ├── action/
│   │   │   └── manifests.ts               # Create button action
│   │   ├── views/
│   │   │   └── table/
│   │   │       └── {entity}-table-collection-view.element.ts
│   │   ├── {entity}-collection.element.ts # Custom collection with search (optional)
│   │   ├── constants.ts                   # Collection alias
│   │   └── manifests.ts                   # Collection + view manifests
│   ├── entity-actions/
│   │   ├── {entity}-create.action.ts      # Create entity action (for "+" button)
│   │   └── manifests.ts                   # Entity action manifests
│   ├── menu/
│   │   └── manifests.ts                   # Menu item registration
│   ├── repository/
│   │   ├── collection/
│   │   │   ├── {entity}-collection.server.data-source.ts
│   │   │   ├── {entity}-collection.repository.ts
│   │   │   └── index.ts
│   │   ├── detail/
│   │   │   ├── {entity}-detail.server.data-source.ts
│   │   │   ├── {entity}-detail.store.ts
│   │   │   ├── {entity}-detail.repository.ts
│   │   │   └── index.ts
│   │   ├── constants.ts                   # Repository/store aliases
│   │   ├── index.ts
│   │   └── manifests.ts
│   ├── workspace/
│   │   ├── {entity}/
│   │   │   ├── {entity}-workspace.context.ts
│   │   │   ├── {entity}-workspace.context-token.ts   # Separate context token
│   │   │   ├── {entity}-workspace-editor.element.ts
│   │   │   ├── views/
│   │   │   │   └── {entity}-details-workspace-view.element.ts
│   │   │   ├── paths.ts                   # URL path patterns
│   │   │   └── manifests.ts
│   │   ├── {entity}-root/
│   │   │   ├── index.ts
│   │   │   ├── paths.ts                   # Root workspace path
│   │   │   └── manifests.ts
│   │   ├── constants.ts                   # Workspace aliases
│   │   ├── index.ts
│   │   └── manifests.ts
│   ├── constants.ts                       # Re-exports all constants
│   ├── entity.ts                          # Entity type constants
│   ├── types.ts                           # TypeScript interfaces
│   ├── type-mapper.ts                     # API <-> Model mapping
│   ├── index.ts                           # Public exports
│   └── manifests.ts                       # Aggregates all manifests
├── core/
│   └── command/                           # CommandStore pattern (shared)
│       ├── command.base.ts
│       ├── command.store.ts
│       └── implement/
│           └── partial-update.command.ts
├── section/                               # Section integration
│   ├── menu/
│   │   └── manifests.ts
│   ├── sidebar/
│   │   └── manifests.ts
│   └── manifests.ts
└── bundle.manifests.ts                    # Root manifest aggregation
```

---

## 1. Constants & Entity Types

### Entity Type Definition

**File:** `{entity}/entity.ts`

```typescript
export const UAI_CONNECTION_ENTITY_TYPE = 'uai:connection';
export const UAI_CONNECTION_ROOT_ENTITY_TYPE = 'uai:connection-root';

export type UaiConnectionEntityType = typeof UAI_CONNECTION_ENTITY_TYPE;
export type UaiConnectionRootEntityType = typeof UAI_CONNECTION_ROOT_ENTITY_TYPE;
```

### Centralized Constants (Re-exports)

**File:** `{entity}/constants.ts`

Re-exports all constants from child modules for convenient imports:

```typescript
export { UAI_CONNECTION_ENTITY_TYPE, UAI_CONNECTION_ROOT_ENTITY_TYPE } from './entity.js';
export type { UaiConnectionEntityType, UaiConnectionRootEntityType } from './entity.js';

export * from './workspace/constants.js';
export * from './repository/constants.js';
export * from './collection/constants.js';

export const UAI_CONNECTION_ICON = 'icon-wall-plug';
```

### Workspace Constants

**File:** `{entity}/workspace/constants.ts`

```typescript
export const UAI_CONNECTION_WORKSPACE_ALIAS = 'UmbracoAi.Workspace.Connection';
export const UAI_CONNECTION_ROOT_WORKSPACE_ALIAS = 'UmbracoAi.Workspace.ConnectionRoot';
```

### Repository Constants

**File:** `{entity}/repository/constants.ts`

```typescript
export const UAI_CONNECTION_DETAIL_REPOSITORY_ALIAS = 'UmbracoAi.Repository.Connection.Detail';
export const UAI_CONNECTION_DETAIL_STORE_ALIAS = 'UmbracoAi.Store.Connection.Detail';
export const UAI_CONNECTION_COLLECTION_REPOSITORY_ALIAS = 'UmbracoAi.Repository.Connection.Collection';
```

### Collection Constants

**File:** `{entity}/collection/constants.ts`

```typescript
export const UAI_CONNECTION_COLLECTION_ALIAS = 'UmbracoAi.Collection.Connection';
```

**Naming Conventions:**
- Entity types: Use prefixed format like `uai:connection` or CMS standard `webhook`
- Aliases: Use dot notation like `UmbracoAi.Workspace.Connection`
- Icon: Define once at feature root, reference everywhere

---

## 2. Type Definitions

### View Models

**File:** `{entity}/types.ts`

```typescript
import type { UmbEntityModel } from "@umbraco-cms/backoffice/entity";

// Detail model for workspace editing
export interface UaiConnectionDetailModel extends UmbEntityModel {
    unique: string;           // GUID identifier
    entityType: string;       // Must match entity type constant
    alias: string;            // URL-safe identifier
    name: string;             // Display name
    providerId: string;       // Provider reference
    settings: Record<string, unknown> | null;
    isActive: boolean;
}

// Collection item model (lighter weight for lists)
export interface UaiConnectionItemModel extends UmbEntityModel {
    unique: string;
    entityType: string;
    name: string;
    providerId: string;
    isActive: boolean;
}
```

### Type Mapper

**File:** `{entity}/type-mapper.ts`

Maps between API response models and internal view models:

```typescript
import type { ConnectionResponseModel, ConnectionItemResponseModel } from "../api/types.gen.js";
import { UAI_CONNECTION_ENTITY_TYPE } from "./constants.js";
import type { UaiConnectionDetailModel, UaiConnectionItemModel } from "./types.js";

export const UaiConnectionTypeMapper = {
    toDetailModel(response: ConnectionResponseModel): UaiConnectionDetailModel {
        return {
            unique: response.id,
            entityType: UAI_CONNECTION_ENTITY_TYPE,
            alias: response.alias,
            name: response.name,
            providerId: response.providerId,
            settings: (response.settings as Record<string, unknown>) ?? null,
            isActive: response.isActive,
        };
    },

    toItemModel(response: ConnectionItemResponseModel): UaiConnectionItemModel {
        return {
            unique: response.id,
            entityType: UAI_CONNECTION_ENTITY_TYPE,
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
            alias: model.alias,
            name: model.name,
            settings: model.settings,
            isActive: model.isActive,
        };
    },
};
```

---

## 3. Repository Layer

The repository layer follows UmbDetailRepositoryBase pattern with four components.

### 3.1 Server Data Source

**File:** `{entity}/repository/detail/{entity}-detail.server.data-source.ts`

Implements `UmbDetailDataSource<TModel>` interface:

```typescript
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import type { UmbDetailDataSource } from "@umbraco-cms/backoffice/repository";
import { tryExecuteAndNotify } from "@umbraco-cms/backoffice/resources";
import { ConnectionsService } from "../../../api/sdk.gen.js";
import { UaiConnectionTypeMapper } from "../../type-mapper.js";
import type { UaiConnectionDetailModel } from "../../types.js";
import { UAI_CONNECTION_ENTITY_TYPE } from "../../constants.js";

/**
 * Server data source for Connection detail operations.
 */
export class UaiConnectionDetailServerDataSource implements UmbDetailDataSource<UaiConnectionDetailModel> {
    #host: UmbControllerHost;

    constructor(host: UmbControllerHost) {
        this.#host = host;
    }

    /**
     * Creates a scaffold for a new entity.
     */
    async createScaffold(preset?: Partial<UaiConnectionDetailModel>) {
        const scaffold: UaiConnectionDetailModel = {
            unique: "",                              // Empty for new entities
            entityType: UAI_CONNECTION_ENTITY_TYPE,
            alias: "",
            name: "",
            providerId: preset?.providerId ?? "",
            settings: null,
            isActive: true,
            ...preset,
        };

        return { data: scaffold };
    }

    /**
     * Reads an entity by its unique identifier.
     */
    async read(unique: string) {
        const { data, error } = await tryExecuteAndNotify(
            this.#host,
            ConnectionsService.getConnectionById({ path: { id: unique } })
        );

        if (error || !data) {
            return { error };
        }

        return { data: UaiConnectionTypeMapper.toDetailModel(data) };
    }

    /**
     * Creates a new entity.
     */
    async create(model: UaiConnectionDetailModel, _parentUnique: string | null) {
        const requestBody = UaiConnectionTypeMapper.toCreateRequest(model);

        const { response, error } = await tryExecuteAndNotify(
            this.#host,
            ConnectionsService.postConnection({ body: requestBody })
        );

        if (error) {
            return { error };
        }

        // Extract the ID from the Location header
        const locationHeader = response?.headers?.get("Location") ?? "";
        const unique = locationHeader.split("/").pop() ?? "";

        return {
            data: {
                ...model,
                unique,
            },
        };
    }

    /**
     * Updates an existing entity.
     */
    async update(model: UaiConnectionDetailModel) {
        const requestBody = UaiConnectionTypeMapper.toUpdateRequest(model);

        const { error } = await tryExecuteAndNotify(
            this.#host,
            ConnectionsService.putConnectionById({
                path: { id: model.unique },
                body: requestBody,
            })
        );

        if (error) {
            return { error };
        }

        return { data: model };
    }

    /**
     * Deletes an entity by its unique identifier.
     */
    async delete(unique: string) {
        const { error } = await tryExecuteAndNotify(
            this.#host,
            ConnectionsService.deleteConnectionById({ path: { id: unique } })
        );

        if (error) {
            return { error };
        }

        return {};
    }
}
```

### 3.2 Detail Store

**File:** `{entity}/repository/detail/{entity}-detail.store.ts`

```typescript
import { UmbDetailStoreBase } from "@umbraco-cms/backoffice/store";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";
import type { UaiConnectionDetailModel } from "../../types.js";

export const UAI_CONNECTION_DETAIL_STORE_CONTEXT = new UmbContextToken<UaiConnectionDetailStore>(
    "UaiConnectionDetailStore"
);

/**
 * Store for entity detail data.
 * Extends the CMS detail store base for consistent caching behavior.
 */
export class UaiConnectionDetailStore extends UmbDetailStoreBase<UaiConnectionDetailModel> {
    constructor(host: UmbControllerHost) {
        super(host, UAI_CONNECTION_DETAIL_STORE_CONTEXT.toString());
    }
}

export { UaiConnectionDetailStore as api };
```

### 3.3 Detail Repository

**File:** `{entity}/repository/detail/{entity}-detail.repository.ts`

```typescript
import { UmbDetailRepositoryBase } from "@umbraco-cms/backoffice/repository";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UaiConnectionDetailServerDataSource } from "./connection-detail.server.data-source.js";
import { UAI_CONNECTION_DETAIL_STORE_CONTEXT } from "./connection-detail.store.js";
import type { UaiConnectionDetailModel } from "../../types.js";

/**
 * Repository for entity detail CRUD operations.
 * Uses UmbDetailRepositoryBase for consistent CMS patterns.
 */
export class UaiConnectionDetailRepository extends UmbDetailRepositoryBase<UaiConnectionDetailModel> {
    constructor(host: UmbControllerHost) {
        super(host, UaiConnectionDetailServerDataSource, UAI_CONNECTION_DETAIL_STORE_CONTEXT);
    }

    override async create(model: UaiConnectionDetailModel) {
        return super.create(model, null);  // null parent for root-level entities
    }
}

export { UaiConnectionDetailRepository as api };
```

### 3.4 Detail Index

**File:** `{entity}/repository/detail/index.ts`

```typescript
export { UaiConnectionDetailServerDataSource } from "./connection-detail.server.data-source.js";
export { UaiConnectionDetailStore, UAI_CONNECTION_DETAIL_STORE_CONTEXT } from "./connection-detail.store.js";
export { UaiConnectionDetailRepository } from "./connection-detail.repository.js";
```

### 3.5 Collection Server Data Source

**File:** `{entity}/repository/collection/{entity}-collection.server.data-source.ts`

```typescript
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import type { UmbCollectionDataSource, UmbCollectionFilterModel } from "@umbraco-cms/backoffice/collection";
import { tryExecuteAndNotify } from "@umbraco-cms/backoffice/resources";
import { ConnectionsService } from "../../../api/sdk.gen.js";
import { UaiConnectionTypeMapper } from "../../type-mapper.js";
import type { UaiConnectionItemModel } from "../../types.js";

/**
 * Server data source for entity collection operations.
 */
export class UaiConnectionCollectionServerDataSource implements UmbCollectionDataSource<UaiConnectionItemModel> {
    #host: UmbControllerHost;

    constructor(host: UmbControllerHost) {
        this.#host = host;
    }

    /**
     * Gets all entities as collection items.
     */
    async getCollection(filter: UmbCollectionFilterModel) {
        const { data, error } = await tryExecuteAndNotify(
            this.#host,
            ConnectionsService.getConnections({
                query: {
                    skip: filter.skip ?? 0,
                    take: filter.take ?? 100,
                },
            })
        );

        if (error || !data) {
            return { error };
        }

        const items = data.items.map(UaiConnectionTypeMapper.toItemModel);

        return {
            data: {
                items,
                total: data.total,
            },
        };
    }
}
```

### 3.6 Collection Repository

**File:** `{entity}/repository/collection/{entity}-collection.repository.ts`

```typescript
import type { UmbCollectionFilterModel, UmbCollectionRepository } from "@umbraco-cms/backoffice/collection";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbRepositoryBase } from "@umbraco-cms/backoffice/repository";
import { UaiConnectionCollectionServerDataSource } from "./connection-collection.server.data-source.js";

/**
 * Repository for entity collection operations.
 */
export class UaiConnectionCollectionRepository extends UmbRepositoryBase implements UmbCollectionRepository {
    #collectionSource: UaiConnectionCollectionServerDataSource;

    constructor(host: UmbControllerHost) {
        super(host);
        this.#collectionSource = new UaiConnectionCollectionServerDataSource(host);
    }

    async requestCollection(filter: UmbCollectionFilterModel) {
        return this.#collectionSource.getCollection(filter);
    }
}

export { UaiConnectionCollectionRepository as api };
```

### 3.7 Collection Index

**File:** `{entity}/repository/collection/index.ts`

```typescript
export { UaiConnectionCollectionServerDataSource } from "./connection-collection.server.data-source.js";
export { UaiConnectionCollectionRepository } from "./connection-collection.repository.js";
```

### 3.8 Repository Manifests

**File:** `{entity}/repository/manifests.ts`

```typescript
import type { ManifestRepository, ManifestStore } from "@umbraco-cms/backoffice/extension-registry";
import {
    UAI_CONNECTION_DETAIL_REPOSITORY_ALIAS,
    UAI_CONNECTION_DETAIL_STORE_ALIAS,
    UAI_CONNECTION_COLLECTION_REPOSITORY_ALIAS,
} from "./constants.js";

export const connectionRepositoryManifests: Array<ManifestRepository | ManifestStore> = [
    {
        type: "repository",
        alias: UAI_CONNECTION_DETAIL_REPOSITORY_ALIAS,
        name: "Connection Detail Repository",
        api: () => import("./detail/connection-detail.repository.js"),
    },
    {
        type: "store",
        alias: UAI_CONNECTION_DETAIL_STORE_ALIAS,
        name: "Connection Detail Store",
        api: () => import("./detail/connection-detail.store.js"),
    },
    {
        type: "repository",
        alias: UAI_CONNECTION_COLLECTION_REPOSITORY_ALIAS,
        name: "Connection Collection Repository",
        api: () => import("./collection/connection-collection.repository.js"),
    },
];
```

### 3.9 Repository Index

**File:** `{entity}/repository/index.ts`

```typescript
export * from "./detail/index.js";
export * from "./collection/index.js";
export { connectionRepositoryManifests } from "./manifests.js";
```

---

## 4. CommandStore Pattern

The CommandStore pattern enables optimistic UI updates with command replay when the model refreshes from the server.

### 4.1 Command Interface

**File:** `core/command/command.base.ts`

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

### 4.2 Command Store

**File:** `core/command/command.store.ts`

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

### 4.3 Partial Update Command

**File:** `core/command/implement/partial-update.command.ts`

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

**Key features:**
- `correlationId` allows replacing commands for the same field (e.g., typing in name field replaces previous name command)
- `mute()/unmute()` prevents command accumulation during save operations
- `reset()` clears commands after successful save
- Commands are replayed on model refresh to preserve unsaved changes

---

## 5. Workspace Context

### 5.1 Context Token (Separate File)

**File:** `{entity}/workspace/{entity}/{entity}-workspace.context-token.ts`

```typescript
import type { UaiConnectionWorkspaceContext } from "./connection-workspace.context.js";
import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";
import type { UmbSubmittableWorkspaceContext } from "@umbraco-cms/backoffice/workspace";
import { UAI_CONNECTION_ENTITY_TYPE } from "../../constants.js";

export const UAI_CONNECTION_WORKSPACE_CONTEXT = new UmbContextToken<
    UmbSubmittableWorkspaceContext,
    UaiConnectionWorkspaceContext
>(
    "UmbWorkspaceContext",
    undefined,
    (context): context is UaiConnectionWorkspaceContext =>
        context.getEntityType?.() === UAI_CONNECTION_ENTITY_TYPE
);
```

### 5.2 Workspace Context

**File:** `{entity}/workspace/{entity}/{entity}-workspace.context.ts`

```typescript
import type { UmbRoutableWorkspaceContext } from "@umbraco-cms/backoffice/workspace";
import {
    UmbWorkspaceRouteManager,
    UmbSubmittableWorkspaceContextBase,
    UmbWorkspaceIsNewRedirectController,
    UmbWorkspaceIsNewRedirectControllerAlias,
} from "@umbraco-cms/backoffice/workspace";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbBasicState, UmbObjectState } from "@umbraco-cms/backoffice/observable-api";
import { UmbEntityContext } from "@umbraco-cms/backoffice/entity";
import { UmbValidationContext } from "@umbraco-cms/backoffice/validation";
import { UaiConnectionDetailRepository } from "../../repository/detail/connection-detail.repository.js";
import { UAI_CONNECTION_WORKSPACE_ALIAS, UAI_CONNECTION_ENTITY_TYPE } from "../../constants.js";
import type { UaiConnectionDetailModel } from "../../types.js";
import type { UaiCommand } from "../../../core/command/command.base.js";
import { UaiCommandStore } from "../../../core/command/command.store.js";
import { UaiConnectionWorkspaceEditorElement } from "./connection-workspace-editor.element.js";

/**
 * Workspace context for editing entities.
 * Handles CRUD operations, state management, and command tracking.
 */
export class UaiConnectionWorkspaceContext
    extends UmbSubmittableWorkspaceContextBase<UaiConnectionDetailModel>
    implements UmbRoutableWorkspaceContext
{
    readonly routes = new UmbWorkspaceRouteManager(this);

    #unique = new UmbBasicState<string | undefined>(undefined);
    readonly unique = this.#unique.asObservable();

    #model = new UmbObjectState<UaiConnectionDetailModel | undefined>(undefined);
    readonly model = this.#model.asObservable();

    #repository: UaiConnectionDetailRepository;
    #commandStore = new UaiCommandStore();
    #entityContext = new UmbEntityContext(this);

    constructor(host: UmbControllerHost) {
        super(host, UAI_CONNECTION_WORKSPACE_ALIAS);

        this.#repository = new UaiConnectionDetailRepository(this);
        this.addValidationContext(new UmbValidationContext(this));

        this.#entityContext.setEntityType(UAI_CONNECTION_ENTITY_TYPE);
        this.observe(this.unique, (unique) => this.#entityContext.setUnique(unique ?? null));

        this.routes.setRoutes([
            {
                path: "create",
                component: UaiConnectionWorkspaceEditorElement,
                setup: async () => {
                    await this.scaffold();

                    new UmbWorkspaceIsNewRedirectController(
                        this,
                        this,
                        this.getHostElement().shadowRoot!.querySelector("umb-router-slot")!
                    );
                },
            },
            {
                path: "edit/:unique",
                component: UaiConnectionWorkspaceEditorElement,
                setup: (_component, info) => {
                    this.removeUmbControllerByAlias(UmbWorkspaceIsNewRedirectControllerAlias);
                    this.load(info.match.params.unique);
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

    /**
     * Creates a scaffold for a new entity.
     */
    async scaffold(providerId?: string) {
        this.resetState();
        const { data } = await this.#repository.createScaffold({ providerId });
        if (data) {
            this.#model.setValue(data);
            this.setIsNew(true);
        }
    }

    /**
     * Loads an existing entity by ID.
     */
    async load(id: string) {
        this.resetState();
        const { data, asObservable } = await this.#repository.requestByUnique(id);

        if (asObservable) {
            this.observe(
                asObservable(),
                (model) => {
                    if (model) {
                        this.#unique.setValue(model.unique);
                        const newModel = structuredClone(model);
                        // Replay any pending commands
                        this.#commandStore.getAll().forEach((command) => command.execute(newModel));
                        this.#model.setValue(newModel);
                        this.setIsNew(false);
                    }
                },
                "_observeModel"
            );
        }

        return data;
    }

    /**
     * Handles a command to update the model.
     * Commands are tracked for replay after model refresh.
     */
    handleCommand(command: UaiCommand) {
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
        return UAI_CONNECTION_ENTITY_TYPE;
    }

    /**
     * Saves the entity (create or update).
     */
    async submit() {
        const model = this.#model.getValue();
        if (!model) return;

        // Mute command store during submit
        this.#commandStore.mute();

        try {
            if (this.getIsNew()) {
                const { data, error } = await this.#repository.create(model);

                if (error) {
                    throw error;
                }

                if (data) {
                    this.#unique.setValue(data.unique);
                    this.#model.setValue(data);
                }
            } else {
                const { error } = await this.#repository.save(model);

                if (error) {
                    throw error;
                }
            }

            this.#commandStore.reset();
            this.setIsNew(false);
        } finally {
            this.#commandStore.unmute();
        }
    }
}

export { UaiConnectionWorkspaceContext as api };
```

---

## 6. URL Path Patterns

### 6.1 Entity Workspace Paths

**File:** `{entity}/workspace/{entity}/paths.ts`

```typescript
import { UMB_WORKSPACE_PATH_PATTERN } from "@umbraco-cms/backoffice/workspace";
import { UmbPathPattern } from "@umbraco-cms/backoffice/router";
import { UAI_CONNECTION_ENTITY_TYPE } from "../../constants.js";
import { UMB_SETTINGS_SECTION_PATHNAME } from "@umbraco-cms/backoffice/settings";

export const UAI_CONNECTION_WORKSPACE_PATH = UMB_WORKSPACE_PATH_PATTERN.generateAbsolute({
    sectionName: UMB_SETTINGS_SECTION_PATHNAME,
    entityType: UAI_CONNECTION_ENTITY_TYPE,
});

export const UAI_CREATE_CONNECTION_WORKSPACE_PATH = `${UAI_CONNECTION_WORKSPACE_PATH}/create`;

export const UAI_EDIT_CONNECTION_WORKSPACE_PATH_PATTERN = new UmbPathPattern<{ unique: string }>(
    'edit/:unique',
    UAI_CONNECTION_WORKSPACE_PATH,
);
```

### 6.2 Root Workspace Path

**File:** `{entity}/workspace/{entity}-root/paths.ts`

```typescript
import { UMB_WORKSPACE_PATH_PATTERN } from "@umbraco-cms/backoffice/workspace";
import { UAI_CONNECTION_ROOT_ENTITY_TYPE } from "../../constants.js";
import { UMB_SETTINGS_SECTION_PATHNAME } from "@umbraco-cms/backoffice/settings";

export const UAI_CONNECTION_ROOT_WORKSPACE_PATH = UMB_WORKSPACE_PATH_PATTERN.generateAbsolute({
    sectionName: UMB_SETTINGS_SECTION_PATHNAME,
    entityType: UAI_CONNECTION_ROOT_ENTITY_TYPE,
});
```

### 6.3 Root Workspace Index

**File:** `{entity}/workspace/{entity}-root/index.ts`

```typescript
export * from "./paths.js";
```

**Usage in components:**

```typescript
// Navigation link in editor
href=${UAI_CONNECTION_ROOT_WORKSPACE_PATH}

// Edit link with dynamic unique
href=${UAI_EDIT_CONNECTION_WORKSPACE_PATH_PATTERN.generateAbsolute({ unique: item.unique })}

// Create link
href=${UAI_CREATE_CONNECTION_WORKSPACE_PATH}
```

---

## 7. Workspace Editor Element

**File:** `{entity}/workspace/{entity}/{entity}-workspace-editor.element.ts`

```typescript
import { css, html, customElement, state, when } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import type { UUIInputElement, UUIInputEvent } from "@umbraco-cms/backoffice/external/uui";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { UAI_CONNECTION_WORKSPACE_CONTEXT } from "./connection-workspace.context-token.js";
import { UAI_CONNECTION_WORKSPACE_ALIAS } from "../../constants.js";
import type { UaiConnectionDetailModel } from "../../types.js";
import { UaiPartialUpdateCommand } from "../../../core/command/implement/partial-update.command.js";
import { UAI_CONNECTION_ROOT_WORKSPACE_PATH } from "../connection-root/paths.js";

@customElement("uai-connection-workspace-editor")
export class UaiConnectionWorkspaceEditorElement extends UmbLitElement {
    #workspaceContext?: typeof UAI_CONNECTION_WORKSPACE_CONTEXT.TYPE;

    @state()
    private _model?: UaiConnectionDetailModel;

    @state()
    private _isNew?: boolean;

    @state()
    private _aliasLocked = true;

    constructor() {
        super();

        this.consumeContext(UAI_CONNECTION_WORKSPACE_CONTEXT, (context) => {
            if (!context) return;
            this.#workspaceContext = context;
            this.observe(context.model, (model) => {
                this._model = model;
            });
            this.observe(context.isNew, (isNew) => {
                this._isNew = isNew;
                if (isNew) {
                    requestAnimationFrame(() => {
                        (this.shadowRoot?.querySelector("#name") as HTMLElement)?.focus();
                    });
                }
            });
        });
    }

    #onNameChange(event: UUIInputEvent) {
        event.stopPropagation();
        const target = event.composedPath()[0] as UUIInputElement;
        const name = target.value.toString();

        // If alias is locked and creating new, generate alias from name
        if (this._aliasLocked && this._isNew) {
            const alias = this.#generateAlias(name);
            this.#workspaceContext?.handleCommand(
                new UaiPartialUpdateCommand<UaiConnectionDetailModel>({ name, alias }, "name-alias")
            );
        } else {
            this.#workspaceContext?.handleCommand(
                new UaiPartialUpdateCommand<UaiConnectionDetailModel>({ name }, "name")
            );
        }
    }

    #onAliasChange(event: UUIInputEvent) {
        event.stopPropagation();
        const target = event.composedPath()[0] as UUIInputElement;
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiConnectionDetailModel>({ alias: target.value.toString() }, "alias")
        );
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
        if (!this._model) return html`<uui-loader></uui-loader>`;

        return html`
            <umb-workspace-editor alias="${UAI_CONNECTION_WORKSPACE_ALIAS}">
                <div id="header" slot="header">
                    <uui-button
                        href=${UAI_CONNECTION_ROOT_WORKSPACE_PATH}
                        label="Back to connections"
                        compact
                    >
                        <uui-icon name="icon-arrow-left"></uui-icon>
                    </uui-button>
                    <uui-input
                        id="name"
                        .value=${this._model.name}
                        @input="${this.#onNameChange}"
                        label="Name"
                        placeholder="Enter connection name"
                    >
                        <uui-input-lock
                            slot="append"
                            id="alias"
                            name="alias"
                            label="Alias"
                            placeholder="Enter alias"
                            .value=${this._model.alias}
                            ?auto-width=${!!this._model.name}
                            ?locked=${this._aliasLocked}
                            ?readonly=${this._aliasLocked || !this._isNew}
                            @input=${this.#onAliasChange}
                            @lock-change=${this.#onToggleAliasLock}
                        ></uui-input-lock>
                    </uui-input>
                </div>

                ${when(
                    !this._isNew && this._model,
                    () => html`<umb-workspace-entity-action-menu slot="action-menu"></umb-workspace-entity-action-menu>`
                )}

                <div slot="footer-info" id="footer">
                    <a href=${UAI_CONNECTION_ROOT_WORKSPACE_PATH}>Connections</a>
                    / ${this._model.name || "Untitled"}
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
                gap: var(--uui-size-space-2);
            }

            #name {
                width: 100%;
                flex: 1 1 auto;
                align-items: center;
            }

            #footer {
                padding: 0 var(--uui-size-layout-1);
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

---

## 8. Workspace Views

**File:** `{entity}/workspace/{entity}/views/{entity}-details-workspace-view.element.ts`

```typescript
import { css, html, customElement, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import type { UaiConnectionDetailModel } from "../../../types.js";
import { UaiPartialUpdateCommand } from "../../../../core/index.js";
import { UAI_CONNECTION_WORKSPACE_CONTEXT } from "../connection-workspace.context-token.js";

/**
 * Workspace view for entity details.
 */
@customElement("uai-connection-details-workspace-view")
export class UaiConnectionDetailsWorkspaceViewElement extends UmbLitElement {
    #workspaceContext?: typeof UAI_CONNECTION_WORKSPACE_CONTEXT.TYPE;

    @state()
    private _model?: UaiConnectionDetailModel;

    @state()
    private _isNew?: boolean;

    constructor() {
        super();
        this.consumeContext(UAI_CONNECTION_WORKSPACE_CONTEXT, (context) => {
            if (context) {
                this.#workspaceContext = context;
                this.observe(context.model, (model) => (this._model = model));
                this.observe(context.isNew, (isNew) => (this._isNew = isNew));
            }
        });
    }

    #onProviderChange(event: Event) {
        event.stopPropagation();
        const target = event.target as HTMLInputElement;
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiConnectionDetailModel>({ providerId: target.value }, "providerId")
        );
    }

    #onActiveChange(event: Event) {
        event.stopPropagation();
        const target = event.target as HTMLInputElement;
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiConnectionDetailModel>({ isActive: target.checked }, "isActive")
        );
    }

    render() {
        if (!this._model) return html`<uui-loader></uui-loader>`;

        return html`
            <uui-box headline="Connection Details">
                <umb-property-layout label="Provider" description="AI provider for this connection">
                    <uui-input
                        slot="editor"
                        .value=${this._model.providerId}
                        @change=${this.#onProviderChange}
                        placeholder="e.g., openai"
                        ?disabled=${!this._isNew}
                    ></uui-input>
                </umb-property-layout>

                <umb-property-layout label="Active" description="Enable or disable this connection">
                    <uui-toggle slot="editor" .checked=${this._model.isActive} @change=${this.#onActiveChange}></uui-toggle>
                </umb-property-layout>
            </uui-box>
        `;
    }

    static styles = [
        UmbTextStyles,
        css`
            :host {
                display: block;
                padding: var(--uui-size-layout-1);
            }

            uui-box {
                margin-bottom: var(--uui-size-layout-1);
            }
        `,
    ];
}

export default UaiConnectionDetailsWorkspaceViewElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-connection-details-workspace-view": UaiConnectionDetailsWorkspaceViewElement;
    }
}
```

---

## 9. Collection Views

### 9.1 Table Collection View

**File:** `{entity}/collection/views/table/{entity}-table-collection-view.element.ts`

```typescript
import { html, customElement, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import type { UmbTableColumn, UmbTableItem } from "@umbraco-cms/backoffice/components";
import { UMB_COLLECTION_CONTEXT } from "@umbraco-cms/backoffice/collection";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import type { UaiConnectionItemModel } from "../../../types.js";
import { UAI_CONNECTION_ICON } from "../../../constants.js";
import { UAI_EDIT_CONNECTION_WORKSPACE_PATH_PATTERN } from "../../../workspace/connection/paths.js";

/**
 * Table view for the entity collection.
 */
@customElement("uai-connection-table-collection-view")
export class UaiConnectionTableCollectionViewElement extends UmbLitElement {
    @state()
    private _items: UmbTableItem[] = [];

    private _columns: UmbTableColumn[] = [
        { name: "Name", alias: "name" },
        { name: "Provider", alias: "provider" },
        { name: "Status", alias: "status" },
    ];

    constructor() {
        super();
        this.consumeContext(UMB_COLLECTION_CONTEXT, (ctx) => {
            if (ctx) {
                this.observe(ctx.items, (items) => this.#createTableItems(items as UaiConnectionItemModel[]));
            }
        });
    }

    #createTableItems(items: UaiConnectionItemModel[]) {
        this._items = items.map((item) => ({
            id: item.unique,
            icon: UAI_CONNECTION_ICON,
            data: [
                {
                    columnAlias: "name",
                    value: html`<a
                        href=${UAI_EDIT_CONNECTION_WORKSPACE_PATH_PATTERN.generateAbsolute({ unique: item.unique })}
                        >${item.name}</a
                    >`,
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

    static styles = [UmbTextStyles];
}

export default UaiConnectionTableCollectionViewElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-connection-table-collection-view": UaiConnectionTableCollectionViewElement;
    }
}
```

### 9.2 Collection with Search

To add search functionality to a collection, you need:
1. A custom collection element that renders a toolbar with filter field
2. Backend API support for the `filter` parameter
3. Data source that passes the filter to the API

#### Custom Collection Element

**File:** `{entity}/collection/{entity}-collection.element.ts`

```typescript
import { html, customElement } from "@umbraco-cms/backoffice/external/lit";
import { UmbCollectionDefaultElement } from "@umbraco-cms/backoffice/collection";

/**
 * Custom collection element with search header.
 */
@customElement("uai-connection-collection")
export class UaiConnectionCollectionElement extends UmbCollectionDefaultElement {
    protected override renderToolbar() {
        return html`
            <umb-collection-toolbar slot="header">
                <umb-collection-filter-field></umb-collection-filter-field>
            </umb-collection-toolbar>
        `;
    }
}

export { UaiConnectionCollectionElement as element };

declare global {
    interface HTMLElementTagNameMap {
        "uai-connection-collection": UaiConnectionCollectionElement;
    }
}
```

**Key points:**
- Extend `UmbCollectionDefaultElement`
- Override `renderToolbar()` to customize the header
- Use `<umb-collection-toolbar>` as the wrapper with `slot="header"`
- Include `<umb-collection-filter-field>` for the search input
- The filter field automatically debounces (500ms) and calls `setFilter({ filter })` on the collection context

#### Collection Server Data Source with Filter

**File:** `{entity}/repository/collection/{entity}-collection.server.data-source.ts`

```typescript
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import type { UmbCollectionDataSource, UmbCollectionFilterModel } from "@umbraco-cms/backoffice/collection";
import { tryExecuteAndNotify } from "@umbraco-cms/backoffice/resources";
import { ConnectionsService } from "../../../api/sdk.gen.js";
import { UaiConnectionTypeMapper } from "../../type-mapper.js";
import type { UaiConnectionItemModel } from "../../types.js";

export class UaiConnectionCollectionServerDataSource implements UmbCollectionDataSource<UaiConnectionItemModel> {
    #host: UmbControllerHost;

    constructor(host: UmbControllerHost) {
        this.#host = host;
    }

    async getCollection(filter: UmbCollectionFilterModel) {
        const { data, error } = await tryExecuteAndNotify(
            this.#host,
            ConnectionsService.getConnections({
                query: {
                    filter: filter.filter,  // Pass the search term to API
                    skip: filter.skip ?? 0,
                    take: filter.take ?? 100,
                },
            })
        );

        if (error || !data) {
            return { error };
        }

        const items = data.items.map(UaiConnectionTypeMapper.toItemModel);

        return {
            data: {
                items,
                total: data.total,
            },
        };
    }
}
```

**Important:** The `filter.filter` property contains the search term entered by the user. Pass this to your backend API for server-side filtering.

### 9.3 Collection Manifests

**File:** `{entity}/collection/manifests.ts`

```typescript
import type { ManifestCollection, ManifestCollectionView, ManifestCollectionAction } from "@umbraco-cms/backoffice/collection";
import { UAI_CONNECTION_COLLECTION_ALIAS } from "./constants.js";
import { UAI_CONNECTION_COLLECTION_REPOSITORY_ALIAS } from "../repository/constants.js";
import { connectionCollectionActionManifests } from "./action/manifests.js";

export const connectionCollectionManifests: Array<ManifestCollection | ManifestCollectionView | ManifestCollectionAction> = [
    {
        type: "collection",
        kind: "default",
        alias: UAI_CONNECTION_COLLECTION_ALIAS,
        name: "Connection Collection",
        element: () => import("./connection-collection.element.js"),  // Custom element with search
        meta: {
            repositoryAlias: UAI_CONNECTION_COLLECTION_REPOSITORY_ALIAS,
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
        conditions: [{ alias: "Umb.Condition.CollectionAlias", match: UAI_CONNECTION_COLLECTION_ALIAS }],
    },
    ...connectionCollectionActionManifests,
];
```

**Note:** To enable search, add the `element` property pointing to your custom collection element while keeping `kind: "default"`.

### 9.4 Collection Action (Create Button)

**File:** `{entity}/collection/action/manifests.ts`

```typescript
import type { ManifestCollectionAction } from "@umbraco-cms/backoffice/collection";
import { UAI_CONNECTION_COLLECTION_ALIAS } from "../../constants.js";
import { UAI_CREATE_CONNECTION_WORKSPACE_PATH } from "../../workspace/connection/paths.js";

export const connectionCollectionActionManifests: ManifestCollectionAction[] = [
    {
        type: "collectionAction",
        kind: "button",
        alias: "UmbracoAi.CollectionAction.Connection.Create",
        name: "Create Connection",
        meta: {
            label: "Create",
            href: UAI_CREATE_CONNECTION_WORKSPACE_PATH,
        },
        conditions: [{ alias: "Umb.Condition.CollectionAlias", match: UAI_CONNECTION_COLLECTION_ALIAS }],
    },
];
```

---

## 10. Menu Integration

**File:** `{entity}/menu/manifests.ts`

```typescript
import type { ManifestMenuItem } from "@umbraco-cms/backoffice/menu";
import { UAI_CONNECTION_ROOT_ENTITY_TYPE, UAI_CONNECTION_ICON } from "../constants.js";

export const connectionMenuManifests: ManifestMenuItem[] = [
    {
        type: "menuItem",
        alias: "UmbracoAi.MenuItem.Connections",
        name: "Connections Menu Item",
        weight: 100,
        meta: {
            label: "Connections",
            icon: UAI_CONNECTION_ICON,
            entityType: UAI_CONNECTION_ROOT_ENTITY_TYPE,
            menus: ["UmbracoAi.Menu.Settings"],
        },
    },
];
```

---

## 11. Entity Actions (Menu Item "+" Button)

Entity actions add action buttons (like "+") next to menu items in the sidebar. When a sidebar uses `kind: "menuWithEntityActions"`, it automatically discovers entity actions that match the menu item's `entityType` and renders them as clickable icons.

### 11.1 Entity Action Implementation

**File:** `{entity}/entity-actions/{entity}-create.action.ts`

```typescript
import type { UmbEntityActionArgs } from "@umbraco-cms/backoffice/entity-action";
import { UmbEntityActionBase } from "@umbraco-cms/backoffice/entity-action";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UAI_CREATE_CONNECTION_WORKSPACE_PATH } from "../workspace/connection/paths.js";

/**
 * Entity action for creating a new connection.
 * Used by the menu item to show a "+" button.
 */
export class UaiConnectionCreateEntityAction extends UmbEntityActionBase<never> {
    constructor(host: UmbControllerHost, args: UmbEntityActionArgs<never>) {
        super(host, args);
    }

    override async execute() {
        history.pushState(null, "", UAI_CREATE_CONNECTION_WORKSPACE_PATH);
    }
}

export { UaiConnectionCreateEntityAction as api };
```

### 11.2 Entity Action Manifest

**File:** `{entity}/entity-actions/manifests.ts`

```typescript
import { UAI_CONNECTION_ROOT_ENTITY_TYPE } from "../constants.js";

export const connectionEntityActionManifests: Array<UmbExtensionManifest> = [
    {
        type: "entityAction",
        kind: "default",
        alias: "UmbracoAi.EntityAction.Connection.Create",
        name: "Create Connection Entity Action",
        weight: 1200,
        api: () => import("./connection-create.action.js"),
        forEntityTypes: [UAI_CONNECTION_ROOT_ENTITY_TYPE],
        meta: {
            icon: "icon-add",
            label: "Create",
            additionalOptions: true,
        },
    },
];
```

**Key properties:**
- `type: "entityAction"` - Registers as an entity action
- `kind: "default"` - Uses the default entity action behavior (custom API)
- `forEntityTypes` - Must include the root entity type (e.g., `uai:connection-root`) to appear next to the menu item
- `meta.icon` - Icon shown in the sidebar (typically `icon-add` for create actions)
- `meta.label` - Tooltip/label for the action
- `meta.additionalOptions` - When `true`, shows in the "more" menu if multiple actions exist
- `weight` - Higher weight = higher priority (appears first)

### 11.3 How Entity Actions Work with Menus

For the "+" button to appear next to a menu item:

1. **Sidebar must use `menuWithEntityActions` kind:**
   ```typescript
   {
       type: "sectionSidebarApp",
       kind: "menuWithEntityActions",  // Required!
       meta: {
           menu: "UmbracoAi.Menu.Settings",
       },
   }
   ```

2. **Menu item must have an `entityType`:**
   ```typescript
   {
       type: "menuItem",
       meta: {
           entityType: UAI_CONNECTION_ROOT_ENTITY_TYPE,  // Required!
           // ...
       },
   }
   ```

3. **Entity action must target the same entity type:**
   ```typescript
   {
       type: "entityAction",
       forEntityTypes: [UAI_CONNECTION_ROOT_ENTITY_TYPE],  // Must match!
       // ...
   }
   ```

### 11.4 Alternative: `kind: 'create'` with Options

For entities with multiple create options (e.g., "Create Item" vs "Create Folder"), use `kind: 'create'` with `entityCreateOptionAction` manifests instead. This is more complex but supports presenting multiple creation choices to the user.

---

## 12. Root Workspace (Collection Container)

**File:** `{entity}/workspace/{entity}-root/manifests.ts`

```typescript
import {
    UAI_CONNECTION_ROOT_WORKSPACE_ALIAS,
    UAI_CONNECTION_ROOT_ENTITY_TYPE,
    UAI_CONNECTION_ICON,
    UAI_CONNECTION_COLLECTION_ALIAS,
} from "../../constants.js";
import { UMB_WORKSPACE_CONDITION_ALIAS } from "@umbraco-cms/backoffice/workspace";

export const manifests: Array<UmbExtensionManifest> = [
    {
        type: "workspace",
        kind: "default",
        alias: UAI_CONNECTION_ROOT_WORKSPACE_ALIAS,
        name: "Connection Root Workspace",
        meta: {
            entityType: UAI_CONNECTION_ROOT_ENTITY_TYPE,
            headline: "Connections",
        },
    },
    {
        type: "workspaceView",
        kind: "collection",
        alias: "UmbracoAi.WorkspaceView.ConnectionRoot.Collection",
        name: "Connection Root Collection Workspace View",
        meta: {
            label: "Collection",
            pathname: "collection",
            icon: UAI_CONNECTION_ICON,
            collectionAlias: UAI_CONNECTION_COLLECTION_ALIAS,
        },
        conditions: [
            {
                alias: UMB_WORKSPACE_CONDITION_ALIAS,
                match: UAI_CONNECTION_ROOT_WORKSPACE_ALIAS,
            },
        ],
    },
];
```

---

## 13. Workspace Manifests

**File:** `{entity}/workspace/{entity}/manifests.ts`

```typescript
import { UmbSubmitWorkspaceAction } from "@umbraco-cms/backoffice/workspace";
import { UAI_CONNECTION_WORKSPACE_ALIAS, UAI_CONNECTION_ENTITY_TYPE } from "../../constants.js";
import { UMB_WORKSPACE_CONDITION_ALIAS } from "@umbraco-cms/backoffice/workspace";

export const manifests: Array<UmbExtensionManifest> = [
    {
        type: "workspace",
        kind: "routable",
        alias: UAI_CONNECTION_WORKSPACE_ALIAS,
        name: "Connection Workspace",
        api: () => import("./connection-workspace.context.js"),
        meta: {
            entityType: UAI_CONNECTION_ENTITY_TYPE,
        },
    },
    {
        type: "workspaceView",
        alias: "UmbracoAi.Workspace.Connection.View.Details",
        name: "Connection Details Workspace View",
        js: () => import("./views/connection-details-workspace-view.element.js"),
        weight: 100,
        meta: {
            label: "Details",
            pathname: "details",
            icon: "icon-settings",
        },
        conditions: [
            {
                alias: UMB_WORKSPACE_CONDITION_ALIAS,
                match: UAI_CONNECTION_WORKSPACE_ALIAS,
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
                alias: UMB_WORKSPACE_CONDITION_ALIAS,
                match: UAI_CONNECTION_WORKSPACE_ALIAS,
            },
        ],
    },
];
```

---

## 14. Manifest Aggregation

### Workspace Manifests

**File:** `{entity}/workspace/manifests.ts`

```typescript
import { manifests as connectionManifests } from './connection/manifests.js';
import { manifests as connectionRootManifests } from './connection-root/manifests.js';

export const connectionWorkspaceManifests: Array<UmbExtensionManifest> = [
    ...connectionManifests,
    ...connectionRootManifests
];
```

### Workspace Index

**File:** `{entity}/workspace/index.ts`

```typescript
export { UaiConnectionWorkspaceContext } from "./connection/connection-workspace.context.js";
export { UAI_CONNECTION_WORKSPACE_CONTEXT } from "./connection/connection-workspace.context-token.js";
export { connectionWorkspaceManifests } from "./manifests.js";
export * from "./connection-root/index.js";
```

### Feature Manifests

**File:** `{entity}/manifests.ts`

```typescript
import { connectionCollectionManifests } from "./collection/manifests.js";
import { connectionMenuManifests } from "./menu/manifests.js";
import { connectionRepositoryManifests } from "./repository/manifests.js";
import { connectionWorkspaceManifests } from "./workspace/manifests.js";

export const connectionManifests = [
    ...connectionCollectionManifests,
    ...connectionMenuManifests,
    ...connectionRepositoryManifests,
    ...connectionWorkspaceManifests,
];
```

### Feature Index

**File:** `{entity}/index.ts`

```typescript
export * from "./constants.js";
export * from "./types.js";
export * from "./type-mapper.js";
export { connectionManifests } from "./manifests.js";
```

---

## 15. Implementation Checklist

### Core Infrastructure
- [ ] Define entity types (`entity.ts`)
- [ ] Create TypeScript interfaces (`types.ts`)
- [ ] Create type mapper (`type-mapper.ts`)
- [ ] Set up constants hierarchy (entity, workspace, repository, collection)

### Repository Layer
- [ ] Detail server data source (CRUD operations)
- [ ] Detail store with context token
- [ ] Detail repository extending `UmbDetailRepositoryBase`
- [ ] Collection server data source
- [ ] Collection repository implementing `UmbCollectionRepository`
- [ ] Repository manifests
- [ ] Index files for exports

### CommandStore (if not already present)
- [ ] Command interface and base class
- [ ] Command store with mute/unmute
- [ ] Partial update command implementation

### Workspace
- [ ] Workspace context token (separate file)
- [ ] Workspace context with CommandStore
- [ ] Workspace editor element
- [ ] Workspace views (tabbed content)
- [ ] Path patterns (create, edit)
- [ ] Workspace manifests

### Collection
- [ ] Table collection view
- [ ] Collection action (Create button)
- [ ] Collection manifests
- [ ] Custom collection element with search (optional)

### Entity Actions
- [ ] Create entity action class (extends `UmbEntityActionBase`)
- [ ] Entity action manifests (targets root entity type)

### Section Integration
- [ ] Menu item manifest (with `entityType` in meta)
- [ ] Root workspace manifests

### Final Assembly
- [ ] Feature manifests aggregation
- [ ] Feature index exports
- [ ] Bundle manifests update
- [ ] Build and test

---

## Key Patterns Summary

| Pattern | Purpose | Key Base Class |
|---------|---------|----------------|
| Data Source | API communication | `UmbDetailDataSource<T>` / `UmbCollectionDataSource<T>` |
| Store | Client-side cache | `UmbDetailStoreBase<T>` |
| Repository | Coordinates data source + store | `UmbDetailRepositoryBase<T>` |
| Workspace Context | Entity state management | `UmbSubmittableWorkspaceContextBase<T>` |
| CommandStore | Optimistic UI with replay | `UaiCommandStore` (custom) |
| Entity Action | Menu item actions ("+" button) | `UmbEntityActionBase<T>` |
| Path Pattern | Type-safe URL generation | `UmbPathPattern<T>` |

---

## Common Imports Reference

```typescript
// Controller/Context
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";

// Repository
import type { UmbDetailDataSource } from "@umbraco-cms/backoffice/repository";
import { UmbDetailRepositoryBase, UmbRepositoryBase } from "@umbraco-cms/backoffice/repository";
import { tryExecuteAndNotify } from "@umbraco-cms/backoffice/resources";

// Store
import { UmbDetailStoreBase } from "@umbraco-cms/backoffice/store";

// Workspace
import type { UmbRoutableWorkspaceContext, UmbSubmittableWorkspaceContext } from "@umbraco-cms/backoffice/workspace";
import {
    UmbSubmittableWorkspaceContextBase,
    UmbWorkspaceRouteManager,
    UmbSubmitWorkspaceAction,
    UmbWorkspaceIsNewRedirectController,
    UMB_WORKSPACE_CONDITION_ALIAS,
    UMB_WORKSPACE_PATH_PATTERN,
} from "@umbraco-cms/backoffice/workspace";

// Router
import { UmbPathPattern } from "@umbraco-cms/backoffice/router";

// Observable
import { UmbObjectState, UmbBasicState } from "@umbraco-cms/backoffice/observable-api";

// Entity
import { UmbEntityContext } from "@umbraco-cms/backoffice/entity";
import type { UmbEntityModel } from "@umbraco-cms/backoffice/entity";

// Entity Action
import type { UmbEntityActionArgs } from "@umbraco-cms/backoffice/entity-action";
import { UmbEntityActionBase } from "@umbraco-cms/backoffice/entity-action";

// Validation
import { UmbValidationContext } from "@umbraco-cms/backoffice/validation";

// Lit Elements
import { css, html, customElement, state, when } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";

// Collection
import type { UmbCollectionRepository, UmbCollectionDataSource, UmbCollectionFilterModel } from "@umbraco-cms/backoffice/collection";
import { UMB_COLLECTION_CONTEXT } from "@umbraco-cms/backoffice/collection";

// Components
import type { UmbTableColumn, UmbTableItem } from "@umbraco-cms/backoffice/components";

// Settings Section
import { UMB_SETTINGS_SECTION_PATHNAME } from "@umbraco-cms/backoffice/settings";
```
