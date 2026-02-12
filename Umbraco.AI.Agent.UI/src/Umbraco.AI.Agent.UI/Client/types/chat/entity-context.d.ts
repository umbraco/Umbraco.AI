import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";
import type { UmbContextMinimal } from "@umbraco-cms/backoffice/context-api";
import type { Observable } from "rxjs";
/**
 * Shared entity context contract.
 *
 * Allows tools to operate on an entity without knowing which surface they're in.
 * The copilot provides it by wrapping the host workspace's entity state.
 * The chat could provide it via a side-drawer editor.
 *
 * Tools that operate on entities consume this context -- they don't know
 * which surface they're in. If the context doesn't exist in the current
 * surface, the tool doesn't have access to entity state.
 *
 * Today this is handled by the tool simply not being registered outside copilot.
 * In the future, Umbraco's conditions framework on the manifest would gate tool
 * resolution automatically (e.g., conditions: [{ alias: "Umb.Condition.Context",
 * context: "UAI_ENTITY_CONTEXT" }]).
 *
 * Extends UmbContextMinimal so it can be used with UmbContextToken.
 */
export interface UaiEntityContextApi extends UmbContextMinimal {
    /** Observable of the current entity type (e.g., "document", "media"). */
    readonly entityType$: Observable<string | undefined>;
    /** Observable of the current entity key. */
    readonly entityKey$: Observable<string | undefined>;
    /**
     * Get a value from the current entity using a JSON path.
     * @param path JSON path to the value (e.g., "title", "price.amount", "inventory.quantity")
     * @returns The value at the path, or undefined if not found
     */
    getValue(path: string): unknown;
    /**
     * Set a value in the current entity using a JSON path.
     * Changes are staged -- the user must click Save to persist.
     * @param path JSON path to the value (e.g., "title", "price.amount", "inventory.quantity")
     * @param value The value to set
     */
    setValue(path: string, value: unknown): void;
    /** Observable indicating whether the entity has unsaved changes. */
    readonly isDirty$: Observable<boolean>;
}
/**
 * Context token for consuming entity context.
 * Frontend tools that operate on entities consume this context.
 */
export declare const UAI_ENTITY_CONTEXT: UmbContextToken<UaiEntityContextApi, UaiEntityContextApi>;
//# sourceMappingURL=entity-context.d.ts.map