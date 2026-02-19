# Plan: Test Feature Entity Picker System

## Goal
Enable the test workspace editor to pick a target entity (prompt, agent, etc.) using a picker UI based on the test feature type. Entities live in different packages, so we need a plugin system using manifests.

## Problem
Currently, the test target is a simple text input where users manually enter an ID or alias. This is error-prone and doesn't provide:
- Visual selection from available entities
- Entity names and descriptions
- Package-specific entity discovery (prompts from Prompt package, agents from Agent package)

## Architecture

```
┌─────────────────────────────────────────────────────────┐
│                    Umbraco.AI (Core)                    │
│  - Define Repository API Interface                      │
│  - Define Extension Type (manifest)                     │
│  - Create Picker Element                                │
│  - Export types for consumers                           │
└─────────────────────────────────────────────────────────┘
                          │
                          │ implements
                          ▼
┌─────────────────────────────────────────────────────────┐
│          Umbraco.AI.Prompt / Umbraco.AI.Agent           │
│  - Implement Repository (extends UmbControllerBase)     │
│  - Register Manifest (type: 'repository')               │
│  - Map entities to picker data                          │
└─────────────────────────────────────────────────────────┘
                          │
                          │ consumed by
                          ▼
┌─────────────────────────────────────────────────────────┐
│                Umbraco.AI Test Workspace                │
│  - Query repositories by feature type                   │
│  - Display entities in picker                           │
│  - Set target.targetId                                  │
└─────────────────────────────────────────────────────────┘
```

## Key Decisions

1. **Remove `isAlias` field** - Simplify target model to just `targetId: string`
2. **Follow existing `uai-tool-picker` pattern** - Reuse established picker approach
3. **Single-select picker** - Only one target per test
4. **Use `UAI_ITEM_PICKER_MODAL`** - Reuse existing modal infrastructure
5. **Repository pattern** - Similar to `UaiFrontendToolRepositoryApi` pattern used in agent permissions

## Data Flow

1. User selects test feature type (e.g., "prompt-completion")
2. Picker queries extension registry for repository with matching `testFeatureType`
3. Repository fetches entities from its package's API
4. Modal displays entities with name, description, icon
5. User selects entity
6. Picker updates `target.targetId` with selected entity's ID/alias

## Implementation Steps

### Phase 0: Research - Check for Existing CMS Infrastructure

**BEFORE IMPLEMENTING CUSTOM SOLUTION:**

Research if Umbraco CMS already provides reusable picker infrastructure that we could leverage:

1. **Content Picker Pattern**
   - Check `@umbraco-cms/backoffice` for generic entity picker components
   - Look for: `umb-input-entity-picker`, `umb-entity-picker`, or similar
   - Investigate picker modal system and reusability

2. **Extension Registry Patterns**
   - Check if CMS has entity provider extension types
   - Look for generic repository patterns for entity lists
   - Check manifest types for entity sources

3. **Modal Picker System**
   - Verify if `UMB_MODAL_MANAGER_CONTEXT` supports generic entity picking
   - Check for standard item picker modals with customizable data sources

4. **Form Control Patterns**
   - Check `UmbFormControlMixin` usage for entity selection
   - Look for standard picker element patterns in CMS codebase

**Search Strategy:**
```bash
# In additional working directory: D:\Work\Umbraco\Umbraco.CMS\Umbraco.CMS
# Search for picker patterns
grep -r "picker.*element" --include="*.ts" | head -20
grep -r "entity.*picker" --include="*.ts" | head -20
grep -r "ManifestPicker" --include="*.ts" | head -20
```

**Decision Point:**
- If suitable CMS infrastructure exists → Adapt to use it instead of custom implementation
- If no suitable infrastructure → Proceed with custom implementation (Phases 1-7 below)

---

### Phase 1: Core Package - Define Contracts

**Files to create:**
- `Umbraco.AI/src/Umbraco.AI.Web.StaticAssets/Client/src/test/test-feature-entity-repository.ts`
  - `UaiTestFeatureEntityData` interface (id, name, description, icon)
  - `UaiTestFeatureEntityRepositoryApi` interface (getEntities, getEntity)

- `Umbraco.AI/src/Umbraco.AI.Web.StaticAssets/Client/src/test/extensions/uai-test-feature-entity-repository.extension.ts`
  - `ManifestUaiTestFeatureEntityRepository` interface
  - Extends `ManifestRepository<UaiTestFeatureEntityRepositoryApi>`
  - Meta property: `testFeatureType: string`

