import { describe, expect, it } from "vitest";
import { stashPendingFirstMessage, takePendingFirstMessage } from "./pending-first-message.js";

const A = "11111111-1111-1111-1111-111111111111";
const B = "22222222-2222-2222-2222-222222222222";

describe("pendingFirstMessage", () => {
    it("returns nothing when nothing was stashed", () => {
        expect(takePendingFirstMessage(A)).toBeUndefined();
    });

    it("replays the turn for the conversation it was stashed for", () => {
        stashPendingFirstMessage(A, { content: "Hello" });

        expect(takePendingFirstMessage(A)).toEqual({ content: "Hello" });
    });

    it("carries content parts alongside the text", () => {
        const contentParts = [{ type: "text", text: "Hello" }] as never;
        stashPendingFirstMessage(A, { content: "Hello", contentParts });

        expect(takePendingFirstMessage(A)?.contentParts).toBe(contentParts);
    });

    it("drains on take so the turn cannot replay twice", () => {
        stashPendingFirstMessage(A, { content: "Hello" });
        takePendingFirstMessage(A);

        expect(takePendingFirstMessage(A)).toBeUndefined();
    });

    it("drops a turn whose conversation was abandoned, rather than replaying it later", () => {
        stashPendingFirstMessage(A, { content: "Hello" });

        // The user navigated to a different conversation instead of the one just created.
        expect(takePendingFirstMessage(B)).toBeUndefined();
        // Re-opening the abandoned conversation later must not resurrect the turn.
        expect(takePendingFirstMessage(A)).toBeUndefined();
    });

    it("keeps only the most recent handoff", () => {
        stashPendingFirstMessage(A, { content: "First" });
        stashPendingFirstMessage(B, { content: "Second" });

        expect(takePendingFirstMessage(B)).toEqual({ content: "Second" });
    });
});
