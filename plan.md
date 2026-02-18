# Frontend Context Contributor Manifest — Implementation Plan

## Problem

Context assembly in the copilot is **hardcoded** in `copilot.context.ts:sendUserMessage()`:

```typescript
// Hardcoded surface context
context.push({ description: "surface", value: JSON.stringify({ surface: "copilot" }) });
// Hardcoded section context
const currentSection = getSectionPathnameFromUrl();
if (currentSection) { context.push({ ... }); }
// Hardcoded entity context
const entityContext = await this.#entityAdapterContext.serializeSelectedEntity();
if (entityContext) { context.push({ ... }); }
```

This prevents extensibility — third parties can't add context, the Prompt package can't reuse the same contributors, and adding new context types requires modifying copilot code.

## Goal

Introduce a **manifest-driven request context contributor system** that mirrors the backend `IAIRuntimeContextContributor` pipeline. Contributors register via the extension registry, a collector gathers and invokes them, and any consumer (copilot, prompt, future chat) calls the collector to build the `UaiRequestContextItem[]` array.

### Naming Rationale

The **backend** has `IAIRuntimeContextContributor` — it contributes to the `AIRuntimeContext` by processing `AIRequestContextItem`s that arrive in the HTTP request.

The **frontend** produces those request items. So the frontend side is `UaiRequestContextContributor` — it contributes `UaiRequestContextItem`s that get sent in the request. This aligns with the existing `request-context/` module that already owns `UaiRequestContextItem`.

## Design Decisions

1. **Core request context contributors are unconditional** — section and entity contributors always run regardless of consumer. No condition needed.
2. **Product-specific contributors use product-specific conditions** — surface identity is an agent concern (uses `UaiAgentSurfaceCondition` from Agent.UI/Copilot). Prompt-specific context would use prompt conditions.
3. **The manifest type and collector live in Core** (`@umbraco-ai/core`) since request context items are a Core concept (`UaiRequestContextItem`).
4. **Contributors use `ManifestApi` pattern** matching `uaiAgentFrontendTool` — lazy-loaded API class with a `contribute(context)` method, mirroring the backend `void Contribute(AIRuntimeContext context)` pattern.

## Architecture

```
@umbraco-ai/core (Umbraco.AI)
├── ManifestUaiRequestContextContributor (type definition)
├── UaiRequestContextContributorApi (interface)
├── UaiRequestContextCollector (collects + invokes contributors)
├── SectionRequestContextContributor (unconditional)
└── EntityRequestContextContributor (unconditional)

@umbraco-ai/agent-copilot (Umbraco.AI.Agent.Copilot)
├── SurfaceRequestContextContributor (copilot-scoped)
└── Updated copilot.context.ts (uses UaiRequestContextCollector)
```

## Implementation Steps

### Step 1: Define the manifest type and API interface in Core

**New file:** `Umbraco.AI/src/Umbraco.AI.Web.StaticAssets/Client/src/request-context/extension-type.ts`

