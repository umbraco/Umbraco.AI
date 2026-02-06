# Entity Adapter Architecture for Umbraco.AI.Agent

## Implementation Status

### ✅ Phase 1: Minimal Implementation (Complete)

**Goal**: Test entity adapter architecture with documents and simple text properties.

**Completed:**

- [x] Workspace Registry (cross-DOM context access)
- [x] Entity Adapter types and interfaces
- [x] Document Adapter (documents only, TextBox/TextArea properties)
- [x] Entity Adapter Context (detection, selection, serialization)
- [x] Entity Selector UI component (shows current entity with icon)
- [x] CopilotContext integration (exposes entity context to UI)
- [x] Context injection into agent requests (AG-UI protocol)
- [x] Backend context processing (appends to system prompt)
- [x] Reactive name updates (via `variants` observable)
- [x] Reactive icon updates (via `structure.ownerContentType` observable)
- [x] Property mutation API (`applyPropertyChange`) for TextBox/TextArea

**Files created/modified:**
| File | Status |
|------|--------|
| `entity-adapter/types.ts` | ✅ Created |
| `entity-adapter/adapters/document.adapter.ts` | ✅ Created |
| `entity-adapter/entity-adapter.context.ts` | ✅ Created |
| `entity-adapter/index.ts` | ✅ Created |
| `copilot/components/entity-selector/entity-selector.element.ts` | ✅ Created |
| `copilot/components/entity-selector/index.ts` | ✅ Created |
| `copilot/copilot.context.ts` | ✅ Modified |
| `copilot/services/copilot-run.controller.ts` | ✅ Modified |
| `copilot/transport/uai-agent-client.ts` | ✅ Modified |
| `copilot/components/sidebar/copilot-sidebar.element.ts` | ✅ Modified |
| `Umbraco.AI.Agent.Web/.../RunAgentController.cs` | ✅ Modified |

**Key technical findings:**

- `UmbDocumentWorkspaceContext.name()` method returns observable that only emits initial value
- Use `variants` observable instead for reactive name updates
- `structure.ownerContentType` observable provides document type icon
- Adapters own the logic for name/icon observables (not the context)

### 🔲 Phase 2: Future Work

- [ ] Extension manifest registration (currently hardcoded adapter)
- [ ] Media adapter
- [x] Property mutation API (`applyPropertyChange` on adapters/context)
- [x] Frontend tool for property mutation (`setPropertyValue` tool)
- [x] Enhanced serialization using content type structure (shows all properties, not just those with values)
- [ ] Complex property editors (block grid, media picker, RichText)
- [ ] Nested modal handling tests
- [ ] Entity URL generation (`getEditorUrl`)
- [ ] Third-party adapter pattern (Commerce, etc.)

---

### ✅ Property Mutation API (Complete)

**Goal**: Enable AI tools to update entity property values in the workspace (staged changes).

**Implementation Flow:**

```
Tool Call (LLM)
  → CopilotContext.applyPropertyChange(change)
    → EntityAdapterContext.applyPropertyChange(change)
      → Adapter.applyPropertyChange(workspaceContext, change)
        → workspaceContext.setPropertyValue(alias, value, variantId)
```

**Key Design Decisions:**

1. **Staged changes only** - Changes are applied to the workspace context, not persisted. User must click Save.
2. **Adapter validation** - Document adapter validates property exists and is a supported editor type.
3. **Supported editors** - TextBox and TextArea only (consistent with serialization).
4. **Variant support** - Culture and segment can be specified for variant content.
5. **Graceful error handling** - Returns `UaiPropertyChangeResult` with success/error instead of throwing.

**Types Added:**

```typescript
interface UaiPropertyChange {
    alias: string; // Property alias
    value: unknown; // New value
    culture?: string; // For variant content (undefined = invariant)
    segment?: string; // For segmented content
}

interface UaiPropertyChangeResult {
    success: boolean;
    error?: string; // Human-readable error message
}
```

