import { PropertyValueOperationToolBase } from "./internal/property-value-tool-base.js";
import type { PropertyPathSegment } from "./internal/property-value-operation.client.js";

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

function parsePath(input: unknown): PropertyPathSegment[] | null {
    if (!Array.isArray(input) || input.length === 0) {
        return null;
    }

    const result: PropertyPathSegment[] = [];
    for (let i = 0; i < input.length; i++) {
        const segment = input[i];
        if (i % 2 === 0) {
            if (typeof segment !== "string" || segment.length === 0) {
                return null;
            }
            result.push(segment);
        } else {
            if (
                typeof segment !== "object" ||
                segment === null ||
                typeof (segment as { blockKey?: unknown }).blockKey !== "string"
            ) {
                return null;
            }
            result.push({ blockKey: (segment as { blockKey: string }).blockKey });
        }
    }

    return result;
}

function readVariant(args: Record<string, unknown>) {
    const culture = typeof args.culture === "string" ? args.culture : null;
    const segment = typeof args.segment === "string" ? args.segment : null;
    if (culture === null && segment === null) {
        return undefined;
    }
    return { culture, segment };
}

export { parsePath, readVariant };
