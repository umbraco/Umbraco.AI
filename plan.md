# Guardrails UI Implementation Plan

## Overview

Add a complete "Guardrails" feature to the Umbraco.AI frontend, following the exact patterns established by the existing **Contexts** feature (for the workspace/editor/menu/collection/actions) and **Test Graders** (for the rules sub-editor within the guardrail editor). Also add a **Guardrails Picker** component (copying the Context Picker) for use in Profiles, Prompts, Agents, and Tests.

The backend Management API already exists with full CRUD + evaluator listing endpoints. The OpenAPI client needs to be regenerated.

---

## Phase 1: OpenAPI Client Generation

### 1.1 Regenerate the OpenAPI client
- Run the demo site with `DemoSite-Claude` profile
- Run `npm run generate-client` to pick up the new Guardrail endpoints
- This produces the `GuardrailsService`, types (`GuardrailResponseModel`, `GuardrailItemResponseModel`, `GuardrailRuleModel`, `GuardrailEvaluatorInfoModel`, etc.) in `api/sdk.gen.ts` and `api/types.gen.ts`

---

## Phase 2: Core Guardrail Feature (copying Contexts pattern)

Create `Umbraco.AI/src/Umbraco.AI.Web.StaticAssets/Client/src/guardrail/` with the following structure:

### 2.1 Constants & Entity Types

**`guardrail/entity.ts`** (copy from `context/entity.ts`)
```ts
export const UAI_GUARDRAIL_ENTITY_TYPE = "uai:guardrail";
export const UAI_GUARDRAIL_ROOT_ENTITY_TYPE = "uai:guardrail-root";
```

**`guardrail/constants.ts`** (copy from `context/constants.ts`)
```ts
export const UAI_GUARDRAIL_ICON = "icon-shield"; // shield icon for safety
// Re-export entity types and repository/workspace/collection constants
```

### 2.2 Types

**`guardrail/types.ts`** (adapted from `context/types.ts`)
```ts
export interface UaiGuardrailRuleModel {
    id: string;
    evaluatorId: string;
    name: string;
    phase: "PreGenerate" | "PostGenerate";
    action: "Block" | "Warn";
    config: Record<string, unknown> | null;
    sortOrder: number;
}

export interface UaiGuardrailDetailModel {
    unique: string;
    entityType: UaiGuardrailEntityType;
    alias: string;
    name: string;
    rules: UaiGuardrailRuleModel[];
    dateCreated: string | null;
    dateModified: string | null;
    version: number;
}

export interface UaiGuardrailItemModel {
    unique: string;
    entityType: UaiGuardrailEntityType;
    alias: string;
    name: string;
    ruleCount: number;
    dateCreated: string | null;
    dateModified: string | null;
}
```

### 2.3 Type Mapper

**`guardrail/type-mapper.ts`** (copy from `context/type-mapper.ts`)
- `toDetailModel(response)` → `UaiGuardrailDetailModel`
- `toItemModel(response)` → `UaiGuardrailItemModel`
- `toRuleModel(rule)` → `UaiGuardrailRuleModel`
- `toCreateRequest(model)` → API create request
- `toUpdateRequest(model)` → API update request
- `toRuleRequest(rule)` → API rule model

### 2.4 Repository Layer

**`guardrail/repository/constants.ts`**
```ts
export const UAI_GUARDRAIL_DETAIL_REPOSITORY_ALIAS = "UmbracoAI.Repository.Guardrail.Detail";
export const UAI_GUARDRAIL_DETAIL_STORE_ALIAS = "UmbracoAI.Store.Guardrail.Detail";
export const UAI_GUARDRAIL_COLLECTION_REPOSITORY_ALIAS = "UmbracoAI.Repository.Guardrail.Collection";
```

