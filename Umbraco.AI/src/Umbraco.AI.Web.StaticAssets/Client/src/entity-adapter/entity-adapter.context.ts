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
    UaiValueChange,
    UaiValueChangeResult,
    UaiSerializedEntity,
} from "./types.js";

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

    /** Promise that resolves when initial workspace registry consumption and refresh is complete */
    readonly #initialized: Promise<void>;

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
     * Refresh detected entities from workspace registry.
     */
    async #refresh(): Promise<void> {
        if (!this.#workspaceRegistry) return;

        const entries = this.#workspaceRegistry.getAll();
        const detected: UaiDetectedEntity[] = [];
        const currentKeys = new Set<string>();

        for (const entry of entries) {
            // Find an adapter that can handle this workspace
            const adapter = await this.#findAdapterAsync(entry.context);

            if (adapter) {
                const entityContext = adapter.extractEntityContext(entry.context);
                const key = `${entityContext.entityType}:${entityContext.unique ?? "new"}`;
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
     * Subscribe to adapter observables for reactive updates (name, icon).
     */
    #subscribeToAdapter(key: string, adapter: UaiEntityAdapterApi, ctx: unknown): void {
        const subs: Subscription[] = [];

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
