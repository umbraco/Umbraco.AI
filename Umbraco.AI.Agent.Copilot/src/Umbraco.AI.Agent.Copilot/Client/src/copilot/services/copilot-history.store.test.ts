import { describe, it, expect, beforeEach } from "vitest";
import type { UaiChatMessage } from "@umbraco-ai/agent-ui";
import { UaiCopilotHistoryStore } from "./copilot-history.store.js";

const STORAGE_KEY = "umb:uai-copilot:history";

/** Minimal in-memory Storage double. */
class FakeStorage implements Storage {
    #map = new Map<string, string>();
    get length() {
        return this.#map.size;
    }
    clear() {
        this.#map.clear();
    }
    getItem(key: string) {
        return this.#map.get(key) ?? null;
    }
    setItem(key: string, value: string) {
        this.#map.set(key, value);
    }
    removeItem(key: string) {
        this.#map.delete(key);
    }
    key(index: number) {
        return [...this.#map.keys()][index] ?? null;
    }
    /** Test helper: raw read. */
    raw(key: string) {
        return this.#map.get(key);
    }
}

function msg(id: string, content: string): UaiChatMessage {
    return { id, role: "user", content, timestamp: new Date("2026-01-01T00:00:00.000Z") };
}

describe("UaiCopilotHistoryStore", () => {
    let storage: FakeStorage;

    beforeEach(() => {
        storage = new FakeStorage();
    });

    it("round-trips a thread and revives the timestamp as a Date", () => {
        const store = new UaiCopilotHistoryStore(storage);
        store.save("document:a", [msg("1", "hello")]);

        const loaded = store.load("document:a");
        expect(loaded).toHaveLength(1);
        expect(loaded![0].content).toBe("hello");
        expect(loaded![0].timestamp).toBeInstanceOf(Date);
    });

    it("has() reflects whether a non-empty thread is stored", () => {
        const store = new UaiCopilotHistoryStore(storage);
        expect(store.has("document:a")).toBe(false);
        store.save("document:a", [msg("1", "hi")]);
        expect(store.has("document:a")).toBe(true);
    });

    it("saving an empty conversation removes the thread instead of storing it", () => {
        const store = new UaiCopilotHistoryStore(storage);
        store.save("document:a", [msg("1", "hi")]);
        store.save("document:a", []);
        expect(store.has("document:a")).toBe(false);
        expect(store.load("document:a")).toBeUndefined();
    });

    it("remove() forgets a thread", () => {
        const store = new UaiCopilotHistoryStore(storage);
        store.save("document:a", [msg("1", "hi")]);
        store.remove("document:a");
        expect(store.load("document:a")).toBeUndefined();
    });

    it("keeps threads isolated per key", () => {
        const store = new UaiCopilotHistoryStore(storage);
        store.save("document:a", [msg("1", "for a")]);
        store.save("document:b", [msg("2", "for b")]);
        expect(store.load("document:a")![0].content).toBe("for a");
        expect(store.load("document:b")![0].content).toBe("for b");
    });

    it("discards the blob on a schema-version mismatch", () => {
        storage.setItem(
            STORAGE_KEY,
            JSON.stringify({ version: 999, threads: { "document:a": { messages: [msg("1", "x")], updatedAt: 1 } } }),
        );
        const store = new UaiCopilotHistoryStore(storage);
        expect(store.load("document:a")).toBeUndefined();
    });

    it("degrades gracefully on corrupt JSON", () => {
        storage.setItem(STORAGE_KEY, "{ not valid json");
        const store = new UaiCopilotHistoryStore(storage);
        expect(() => store.load("document:a")).not.toThrow();
        expect(store.load("document:a")).toBeUndefined();
    });

    it("evicts the least-recently-updated thread when over the size cap, keeping the newest", () => {
        // Tiny cap forces eviction after two threads.
        const store = new UaiCopilotHistoryStore(storage, 400);
        store.save("document:old", [msg("1", "x".repeat(200))]);
        store.save("document:new", [msg("2", "y".repeat(200))]);

        // The just-written key is never evicted; the older one is dropped to fit the cap.
        expect(store.has("document:new")).toBe(true);
        expect(store.has("document:old")).toBe(false);
    });

    it("does nothing (no throw) when no storage backend is available", () => {
        const store = new UaiCopilotHistoryStore(undefined);
        expect(() => store.save("document:a", [msg("1", "hi")])).not.toThrow();
        expect(store.load("document:a")).toBeUndefined();
        expect(store.has("document:a")).toBe(false);
        expect(store.loadAgentId("document:a")).toBeUndefined();
        expect(store.getLastAgentId()).toBeUndefined();
        expect(() => store.rememberLastAgentId("agent-1")).not.toThrow();
    });

    it("stores the agent a thread ran with, per key", () => {
        const store = new UaiCopilotHistoryStore(storage);
        store.save("document:a", [msg("1", "hi")], "agent-legal");
        store.save("document:b", [msg("2", "hi")], "agent-content");

        expect(store.loadAgentId("document:a")).toBe("agent-legal");
        expect(store.loadAgentId("document:b")).toBe("agent-content");
        expect(store.loadAgentId("document:unknown")).toBeUndefined();
    });

    it("remembers the last picked agent independently of any thread", () => {
        const store = new UaiCopilotHistoryStore(storage);
        store.rememberLastAgentId("agent-legal");

        // Survives a fresh store over the same storage — the point is it outlives a reload.
        expect(new UaiCopilotHistoryStore(storage).getLastAgentId()).toBe("agent-legal");
    });

    it("keeps the last picked agent when threads are written and removed", () => {
        const store = new UaiCopilotHistoryStore(storage);
        store.rememberLastAgentId("agent-legal");
        store.save("document:a", [msg("1", "hi")], "agent-content");
        store.remove("document:a");

        expect(store.getLastAgentId()).toBe("agent-legal");
    });

    it("reads a thread saved before agents were recorded", () => {
        // Threads written by an earlier build carry no agentId; they must still load.
        storage.setItem(
            STORAGE_KEY,
            JSON.stringify({
                version: 1,
                threads: { "document:a": { messages: [msg("1", "hi")], updatedAt: 1 } },
            }),
        );
        const store = new UaiCopilotHistoryStore(storage);

        expect(store.load("document:a")).toHaveLength(1);
        expect(store.loadAgentId("document:a")).toBeUndefined();
    });
});