**`guardrail/repository/detail/`** (copy from `context/repository/detail/`)
- `guardrail-detail.store.ts` — `UaiGuardrailDetailStore` (extends `UmbDetailStoreBase`)
- `guardrail-detail.server.data-source.ts` — CRUD via `GuardrailsService`
- `guardrail-detail.repository.ts` — extends `UmbDetailRepositoryBase`, dispatches entity action events

**`guardrail/repository/collection/`** (copy from `context/repository/collection/`)
- `guardrail-collection.server.data-source.ts` — list via `GuardrailsService.getAllGuardrails()`
- `guardrail-collection.repository.ts` — extends `UmbRepositoryBase`

**`guardrail/repository/evaluator/`** (new — for evaluator info)
- `guardrail-evaluator-item.repository.ts` — fetches evaluator types via `GuardrailsService.getAllGuardrailEvaluators()`

**`guardrail/repository/manifests.ts`** — registers all repositories + store

### 2.5 Menu Item

**`guardrail/menu/manifests.ts`** (copy from `context/menu/manifests.ts`)
```ts
{
    type: "menuItem",
    kind: "entityContainer",
    alias: "UmbracoAI.MenuItem.Guardrails",
    name: "Guardrails Menu Item",
    weight: -10,  // Below Contexts (0), above Tests (-80)
    meta: {
        label: "Guardrails",
        icon: UAI_GUARDRAIL_ICON,
        entityType: UAI_GUARDRAIL_ROOT_ENTITY_TYPE,
        childEntityTypes: [UAI_GUARDRAIL_ENTITY_TYPE],
        menus: [UAI_CORE_MENU_ALIAS],
    },
}
```

### 2.6 Collection (List View)

**`guardrail/collection/constants.ts`**
```ts
export const UAI_GUARDRAIL_COLLECTION_ALIAS = "UmbracoAI.Collection.Guardrail";
```

**`guardrail/collection/guardrail-collection.element.ts`** (copy from `context/collection/context-collection.element.ts`)
- Custom collection with search header

**`guardrail/collection/views/table/guardrail-table-collection-view.element.ts`** (copy from context table view)
- Table columns: Name (link), Alias (tag), Rules (count), Modified (date)

**`guardrail/collection/action/`** (copy from `context/collection/action/`)
- `guardrail-create-collection-action.element.ts` — "Create Guardrail" button
- `manifests.ts`

**`guardrail/collection/bulk-action/`** (copy from `context/collection/bulk-action/`)
- `guardrail-bulk-delete.action.ts` — bulk delete using `UaiBulkDeleteActionBase`
- `manifests.ts`

**`guardrail/collection/manifests.ts`** — registers collection, view, actions, bulk actions

### 2.7 Entity Actions

**`guardrail/entity-actions/manifests.ts`** (copy from `context/entity-actions/manifests.ts`)
- Create action (on root entity type) — navigates to create workspace
- Delete action (on entity type) — uses `UaiDeleteActionBase`

**`guardrail/entity-actions/guardrail-create.action.ts`**
**`guardrail/entity-actions/guardrail-delete.action.ts`**

### 2.8 Workspace

**`guardrail/workspace/constants.ts`**
```ts
export const UAI_GUARDRAIL_WORKSPACE_ALIAS = "UmbracoAI.Workspace.Guardrail";
export const UAI_GUARDRAIL_ROOT_WORKSPACE_ALIAS = "UmbracoAI.Workspace.GuardrailRoot";
```

**`guardrail/workspace/guardrail-root/`** (copy from `context/workspace/context-root/`)
- `manifests.ts` — root workspace + collection view
- `paths.ts` — `UAI_GUARDRAIL_ROOT_WORKSPACE_PATH`

**`guardrail/workspace/guardrail/`** (copy from `context/workspace/context/`)

- **`paths.ts`** — workspace, create, and edit path patterns
- **`guardrail-workspace.context-token.ts`** — context token
- **`guardrail-workspace.context.ts`** — `UaiGuardrailWorkspaceContext` (extends `UmbSubmittableWorkspaceContextBase`)
  - Same scaffold/load/save/handleCommand/reload pattern as context workspace
