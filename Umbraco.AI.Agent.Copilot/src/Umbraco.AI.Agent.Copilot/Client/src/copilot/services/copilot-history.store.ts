import type { UaiChatMessage } from "@umbraco-ai/agent-ui";

/**
 * Per-node local chat history for the copilot.
 *
 * Stores one conversation per entity (keyed by the detected-entity key, e.g. `document:{guid}`) in
 * `localStorage`, so re-opening the copilot on an item you've chatted with before restores that
 * thread. This is convenience persistence, not a system of record:
 *
 * - All threads live under a single versioned blob. On a schema-version mismatch the whole blob is
 *   discarded rather than migrated (chat history is disposable).
 * - Total size is capped well under the ~5MB `localStorage` budget; when exceeded, least-recently
 *   updated threads are evicted first (never the thread just written).
 * - Every access is defensive: if `localStorage` is unavailable (private mode, disabled) or the blob
 *   is corrupt, the store degrades to a no-op instead of throwing.
 *
 * Note: stored chats can contain content values, sitting in the browser's `localStorage`. That's
 * fine on a personal machine but worth being aware of on shared logins.
 */

const STORAGE_KEY = "umb:uai-copilot:history";
const SCHEMA_VERSION = 1;

/** Soft cap on the serialized blob, comfortably under the ~5MB per-origin localStorage limit. */
const MAX_BYTES = 2_000_000;

interface StoredThread {
    messages: UaiChatMessage[];
    updatedAt: number;
}

interface StoredBlob {
    version: number;
    threads: Record<string, StoredThread>;
}

export class UaiCopilotHistoryStore {
    #storage?: Storage;
    #maxBytes: number;

    /**
     * @param storage Storage backend; defaults to `localStorage`. Injectable for testing. A missing
     * backend (or one that throws on access) disables persistence.
     * @param maxBytes Soft size cap for the serialized blob; injectable for testing.
     */
    constructor(storage: Storage | undefined = safeLocalStorage(), maxBytes: number = MAX_BYTES) {
        this.#storage = storage;
        this.#maxBytes = maxBytes;
    }

    /** Whether a (non-empty) thread is stored for the given key. */
    has(key: string): boolean {
        const thread = this.#read().threads[key];
        return !!thread && thread.messages.length > 0;
    }

    /** Loads the stored thread for a key, reviving message timestamps. Returns undefined if none. */
    load(key: string): UaiChatMessage[] | undefined {
        const thread = this.#read().threads[key];
        if (!thread || thread.messages.length === 0) return undefined;
        return thread.messages.map(reviveMessage);
    }

    /**
     * Saves a thread under a key. Saving an empty conversation removes the key instead (so an empty
     * chat never counts as stored history). Evicts oldest threads if the blob exceeds the size cap.
     */
    save(key: string, messages: UaiChatMessage[]): void {
        if (!this.#storage) return;
        if (messages.length === 0) {
            this.remove(key);
            return;
        }

        const blob = this.#read();
        blob.threads[key] = { messages, updatedAt: Date.now() };
        this.#evict(blob, key);
        this.#write(blob);
    }

    /** Removes the stored thread for a key. */
    remove(key: string): void {
        if (!this.#storage) return;
        const blob = this.#read();
        if (blob.threads[key]) {
            delete blob.threads[key];
            this.#write(blob);
        }
    }

    #read(): StoredBlob {
        if (!this.#storage) return emptyBlob();
        try {
            const raw = this.#storage.getItem(STORAGE_KEY);
            if (!raw) return emptyBlob();
            const parsed = JSON.parse(raw) as Partial<StoredBlob>;
            // Discard anything from an older/unknown schema rather than risk misreading it.
            if (parsed?.version !== SCHEMA_VERSION || typeof parsed.threads !== "object" || !parsed.threads) {
                return emptyBlob();
            }
            return { version: SCHEMA_VERSION, threads: parsed.threads as Record<string, StoredThread> };
        } catch {
            return emptyBlob();
        }
    }

    #write(blob: StoredBlob): void {
        if (!this.#storage) return;
        try {
            this.#storage.setItem(STORAGE_KEY, JSON.stringify(blob));
        } catch {
            // Quota exceeded or storage unavailable — drop silently. History is best-effort.
        }
    }

    /**
     * Evicts least-recently-updated threads until the serialized blob fits the size cap. The thread
     * identified by `keepKey` (the one just written) is never evicted.
     */
    #evict(blob: StoredBlob, keepKey: string): void {
        while (JSON.stringify(blob).length > this.#maxBytes) {
            const evictable = Object.entries(blob.threads)
                .filter(([k]) => k !== keepKey)
                .sort((a, b) => a[1].updatedAt - b[1].updatedAt);
            const oldest = evictable[0];
            if (!oldest) break; // Only the kept thread remains; can't shrink further.
            delete blob.threads[oldest[0]];
            // eslint-disable-next-line no-console
            console.debug(`[copilot-history] evicted oldest thread "${oldest[0]}" to stay under size cap`);
        }
    }
}

function emptyBlob(): StoredBlob {
    return { version: SCHEMA_VERSION, threads: {} };
}

/** JSON revives `timestamp` as a string; message rendering expects a Date. */
function reviveMessage(message: UaiChatMessage): UaiChatMessage {
    return { ...message, timestamp: new Date(message.timestamp) };
}

function safeLocalStorage(): Storage | undefined {
    try {
        return globalThis.localStorage;
    } catch {
        return undefined;
    }
}