**Files Modified:**
| File | Changes |
|------|---------|
| `entity-adapter/types.ts` | Added `UaiPropertyChange`, `UaiPropertyChangeResult`, updated `UaiEntityAdapterApi` |
| `entity-adapter/adapters/document.adapter.ts` | Added `applyPropertyChange` method |
| `entity-adapter/entity-adapter.context.ts` | Added `applyPropertyChange` method |
| `entity-adapter/index.ts` | Exported new types |
| `copilot/copilot.context.ts` | Added `applyPropertyChange` method |

---

### ✅ Frontend Tool: setPropertyValue (Complete)

**Goal**: Expose property mutation to AI agents via the AG-UI tool call protocol.

**Tool Definition:**

```typescript
{
  type: "uaiAgentTool",
  alias: "Uai.AgentTool.SetPropertyValue",
  meta: {
    toolName: "setPropertyValue",
    description: "Update a property value on the currently selected entity...",
    parameters: {
      type: "object",
      properties: {
        alias: { type: "string", description: "Property alias" },
        value: { type: "string", description: "New value" },
        culture: { type: "string", description: "Optional culture code" },
        segment: { type: "string", description: "Optional segment" },
      },
      required: ["alias", "value"],
    },
  },
}
```

**Files Created:**
| File | Purpose |
|------|---------|
| `agent/tools/entity/set-property-value.api.ts` | Tool execution logic |
| `agent/tools/entity/manifests.ts` | Tool manifest definition |

**Files Modified:**
| File | Changes |
|------|---------|
| `agent/tools/manifests.ts` | Added entity tools to exports |

**Usage Example (LLM perspective):**

```json
{
    "tool": "setPropertyValue",
    "args": {
        "alias": "title",
        "value": "My Updated Title"
    }
}
```

**Response:**

```json
{ "success": true }
// or
{ "success": false, "error": "Property 'xyz' not found..." }
```

---

## Overview

Design a standardized mechanism for AI tools to interact with any Umbraco entity being edited. The system must:

1. Construct URLs to navigate to entity editors
2. Detect when editing an entity (with nested modal support)
3. Serialize entity details for LLM context
4. Apply property changes to workspace context (staged for user review)

## Design Principles

- **Extension-based**: Third-party packages (Commerce, etc.) register adapters via manifests
- **Property-focused frontend**: Frontend tools modify property values; backend CRUD is future scope
- **Staged changes**: Apply to workspace context, user saves manually
- **Nested context support**: Handle modals containing editors via context stack

## Architecture Components

### 1. Extension Manifest Type (`uaiEntityAdapter`)

```typescript
interface ManifestUaiEntityAdapter extends ManifestApi<UaiEntityAdapterApi> {
    type: "uaiEntityAdapter";
    meta: {
        entityType: string; // e.g., "document", "media", "uc:order"
        priority?: number; // Higher = checked first (default 0)
    };
}
```

### 2. Entity Adapter API Interface

```typescript
interface UaiEntityAdapterApi extends UmbApi {
    readonly entityType: string;

    // Detection - returns entity context with any additional identifiers needed
    canHandle(workspaceContext: unknown): boolean;

    // Extract entity context from workspace (includes storeId, parentId, etc.)
    extractEntityContext(workspaceContext: unknown): UaiEntityContext;

    // URL generation - receives full entity context
    getEditorUrl(entityContext: UaiEntityContext, options?: UaiEditorUrlOptions): string;

    // LLM serialization
    serializeForLlm(workspaceContext: unknown): Promise<UaiSerializedEntity>;

    // Property mutation (staged)
    applyPropertyChange(workspaceContext: unknown, change: UaiPropertyChange): Promise<UaiPropertyChangeResult>;

    // Schema for LLM understanding
    getPropertySchema(workspaceContext: unknown): Promise<UaiPropertySchema[]>;
}

// Flexible entity context supporting hierarchical relationships (recursive for any depth)
interface UaiEntityContext {
    entityType: string;
    unique: string | null; // null for "create" scenarios
    // Recursive parent context - supports any nesting depth
    // e.g., Region → Country → Store
    parentContext?: UaiEntityContext;
}

// Example: Commerce Region (3 levels deep)
// URL: /workspace/uc:store-settings/{storeId}/uc:country/{countryId}/uc:region/{regionId}
const regionContext: UaiEntityContext = {
    entityType: "uc:region",
    unique: "region-guid",
    parentContext: {
        entityType: "uc:country",
        unique: "country-guid",
        parentContext: {
            entityType: "uc:store-settings",
            unique: "store-guid",
        },
    },
};

interface UaiEditorUrlOptions {
    culture?: string;
    segment?: string;
    view?: string;
}
```