```typescript
import type { ManifestApi } from "@umbraco-cms/backoffice/extension-api";
import type { UmbApi } from "@umbraco-cms/backoffice/extension-api";
import type { UaiRequestContextItem } from "./types.js";

/**
 * Extension type alias for request context contributors.
 */
export const UAI_REQUEST_CONTEXT_CONTRIBUTOR_EXTENSION_TYPE = "uaiRequestContextContributor";

/**
 * Mutable context bag passed to each contributor.
 * Mirrors the backend AIRuntimeContext pattern — contributors
 * call `add()` to push context items rather than returning them.
 */
export class UaiRequestContext {
    readonly #items: UaiRequestContextItem[] = [];

    /**
     * Add a context item to the request context.
     */
    add(item: UaiRequestContextItem): void {
        this.#items.push(item);
    }

    /**
     * Get all contributed context items.
     */
    getItems(): UaiRequestContextItem[] {
        return [...this.#items];
    }
}

/**
 * API interface for request context contributors.
 * Implement this to contribute context items to AI requests.
 *
 * Frontend counterpart of the backend IAIRuntimeContextContributor.
 * While the backend contributors *process* request context items into
 * runtime context, frontend contributors *produce* the request context
 * items that get sent in the request.
 *
 * Mirrors the backend signature: `void Contribute(AIRuntimeContext context)`
 */
export interface UaiRequestContextContributorApi extends UmbApi {
    /**
     * Contribute context items to the request context.
     * Called once per message send / prompt execution.
     * Add items via `context.add(item)`. No-op to contribute nothing.
     *
     * @param context The mutable request context to contribute to.
     */
    contribute(context: UaiRequestContext): Promise<void>;
}

/**
 * Manifest for request context contributor extensions.
 *
 * Request context contributors are invoked before each AI request to gather
 * ambient context (current section, entity, surface, etc.) into
 * UaiRequestContextItem[] for the backend.
 *
 * Core contributors are unconditional (always run).
 * Product-specific contributors can use Umbraco's conditions framework
 * to gate when they contribute.
 *
 * @example
 * ```typescript
 * // Unconditional contributor (always contributes)
 * const manifest: ManifestUaiRequestContextContributor = {
 *     type: "uaiRequestContextContributor",
 *     alias: "UmbracoAI.RequestContextContributor.Section",
 *     name: "Section Request Context Contributor",
 *     api: () => import("./section.contributor.js"),
 *     weight: 100,
 * };
 *
 * // Conditional contributor (only in copilot surface)
 * const manifest: ManifestUaiRequestContextContributor = {
 *     type: "uaiRequestContextContributor",
 *     alias: "UmbracoAI.RequestContextContributor.Surface",
 *     name: "Surface Request Context Contributor",
 *     api: () => import("./surface.contributor.js"),
 *     weight: 200,
 *     conditions: [{ alias: "Umb.Condition.SomeCondition", ... }],
 * };
 * ```
 */
export interface ManifestUaiRequestContextContributor extends ManifestApi<UaiRequestContextContributorApi> {
    type: typeof UAI_REQUEST_CONTEXT_CONTRIBUTOR_EXTENSION_TYPE;
}

declare global {
    interface UmbExtensionManifestMap {
        uaiRequestContextContributor: ManifestUaiRequestContextContributor;
    }
}
```

### Step 2: Create the request context collector in Core

**New file:** `Umbraco.AI/src/Umbraco.AI.Web.StaticAssets/Client/src/request-context/request-context-collector.ts`

The collector queries `uaiRequestContextContributor` manifests from the extension registry and invokes them all when `collect()` is called. Uses the CMS-provided `createExtensionApiByAlias` to load API instances (same pattern as `agent-permissions-workspace-view.element.ts`), with caching to avoid reloading on each `collect()` call.

```typescript
import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import {
    createExtensionApiByAlias,
    umbExtensionsRegistry,
} from "@umbraco-cms/backoffice/extension-registry";
import {
    UAI_REQUEST_CONTEXT_CONTRIBUTOR_EXTENSION_TYPE,
    UaiRequestContext,
    type ManifestUaiRequestContextContributor,
    type UaiRequestContextContributorApi,
} from "./extension-type.js";
import type { UaiRequestContextItem } from "./types.js";

/**
 * Collects request context from all registered contributors.
 *
 * Queries uaiRequestContextContributor extensions and uses the CMS
 * `createExtensionApiByAlias` to load + instantiate each API.
 * API instances are cached for the lifetime of the collector.
 */
export class UaiRequestContextCollector extends UmbControllerBase {
    readonly #apiCache = new Map<string, UaiRequestContextContributorApi>();

    constructor(host: UmbControllerHost) {
        super(host);
    }

    /**
     * Collect request context items from all resolved contributors.
     * Creates a mutable UaiRequestContext, passes it through each
     * contributor (mirroring the backend Contribute pattern), and
     * returns the accumulated items.
     *
     * @returns Aggregated request context items from all contributors.
     */
    async collect(): Promise<UaiRequestContextItem[]> {
        const manifests = umbExtensionsRegistry.getByType(
            UAI_REQUEST_CONTEXT_CONTRIBUTOR_EXTENSION_TYPE,
        ) as ManifestUaiRequestContextContributor[];

        const context = new UaiRequestContext();

        for (const manifest of manifests) {
            try {
                const api = await this.#getOrLoadApi(manifest);
                if (api) {
                    await api.contribute(context);
                }
            } catch (e) {
                console.error(
                    `[UaiRequestContextCollector] Contributor ${manifest.alias} failed:`, e,
                );
            }
        }

        return context.getItems();
    }

    async #getOrLoadApi(
        manifest: ManifestUaiRequestContextContributor,
    ): Promise<UaiRequestContextContributorApi | undefined> {
        const cached = this.#apiCache.get(manifest.alias);
        if (cached) return cached;

        const api = await createExtensionApiByAlias<UaiRequestContextContributorApi>(
            this,
            manifest.alias,
        );

        this.#apiCache.set(manifest.alias, api);
        return api;
    }
}
```

