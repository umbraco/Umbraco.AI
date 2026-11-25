/**
 * Per-request options that make a refused stream request final.
 *
 * The generated SSE client retries every failed request (a 4xx included) forever with backoff and
 * swallows the error, so a refused stream (403 "Agent not available", an expired 401) looked like an
 * endless "Thinking..." with the same request re-sent. Here a 4xx response is read for its
 * ProblemDetails and thrown as a `UaiStreamRejectedError`, which `onSseError` rethrows so it
 * ends the stream instead of triggering a retry. Network errors and 5xx still retry with backoff.
 */
export interface UaiStreamRequestOptions {
    fetch: (input: RequestInfo | URL, init?: RequestInit) => Promise<Response>;
    onSseError: (error: unknown) => void;
}

/** A stream request the server refused with a 4xx. Never retried. */
export class UaiStreamRejectedError extends Error {
    readonly status: number;

    constructor(status: number, message: string) {
        super(message);
        this.name = "UaiStreamRejectedError";
        this.status = status;
    }
}

async function readProblemMessage(response: Response): Promise<string> {
    try {
        const problem = (await response.json()) as { detail?: unknown; title?: unknown };
        for (const text of [problem.detail, problem.title]) {
            if (typeof text === "string" && text.trim()) return text;
        }
    } catch {
        // No JSON body: fall through to the status text.
    }
    return response.statusText || `The request was refused (${response.status}).`;
}

/** Builds the per-request options that stop the generated SSE client retrying a 4xx. */
export function createStreamRequestOptions(): UaiStreamRequestOptions {
    return {
        fetch: async (input, init) => {
            const response = await globalThis.fetch(input, init);
            if (response.status >= 400 && response.status < 500) {
                throw new UaiStreamRejectedError(response.status, await readProblemMessage(response));
            }
            return response;
        },
        onSseError: (error) => {
            if (error instanceof UaiStreamRejectedError) throw error;
        },
    };
}