### 3. Entity Adapter Registry

- Loads adapters from extension registry
- Caches instantiated adapter APIs
- Resolves adapter by entity type or workspace context (priority-ordered)

### 4. Entity Adapter Context

**Challenge**: Umbraco's context system is DOM-scoped (travels upward via `consumeContext()`), but the Copilot sidebar is in a separate DOM subtree and cannot directly consume workspace contexts.

**Solution**: The **Workspace Registry** (✅ implemented) solves cross-DOM access. The Entity Adapter Context consumes it to provide semantic entity detection.

#### Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    Workspace Registry (✅ done)                  │
│  - Intercepts workspace manifest APIs via Proxy                 │
│  - Auto-registers/unregisters workspace contexts                │
│  - Provides: getAll(), getByEntity(), changes$                  │
└─────────────────────────────────────────────────────────────────┘
                              ↓ consumes
┌─────────────────────────────────────────────────────────────────┐
│                    Entity Adapter Context                        │
│  - Subscribes to workspaceRegistry.changes$                     │
│  - Finds adapter for each workspace via Adapter Registry        │
│  - Extracts UaiEntityContext using adapter.extractEntityContext │
│  - Exposes: detectedEntities$, getCurrentEntity(), etc.         │
└─────────────────────────────────────────────────────────────────┘
                              ↓ consumed by
┌─────────────────────────────────────────────────────────────────┐
│                       Copilot Context                            │
│  - Delegates entity operations to Entity Adapter Context        │
│  - Tools call: getCurrentEntity(), applyPropertyChange(), etc.  │
└─────────────────────────────────────────────────────────────────┘
```

#### Implementation

```typescript
import { workspaceRegistry } from "../workspace-registry/index.js";

interface UaiDetectedEntity {
    key: string; // entityType:unique
    entityContext: UaiEntityContext; // Extracted by adapter
    adapter: UaiEntityAdapterApi; // The matched adapter
    workspaceContext: object; // Live workspace context
}

class UaiEntityAdapterContext extends UmbContextBase {
    readonly #adapterRegistry: UaiEntityAdapterRegistry;
    readonly #detectedEntities$ = new UmbArrayState<UaiDetectedEntity>([], (e) => e.key);

    constructor(host: UmbControllerHost) {
        super(host, UAI_ENTITY_ADAPTER_CONTEXT);

        // Subscribe to workspace changes
        workspaceRegistry.changes$.subscribe(() => this.#refreshDetectedEntities());

        // Initial detection
        this.#refreshDetectedEntities();
    }

    /** All entities currently being edited */
    get detectedEntities() {
        return this.#detectedEntities$.asObservable();
    }

    /** Get the "top" entity (most recently opened, or innermost modal) */
    getCurrentEntity(): UaiDetectedEntity | undefined {
        const all = this.#detectedEntities$.getValue();
        return all[all.length - 1]; // Last = most recent/innermost
    }

    /** Get entity by type and unique */
    getEntity(entityType: string, unique: string): UaiDetectedEntity | undefined {
        return this.#detectedEntities$
            .getValue()
            .find((e) => e.entityContext.entityType === entityType && e.entityContext.unique === unique);
    }

    #refreshDetectedEntities(): void {
        const detected: UaiDetectedEntity[] = [];

        for (const entry of workspaceRegistry.getAll()) {
            // Find adapter that can handle this workspace
            const adapter = this.#adapterRegistry.findForWorkspace(entry.context);

            if (adapter) {
                detected.push({
                    key: `${entry.entityType}:${entry.entityUnique}`,
                    entityContext: adapter.extractEntityContext(entry.context),
                    adapter,
                    workspaceContext: entry.context,
                });
            }
            // No adapter match = skip (e.g., block workspaces without adapter)
        }

        this.#detectedEntities$.setValue(detected);
    }

