/**
 * Entity Adapter Context
 *
 * Provides entity detection and serialization for AI tools.
 * Consumes the Workspace Registry context to track active workspaces and matches
 * them with entity adapters for serialization.
 */

import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import {
    BehaviorSubject,
    combineLatest,
    map,
    type Observable,
    type Subscription,
} from "@umbraco-cms/backoffice/external/rxjs";
import { createExtensionApi } from "@umbraco-cms/backoffice/extension-api";
import { umbExtensionsRegistry } from "@umbraco-cms/backoffice/extension-registry";
import { UAI_WORKSPACE_REGISTRY_CONTEXT, type UaiWorkspaceRegistryContext } from "../workspace-registry/index.js";
import { UAI_ENTITY_ADAPTER_EXTENSION_TYPE, type ManifestEntityAdapter } from "./extension-type.js";
import type {
    UaiDetectedEntity,
    UaiEntityAdapterApi,
    UaiEntityContext,
    UaiPersistResult,
    UaiValueChange,
    UaiValueChangeResult,
    UaiSerializedEntity,
} from "./types.js";

/**
 * The identity of a detected entity: `entityType:unique`, falling back to `entityType:new` for an
 * entity that has no id yet. Shared by detection and the reactive-unique watcher so the two can
 * never disagree about what an entity's key should be.
 */
function buildEntityKey(entityContext: UaiEntityContext): string {
    return `${entityContext.entityType}:${entityContext.unique ?? "new"}`;
}

/**
 * Context for entity adapter operations.
 *
 * Responsibilities:
 * - Watch workspace registry for active workspaces
 * - Match workspaces to entity adapters (from extension registry)
 * - Track detected entities with adapters
 * - Manage selected entity for context injection
 * - Serialize selected entity for LLM context
 */
export class UaiEntityAdapterContext extends UmbControllerBase {
    /** Workspace registry context (consumed) */
    #workspaceRegistry?: UaiWorkspaceRegistryContext;

    /** Cached adapter instances by manifest alias */
    readonly #adaptersCache = new Map<string, UaiEntityAdapterApi>();

    /** All detected entities with matching adapters */
    readonly #detectedEntities$ = new BehaviorSubject<UaiDetectedEntity[]>([]);

    /** Key of the currently selected entity */
    readonly #selectedKey$ = new BehaviorSubject<string | undefined>(undefined);

    /** Subscriptions to workspace observables, keyed by entity key */
    readonly #subscriptions = new Map<string, Subscription[]>();

    /**
     * Last unique observed for a workspace context.
     *
     * A workspace publishes its id on its unique observable before its synchronous `getUnique()`
     * catches up, so an adapter's snapshot read can still say "no id" at the moment we're told the id
     * exists. Remembering what we were told lets detection key the entity correctly on that pass
     * instead of waiting for a later, unprompted refresh that may never come.
     */
    readonly #observedUniques = new WeakMap<object, string>();

    /** Promise that resolves when initial workspace registry consumption and refresh is complete */
    readonly #initialized: Promise<void>;

    /** True while a detection pass is running; see #refresh. */
    #refreshing = false;

    /** Set when a refresh is requested mid-pass, so one more pass runs afterwards. */
    #refreshQueued = false;

