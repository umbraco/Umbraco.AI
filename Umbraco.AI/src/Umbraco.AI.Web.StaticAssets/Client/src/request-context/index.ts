/**
 * Request Context Module
 *
 * Types and helpers for building context items to send to AI operations.
 *
 * @example
 * ```typescript
 * import { createEntityContextItem, type UaiRequestContextItem } from '@umbraco-ai/core/request-context';
 *
 * const context: UaiRequestContextItem[] = [];
 *
 * // Add serialized entity context
 * if (serializedEntity) {
 *   context.push(createEntityContextItem(serializedEntity));
 * }
 *
 * // Add custom context
 * context.push({ description: "User selected text", value: selectedText });
 * ```
 */

// Types
export type { UaiRequestContextItem } from "./types.js";

// Helpers
export { createEntityContextItem, createSelectionContextItem } from "./helpers.js";
