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
 * Helper to create context item from a serialized element (e.g., a block within a document).
 * Uses "elementType" instead of "entityType" so the backend can distinguish element from entity context.
 */
export function createElementContextItem(entity: UaiSerializedEntity): UaiRequestContextItem {
    // Re-serialize with "elementType" field instead of "entityType" for backend detection
    const elementPayload = {
        elementType: entity.entityType,
        unique: entity.unique,
        name: entity.name,
        data: entity.data,
    };
    return {
        description: `Currently editing element: ${entity.name}`,
        value: JSON.stringify(elementPayload),
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