    // Delegated operations (use adapter + workspace context)

    async serializeCurrentEntity(): Promise<UaiSerializedEntity | undefined> {
        const current = this.getCurrentEntity();
        if (!current) return undefined;
        return current.adapter.serializeForLlm(current.workspaceContext);
    }

    async applyPropertyChange(change: UaiPropertyChange): Promise<UaiPropertyChangeResult> {
        const current = this.getCurrentEntity();
        if (!current) throw new Error("No entity in context");
        return current.adapter.applyPropertyChange(current.workspaceContext, change);
    }
}
```

#### Key Benefits

1. **Separation of concerns**: Workspace Registry handles mechanics (interception, lifecycle), Entity Adapter Context handles semantics (what entity, how to serialize)

2. **Real-time updates**: `changes$` subscription ensures detected entities stay in sync as workspaces open/close

3. **Multiple entities supported**: `getAll()` returns all active workspaces (document + block parent), tools can choose which to operate on

4. **Adapter-driven**: Each entity type defines its own serialization/mutation logic via pluggable adapters

5. **No URL parsing needed**: Workspace Registry already provides entityType and entityUnique from the workspace context itself

---

### ✅ Implemented: Workspace Registry Module

**Status**: Complete (commit `61e1e66`)

**Location**: `src/Umbraco.AI.Agent.Web.StaticAssets/Client/src/workspace-registry/`

The workspace decorator pattern has been implemented as a standalone module that auto-registers workspace contexts for cross-DOM access.

#### Implementation Structure

```
workspace-registry/
├── index.ts                 # Public exports
├── types.ts                 # TypeScript interfaces
├── workspace.registry.ts    # Singleton registry service
└── workspace.decorator.ts   # Manifest API interception
```

#### Key Design Decisions

1. **Entity-based keys**: Uses `${entityType}:${unique}` (e.g., `document:abc-123`) for deduplication instead of UUIDs. This prevents duplicate entries when the same entity is re-opened.

2. **Temporary UUID for new entities**: Workspaces start with a temporary UUID key, then re-key when `unique` observable emits. Handles "create" scenarios where unique isn't immediately available.

3. **LIFO cleanup order**: Navigation-based cleanup processes entries in reverse insertion order, so nested workspaces (block) are removed before parents (document).

4. **Minimal public API**:
    - `getAll(): WorkspaceEntry[]` - All active workspaces
    - `getByEntity(entityType, unique): WorkspaceEntry | undefined` - Specific lookup
    - `changes$: Observable<WorkspaceChangeEvent>` - Change notifications

5. **Navigation cleanup fallback**: Uses `history.pushState/replaceState` interception + `popstate`/`hashchange` events to clean up disconnected workspaces when `destroy()` isn't called.

#### Types

```typescript
interface WorkspaceEntry {
    context: object; // The workspace context instance
    alias: string; // Manifest alias (e.g., "Umb.Workspace.Document")
    entityType: string | undefined; // Entity type (e.g., "document", "media")
    entityUnique: string | undefined; // Entity GUID
}

