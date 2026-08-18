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
 * fine on a personal machine but worth being aware of on shared logins — so on a shared machine,
 * the caller (see `UaiCopilotContext`) ties this store to the backoffice session in two ways:
 * - Scoped by user ({@link setUserScope}): each login reads and writes its own bucket, so switching
 *   who's signed in never surfaces the previous person's threads — it doesn't destroy them, they're
 *   just not visible until that person signs back in themselves.
 * - Bounded by session lifetime: an explicit sign-out clears the current scope's history
 *   immediately, and a session timeout is forgiven only if the user resumes on the same calendar day
 *   ({@link recordTimeout} / {@link consumeTimeout}).
 */

const STORAGE_KEY = "umb:uai-copilot:history";
const SCHEMA_VERSION = 1;

/** Soft cap on the serialized blob, comfortably under the ~5MB per-origin localStorage limit. */
const MAX_BYTES = 2_000_000;

interface StoredThread {
    messages: UaiChatMessage[];
    /** The agent the thread was last run with, so re-opening it resumes with the same one. */
    agentId?: string;
    updatedAt: number;
}

interface StoredBlob {
    version: number;
    threads: Record<string, StoredThread>;
    /**
     * The agent the user last picked, used for items with no thread of their own. Without it, every
     * fresh item would silently drop back to the default agent after a reload.
     */
    lastAgentId?: string;
    /**
     * When a session timeout was last observed, pending a decision on re-authentication (see
     * {@link UaiCopilotHistoryStore.consumeTimeout}). Absent otherwise.
     */
    timedOutAt?: number;
}

export class UaiCopilotHistoryStore {
    #storage?: Storage;
    #maxBytes: number;
    #userScope?: string;

    /**
     * @param storage Storage backend. Omitted (or `undefined`) falls back to `localStorage`; pass
     * `null` to disable persistence outright. The two are distinct on purpose: `undefined` is what a
     * caller supplies when it has no opinion, so it must not silently mean "off" — a test that wants
     * a store with no backend has to say so, and gets the same result whether or not the environment
     * happens to provide `localStorage`.
     * @param maxBytes Soft size cap for the serialized blob; injectable for testing.
     */
    constructor(storage: Storage | null | undefined = safeLocalStorage(), maxBytes: number = MAX_BYTES) {
        this.#storage = storage ?? undefined;
        this.#maxBytes = maxBytes;
    }

    /**
     * Scopes all subsequent reads/writes to the given user, so two different logins on the same
     * browser never see each other's threads — switching scope doesn't touch the other user's data,
     * it just stops looking at it; it's there again if that user comes back.
     *
     * Call once the current user's id is known (it may not be yet, right when the store is first
     * constructed — see `UaiCopilotContext`) and again if the authenticated user ever changes without
     * a full page reload in between. `undefined` (the default) falls back to a single unscoped
     * bucket, which is also what every existing caller/test that never calls this method continues
     * to use.
     */
    setUserScope(userId: string | undefined): void {
        this.#userScope = userId;
    }

    get #storageKey(): string {
        return this.#userScope ? `${STORAGE_KEY}:${this.#userScope}` : STORAGE_KEY;
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

    /** The agent a stored thread was last run with, if any. */
    loadAgentId(key: string): string | undefined {
        return this.#read().threads[key]?.agentId;
    }

    /** The agent the user last picked, used when an item has no thread of its own. */
    getLastAgentId(): string | undefined {
        return this.#read().lastAgentId;
    }

    /** Records the user's agent choice as the fallback for items without their own thread. */
    rememberLastAgentId(agentId: string | undefined): void {
        if (!this.#storage) return;

        const blob = this.#read();
        if (blob.lastAgentId === agentId) return;

        blob.lastAgentId = agentId;
        this.#write(blob);
    }

    /**
     * Saves a thread under a key, along with the agent it ran with. Saving an empty conversation
     * removes the key instead (so an empty chat never counts as stored history). Evicts oldest
     * threads if the blob exceeds the size cap.
     */
    save(key: string, messages: UaiChatMessage[], agentId?: string): void {
        if (!this.#storage) return;
        if (messages.length === 0) {
            this.remove(key);
            return;
        }

        const blob = this.#read();
        blob.threads[key] = { messages, agentId, updatedAt: Date.now() };
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

    /**
     * Records that a session timeout just occurred, so a later {@link consumeTimeout} call (once
     * the user re-authenticates) can decide whether the gap warrants clearing history. An explicit
     * sign-out doesn't go through this — it clears immediately, unconditionally.
     */
    recordTimeout(): void {
        if (!this.#storage) return;
        const blob = this.#read();
        blob.timedOutAt = Date.now();
        this.#write(blob);
    }

    /**
     * Resolves a previously recorded timeout on re-authentication. Crossing a calendar day boundary
     * since the timeout is treated as enough of a break to start fresh — history is cleared
     * regardless of how few hours actually elapsed (e.g. timing out at 11:58pm and returning at
     * 12:05am still clears). A same-day timeout is forgiven. Either way the marker is consumed, so
     * it isn't re-evaluated the next time the store happens to be read. A no-op if no timeout is
     * pending (the common case — most sessions end via tab close, not a live timeout event).
     */
    consumeTimeout(): void {
        if (!this.#storage) return;
        const blob = this.#read();
        const timedOutAt = blob.timedOutAt;
        if (timedOutAt === undefined) return;

        delete blob.timedOutAt;

        if (!isSameLocalDay(timedOutAt, Date.now())) {
            this.clearAll();
            return;
        }

        this.#write(blob);
    }

    /** Wipes all stored history, preferences, and any pending timeout marker for the current scope. */
    clearAll(): void {
        if (!this.#storage) return;
        try {
            this.#storage.removeItem(this.#storageKey);
        } catch {
            // Storage unavailable — nothing to clear.
        }
    }

    #read(): StoredBlob {
        if (!this.#storage) return emptyBlob();
        try {
            const raw = this.#storage.getItem(this.#storageKey);
            if (!raw) return emptyBlob();
            const parsed = JSON.parse(raw) as Partial<StoredBlob>;
            // Discard anything from an older/unknown schema rather than risk misreading it.
            if (parsed?.version !== SCHEMA_VERSION || typeof parsed.threads !== "object" || !parsed.threads) {
                return emptyBlob();
            }
            return {
                version: SCHEMA_VERSION,
                threads: parsed.threads as Record<string, StoredThread>,
                lastAgentId: typeof parsed.lastAgentId === "string" ? parsed.lastAgentId : undefined,
                timedOutAt: typeof parsed.timedOutAt === "number" ? parsed.timedOutAt : undefined,
            };
        } catch {
            return emptyBlob();
        }
    }

    #write(blob: StoredBlob): void {
        if (!this.#storage) return;
        try {
            this.#storage.setItem(this.#storageKey, JSON.stringify(blob));
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

/** Whether two epoch timestamps fall on the same local calendar day. */
function isSameLocalDay(a: number, b: number): boolean {
    const dateA = new Date(a);
    const dateB = new Date(b);
    return (
        dateA.getFullYear() === dateB.getFullYear() &&
        dateA.getMonth() === dateB.getMonth() &&
        dateA.getDate() === dateB.getDate()
    );
}

function safeLocalStorage(): Storage | undefined {
    try {
        return globalThis.localStorage;
    } catch {
        return undefined;
    }
}
