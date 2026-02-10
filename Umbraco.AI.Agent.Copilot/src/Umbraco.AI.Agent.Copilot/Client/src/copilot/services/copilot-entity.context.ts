import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UaiEntityAdapterContext } from "@umbraco-ai/core";
import type { UaiEntityContextApi } from "@umbraco-ai/agent-ui";
import { map, Observable } from "rxjs";

/**
 * Copilot Entity Context
 *
 * Implements UaiEntityContextApi by wrapping UaiEntityAdapterContext from core.
 * This adapter translates the shared entity context contract (from agent-ui)
 * into the core's entity adapter system.
 *
 * Provides:
 * - Entity type and key observables
 * - Property value access via serialized entity
 * - Property value mutation via applyPropertyChange
 * - Dirty state tracking
 *
 * This is the copilot's implementation of the entity context contract.
 * In the future, the chat surface could provide its own implementation via
 * a side-drawer editor.
 */
export class UaiCopilotEntityContext extends UmbControllerBase implements UaiEntityContextApi {
    #entityAdapterContext: UaiEntityAdapterContext;

    /** Cache of serialized properties for fast synchronous access */
    #cachedProperties = new Map<string, unknown>();

    constructor(host: UmbControllerHost, entityAdapterContext: UaiEntityAdapterContext) {
        super(host);
        this.#entityAdapterContext = entityAdapterContext;

        // Update property cache whenever entity changes
        this.observe(this.#entityAdapterContext.selectedEntity$, async (entity) => {
            if (!entity) {
                this.#cachedProperties.clear();
                return;
            }

            // Serialize the entity and cache properties
            const serialized = await this.#entityAdapterContext.serializeSelectedEntity();
            if (serialized) {
                this.#cachedProperties.clear();
                for (const prop of serialized.properties) {
                    this.#cachedProperties.set(prop.alias, prop.value);
                }
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

    // ─── Property Access ────────────────────────────────────────────────────────

    /**
     * Get a property value from the cached serialized entity.
     * This is synchronous and uses the cached property map.
     * @param alias The property alias
     * @returns The property value, or undefined if not found
     */
    getPropertyValue(alias: string): unknown {
        return this.#cachedProperties.get(alias);
    }

    /**
     * Set a property value on the current entity.
     * Changes are staged -- the user must click Save to persist.
     * @param alias The property alias
     * @param value The value to set
     */
    setPropertyValue(alias: string, value: unknown): void {
        // Update the cache optimistically
        this.#cachedProperties.set(alias, value);

        // Apply the change via the entity adapter (async fire-and-forget)
        // The applyPropertyChange returns a Promise, but we don't await it here
        // to keep setPropertyValue synchronous as per the interface
        this.#entityAdapterContext.applyPropertyChange({
            alias,
            value,
        }).catch((error) => {
            console.error(`[UaiCopilotEntityContext] Failed to apply property change for ${alias}:`, error);
            // Revert the optimistic update on error
            this.#cachedProperties.delete(alias);
        });
    }
}

export default UaiCopilotEntityContext;
