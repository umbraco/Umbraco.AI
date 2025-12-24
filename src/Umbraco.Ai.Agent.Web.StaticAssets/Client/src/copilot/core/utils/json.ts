/**
 * Safely parse JSON with fallback value.
 * @param json The JSON string to parse
 * @param fallback The fallback value if parsing fails (defaults to empty object)
 * @returns The parsed value or fallback
 */
export function safeParseJson<T = Record<string, unknown>>(
  json: string | undefined,
  fallback: T = {} as T
): T {
  if (!json) return fallback;
  try {
    return JSON.parse(json) as T;
  } catch {
    return fallback;
  }
}