- `Umbraco.AI/src/Umbraco.AI.Web.StaticAssets/Client/src/exports.ts`
  - Export types for consumer packages

**Key Interface:**
```typescript
export interface UaiTestFeatureEntityData {
    id: string;           // Entity ID or alias
    name: string;         // Display name
    description?: string; // Optional description
    icon: string;         // Umbraco icon name
}

export interface UaiTestFeatureEntityRepositoryApi extends UmbApi {
    getEntities(): Promise<UaiTestFeatureEntityData[]>;
    getEntity(idOrAlias: string): Promise<UaiTestFeatureEntityData | undefined>;
}

export interface ManifestUaiTestFeatureEntityRepository
    extends ManifestRepository<UaiTestFeatureEntityRepositoryApi> {
    meta: {
        testFeatureType: string; // Links repository to test feature type
    };
}
```

### Phase 2: Core Package - Create Picker Element

**File to create:**
- `Umbraco.AI/src/Umbraco.AI.Web.StaticAssets/Client/src/test/components/test-feature-entity-picker.element.ts`

**Pattern:**
- Extends `UmbFormControlMixin<string | undefined>`
- Properties: `testFeatureId`, `value`, `readonly`
- Discovers repository via `umbExtensionsRegistry.getByTypeAndFilter()`
- Opens `UAI_ITEM_PICKER_MODAL` for selection
- Renders selected item with `uui-ref-node`
- Renders "Select Target" button when empty
- Fires `UmbChangeEvent` on selection change

**Key Behavior:**
- When `testFeatureId` changes → reload repository
- When `value` is set → load selected entity from repository
- Modal shows all available entities from `repository.getEntities()`
- Single-select mode (unlike multi-select tool picker)
- Shows empty state when no testFeatureId or no repository found

**Visual Structure:**
```
┌─────────────────────────────────────────┐
│ [When no entity selected]              │
│  ┌──────────────────────────────────┐  │
│  │ + Select Target                  │  │
│  └──────────────────────────────────┘  │
└─────────────────────────────────────────┘

┌─────────────────────────────────────────┐
│ [When entity selected]                 │
│  ┌──────────────────────────────────┐  │
│  │ [icon] My Prompt                 │  │
│  │        Description text          │🗑│
│  └──────────────────────────────────┘  │
└─────────────────────────────────────────┘
```

### Phase 3: Prompt Package - Implement Repository

**Files to create:**
- `Umbraco.AI.Prompt/src/Umbraco.AI.Prompt.Web.StaticAssets/Client/src/test/prompt-test-entity.repository.ts`
  - Extends `UmbControllerBase`
  - Implements `UaiTestFeatureEntityRepositoryApi`
  - `getEntities()` → Fetches prompts via `PromptsService.getPrompts()`
  - `getEntity(idOrAlias)` → Fetches single prompt via `PromptsService.getPromptByIdOrAlias()`
  - Maps to `UaiTestFeatureEntityData` format
  - Uses `icon: "icon-script-alt"`

- `Umbraco.AI.Prompt/src/Umbraco.AI.Prompt.Web.StaticAssets/Client/src/manifests/test-entity.manifests.ts`
  - Register manifest with `type: "repository"`
  - `alias: "Uai.Repository.TestFeatureEntity.Prompt"`
  - `meta.testFeatureType: "prompt-completion"` (must match backend feature ID)
  - `api: () => import("../test/prompt-test-entity.repository.js")`

- `Umbraco.AI.Prompt/src/Umbraco.AI.Prompt.Web.StaticAssets/Client/src/manifests.ts`
  - Import and export test entity manifests

**Example Repository:**
```typescript
export class PromptTestFeatureEntityRepository
    extends UmbControllerBase
    implements UaiTestFeatureEntityRepositoryApi {

    async getEntities(): Promise<UaiTestFeatureEntityData[]> {
        const response = await PromptsService.getPrompts();
        return response.data?.items.map(prompt => ({
            id: prompt.alias,
            name: prompt.name,
            description: prompt.description || undefined,
            icon: "icon-script-alt",
        })) ?? [];
    }

    async getEntity(idOrAlias: string): Promise<UaiTestFeatureEntityData | undefined> {
        const response = await PromptsService.getPromptByIdOrAlias({
            promptIdOrAlias: idOrAlias
        });
        // ... map to UaiTestFeatureEntityData
    }
}
```

### Phase 4: Agent Package - Implement Repository

