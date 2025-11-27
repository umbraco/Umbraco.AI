# Connection Provider Selection Enhancement Plan

## Overview

Update the connection creation flow to require selecting a provider **first**, before opening the create workspace. Once selected, the provider becomes locked and cannot be changed for that connection. This follows the pattern used by Umbraco CMS Content creation (dynamic options from API).

## Existing Infrastructure

- **Provider API already exists**: `GET /ai/management/api/v1.0/providers` returns `ProviderItemResponseModel` with `Id`, `Name`, `Capabilities`
- **Generated TypeScript client**: Already has `ProviderService.getProvider()` available

## Architecture Pattern

Following the **Content create pattern** (dynamic API-based options) rather than Data Type pattern (static manifests):

1. **Entity Action** triggers create flow
2. **Modal** fetches available providers from API and displays them
3. **Selection** navigates to workspace with provider in URL
4. **Workspace** scaffolds with provider pre-set and locked

## Current vs Target State

| Aspect | Current | Target |
|--------|---------|--------|
| Create button | Simple link to `/create` | Dropdown with provider list |
| Provider selection | Text input in workspace | Pre-selected via URL param |
| Provider editability | Disabled after save | Never editable (determined by URL) |
| User experience | Manual entry, no discovery | Visual picker, clear options |

---

## Implementation Plan

### Phase 1: Frontend - Provider Item Repository

**Goal:** Create frontend infrastructure to fetch providers from existing API.

**New Files:**
```
src/Umbraco.Ai.Web.StaticAssets/Client/src/provider/
├── types.ts
├── constants.ts
├── repository/
│   ├── item/
│   │   ├── provider-item.repository.ts
│   │   ├── provider-item.server.data-source.ts
│   │   └── index.ts
│   └── manifests.ts
└── manifests.ts
```

Uses existing `ProviderService.getProvider()` from generated client.

#### 1.1 Create Provider Types

**File:** `src/Umbraco.Ai.Web.StaticAssets/Client/src/provider/types.ts`

```typescript
export interface UaiProviderItemModel {
    id: string;
    name: string;
    capabilities: string[];
}
```

#### 1.2 Create Provider Item Repository

**File:** `src/Umbraco.Ai.Web.StaticAssets/Client/src/provider/repository/item/provider-item.repository.ts`

- Fetches provider list from existing API via generated client
- No store needed initially (simple fetch)

---

### Phase 2: Frontend - Connection Create Options Modal

**Goal:** Create modal that fetches providers and displays selection list (like Document create modal).

**New Files:**
```
src/Umbraco.Ai.Web.StaticAssets/Client/src/connection/modals/
├── create-options/
│   ├── connection-create-options-modal.token.ts
│   ├── connection-create-options-modal.element.ts
│   └── index.ts
└── manifests.ts
```

#### 2.1 Create Modal Token

**File:** `src/Umbraco.Ai.Web.StaticAssets/Client/src/connection/modals/create-options/connection-create-options-modal.token.ts`

```typescript
import { UmbModalToken } from "@umbraco-cms/backoffice/modal";

export interface UaiConnectionCreateOptionsModalData {
    headline?: string;
}

export interface UaiConnectionCreateOptionsModalValue {
    providerAlias: string;
}

export const UAI_CONNECTION_CREATE_OPTIONS_MODAL = new UmbModalToken<
    UaiConnectionCreateOptionsModalData,
    UaiConnectionCreateOptionsModalValue
>("UmbracoAi.Modal.Connection.CreateOptions", {
    modal: {
        type: "sidebar",
        size: "small",
    },
});
```

#### 2.2 Create Modal Element

**File:** `src/Umbraco.Ai.Web.StaticAssets/Client/src/connection/modals/create-options/connection-create-options-modal.element.ts`

