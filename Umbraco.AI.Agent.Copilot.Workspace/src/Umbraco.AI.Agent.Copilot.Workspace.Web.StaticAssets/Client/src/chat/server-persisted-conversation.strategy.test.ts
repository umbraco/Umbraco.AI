import { describe, expect, it } from "vitest";
import type { UaiChatMessage } from "@umbraco-ai/agent";
import type { UmbController, UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UaiServerPersistedConversationStrategy } from "./server-persisted-conversation.strategy.js";
import { UaiConversationRepository } from "../conversation/repository/conversation.repository.js";

/** Minimal in-memory host — the repository is never actually called by the behaviour under test here. */
function createFakeHost(): UmbControllerHost {
    const controllers = new Set<UmbController>();
    return {
        hasUmbController: (controller) => controllers.has(controller),
        getUmbControllers: (filterMethod) => [...controllers].filter(filterMethod),
        addUmbController: (controller) => void controllers.add(controller),
        removeUmbControllerByAlias: () => {},
        removeUmbController: (controller) => void controllers.delete(controller),
        getHostElement: () => document.createElement("div"),
    };
}

function message(id: string): UaiChatMessage {
    return { id, role: "user", content: id };
}

function createStrategy(): UaiServerPersistedConversationStrategy {
    return new UaiServerPersistedConversationStrategy(new UaiConversationRepository(createFakeHost()));
}

/**
 * Regression coverage for umbraco/Umbraco.AI#375's resync fix: the client's own "already sent" boundary
 * (`#persisted`) can go stale after a dropped connection, silently causing every later turn to resend
 * (and the server to re-receive) content that's already durably saved. `onServerPersistedBoundary`
 * corrects it from the server's own report instead of only ever inferring it from a clean turn finish.
 */
describe("UaiServerPersistedConversationStrategy — onServerPersistedBoundary", () => {
    it("advances the boundary to just past the reported message", () => {
        const strategy = createStrategy();
        const allMessages = [message("msg-1"), message("msg-2"), message("msg-3")];

        strategy.onServerPersistedBoundary("msg-2", allMessages);

        // outbound() slices from the boundary — only what's genuinely after the reported message remains.
        expect(strategy.outbound(allMessages)).toEqual([message("msg-3")]);
    });

    it("never retreats the boundary on a stale or out-of-order report", () => {
        const strategy = createStrategy();
        const allMessages = [message("msg-1"), message("msg-2"), message("msg-3")];

        strategy.onTurnComplete(allMessages); // boundary = 3 (everything currently held is persisted)
        strategy.onServerPersistedBoundary("msg-1", allMessages); // a stale report naming an earlier message

        expect(strategy.outbound(allMessages)).toEqual([]);
    });

    it("is a no-op when the reported id isn't found in the current messages", () => {
        const strategy = createStrategy();
        const allMessages = [message("msg-1")];

        strategy.onServerPersistedBoundary("msg-does-not-exist", allMessages);

        expect(strategy.outbound(allMessages)).toEqual(allMessages);
    });
});