- **`guardrail-workspace-editor.element.ts`** — Editor shell with:
  - Back button to collection
  - Name input with alias lock (same pattern as context editor)
  - Action menu for existing entities
  - Footer breadcrumb
- **`views/guardrail-details-workspace-view.element.ts`** — Settings view:
  - Contains the **Rules Editor** (`<uai-guardrail-rule-config-builder>`) — see Phase 3
- **`views/guardrail-info-workspace-view.element.ts`** — Info view:
  - Version history (`<uai-version-history>`)
  - Info box (Id, Date Created, Date Modified)
- **`manifests.ts`** — registers workspace, views (Settings + Info), save action

**`guardrail/workspace/manifests.ts`** — aggregates guardrail + guardrail-root manifests

### 2.9 Barrel Exports & Registration

**`guardrail/index.ts`** — re-exports constants, types, collection, components, repository, workspace
**`guardrail/exports.ts`** — exports the picker component (for external use by add-ons)
**`guardrail/manifests.ts`** — aggregates all sub-manifests (collection, entity-actions, menu, repository, workspace)

**Update `src/manifests.ts`** — add `guardrailManifests` to the main bundle
**Update `src/index.ts`** — add `export * from "./guardrail/index.js"`

---

## Phase 3: Rules Editor (copying Test Graders pattern)

### 3.1 Rule Config Builder Component

**`guardrail/components/rule-config-builder/rule-config-builder.element.ts`** (copy from `test/components/grader-config-builder/`)

`<uai-guardrail-rule-config-builder>` — List of rules with add/edit/remove:
- Uses `<uui-ref-list>` + `<uui-ref-node>` for each rule
- Shows rule name, detail summary (evaluator type, phase, action)
- "Add Rule" button → opens evaluator type picker (reuse `UAI_ITEM_PICKER_MODAL`)
- On type selection → opens rule config editor modal
- Edit → opens rule config editor modal directly (skip type picker)
- Remove → removes from list
- Dispatches `UmbChangeEvent` on changes

**`guardrail/components/rule-config-builder/index.ts`**
**`guardrail/components/rule-config-builder/manifests.ts`** (empty array - component registered via barrel)

### 3.2 Rule Config Editor Modal

**`guardrail/modals/rule-config-editor/`** (copy from `test/modals/grader-config-editor/`)

`<uai-guardrail-rule-config-editor-modal>` — Sidebar modal (medium) with:

**Modal Data/Value interfaces:**
```ts
interface UaiGuardrailRuleConfigEditorModalData {
    evaluatorId: string;
    evaluatorName: string;
    existingRule?: UaiGuardrailRuleConfig;
}

interface UaiGuardrailRuleConfigEditorModalValue {
    rule: UaiGuardrailRuleConfig;
}
```

**Form fields:**
- **Name** (required text input)
- **Phase** (dropdown: PreGenerate, PostGenerate — default PostGenerate)
- **Action** (dropdown: Block, Warn — default Block)
- **Evaluator Config** (dynamic `<uai-model-editor>` if evaluator has configSchema)

**Fetches evaluator schema** from `GuardrailEvaluatorInfoModel.configSchema` on modal open.

**`guardrail/modals/rule-config-editor/guardrail-rule-config-editor-modal.token.ts`**
**`guardrail/modals/rule-config-editor/guardrail-rule-config-editor-modal.element.ts`**
**`guardrail/modals/rule-config-editor/index.ts`**
**`guardrail/modals/manifests.ts`** — registers modal

### 3.3 Rule Types

Add to `guardrail/types.ts`:
```ts
export interface UaiGuardrailRuleConfig {
    id: string;
    evaluatorId: string;
    name: string;
    phase: "PreGenerate" | "PostGenerate";
    action: "Block" | "Warn";
    config?: Record<string, unknown>;
    sortOrder: number;
}

export function createEmptyRuleConfig(): UaiGuardrailRuleConfig { ... }
export function getRuleSummary(rule: UaiGuardrailRuleConfig, evaluatorName?: string): string { ... }
```