```typescript
@customElement("uai-connection-create-options-modal")
export class UaiConnectionCreateOptionsModalElement extends UmbModalBaseElement<
    UaiConnectionCreateOptionsModalData,
    UaiConnectionCreateOptionsModalValue
> {
    #providerRepository = new UaiProviderItemRepository(this);

    @state()
    private _providers: UaiProviderItemModel[] = [];

    override async firstUpdated() {
        await this.#loadProviders();
    }

    async #loadProviders() {
        const { data } = await this.#providerRepository.requestItems();
        this._providers = data ?? [];
    }

    #onSelect(providerAlias: string) {
        this.value = { providerAlias };
        this.modalContext?.submit();
    }

    override render() {
        return html`
            <umb-body-layout headline=${this.data?.headline ?? "Select Provider"}>
                <uui-box>
                    <uui-ref-list>
                        ${this._providers.map(provider => html`
                            <uui-ref-node
                                name=${provider.name}
                                detail=${provider.id}
                                @open=${() => this.#onSelect(provider.id)}
                                selectable
                            >
                                <umb-icon slot="icon" name="icon-cloud"></umb-icon>
                            </uui-ref-node>
                        `)}
                    </uui-ref-list>
                </uui-box>
                <uui-button slot="actions" @click=${() => this.modalContext?.reject()}>
                    Cancel
                </uui-button>
            </umb-body-layout>
        `;
    }
}
```

#### 2.3 Register Modal Manifest

**File:** `src/Umbraco.Ai.Web.StaticAssets/Client/src/connection/modals/manifests.ts`

```typescript
export const manifests: Array<ManifestModal> = [
    {
        type: "modal",
        alias: "UmbracoAi.Modal.Connection.CreateOptions",
        name: "Connection Create Options Modal",
        element: () => import("./create-options/connection-create-options-modal.element.js"),
    },
];
```

---

### Phase 3: Frontend - Update Routing

**Goal:** Add provider alias to the create workspace URL pattern.

#### 3.1 Update Path Pattern

**File:** `src/Umbraco.Ai.Web.StaticAssets/Client/src/connection/workspace/connection/paths.ts`

```typescript
export const UAI_CREATE_CONNECTION_WORKSPACE_PATH_PATTERN = new UmbPathPattern<{
    providerAlias: string;
}>("create/:providerAlias", UAI_CONNECTION_WORKSPACE_PATH);
```

#### 3.2 Update Workspace Context Routes

**File:** `src/Umbraco.Ai.Web.StaticAssets/Client/src/connection/workspace/connection/connection-workspace.context.ts`

```typescript
this.routes.setRoutes([
    {
        path: "create/:providerAlias",
        component: UaiConnectionWorkspaceEditorElement,
        setup: async (_component, info) => {
            const providerAlias = info.match.params.providerAlias;
            await this.scaffold(providerAlias);
        },
    },
    {
        path: "edit/:unique",
        component: UaiConnectionWorkspaceEditorElement,
        setup: async (_component, info) => {
            const unique = info.match.params.unique;
            await this.load(unique);
        },
    },
]);
```

---

### Phase 4: Frontend - Collection View Dropdown Button

**Goal:** Replace the simple "Create" button with a dropdown showing available providers.

#### 4.1 Create Custom Collection Action Element

**File:** `src/Umbraco.Ai.Web.StaticAssets/Client/src/connection/collection/action/connection-create-collection-action.element.ts`

```typescript
@customElement("uai-connection-create-collection-action")
export class UaiConnectionCreateCollectionActionElement extends UmbLitElement {
    #providerRepository = new UaiProviderItemRepository(this);

    @state() _providers: UaiProviderItemModel[] = [];
    @state() _popoverOpen = false;

    override async connectedCallback() {
        super.connectedCallback();
        const { data } = await this.#providerRepository.requestItems();
        this._providers = data ?? [];
    }

    #onSelect(providerId: string) {
        this._popoverOpen = false;
        const path = UAI_CREATE_CONNECTION_WORKSPACE_PATH_PATTERN.generateAbsolute({
            providerAlias: providerId,
        });
        history.pushState(null, "", path);
    }

    override render() {
        return html`
            <uui-button popovertarget="create-popover">
                Create <uui-symbol-expand .open=${this._popoverOpen}></uui-symbol-expand>
            </uui-button>
            <uui-popover-container id="create-popover" placement="bottom-end">
                <umb-popover-layout>
                    ${this._providers.map(p => html`
                        <uui-menu-item label=${p.name} @click=${() => this.#onSelect(p.id)}>
                            <umb-icon slot="icon" name="icon-cloud"></umb-icon>
                        </uui-menu-item>
                    `)}
                </umb-popover-layout>
            </uui-popover-container>
        `;
    }
}
```