**Files to create:**
- `Umbraco.AI.Agent/src/Umbraco.AI.Agent.Web.StaticAssets/Client/src/test/agent-test-entity.repository.ts`
  - Same pattern as Prompt repository
  - Fetches agents via `AgentsService.getAgents()` and `AgentsService.getAgentByIdOrAlias()`
  - Uses `icon: "icon-robot"`

- `Umbraco.AI.Agent/src/Umbraco.AI.Agent.Web.StaticAssets/Client/src/manifests/test-entity.manifests.ts`
  - `alias: "Uai.Repository.TestFeatureEntity.Agent"`
  - `meta.testFeatureType: "agent-tool-test"` (must match backend feature ID)

### Phase 5: Backend Model Changes

**Files to modify:**
- `Umbraco.AI/src/Umbraco.AI.Web/Api/Management/Test/Models/TestTargetModel.cs`
  - Simplify to: `public string TargetId { get; set; } = string.Empty;`
  - Remove `IsAlias` property

**Regenerate OpenAPI client:**
```bash
cd Umbraco.AI/src/Umbraco.AI.Web.StaticAssets/Client
npm run generate-client
```

**Commit strategy:**
- Backend model change + OpenAPI regeneration = one commit
- Allows frontend to reference updated types

### Phase 6: Update Test Workspace Editor

**Files to modify:**
- `Umbraco.AI/src/Umbraco.AI.Web.StaticAssets/Client/src/test/workspace/test/views/test-workspace-editor.element.ts`

**Changes:**
1. Replace target input section (lines 332-348):
```html
<uui-form-layout-item>
    <uui-label for="target" slot="label" required>Target</uui-label>
    <uai-test-feature-entity-picker
        id="target"
        .testFeatureId=${this._model.testFeatureId}
        .value=${this._model.target.targetId}
        @change=${this.#onTargetChange}
    ></uai-test-feature-entity-picker>
    <small slot="description">Select the entity to test (prompt, agent, etc.)</small>
</uui-form-layout-item>
```

2. Replace handler methods:
```typescript
#onTargetChange(event: UmbChangeEvent) {
    event.stopPropagation();
    const picker = event.target as UaiTestFeatureEntityPickerElement;
    this.#workspaceContext?.handleCommand(
        new UaiPartialUpdateCommand<UaiTestDetailModel>(
            { target: { targetId: picker.value ?? "" } },
            "target",
        ),
    );
}
```

3. Remove obsolete handlers:
   - Delete `#onTargetIdChange`
   - Delete `#onTargetIsAliasChange`

4. Update imports:
   - Add import for `UaiTestFeatureEntityPickerElement`

### Phase 7: Update TypeScript Types

**Files to modify:**
- `Umbraco.AI/src/Umbraco.AI.Web.StaticAssets/Client/src/test/types.ts`
  - Update `UaiTestDetailModel.target` type to match simplified `TestTargetModel`
  - Change from `{ targetId: string; isAlias: boolean }` to just `{ targetId: string }`

- `Umbraco.AI/src/Umbraco.AI.Web.StaticAssets/Client/src/test/type-mapper.ts`
  - Update mapper functions to handle simplified target model
  - Remove any `isAlias` mapping logic

## Testing Plan

### Unit Testing (Manual)

1. **Core Package - Picker Element:**
   - ✅ Renders empty state when no `testFeatureId`
   - ✅ Shows "no repository" message when feature type has no provider
   - ✅ Loads and displays selected entity from repository
   - ✅ Opens modal and shows available entities
   - ✅ Selection updates model and fires change event
   - ✅ Remove button clears selection

2. **Prompt Package - Repository:**
   - ✅ Fetches all prompts via API
   - ✅ Resolves single prompt by alias/ID
   - ✅ Manifest is registered in extension registry
   - ✅ Picker shows prompts when feature type is "prompt-completion"

3. **Agent Package - Repository:**
   - ✅ Fetches all agents via API
   - ✅ Resolves single agent by alias/ID
   - ✅ Manifest is registered in extension registry
   - ✅ Picker shows agents when feature type is "agent-tool-test"

### Integration Testing

1. **Create Test with Prompt:**
   - Create new test
   - Select "Prompt Completion" feature type
   - Target picker shows list of prompts
   - Select a prompt
   - Save test
   - Verify `targetId` is saved correctly

2. **Create Test with Agent:**
   - Create new test
   - Select "Agent Tool Test" feature type
   - Target picker shows list of agents
   - Select an agent
   - Save test
   - Verify `targetId` is saved correctly

3. **Edit Existing Test:**
   - Open existing test
   - Verify selected entity is displayed
   - Change entity selection
   - Save changes
   - Verify updated `targetId`