### Step 3: Implement the Section request context contributor in Core

**New file:** `Umbraco.AI/src/Umbraco.AI.Web.StaticAssets/Client/src/request-context/contributors/section.contributor.ts`

Extracts the current section from the URL and creates a request context item. Unconditional — always runs.

```typescript
import type { UaiRequestContextContributorApi, UaiRequestContext } from "../extension-type.js";

/**
 * Contributes the current backoffice section to the request context.
 * Frontend counterpart of backend SectionContextContributor.
 *
 * Unconditional — always contributes when a section is detected.
 */
export default class UaiSectionRequestContextContributor implements UaiRequestContextContributorApi {
    async contribute(context: UaiRequestContext): Promise<void> {
        const section = this.#getSectionFromUrl();
        if (!section) return;

        context.add({
            description: `Current section: ${section}`,
            value: JSON.stringify({ section }),
        });
    }

    #getSectionFromUrl(): string | null {
        const match = window.location.pathname.match(/\/section\/([^/]+)/);
        return match?.[1] ?? null;
    }

    destroy(): void { /* no-op */ }
}
```

### Step 4: Implement the Entity request context contributor in Core

**New file:** `Umbraco.AI/src/Umbraco.AI.Web.StaticAssets/Client/src/request-context/contributors/entity.contributor.ts`

Consumes `UaiEntityAdapterContext` (from the workspace registry + entity adapter system) and serializes the selected entity. Unconditional — always runs; contributes nothing if no entity is selected.

```typescript
import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UaiEntityAdapterContext } from "../../entity-adapter/entity-adapter.context.js";
import { createEntityContextItem } from "../helpers.js";
import type { UaiRequestContextContributorApi, UaiRequestContext } from "../extension-type.js";

/**
 * Contributes the currently selected entity to the request context.
 * Frontend counterpart of backend SerializedEntityContributor.
 *
 * Unconditional — always contributes when an entity is selected.
 * No-op if no entity is open or no adapter matches.
 */
export default class UaiEntityRequestContextContributor
    extends UmbControllerBase
    implements UaiRequestContextContributorApi
{
    #entityAdapterContext: UaiEntityAdapterContext;

    constructor(host: UmbControllerHost) {
        super(host);
        this.#entityAdapterContext = new UaiEntityAdapterContext(host);
    }

    async contribute(context: UaiRequestContext): Promise<void> {
        const serialized = await this.#entityAdapterContext.serializeSelectedEntity();
        if (!serialized) return;

        context.add(createEntityContextItem(serialized));
    }
}
```

**Note:** The entity contributor creates its own `UaiEntityAdapterContext` instance. Since contributors are instantiated once and cached by the collector, the overhead is a single extra adapter context that observes the same workspace registry. This is acceptable — the adapter context is lightweight and reactive.

### Step 5: Register contributor manifests in Core

**New file:** `Umbraco.AI/src/Umbraco.AI.Web.StaticAssets/Client/src/request-context/manifests.ts`

```typescript
import type { ManifestUaiRequestContextContributor } from "./extension-type.js";

export const requestContextManifests: ManifestUaiRequestContextContributor[] = [
    {
        type: "uaiRequestContextContributor",
        alias: "UmbracoAI.RequestContextContributor.Section",
        name: "Section Request Context Contributor",
        api: () => import("./contributors/section.contributor.js"),
        weight: 100,
    },
    {
        type: "uaiRequestContextContributor",
        alias: "UmbracoAI.RequestContextContributor.Entity",
        name: "Entity Request Context Contributor",
        api: () => import("./contributors/entity.contributor.js"),
        weight: 200,
    },
];
```

**Updated file:** `Umbraco.AI/src/Umbraco.AI.Web.StaticAssets/Client/src/manifests.ts`