interface WorkspaceChangeEvent {
    type: "added" | "removed" | "updated";
    key: string;
    entry: WorkspaceEntry;
}
```

#### Decorator Implementation

Uses Umbraco's `loadManifestApi` utility and RxJS for clean manifest observation:

```typescript
extensionRegistry.extensions
    .pipe(
        map((es) => es.filter((e): e is ManifestWorkspace => e.type === "workspace")),
        distinctUntilChanged((a, b) => a.length === b.length),
    )
    .subscribe((workspaceManifests) => {
        for (const manifest of workspaceManifests) {
            if (wrappedAliases.has(manifest.alias) || !manifest.api) continue;

            const originalApi = manifest.api;
            manifest.api = async () => {
                const ApiClass = await loadManifestApi(originalApi);
                return { api: ApiClass ? createDecoratedClass(ApiClass, alias) : ApiClass };
            };
            wrappedAliases.add(alias);
        }
    });
```

The decorated class uses `Proxy` to intercept construction and wrap `destroy()`:

```typescript
return new Proxy(OriginalClass, {
    construct(target, args, newTarget) {
        const instance = Reflect.construct(target, args, newTarget);

        // Register immediately with temporary key
        let currentKey = crypto.randomUUID();
        workspaceRegistry._register(currentKey, createEntry());

        // Subscribe to unique observable for re-keying
        if (uniqueObservable?.subscribe && entityType) {
            subscription = uniqueObservable.subscribe((uniqueValue) => {
                if (uniqueValue && currentKey.includes("-")) {
                    const entityKey = `${entityType}:${uniqueValue}`;
                    workspaceRegistry._rekey(currentKey, entityKey, createEntry(uniqueValue));
                    currentKey = entityKey;
                }
            });
        }

        // Wrap destroy for cleanup
        const originalDestroy = instance.destroy?.bind(instance);
        instance.destroy = () => {
            subscription?.unsubscribe?.();
            workspaceRegistry._unregister(currentKey);
            originalDestroy?.();
        };

        return instance;
    },
});
```

#### Integration Point

Initialized in the entrypoint:

```typescript
// entrypoints/entrypoint.ts
import { initWorkspaceDecorator } from "../workspace-registry/index.js";