4. **Feature Type Change:**
   - Create test with prompt selected
   - Change feature type to agent
   - Verify target picker updates to show agents
   - Verify previous selection is cleared

5. **Package Not Installed:**
   - Select feature type for package not installed
   - Verify picker shows appropriate message
   - No errors in console

## Benefits

✅ **Package independence** - Test framework doesn't know about entity types
✅ **Extensibility** - New packages can add providers via manifests
✅ **Type safety** - TypeScript interfaces throughout
✅ **Follows established patterns** - Mirrors `uai-tool-picker` approach
✅ **Reuses existing infrastructure** - `UAI_ITEM_PICKER_MODAL`, `UmbFormControlMixin`
✅ **Simplified model** - No `isAlias` field, just `targetId`
✅ **Graceful degradation** - Shows appropriate UI when no repository available
✅ **Lazy loading** - Repositories only loaded when needed
✅ **Visual selection** - Better UX than manual text input

## Open Questions

### 1. **Can we reuse existing Umbraco CMS picker infrastructure?**

**✅ RESEARCH COMPLETE:**

**CMS Picker Infrastructure Found:**
- `@umbraco-cms/backoffice/picker` - Core picker infrastructure
- `@umbraco-cms/backoffice/picker-data-source` - Data source interfaces
- `UmbPickerDataSource` interface (extends `UmbItemRepository`)
- `UmbItemRepository` interface with `requestItems(uniques: string[])` method
- Entity data picker property editor (`entity-data-picker`)

**CMS Pattern:**
- Designed primarily for property editors (content, media, member pickers)
- Uses `UmbItemRepository.requestItems(uniques[])` - fetch items by known IDs
- Requires data source, item store, and complex repository setup
- Works with entity types that have unique IDs

**Our Pattern (uai-tool-picker):**
- Simple repository with `getEntities()` - list all available items
- Lightweight - no store, no complex setup
- Uses existing `UAI_ITEM_PICKER_MODAL` we already have
- Already proven pattern in our codebase

**DECISION: Use custom implementation (Phases 1-7)**

**Rationale:**
1. **Different use case**: We need "list all entities of type X" not "fetch these specific IDs"
2. **Simpler requirements**: No need for stores, caching, or complex repository patterns
3. **Consistency**: Matches our existing `uai-tool-picker` pattern exactly
4. **Less coupling**: Don't depend on CMS property editor infrastructure
5. **Proven approach**: `uai-tool-picker` already works well with this pattern
6. **Lightweight**: Repository just wraps API calls, no additional infrastructure

**What we learned from CMS:**
- Confirmed repository + manifest pattern is the right approach
- CMS uses similar extension registry pattern for data sources
- Our simpler approach is appropriate for our use case

### 2. **Backend test feature type IDs**

Need to verify exact feature type IDs from backend:
- Prompt tests: `"prompt-completion"` or different?
- Agent tests: `"agent-tool-test"` or different?
- Must match exactly for repository lookup

### 3. **Icon customization**

Current approach hardcodes icons per package:
- Prompt: `"icon-script-alt"`
- Agent: `"icon-robot"`

**Options:**
- Keep hardcoded (simple, consistent)
- Allow repositories to specify icons per entity (flexible, more complex)
- Use entity type icons from backend (requires backend changes)

**Recommendation:** Keep hardcoded for v1, add per-entity icons if needed later

### 4. **Error handling**

When repository fetch fails:
- Show error message to user?
- Fall back to text input?
- Log to console and show empty state?

**Recommendation:** Log error, show empty state with message "Unable to load entities"

## Migration Strategy

**Existing Tests:**
- Tests with `target.isAlias` field need migration
- Can be done automatically via data migration
- Or handle gracefully in frontend (ignore `isAlias` if present)

**Backwards Compatibility:**
- Frontend should handle old model structure temporarily
- Backend should accept both old and new structures during transition
- Remove old structure after full migration

## Next Steps

1. **✅ PHASE 0: Research existing CMS infrastructure**
   - Search Umbraco CMS codebase for picker patterns
   - Check backoffice package for generic entity pickers
   - Evaluate if CMS solutions fit our needs

2. **Decision Point:**
   - If CMS infrastructure suitable → Adapt plan to use CMS patterns
   - If custom implementation needed → Proceed with Phases 1-7

3. **Verify backend feature type IDs**
   - Check exact string values used in backend
   - Update manifest registrations to match

4. **Implementation order:**
   - Core contracts → Core picker → Backend changes → Prompt repo → Agent repo → Workspace editor
