/**
 * Request Context Helpers
 *
 * Helper functions for creating context items.
 */

import type { UaiSerializedEntity } from "../entity-adapter/types.js";
import type { UaiRequestContextItem } from "./types.js";

/**
 * Helper to create context item from serialized entity.
 */
export function createEntityContextItem(entity: UaiSerializedEntity): UaiRequestContextItem {
    return {
        description: `Currently editing ${entity.entityType}: ${entity.name}`,
        value: JSON.stringify(entity),
    };
}

/**
 * Helper to create context item from user text selection.
 */
export function createSelectionContextItem(selectedText: string): UaiRequestContextItem {
    return {
        description: `User selected text: "${selectedText.substring(0, 50)}${selectedText.length > 50 ? "..." : ""}"`,
        value: selectedText,
    };
}