Add `...requestContextManifests` to the aggregated manifests array.

### Step 6: Implement Surface request context contributor in Copilot

**New file:** `Umbraco.AI.Agent.Copilot/src/.../Client/src/copilot/contributors/surface.contributor.ts`

```typescript
import type { UaiRequestContextContributorApi, UaiRequestContext } from "@umbraco-ai/core";

/**
 * Contributes the copilot surface identifier to the request context.
 * Frontend counterpart of backend SurfaceContextContributor.
 *
 * Copilot-scoped — registered only in the copilot bundle.
 */
export default class UaiCopilotSurfaceRequestContextContributor
    implements UaiRequestContextContributorApi
{
    async contribute(context: UaiRequestContext): Promise<void> {
        context.add({
            description: "surface",
            value: JSON.stringify({ surface: "copilot" }),
        });
    }

    destroy(): void { /* no-op */ }
}
```

Register in copilot manifests. For now, no condition needed since it's only registered in the copilot bundle. Later, when conditions are needed, they can be added.

### Step 7: Update copilot to use UaiRequestContextCollector

**Updated file:** `Umbraco.AI.Agent.Copilot/src/.../Client/src/copilot/copilot.context.ts`

Replace the hardcoded context assembly in `sendUserMessage()`:

```typescript
// Before:
async sendUserMessage(content: string): Promise<void> {
    const entityContext = await this.#entityAdapterContext.serializeSelectedEntity();
    const context: Array<{ description: string; value: string }> = [];
    // ... hardcoded context items ...
    this.#runController.sendUserMessage(content, context);
}

// After:
async sendUserMessage(content: string): Promise<void> {
    const context = await this.#requestContextCollector.collect();
    this.#runController.sendUserMessage(content, context);
}
```

The copilot constructor creates a `UaiRequestContextCollector` instance. The hardcoded section/entity/surface logic is removed — the registered contributors handle it.

### Step 8: Update exports

**Updated file:** `Umbraco.AI/src/Umbraco.AI.Web.StaticAssets/Client/src/request-context/exports.ts`

Add exports for the new types:

```typescript
export { type UaiRequestContextItem, createEntityContextItem, createSelectionContextItem } from "./index.js";
export {
    UaiRequestContext,
    type UaiRequestContextContributorApi,
    type ManifestUaiRequestContextContributor,
} from "./extension-type.js";
export { UaiRequestContextCollector } from "./request-context-collector.js";
```

### Step 9: Update `@umbraco-ai/core` type declarations

The types file that add-on packages consume (`types/umbraco-ai-core-types.d.ts` or equivalent) needs to export `UaiRequestContextContributorApi`, `ManifestUaiRequestContextContributor`, and `UaiRequestContextCollector` so the Copilot and other consumers can import them.

## File Summary

| File | Action | Package |
|------|--------|---------|
| `request-context/extension-type.ts` | **New** — manifest type + API interface | Core |
| `request-context/request-context-collector.ts` | **New** — collector class | Core |
| `request-context/contributors/section.contributor.ts` | **New** — section contributor | Core |
| `request-context/contributors/entity.contributor.ts` | **New** — entity contributor | Core |
| `request-context/manifests.ts` | **New** — manifest registrations | Core |
| `request-context/exports.ts` | **Edit** — add new exports | Core |
| `request-context/index.ts` | **Edit** — add new re-exports | Core |
| `manifests.ts` (root) | **Edit** — add `requestContextManifests` | Core |
| `copilot/contributors/surface.contributor.ts` | **New** — surface contributor | Copilot |
| `copilot/manifests.ts` | **Edit** — add surface contributor manifest | Copilot |
| `copilot/copilot.context.ts` | **Edit** — use `UaiRequestContextCollector` | Copilot |

## Not In Scope

- **Conditions on contributors** — The infrastructure supports conditions via the extension registry, but no custom condition types are defined in this pass. Core contributors are unconditional. The copilot surface contributor is scoped by being in the copilot bundle.
- **Prompt package integration** — The Prompt package can consume `UaiRequestContextCollector` when it needs context for prompt execution. That's a separate task.
- **Ordering guarantees** — Contributors use `weight` for ordering, matching Umbraco's extension registry pattern. No explicit pipeline ordering like the backend's `OrderedCollectionBuilder`.