export const onInit: UmbEntryPointOnInit = (_host, _extensionRegistry) => {
    initWorkspaceDecorator(_extensionRegistry);
    // ...
};
```

#### Key Findings During Implementation

1. **`extensions` vs `byType()`**: Use `extensionRegistry.extensions` directly instead of `byType("workspace")` - the latter returns kind-merged copies, while `extensions` returns originals that can be mutated.

2. **`loadManifestApi` utility**: Umbraco provides this in `@umbraco-cms/backoffice/extension-api` - no need to implement custom API resolution.

3. **Host element detection**: Workspace contexts expose host element via `getHostElement()`, `_host`, or `host` properties. Use `Node.isConnected` to check if still in DOM.

4. **TypeScript UUID type**: `crypto.randomUUID()` returns a branded UUID type. Explicitly type as `string` when the key may be replaced with entity-based key.

---

### Nested Modal Detection

**No special handling needed.** The Workspace Registry automatically handles modal workspaces:

1. When a modal opens with a workspace → workspace context is constructed → decorator registers it
2. When modal closes → workspace context is destroyed → decorator unregisters it
3. `workspaceRegistry.getAll()` returns all active workspaces in registration order (parent first, then modal)

The Entity Adapter Context sees all workspaces and can determine which is "current" (last in array = most recently opened/innermost).

### Block Editor Modals (Special Case)

Block editors (Block Grid, Block List) **do have their own workspace context**, but they're editing embedded content, not standalone entities:

| Modal Type                     | Has Workspace Context | Persists To                    |
| ------------------------------ | --------------------- | ------------------------------ |
| Nested Entity (DocType editor) | Yes                   | Own database entity            |
| Block Editor                   | Yes                   | Parent entity's property value |

**Key distinction:**

- Block workspace context exists but is NOT a standalone entity
- Block "submit" saves to parent workspace's property value
- Block has local ID within property, not a database ID

**Handling via adapters:**

Block workspaces are registered by Workspace Registry (entityType: `'block'`), but Entity Adapter Context filters them:

```typescript
#refreshDetectedEntities(): void {
  for (const entry of workspaceRegistry.getAll()) {
    const adapter = this.#adapterRegistry.findForWorkspace(entry.context);
    if (adapter) {
      // Has adapter = standalone entity (document, media, etc.)
      detected.push({ ... });
    }
    // No adapter for 'block' entityType = skip
    // Parent document workspace remains as "current entity"
  }
}
```

This means when editing a block:

- Workspace Registry has both: `[document:abc, block:xyz]`
- Entity Adapter Context has only: `[document:abc]` (block filtered out)
- Tools operate on the document, not the block directly

**Future enhancement**: Block-aware tools could optionally query the Workspace Registry directly for block workspaces to provide block-specific operations (e.g., "modify this block's content"). This would require a block-specific adapter that understands the block's relationship to its parent property.

#### Inline Block Editors (Multiple Simultaneous Blocks)

Block List with inline editing mode creates multiple simultaneous block workspace contexts:

```
[WorkspaceRegistry]
- document:abc   [size: 1]
- block:xyz-1    [size: 2]  ← inline block 1
- block:xyz-2    [size: 3]  ← inline block 2
- block:xyz-3    [size: 4]  ← inline block 3
```

Unlike modal blocks (one at a time), inline blocks are all active simultaneously. Registration order doesn't indicate "current" context since they're all visible.

**Possible resolution strategies:**

1. **Focus tracking** - Track which block element has focus or was last interacted with
2. **Explicit selection** - User tells Copilot which block (e.g., "edit the hero block")
3. **Ignore inline blocks** - Only track modal-opened blocks (clearer "current" semantics)
4. **LLM disambiguation** - When multiple blocks active, LLM asks user to clarify
5. **List exposure** - Copilot shows active blocks, user picks one

This is a UX decision that affects how block-aware tools behave. For initial implementation, ignoring inline blocks (option 3) is simplest - modal blocks have clear "user opened this" intent.

### 5. Serialization Models

```typescript
interface UaiSerializedEntity {
    entityType: string;
    unique: string;
    name: string;
    contentType?: string;
    variant?: { culture?: string; segment?: string };
    // Recursive parent context for LLM awareness of hierarchy
    parentContext?: UaiSerializedEntityParent;
    properties: UaiSerializedProperty[];
    metadata?: Record<string, unknown>;
}

// Simplified parent info for LLM (includes name, recursive)
interface UaiSerializedEntityParent {
    entityType: string;
    unique: string;
    name?: string; // Human-readable name for LLM context
    parentContext?: UaiSerializedEntityParent; // Recursive
}

interface UaiSerializedProperty {
    alias: string;
    label: string;
    editorAlias: string;
    value: unknown;
    valueType: "string" | "number" | "boolean" | "array" | "object" | "richtext" | "media" | "unknown";
    readOnly: boolean;
}
```

## Implementation Steps

### Step 0: ✅ Workspace Registry (COMPLETE)

**Prerequisite for cross-DOM context access**

**Files created:**

- `src/.../workspace-registry/types.ts` - WorkspaceEntry, WorkspaceChangeEvent interfaces
- `src/.../workspace-registry/workspace.registry.ts` - Singleton registry with getAll(), getByEntity(), changes$
- `src/.../workspace-registry/workspace.decorator.ts` - Manifest API interception via Proxy
- `src/.../workspace-registry/index.ts` - Public exports

**Integration:**

- `src/.../entrypoints/entrypoint.ts` - Calls `initWorkspaceDecorator(_extensionRegistry)`

### Step 1: Create Extension Type & Interfaces

**Files to create:**

- `src/.../entity-adapter/uai-entity-adapter.extension.ts` - Manifest type, API interface, data models

### Step 2: Create Adapter Registry

**Files to create:**

- `src/.../entity-adapter/entity-adapter.registry.ts` - Adapter discovery, caching, resolution

### Step 3: Create Entity Adapter Context

**Files to create:**

- `src/.../entity-adapter/entity-adapter.context.ts` - Detection, context stack, unified API

**Integration with workspace-registry:**

```typescript
// Entity Adapter Context will consume workspace registry
import { workspaceRegistry } from "../workspace-registry/index.js";

