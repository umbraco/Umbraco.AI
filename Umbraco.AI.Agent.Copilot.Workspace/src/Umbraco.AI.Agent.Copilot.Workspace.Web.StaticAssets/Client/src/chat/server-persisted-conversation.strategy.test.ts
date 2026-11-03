import { describe, expect, it, vi } from "vitest";
import type { UaiChatMessage } from "@umbraco-ai/agent";
import type { UmbController, UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UaiServerPersistedConversationStrategy } from "./server-persisted-conversation.strategy.js";
import { UaiConversationRepository } from "../conversation/repository/conversation.repository.js";
import type { MessageResponseModel } from "../api/types.gen.js";

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

const AGENT_A = "aaaaaaaa-0000-0000-0000-000000000001";

/** A stored assistant row by `AGENT_A`, as `UaiServerPersistedConversationStrategy.loadInitial` reads it. */
function storedAssistantRow(): MessageResponseModel {
    return {
        id: "m1",
        sequence: 1,
        role: "assistant",
        contentJson: JSON.stringify({ role: "assistant", contents: [{ $type: "text", text: "Hello" }] }),
        contentText: "Hello",
        dateCreated: "2026-10-01T12:00:00Z",
        agentId: AGENT_A,
    } as MessageResponseModel;
}

/**
 * Regression coverage for the contract the Copilot Workspace reopen-race fix depends on:
 * `loadInitial()` must call the caller's `getAgentNames` closure LAZILY, at the moment it actually runs
 * (see its ctor doc) — not capture its result once, earlier. That laziness is what lets
 * `UaiCopilotWorkspaceChatContext#syncTarget` close the real race (the view fires `loadAgents()`
 * without awaiting it, so the agent catalog and the conversation history load concurrently) simply by
 * `await`ing the catalog's load before calling `loadInitialMessages()` — if this strategy instead read
 * `getAgentNames()` once up front (e.g. at construction), awaiting anything in the caller afterwards
 * wouldn't help, since the closure's answer would already be frozen from before the catalog populated.
 *
 * NOTE: `UaiCopilotWorkspaceChatContext` itself can't be constructed under this package's vitest setup
 * today (`UaiWorkspaceAgentRepository`'s real constructor needs `UaiAgentRepository` from
 * `@umbraco-ai/agent`, which that package's `index.ts` barrel doesn't export — a pre-existing
 * resolution gap, unrelated to this fix, that blocks building any test host for that class). This spec
 * is the closest coverage reachable without changing shared test infra; the `#syncTarget` sequencing
 * itself was verified by code trace (see `copilot-workspace-chat.context.ts`).
 */
describe("UaiServerPersistedConversationStrategy — loadInitial reads agent names lazily", () => {
    it("carries the agent's name when the catalog only finishes loading after loadInitial has already started", async () => {
        const repository = new UaiConversationRepository(createFakeHost());
        // Gated, not a plain resolved mock -- deterministic rather than relying on exact microtask-tick
        // counting between this history fetch and the catalog flag below.
        let resolveHistoryFetch!: () => void;
        const historyFetchGate = new Promise<void>((resolve) => (resolveHistoryFetch = resolve));
        vi.spyOn(repository, "requestMessages").mockImplementation(async () => {
            await historyFetchGate;
            return { data: { items: [storedAssistantRow()] } } as Awaited<
                ReturnType<UaiConversationRepository["requestMessages"]>
            >;
        });

        // Empty until the catalog flag flips -- mirrors `UaiCopilotWorkspaceChatContext`'s `#agents`
        // state, which only populates once the agent catalog's own (separately awaited) fetch resolves.
        let agentsLoaded = false;
        const strategy = new UaiServerPersistedConversationStrategy(repository, () =>
            agentsLoaded ? new Map([[AGENT_A, "Agent A"]]) : new Map(),
        );
        strategy.setConversationId("conv-1");

        // loadInitial() is already in flight, blocked on the still-closed history gate, at the moment the
        // catalog flips ready. `getAgentNames()` is only consulted once `requestMessages()` resolves (see
        // the gate above) -- it must see this flip. If the closure were read up front instead of lazily
        // (captured before the history fetch even started), this ordering would still leave it empty.
        const loadInitial = strategy.loadInitial();
        agentsLoaded = true;
        resolveHistoryFetch();
        const messages = await loadInitial;

        expect(messages.find((m) => m.role === "assistant")?.agentName).toBe("Agent A");
    });
});

/**
 * Regression coverage for SC-18: a shared (non-owned) conversation's owner-lookup endpoint 404s
 * indistinguishably from a nonexistent id (see SPEC.md), so `requestMessages` resolves with `data:
 * undefined` rather than throwing. `loadInitial` must fall back to `requestSharedView`'s `messages`
 * field — the same `MessageResponseModel[]` shape — rather than silently rendering empty history.
 */
describe("UaiServerPersistedConversationStrategy — loadInitial falls back to the shared view", () => {
    it("loads messages from requestSharedView when the owner lookup 404s", async () => {
        const repository = new UaiConversationRepository(createFakeHost());
        vi.spyOn(repository, "requestMessages").mockResolvedValue({ data: undefined } as Awaited<
            ReturnType<UaiConversationRepository["requestMessages"]>
        >);
        vi.spyOn(repository, "requestSharedView").mockResolvedValue({
            data: { messages: [storedAssistantRow()] },
        } as Awaited<ReturnType<UaiConversationRepository["requestSharedView"]>>);

        const strategy = new UaiServerPersistedConversationStrategy(repository, () => new Map([[AGENT_A, "Agent A"]]));
        strategy.setConversationId("conv-1");

        const messages = await strategy.loadInitial();

        expect(messages).toHaveLength(1);
        expect(messages[0]?.role).toBe("assistant");
        expect(messages[0]?.agentName).toBe("Agent A");
    });

    it("returns an empty history when neither the owner lookup nor the shared view has data", async () => {
        const repository = new UaiConversationRepository(createFakeHost());
        vi.spyOn(repository, "requestMessages").mockResolvedValue({ data: undefined } as Awaited<
            ReturnType<UaiConversationRepository["requestMessages"]>
        >);
        vi.spyOn(repository, "requestSharedView").mockResolvedValue({ data: undefined } as Awaited<
            ReturnType<UaiConversationRepository["requestSharedView"]>
        >);

        const strategy = new UaiServerPersistedConversationStrategy(repository);
        strategy.setConversationId("conv-1");

        const messages = await strategy.loadInitial();

        expect(messages).toEqual([]);
    });
});
