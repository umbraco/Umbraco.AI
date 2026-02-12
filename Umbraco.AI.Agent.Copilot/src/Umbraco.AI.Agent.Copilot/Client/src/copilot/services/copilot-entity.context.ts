import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UaiEntityAdapterContext } from "@umbraco-ai/core";
import type { UaiEntityContextApi } from "@umbraco-ai/agent-ui";
import { map, Observable } from "rxjs";
import { getValueByPath, setValueByPath } from "../utils/path.js";

/**
 * Copilot Entity Context
 *
 * Implements UaiEntityContextApi by wrapping UaiEntityAdapterContext from core.
 * This adapter translates the shared entity context contract (from agent-ui)
 * into the core's entity adapter system.
 *
 * Provides:
 * - Entity type and key observables
 * - Value access via JSON path navigation
 * - Value mutation via applyValueChange
 * - Dirty state tracking
 *
 * This is the copilot's implementation of the entity context contract.
 * In the future, the chat surface could provide its own implementation via
 * a side-drawer editor.
 */
export class UaiCopilotEntityContext extends UmbControllerBase implements UaiEntityContextApi {
    #entityAdapterContext: UaiEntityAdapterContext;

    /** Cache of serialized entity data for fast synchronous access */
    #cachedData: Record<string, unknown> | null = null;

    constructor(host: UmbControllerHost, entityAdapterContext: UaiEntityAdapterContext) {
        super(host);
        this.#entityAdapterContext = entityAdapterContext;

        // Update data cache whenever entity changes
        this.observe(this.#entityAdapterContext.selectedEntity$, async (entity) => {
            if (!entity) {
                this.#cachedData = null;
                return;
            }

            // Serialize the entity and cache the data
            const serialized = await this.#entityAdapterContext.serializeSelectedEntity();
            if (serialized && serialized.data && typeof serialized.data === 'object') {
                this.#cachedData = serialized.data as Record<string, unknown>;
            } else {
                this.#cachedData = null;
            }
        });
    }

    // ─── Observable Properties ──────────────────────────────────────────────────

    get entityType$(): Observable<string | undefined> {
        return this.#entityAdapterContext.selectedEntity$.pipe(map((entity) => entity?.entityContext.entityType));
    }

    get entityKey$(): Observable<string | undefined> {
        return this.#entityAdapterContext.selectedEntity$.pipe(
            map((entity) => entity?.entityContext.unique ?? undefined),
        );
    }

    get isDirty$(): Observable<boolean> {
        // The entity adapter doesn't track dirty state directly
        // For now, return false - this would need workspace context integration
        // TODO: Integrate with workspace's isDirty$ when available
        return new Observable((subscriber) => {
            subscriber.next(false);
        });
    }

    // ─── Value Access ───────────────────────────────────────────────────────────

    /**
     * Get a value from the cached entity data using a JSON path.
     * This is synchronous and uses the cached data.
     * @param path JSON path to the value (e.g., "title", "price.amount")
     * @returns The value at the path, or undefined if not found
     */
    getValue(path: string): unknown {
        if (!this.#cachedData) {
            return undefined;
        }

        return getValueByPath(this.#cachedData, path);
    }

    /**
     * Set a value in the current entity using a JSON path.
     * Changes are staged -- the user must click Save to persist.
     * @param path JSON path to the value (e.g., "title", "price.amount")
     * @param value The value to set
     */
    setValue(path: string, value: unknown): void {
        // Update the cache optimistically
        if (this.#cachedData) {
            setValueByPath(this.#cachedData, path, value);
        }

        // Apply the change via the entity adapter (async fire-and-forget)
        // The applyValueChange returns a Promise, but we don't await it here
        // to keep setValue synchronous as per the interface
        this.#entityAdapterContext.applyValueChange({
            path,
            value,
        }).catch((error) => {
            console.error(`[UaiCopilotEntityContext] Failed to apply value change for ${path}:`, error);
            // On error, re-serialize to get correct state
            this.#entityAdapterContext.serializeSelectedEntity().then((serialized) => {
                if (serialized && serialized.data && typeof serialized.data === 'object') {
                    this.#cachedData = serialized.data as Record<string, unknown>;
                }
            });
        });
    }
}

export default UaiCopilotEntityContext;
