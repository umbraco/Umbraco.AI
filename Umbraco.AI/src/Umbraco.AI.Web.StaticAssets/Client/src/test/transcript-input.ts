/**
 * A single message of a stored test transcript.
 * Mirrors the `{ role, content }` objects the prompt and agent test features write into `Messages`.
 */
export interface UaiTranscriptMessage {
    role: string;
    content: string;
}

/**
 * Parses the transcript `messages` payload, which the API returns as a JSON element (an array,
 * or occasionally a JSON string) into typed messages. Anything unexpected yields an empty list.
 */
export function parseTranscriptMessages(raw: unknown): UaiTranscriptMessage[] {
    let value = raw;
    if (typeof value === "string") {
        try {
            value = JSON.parse(value);
        } catch {
            return [];
        }
    }
    if (!Array.isArray(value)) return [];

    return value.filter(
        (m): m is UaiTranscriptMessage =>
            typeof m === "object" && m !== null && typeof m.role === "string" && typeof m.content === "string",
    );
}

/**
 * Picks the messages that count as "what the AI was given" for a test run.
 *
 * - Prompt tests (`prompt`): the system message built from the entity context plus the user message(s).
 *   Assistant replies and error entries are not input; a failed run (any `error` entry) has none.
 * - Everything else, including an unknown feature type (agent tests): the last user message. The
 *   transcript also holds the entity-context system message and run-lifecycle system events, which
 *   are noise next to a grader result.
 */
export function pickTranscriptInput(messages: UaiTranscriptMessage[], testFeatureId?: string): UaiTranscriptMessage[] {
    if (testFeatureId === "prompt") {
        // A failed prompt run only records a placeholder system message, not real input.
        if (messages.some((m) => m.role === "error")) return [];
        return messages.filter((m) => m.role === "system" || m.role === "user");
    }

    const lastUser = [...messages].reverse().find((m) => m.role === "user");
    return lastUser ? [lastUser] : [];
}
