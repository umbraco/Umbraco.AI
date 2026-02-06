/**
 * Converts a string to camelCase by splitting on separators (-, _, ., spaces).
 * Used for converting tool/scope IDs to localization key format.
 *
 * @example
 * toCamelCase("content-read") // "contentRead"
 * toCamelCase("media_write") // "mediaWrite"
 * toCamelCase("some.scope") // "someScope"
 */
export function toCamelCase(str: string): string {
    return str
        .split(/[-_.\s]+/)
        .map((word, index) =>
            index === 0 ? word.toLowerCase() : word.charAt(0).toUpperCase() + word.slice(1).toLowerCase(),
        )
        .join("");
}
