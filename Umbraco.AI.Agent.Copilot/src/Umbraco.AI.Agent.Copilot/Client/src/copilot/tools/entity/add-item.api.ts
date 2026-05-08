import { PropertyValueOperationToolBase } from "./internal/property-value-tool-base.js";
import { parsePath, readVariant } from "./internal/path-args.js";

/**
 * Frontend tool: add_item.
 *
 * Adds a new item to a collection-shaped property value (block list, block grid, picker).
 * The dispatcher mints the new item key server-side; the LLM uses the returned blockKey for
 * follow-up operations (e.g. add_item targeting a property nested inside the new item).
 */
export default class AddItemApi extends PropertyValueOperationToolBase {
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

        const opArgs: Record<string, unknown> = {};
        if (typeof args.elementType === "string") {
            opArgs.elementType = args.elementType;
        }
        if (args.values !== undefined) {
            opArgs.values = args.values;
        }
        if (args.settingsValues !== undefined) {
            opArgs.settingsValues = args.settingsValues;
        }
        if (typeof args.position === "number") {
            opArgs.position = args.position;
        }

        const variant = readVariant(args);

        return { operation: "AddItem" as const, path, args: opArgs, variant };
    }
}

