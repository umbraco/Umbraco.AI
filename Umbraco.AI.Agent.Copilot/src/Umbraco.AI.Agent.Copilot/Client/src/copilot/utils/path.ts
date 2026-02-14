/**
 * JSON Path Navigation Utilities
 *
 * Utilities for navigating and manipulating JSON data using dot-notation paths.
 */

/**
 * Get a value from an object using a JSON path.
 * Supports dot notation (e.g., "price.amount", "inventory.quantity").
 *
 * @param obj The object to navigate
 * @param path The dot-notation path
 * @returns The value at the path, or undefined if not found
 *
 * @example
 * const data = { price: { amount: 29.99, currency: "USD" } };
 * getValueByPath(data, "price.amount"); // 29.99
 * getValueByPath(data, "price.tax"); // undefined
 */
export function getValueByPath(obj: Record<string, unknown>, path: string): unknown {
    const parts = path.split('.');
    let current: unknown = obj;

    for (const part of parts) {
        if (current == null || typeof current !== 'object') {
            return undefined;
        }
        current = (current as Record<string, unknown>)[part];
    }

    return current;
}

/**
 * Set a value in an object using a JSON path.
 * Supports dot notation (e.g., "price.amount", "inventory.quantity").
 * Creates intermediate objects if they don't exist.
 *
 * @param obj The object to modify
 * @param path The dot-notation path
 * @param value The value to set
 *
 * @example
 * const data = {};
 * setValueByPath(data, "price.amount", 29.99);
 * // data is now { price: { amount: 29.99 } }
 */
export function setValueByPath(obj: Record<string, unknown>, path: string, value: unknown): void {
    const parts = path.split('.');
    let current: Record<string, unknown> = obj;

    for (let i = 0; i < parts.length - 1; i++) {
        const part = parts[i];
        if (!(part in current) || typeof current[part] !== 'object') {
            current[part] = {};
        }
        current = current[part] as Record<string, unknown>;
    }

    const lastPart = parts[parts.length - 1];
    current[lastPart] = value;
}