class UaiEntityAdapterContext {
    #detectCurrentEntity(): void {
        // Get all active workspaces
        const workspaces = workspaceRegistry.getAll();

        // Find adapter for each, build context stack
        for (const entry of workspaces) {
            const adapter = this.#adapterRegistry.findForWorkspace(entry.context);
            if (adapter) {
                // Add to detected entities...
            }
        }
    }

    // Subscribe to changes for real-time updates
    constructor() {
        workspaceRegistry.changes$.subscribe((event) => {
            this.#detectCurrentEntity();
        });
    }
}
```

### Step 4: Integrate with CopilotContext

**Files to modify:**

- `src/.../copilot/copilot.context.ts` - Add entity adapter context instantiation, delegation methods

### Step 5: Create Core Adapters

**Files to create:**

- `src/.../entity-adapter/adapters/document-adapter.api.ts`
- `src/.../entity-adapter/adapters/media-adapter.api.ts`
- `src/.../entity-adapter/adapters/manifests.ts`

### Step 6: Create Entity Tools

**Files to create:**

- `src/.../agent/tools/entity/get-current-entity.api.ts`
- `src/.../agent/tools/entity/set-property-value.api.ts`
- `src/.../agent/tools/entity/get-property-schema.api.ts`
- `src/.../agent/tools/entity/manifests.ts`

### Step 7: Register Extensions

**Files to modify:**

- `src/.../entrypoints/manifest.ts` - Add entity adapter and tool manifests

## File Structure

```
src/Umbraco.AI.Agent.Web.StaticAssets/Client/src/
├── workspace-registry/                    # ✅ COMPLETE
│   ├── index.ts                           # Public exports
│   ├── types.ts                           # WorkspaceEntry, WorkspaceChangeEvent
│   ├── workspace.registry.ts              # Singleton registry service
│   └── workspace.decorator.ts             # Manifest API interception
├── entity-adapter/                        # TODO
│   ├── index.ts
│   ├── uai-entity-adapter.extension.ts    # Manifest type & interfaces
│   ├── entity-adapter.registry.ts         # Adapter discovery
│   ├── entity-adapter.context.ts          # Detection & unified API (uses workspace-registry)
│   ├── manifests.ts
│   └── adapters/
│       ├── document-adapter.api.ts
│       ├── media-adapter.api.ts
│       └── manifests.ts
├── agent/tools/entity/                    # TODO
│   ├── get-current-entity.api.ts
│   ├── set-property-value.api.ts
│   ├── get-property-schema.api.ts
│   └── manifests.ts
├── copilot/
│   └── copilot.context.ts                 # To be modified
└── entrypoints/
    └── entrypoint.ts                      # ✅ Modified (initWorkspaceDecorator)
```

## Key Integration Points

### Workspace Context Detection

The adapter's `canHandle()` checks workspace context type:

```typescript
canHandle(ctx: unknown): boolean {
  return typeof ctx?.getEntityType === "function"
    && ctx.getEntityType() === "document";
}
```

### Nested Modal Handling

```typescript
// Modal opens with editor
entityAdapterContext.detectContext(workspaceContext, nestingLevel);

