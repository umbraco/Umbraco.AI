import { PropertyValueOperationToolBase } from "./internal/property-value-tool-base.js";
import { parsePath, readVariant } from "./add-item.api.js";

/**
 * Frontend tool: clear_value.
 *
 * Clears the value of the property at the path (sets it to the editor's empty representation).
 */
export default class ClearValueApi extends PropertyValueOperationToolBase {
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

        return {
            operation: "ClearValue" as const,
            path,
            args: undefined,
            variant: readVariant(args),
        };
    }
}