    constructor(host: UmbControllerHost) {
        super(host);

        // Create initialization promise that resolves when workspace registry is consumed and initial refresh is done
        this.#initialized = new Promise<void>((resolve) => {
            // Consume the workspace registry context
            this.consumeContext(UAI_WORKSPACE_REGISTRY_CONTEXT, async (registry) => {
                if (!registry) {
                    resolve(); // Resolve even if no registry (no entities available)
                    return;
                }

                this.#workspaceRegistry = registry;

                // Observe workspace registry changes
                this.observe(registry.changes$, () => this.#refresh());

                // Initial detection - wait for it to complete before resolving
                await this.#refresh();
                resolve();
            });
        });
    }

    override destroy(): void {
        for (const subs of this.#subscriptions.values()) {
            subs.forEach((s) => s.unsubscribe());
        }
        this.#subscriptions.clear();
        super.destroy();
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Public API
    // ─────────────────────────────────────────────────────────────────────────────

    /** Observable of all detected entities */
    get detectedEntities$(): Observable<UaiDetectedEntity[]> {
        return this.#detectedEntities$.asObservable();
    }

    /** Observable of the currently selected entity */
    get selectedEntity$(): Observable<UaiDetectedEntity | undefined> {
        return combineLatest([this.#detectedEntities$, this.#selectedKey$]).pipe(
            map(([entities, key]) => entities.find((e) => e.key === key)),
        );
    }

    /**
     * Set the selected entity by key.
     * Called by UI when user selects a different entity context.
     */
    setSelectedEntityKey(key: string | undefined): void {
        this.#selectedKey$.next(key);
    }

    /**
     * Get all detected entities synchronously.
     */
    getDetectedEntities(): UaiDetectedEntity[] {
        return this.#detectedEntities$.getValue();
    }

    /**
     * Get the selected entity synchronously.
     */
    getSelectedEntity(): UaiDetectedEntity | undefined {
        const key = this.#selectedKey$.getValue();
        return this.#detectedEntities$.getValue().find((e) => e.key === key);
    }

    /**
     * Serialize the selected entity for LLM context injection.
     * Returns undefined if no entity is selected.
     * Waits for initialization to complete before attempting to serialize.
     */
    async serializeSelectedEntity(): Promise<UaiSerializedEntity | undefined> {
        // Wait for workspace registry to be consumed and entities to be detected
        await this.#initialized;

        const selected = this.getSelectedEntity();
        if (!selected) return undefined;
        return selected.adapter.serializeForLlm(selected.workspaceContext);
    }

    /**
     * Apply a value change to the currently selected entity.
     * Changes are staged in the workspace - user must save to persist.
     * Waits for initialization to complete before attempting to apply changes.
     * @param change The value change to apply
     * @returns Result indicating success or failure with error message
     */
    async applyValueChange(change: UaiValueChange): Promise<UaiValueChangeResult> {
        // Wait for workspace registry to be consumed and entities to be detected
        await this.#initialized;

        const selected = this.getSelectedEntity();

        if (!selected) {
            return {
                success: false,
                error: "No entity is currently selected",
            };
        }

        if (!selected.adapter.applyValueChange) {
            return {
                success: false,
                error: `Entity type "${selected.entityContext.entityType}" does not support value changes`,
            };
        }

        return selected.adapter.applyValueChange(selected.workspaceContext, change);
    }

    /**
     * Persist the currently selected entity's staged changes (equivalent of the user clicking
     * Save). Returns a structured "not supported" error when the entity type doesn't own a save
     * action — most commonly when the user has a block workspace selected, since block changes
     * save with the parent document.
     */
    async saveSelectedEntity(): Promise<UaiPersistResult> {
        await this.#initialized;

        const selected = this.getSelectedEntity();
        if (!selected) {
            return { success: false, error: "No entity is currently selected." };
        }

        if (!selected.adapter.save) {
            return {
                success: false,
                error: `Entity type "${selected.entityContext.entityType}" cannot be saved directly. If this is a block, switch to the parent document and save from there.`,
            };
        }

        return selected.adapter.save(selected.workspaceContext);
    }

    /**
     * Persist and publish the currently selected entity. Returns a structured "not supported"
     * error for entity types without a publish concept (media, blocks) — publishing only applies
     * to documents.
     */
    async publishSelectedEntity(): Promise<UaiPersistResult> {
        await this.#initialized;

        const selected = this.getSelectedEntity();
        if (!selected) {
            return { success: false, error: "No entity is currently selected." };
        }

        if (!selected.adapter.publish) {
            return {
                success: false,
                error: `Entity type "${selected.entityContext.entityType}" does not support publish. Try save instead.`,
            };
        }

        return selected.adapter.publish(selected.workspaceContext);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Private
    // ─────────────────────────────────────────────────────────────────────────────

    /**
     * Find an adapter that can handle the given workspace context.
     */
    async #findAdapterAsync(workspaceContext: unknown): Promise<UaiEntityAdapterApi | undefined> {
        const manifests = umbExtensionsRegistry.getByType(UAI_ENTITY_ADAPTER_EXTENSION_TYPE) as ManifestEntityAdapter[];

        for (const manifest of manifests) {
            let adapter = this.#adaptersCache.get(manifest.alias);
            if (!adapter) {
                try {
                    adapter = await createExtensionApi<UaiEntityAdapterApi>(this, manifest);
                    if (adapter) {
                        this.#adaptersCache.set(manifest.alias, adapter);
                    }
                } catch (e) {
                    console.error(`[UaiEntityAdapterContext] Failed to load adapter ${manifest.alias}:`, e);
                    continue;
                }
            }
            if (adapter?.canHandle(workspaceContext)) {
                return adapter;
            }
        }
        return undefined;
    }

    /**
     * Refresh detected entities, coalescing overlapping requests.
     *
     * Detection is async (adapters load lazily) and is now triggered from two places — registry
     * changes and a workspace's unique resolving — so two passes can overlap. Serializing them keeps
     * the emitted entity list from interleaving, and the trailing re-run makes sure a change that
     * landed mid-pass is still picked up.
     */
    async #refresh(): Promise<void> {
        if (this.#refreshing) {
            this.#refreshQueued = true;
            return;
        }

        this.#refreshing = true;
        try {
            do {
                this.#refreshQueued = false;
                await this.#refreshOnce();
            } while (this.#refreshQueued);
        } finally {
            this.#refreshing = false;
        }
    }

    /**
     * Refresh detected entities from workspace registry.
     */
    async #refreshOnce(): Promise<void> {
        if (!this.#workspaceRegistry) return;

        const entries = this.#workspaceRegistry.getAll();
        const detected: UaiDetectedEntity[] = [];
        const currentKeys = new Set<string>();

        for (const entry of entries) {
            // Find an adapter that can handle this workspace
            const adapter = await this.#findAdapterAsync(entry.context);

            if (adapter) {
                const entityContext = this.#withObservedUnique(
                    adapter.extractEntityContext(entry.context),
                    entry.context,
                );
                const key = buildEntityKey(entityContext);
                currentKeys.add(key);

                detected.push({
                    key,
                    name: adapter.getName(entry.context),
                    icon: adapter.getIcon?.(entry.context),
                    entityContext,
                    adapter,
                    workspaceContext: entry.context,
                });

                // Subscribe to observables if not already subscribed
                if (!this.#subscriptions.has(key)) {
                    this.#subscribeToAdapter(key, adapter, entry.context);
                }
            }
            // No adapter match = skip (e.g., block workspaces without adapter)
        }

        // Clean up subscriptions for removed entities
        for (const [key, subs] of this.#subscriptions) {
            if (!currentKeys.has(key)) {
                subs.forEach((s) => s.unsubscribe());
                this.#subscriptions.delete(key);
            }
        }

        this.#detectedEntities$.next(detected);

        // Auto-select deepest (last) if no selection or selection no longer exists
        const currentKey = this.#selectedKey$.getValue();
        if (!currentKey || !detected.find((e) => e.key === currentKey)) {
            this.#selectedKey$.next(detected[detected.length - 1]?.key);
        }
    }

    /**
     * Fills in an entity's unique from what its workspace last told us, when the adapter's snapshot
     * read hasn't caught up yet. Only ever fills a gap — a unique the adapter reports always wins.
     */
    #withObservedUnique(entityContext: UaiEntityContext, ctx: unknown): UaiEntityContext {
        if (entityContext.unique || !ctx || typeof ctx !== "object") return entityContext;

        const observed = this.#observedUniques.get(ctx as object);
        return observed ? { ...entityContext, unique: observed } : entityContext;
    }

    /**
     * Subscribe to adapter observables for reactive updates (name, icon).
     */
    #subscribeToAdapter(key: string, adapter: UaiEntityAdapterApi, ctx: unknown): void {
        const subs: Subscription[] = [];

        // Watch the unique so a key built before the entity finished loading doesn't stay stale.
        // A workspace usually registers while its entity is still loading, so detection sees a null
        // unique and keys it as ":new" — which would otherwise be its identity for the rest of the
        // session, colliding with every other unloaded entity of the same type. Re-detect when the
        // key this workspace *should* have no longer matches the one it was registered under; that
        // comparison also absorbs the subscription's immediate replay, which by definition matches.
        const uniqueObservable = adapter.getUniqueObservable?.(ctx);
        if (uniqueObservable) {
            subs.push(
                uniqueObservable.subscribe((unique) => {
                    if (unique && ctx && typeof ctx === "object") {
                        this.#observedUniques.set(ctx as object, unique);
                    }

                    // Compare against what the entity's key *should* now be. The immediate replay on
                    // subscribe matches by definition, so only a genuine change re-runs detection.
                    const next = buildEntityKey(
                        this.#withObservedUnique(adapter.extractEntityContext(ctx), ctx),
                    );
                    if (next !== key) {
                        void this.#refresh();
                    }
                }),
            );
        }

        // Subscribe to name observable if available
        const nameObservable = adapter.getNameObservable?.(ctx);
        if (nameObservable) {
            subs.push(
                nameObservable.subscribe((name) => {
                    this.#updateEntityProperty(key, "name", name ?? "Untitled");
                }),
            );
        }

        // Subscribe to icon observable if available
        const iconObservable = adapter.getIconObservable?.(ctx);
        if (iconObservable) {
            subs.push(
                iconObservable.subscribe((icon) => {
                    this.#updateEntityProperty(key, "icon", icon);
                }),
            );
        }

        if (subs.length > 0) {
            this.#subscriptions.set(key, subs);
        }
    }

    /**
     * Update a property of a detected entity.
     */
    #updateEntityProperty(key: string, property: "name" | "icon", value: string | undefined): void {
        const entities = this.#detectedEntities$.getValue();
        const index = entities.findIndex((e) => e.key === key);

        if (index !== -1 && entities[index][property] !== value) {
            // Create new array with updated entity (immutable update)
            const updated = [...entities];
            updated[index] = { ...updated[index], [property]: value };
            this.#detectedEntities$.next(updated);
        }
    }
}
