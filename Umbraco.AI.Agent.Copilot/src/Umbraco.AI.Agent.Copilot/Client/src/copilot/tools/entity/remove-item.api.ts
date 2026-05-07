import { PropertyValueOperationToolBase } from "./internal/property-value-tool-base.js";
import { parsePath, readVariant } from "./add-item.api.js";

/**
 * Frontend tool: remove_item.
 *
 * Removes the item with the given blockKey from a collection-shaped property value.
 */
export default class RemoveItemApi extends PropertyValueOperationToolBase {
    protected buildOperation(args: Record<string, unknown>) {
        const path = parsePath(args.path);
        if (path === null) {
            return {
                error: {
                    code: "invalid-path",
                    message:
                        "'path' must be an array alternating property aliases (strings) and block selectors ({ blockKey }).",
                },
            };
        }

        if (typeof args.blockKey !== "string" || args.blockKey.length === 0) {
            return {
                error: {
                    code: "missing-block-key",
                    message: "'blockKey' (UUID string) is required.",
                },
            };
        }

        return {
            operation: "RemoveItem" as const,
            path,
            args: { blockKey: args.blockKey },
            variant: readVariant(args),
        };
    }
}