#### 4.2 Update Collection Action Manifest

**File:** `src/Umbraco.Ai.Web.StaticAssets/Client/src/connection/collection/action/manifests.ts`

```typescript
{
    type: "collectionAction",
    alias: "UmbracoAi.CollectionAction.Connection.Create",
    name: "Create Connection",
    element: () => import("./connection-create-collection-action.element.js"),
}
```

---

### Phase 5: Frontend - Entity Action Opens Modal

**Goal:** Update the "+" entity action to open provider selection modal first.

#### 5.1 Update Entity Action

**File:** `src/Umbraco.Ai.Web.StaticAssets/Client/src/connection/entity-actions/connection-create.action.ts`

```typescript
export class UaiConnectionCreateEntityAction extends UmbEntityActionBase<never> {
    override async execute() {
        const modalManager = await this.getContext(UMB_MODAL_MANAGER_CONTEXT);

        const result = await modalManager.open(this, UAI_CONNECTION_CREATE_OPTIONS_MODAL, {
            data: { headline: "Select AI Provider" },
        }).onSubmit().catch(() => undefined);

        if (!result?.providerAlias) return;

        const path = UAI_CREATE_CONNECTION_WORKSPACE_PATH_PATTERN.generateAbsolute({
            providerAlias: result.providerAlias,
        });
        history.pushState(null, "", path);
    }
}
```

---

### Phase 6: Frontend - Update Details View

**Goal:** Show provider as read-only display (not editable input) since it's now determined by URL.

#### 6.1 Update Details Workspace View

**File:** `src/Umbraco.Ai.Web.StaticAssets/Client/src/connection/workspace/connection/views/connection-details-workspace-view.element.ts`

Replace provider text input with read-only display:

```typescript
<umb-property-layout label="Provider">
    <div slot="editor" class="provider-display">
        <umb-icon name="icon-cloud"></umb-icon>
        <strong>${this._providerName}</strong>
        <small>(${this._model.providerId})</small>
    </div>
</umb-property-layout>
```

Add provider name lookup:

```typescript
@state() private _providerName?: string;

async #loadProviderDetails() {
    if (!this._model?.providerId) return;
    const { data } = await this.#providerRepository.requestItems();
    const provider = data?.find(p => p.id === this._model.providerId);
    if (provider) {
        this._providerName = provider.name;
    }
}
```

---

## File Summary

### New Files (8)
| File | Purpose |
|------|---------|
| `provider/types.ts` | Provider item model type |
| `provider/constants.ts` | Entity type constants |
| `provider/repository/item/provider-item.repository.ts` | Fetch providers |
| `provider/repository/item/provider-item.server.data-source.ts` | API data source |
| `provider/repository/manifests.ts` | Repository registration |
| `provider/manifests.ts` | Provider module manifests |
| `connection/modals/create-options/connection-create-options-modal.token.ts` | Modal token |
| `connection/modals/create-options/connection-create-options-modal.element.ts` | Modal UI |
| `connection/collection/action/connection-create-collection-action.element.ts` | Dropdown button |

### Modified Files (5)
| File | Changes |
|------|---------|
| `connection/workspace/connection/paths.ts` | Add `:providerAlias` to create path |
| `connection/workspace/connection/connection-workspace.context.ts` | Update route to extract provider |
| `connection/workspace/connection/views/connection-details-workspace-view.element.ts` | Read-only provider display |
| `connection/collection/action/manifests.ts` | Use custom element |
| `connection/entity-actions/connection-create.action.ts` | Open modal |

---

## Acceptance Criteria

1. ✅ Collection "Create" button shows dropdown with providers from API
2. ✅ Entity action "+" opens modal with provider list
3. ✅ Selecting provider navigates to `/create/:providerAlias`
4. ✅ Workspace scaffolds with provider pre-populated
5. ✅ Provider shown as read-only (never editable)
6. ✅ Provider-specific settings render correctly