### 3.4 Integration in Details Workspace View

The `guardrail-details-workspace-view.element.ts` renders:
```html
<uui-box headline="Rules">
    <umb-property-layout label="Rules" description="...">
        <uai-guardrail-rule-config-builder
            slot="editor"
            .rules=${this._model.rules}
            @change=${this.#onRulesChange}
        />
    </umb-property-layout>
</uui-box>
```

On change, dispatches `UaiPartialUpdateCommand<UaiGuardrailDetailModel>({ rules }, "update-rules")`.

---

## Phase 4: Guardrails Picker Component (copying Context Picker)

### 4.1 Picker Component

**`guardrail/components/guardrail-picker/guardrail-picker.element.ts`** (copy from `context/components/context-picker/`)

`<uai-guardrail-picker>` — Reusable picker for selecting guardrails:
- Same API as context picker: `multiple`, `readonly`, `min`, `max`, `value`
- Fetches guardrail items via `GuardrailsService.getAllGuardrails()`
- Opens `UAI_ITEM_PICKER_MODAL` for selection
- Shows selected guardrails as `<uui-ref-node>` with name, alias, rule count tag
- Icon: `icon-shield`
- Dispatches `UmbChangeEvent` on selection changes

**`guardrail/components/guardrail-picker/index.ts`**
**`guardrail/components/guardrail-picker/manifests.ts`** (empty array)
**`guardrail/components/index.ts`** — barrel exports both components (rule-config-builder + guardrail-picker)
**`guardrail/exports.ts`** — `export { UaiGuardrailPickerElement } from "./components/guardrail-picker/index.js"`

### 4.2 Profile Integration

**Modify `profile/workspace/profile/views/profile-details-workspace-view.element.ts`:**
- Add a "Guardrails" property layout in the chat settings section, below Contexts:
```html
<umb-property-layout label="Guardrails" description="Guardrails to apply to chat responses">
    <uai-guardrail-picker
        slot="editor"
        multiple
        .value=${chatSettings?.guardrailIds}
        @change=${this.#onGuardrailIdsChange}
    ></uai-guardrail-picker>
</umb-property-layout>
```
- Add `#onGuardrailIdsChange` handler → `#updateChatSettings({ guardrailIds: picker.value })`
- Add `guardrailIds` to `UaiChatProfileSettings` type

> **Note:** This requires the backend to support `GuardrailIds` on profiles (chat settings). If the backend Profile model doesn't have `GuardrailIds` yet, this integration is deferred until it's added.

### 4.3 Prompt Integration

**Modify Prompt add-on frontend** (`Umbraco.AI.Prompt/src/Umbraco.AI.Prompt.Web.StaticAssets/Client/`):
- Add `<uai-guardrail-picker>` to the prompt editor details view
- The Prompt API models already have `GuardrailIds` (added in backend)

### 4.4 Agent Integration

**Modify Agent add-on frontend** (`Umbraco.AI.Agent/src/Umbraco.AI.Agent.Web.StaticAssets/Client/`):
- Add `<uai-guardrail-picker>` to the standard agent config section
- The Agent API models already have `GuardrailIds` on `StandardAgentConfigModel` (added in backend)

### 4.5 Test Integration

