import { PropertyValueOperationToolBase } from "./internal/property-value-tool-base.js";
import { parsePath, readVariant } from "./add-item.api.js";

/**
 * Frontend tool: move_item.
 *
 * Moves the item with the given blockKey to a new zero-based position in a collection-shaped
 * property value.
 */
export default class MoveItemApi extends PropertyValueOperationToolBase {
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

        if (typeof args.position !== "number" || !Number.isInteger(args.position) || args.position < 0) {
            return {
                error: {
                    code: "invalid-position",
                    message: "'position' must be a non-negative integer.",
                },
            };
        }

        return {
            operation: "MoveItem" as const,
            path,
            args: { blockKey: args.blockKey, position: args.position },
            variant: readVariant(args),
        };
    }
}
