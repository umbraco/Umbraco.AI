import { describe, expect, it } from "vitest";
import { parseTranscriptMessages, pickTranscriptInput } from "./transcript-input.js";

describe("parseTranscriptMessages", () => {
    it("parses an array", () => {
        const raw = [{ role: "user", content: "hi" }];
        expect(parseTranscriptMessages(raw)).toEqual(raw);
    });

    it("parses a JSON string", () => {
        expect(parseTranscriptMessages('[{"role":"user","content":"hi"}]')).toEqual([{ role: "user", content: "hi" }]);
    });

    it("returns [] for invalid JSON", () => {
        expect(parseTranscriptMessages("{nope")).toEqual([]);
    });

    it("returns [] for non-arrays", () => {
        expect(parseTranscriptMessages({ role: "user", content: "hi" })).toEqual([]);
        expect(parseTranscriptMessages(null)).toEqual([]);
    });

    it("drops entries with non-string or null content", () => {
        const raw = [
            { role: "user", content: "ok" },
            { role: "user", content: null },
            { role: "user", content: 5 },
            null,
        ];
        expect(parseTranscriptMessages(raw)).toEqual([{ role: "user", content: "ok" }]);
    });
});

describe("pickTranscriptInput", () => {
    const sys = { role: "system", content: "context" };
    const user = { role: "user", content: "question" };
    const assistant = { role: "assistant", content: "answer" };

    it("returns system and user messages in order for prompt tests", () => {
        const user2 = { role: "user", content: "more" };
        expect(pickTranscriptInput([sys, user, user2, assistant], "prompt")).toEqual([sys, user, user2]);
    });

    it("returns [] for a failed prompt run", () => {
        expect(pickTranscriptInput([sys, { role: "error", content: "boom" }], "prompt")).toEqual([]);
    });

    it("returns only the last user message for agent tests, ignoring lifecycle events", () => {
        const first = { role: "user", content: "first" };
        const started = { role: "system", content: "Run started: 1" };
        expect(pickTranscriptInput([sys, first, user, started, assistant], "agent")).toEqual([user]);
    });

    it("falls back to the last user message when the feature id is unknown", () => {
        expect(pickTranscriptInput([sys, user, assistant], undefined)).toEqual([user]);
    });

    it("returns [] when there is no user message", () => {
        expect(pickTranscriptInput([sys], "agent")).toEqual([]);
    });
});