// Modal closes
entityAdapterContext.notifyModalClosed(nestingLevel);
```

### Property Change Flow

1. Tool calls `applyPropertyChange({ alias, value, variant })`
2. Adapter resolves to workspace context's `setPropertyValue()`
3. Change staged in workspace (user sees unsaved indicator)
4. User clicks Save to persist

## Third-Party Extension Pattern

Commerce (or other packages) registers adapters:

```typescript
const orderAdapterManifest: ManifestUaiEntityAdapter = {
    type: "uaiEntityAdapter",
    alias: "Uc.EntityAdapter.Order",
    name: "Order Entity Adapter",
    api: () => import("./order-adapter.api.js"),
    meta: {
        entityType: "uc:order",
        priority: 50,
    },
};
```

### Commerce Adapter Examples

Commerce uses nested workspace paths with varying depths:

**2 levels**: Order → Store

```
/umbraco/section/commerce/workspace/uc:store-management/{storeId}/uc:order/{orderId}
```

**3 levels**: Region → Country → Store

```
/umbraco/section/settings/workspace/uc:store-settings/{storeId}/uc:country/{countryId}/uc:region/{regionId}
```

```typescript
// Helper to build URL from recursive context chain
function buildWorkspaceUrl(section: string, entityContext: UaiEntityContext): string {
    const segments: string[] = [];

    // Walk up the context chain, collect segments
    let ctx: UaiEntityContext | undefined = entityContext;
    while (ctx) {
        if (ctx.unique) {
            segments.unshift(`${ctx.entityType}/${ctx.unique}`);
        } else {
            segments.unshift(`${ctx.entityType}/create`); // Create scenario
        }
        ctx = ctx.parentContext;
    }

    return `/umbraco/section/${section}/workspace/${segments.join("/")}`;
}

// Region adapter (3 levels deep)
class UcRegionEntityAdapterApi implements UaiEntityAdapterApi {
    readonly entityType = "uc:region";

    extractEntityContext(ctx: UcRegionWorkspaceContext): UaiEntityContext {
        return {
            entityType: "uc:region",
            unique: ctx.getUnique(),
            parentContext: {
                entityType: "uc:country",
                unique: ctx.countryId,
                parentContext: {
                    entityType: "uc:store-settings",
                    unique: ctx.storeId,
                },
            },
        };
    }

    getEditorUrl(entityContext: UaiEntityContext): string {
        return buildWorkspaceUrl("settings", entityContext);
        // Result: /umbraco/section/settings/workspace/uc:store-settings/{storeId}/uc:country/{countryId}/uc:region/{regionId}
    }
}
```

This recursive pattern supports any depth of nesting - adapters walk the context chain to build URLs and serialize parent hierarchy for LLM awareness.

## Future Considerations

- **Backend CRUD**: Add optional `create()`, `delete()`, `getRepository()` methods to adapter interface
- **Bulk operations**: Tools for modifying multiple entities
- **Property editor awareness**: Handle complex editors (block grid, media picker) specially
- **Permissions**: Check user permissions before allowing changes

## Critical Files Reference

| File                                           | Purpose                                                           |
| ---------------------------------------------- | ----------------------------------------------------------------- |
| **Implemented**                                |                                                                   |
| `workspace-registry/workspace.registry.ts`     | ✅ Singleton registry for cross-DOM workspace access              |
| `workspace-registry/workspace.decorator.ts`    | ✅ Manifest API interception via Proxy                            |
| `workspace-registry/types.ts`                  | ✅ WorkspaceEntry, WorkspaceChangeEvent interfaces                |
| `entrypoints/entrypoint.ts`                    | ✅ Integration point (initWorkspaceDecorator)                     |
| **To Implement**                               |                                                                   |
| `copilot/copilot.context.ts`                   | Integration point for entity adapter                              |
| `agent/tools/uai-agent-tool.extension.ts`      | Pattern for manifest types                                        |
| **Umbraco CMS Reference**                      |                                                                   |
| `@umbraco-cms/backoffice/extension-api`        | loadManifestApi, UmbExtensionRegistry                             |
| Umbraco CMS `entity.context.ts`                | Entity context pattern                                            |
| Umbraco CMS `document-workspace.context.ts`    | Document workspace reference                                      |
| Umbraco CMS `block-workspace.context.ts`       | Block workspace (entityType: 'block', IS_BLOCK_WORKSPACE_CONTEXT) |
| Umbraco CMS `block-workspace.context-token.ts` | Block workspace type guard                                        |
| Commerce `order-workspace.context.ts`          | Third-party workspace pattern                                     |
