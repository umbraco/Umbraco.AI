import { PropertyValueOperationToolBase } from "./internal/property-value-tool-base.js";
import { parsePath, readVariant } from "./internal/path-args.js";

/**
 * Frontend tool: set_value.
 *
 * Replaces the entire value of the property at the path. Best for simple scalar editors
 * (TextBox, TextArea, Numeric, Toggle, DatePicker). For block-list / block-grid / picker
 * collections the agent should prefer add_item / remove_item / move_item.
 *
 * This is a fresh implementation replacing the prior single-string-path version; the prior
 * argument shape is intentionally not preserved.
 */
export default class SetValueApi extends PropertyValueOperationToolBase {
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

        if (!("value" in args)) {
            return {
                error: {
                    code: "missing-value",
                    message: "'value' is required.",
                },
            };
        }

        return {
            operation: "SetValue" as const,
            path,
            args: { value: args.value },
            variant: readVariant(args),
        };
    }
}
