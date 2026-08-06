import { FilesService } from "../api/sdk.gen.js";
import { agentClientReady } from "../app.js";

/**
 * Matches the file-serving route produced server-side by `AIFileUrlProvider`, capturing the thread
 * and file ids. Kept tolerant of a host prefix so an absolute URL works too.
 */
const FILE_URL_PATTERN = /\/umbraco\/ai\/management\/api\/v\d+\/files\/([^/?#]+)\/([^/?#]+)/i;

/**
 * Splits a stored-file URL into its thread and file ids.
 *
 * @param url - A URL previously produced for a stored file.
 * @returns The ids, or undefined when the URL is not a stored-file URL.
 */
export function parseUaiFileUrl(url: string): { threadId: string; fileId: string } | undefined {
    const match = FILE_URL_PATTERN.exec(url);
    if (!match) {
        return undefined;
    }

    return {
        threadId: decodeURIComponent(match[1]),
        fileId: decodeURIComponent(match[2]),
    };
}

/**
 * Fetches a stored file and returns an object URL for its bytes.
 *
 * The file endpoint is part of the authenticated management API, so it cannot be used as an
 * `<img src>` directly — that request would carry no access token. Callers fetch through the
 * configured API client instead and render the resulting object URL.
 *
 * Callers own the returned URL and must pass it to `URL.revokeObjectURL` when done, otherwise the
 * blob is retained for the lifetime of the document.
 *
 * @param url - The stored-file URL, as it appears on an input content source.
 * @returns An object URL, or undefined when the URL is not a stored-file URL or the fetch fails.
 */
export async function resolveUaiFileObjectUrl(url: string): Promise<string | undefined> {
    const ids = parseUaiFileUrl(url);
    if (!ids) {
        return undefined;
    }

    // The client picks up its auth interceptor during entry-point init; a message can render before
    // that completes, so wait rather than firing an unauthenticated request.
    await agentClientReady;

    const { data, error } = await FilesService.getFile({
        path: ids,
        parseAs: "blob",
    });

    if (error || !data) {
        return undefined;
    }

    return URL.createObjectURL(data as Blob);
}