**Modify test execution UI** (if there's a test run form):
- Add `<uai-guardrail-picker>` for `GuardrailIdsOverride` in test run request forms
- The Test API models already have `GuardrailIdsOverride` (added in backend)

---

## Phase 5: Localization

**`src/lang/`** — Add guardrail-related localization keys:
- `uaiGuardrail_selectGuardrail`, `uaiGuardrail_addGuardrail`, `uaiGuardrail_noGuardrailsAvailable`
- `uaiGuardrail_bulkDeleteConfirm`, `uaiGuardrail_deleteConfirm`
- `uaiGuardrail_rulePhasePreGenerate`, `uaiGuardrail_rulePhasePostGenerate`
- `uaiGuardrail_ruleActionBlock`, `uaiGuardrail_ruleActionWarn`

---

## File Summary

### New Files (~35 files in core guardrail feature)

```
guardrail/
├── entity.ts
├── constants.ts
├── types.ts
├── type-mapper.ts
├── index.ts
├── exports.ts
├── manifests.ts
├── collection/
│   ├── constants.ts
│   ├── guardrail-collection.element.ts
│   ├── index.ts
│   ├── manifests.ts
│   ├── action/
│   │   ├── guardrail-create-collection-action.element.ts
│   │   └── manifests.ts
│   ├── bulk-action/
│   │   ├── guardrail-bulk-delete.action.ts
│   │   └── manifests.ts
│   └── views/
│       └── table/
│           └── guardrail-table-collection-view.element.ts
├── components/
│   ├── index.ts
│   ├── guardrail-picker/
│   │   ├── guardrail-picker.element.ts
│   │   ├── index.ts
│   │   └── manifests.ts
│   └── rule-config-builder/
│       ├── rule-config-builder.element.ts
│       ├── index.ts
│       └── manifests.ts
├── entity-actions/
│   ├── guardrail-create.action.ts
│   ├── guardrail-delete.action.ts
│   └── manifests.ts
├── menu/
│   └── manifests.ts
├── modals/
│   ├── manifests.ts
│   └── rule-config-editor/
│       ├── guardrail-rule-config-editor-modal.element.ts
│       ├── guardrail-rule-config-editor-modal.token.ts
│       └── index.ts
├── repository/
│   ├── constants.ts
│   ├── index.ts
│   ├── manifests.ts
│   ├── collection/
│   │   ├── guardrail-collection.repository.ts
│   │   ├── guardrail-collection.server.data-source.ts
│   │   └── index.ts
│   ├── detail/
│   │   ├── guardrail-detail.repository.ts
│   │   ├── guardrail-detail.server.data-source.ts
│   │   ├── guardrail-detail.store.ts
│   │   └── index.ts
│   └── evaluator/
│       ├── guardrail-evaluator-item.repository.ts
│       └── index.ts
└── workspace/
    ├── constants.ts
    ├── index.ts
    ├── manifests.ts
    ├── guardrail/
    │   ├── guardrail-workspace.context.ts
    │   ├── guardrail-workspace.context-token.ts
    │   ├── guardrail-workspace-editor.element.ts
    │   ├── manifests.ts
    │   ├── paths.ts
    │   ├── index.ts
    │   └── views/
    │       ├── guardrail-details-workspace-view.element.ts
    │       └── guardrail-info-workspace-view.element.ts
    └── guardrail-root/
        ├── manifests.ts
        ├── paths.ts
        └── index.ts
```

### Modified Files

| File | Change |
|------|--------|
| `src/manifests.ts` | Add `guardrailManifests` import and spread |
| `src/index.ts` | Add `export * from "./guardrail/index.js"` |
| `profile/types.ts` | Add `guardrailIds` to `UaiChatProfileSettings` |
| `profile/.../profile-details-workspace-view.element.ts` | Add guardrail picker in chat settings |
| Prompt frontend details view | Add guardrail picker |
| Agent frontend config section | Add guardrail picker |
| Test frontend run forms | Add guardrail override picker |

---

## Implementation Order

1. **Phase 1** — Generate OpenAPI client (prerequisite)
2. **Phase 2** — Core guardrail feature (entity, repo, workspace, collection, menu, actions)
3. **Phase 3** — Rules editor (rule-config-builder, rule-config-editor modal, evaluator repo)
4. **Phase 4** — Guardrail picker + integrations (profile, prompt, agent, test)
5. **Phase 5** — Localization keys

Phases 2-3 can be built and verified independently before Phase 4 integrations.
