import { afterEach, describe, expect, it, vi } from "vitest";
import {
    UaiHitlContext,
    UaiRunController,
    UaiToolRendererManager,
    type UaiAgentItem,
    type UaiChatMessage,
    type UaiConversationStrategy,
} from "@umbraco-ai/agent-ui";
import type { AgentClientCallbacks, UaiAgentClient } from "@umbraco-ai/agent";
import type { UmbController, UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UaiServerPersistedConversationStrategy } from "./server-persisted-conversation.strategy.js";
import { UaiConversationRepository } from "../conversation/repository/conversation.repository.js";

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

/**
 * Runs the real run controller over the Copilot Workspace's real server-persisted strategy, so the
 * persisted-boundary arithmetic is part of what's tested. Only the transport is faked.
 */
async function createHarness() {
    const host = createFakeHost();
    const repository = new UaiConversationRepository(host);
    vi.spyOn(repository, "requestMessages").mockResolvedValue({ data: { items: [] } } as never);
    const truncate = vi.spyOn(repository, "truncateAfterLastUserMessage").mockResolvedValue({} as never);

    const persisted = new UaiServerPersistedConversationStrategy(repository);
    persisted.setConversationId("conversation-1");
    await persisted.loadInitial();

    let callbacks: AgentClientCallbacks | undefined;
    const sendMessage = vi.fn();
    const strategy: UaiConversationStrategy = {
        createClient: (_agent, cb) => {
            callbacks = cb;
            return { sendMessage, reset: vi.fn() } as unknown as UaiAgentClient;
        },
        loadInitial: () => persisted.loadInitial(),
        outbound: (messages) => persisted.outbound(messages),
        onTurnComplete: (messages) => persisted.onTurnComplete(messages),
        onTruncate: (remaining) => persisted.onTruncate(remaining),
    };

    const controller = new UaiRunController(host, new UaiHitlContext(host), {
        toolRendererManager: new UaiToolRendererManager(host),
        conversationStrategy: strategy,
        interruptHandlers: [],
    });
    const agent: UaiAgentItem = { id: "agent-1", name: "Agent", alias: "agent" };
    controller.setAgent(agent);

    let messageCount = 0;
    const streamText = (text: string) => {
        callbacks!.onTextStart?.(`assistant-${++messageCount}`);
        callbacks!.onTextDelta?.(text);
    };
    const finish = () => callbacks!.onRunFinished?.({ outcome: "success" });
    const fail = (message: string, code: string) => callbacks!.onError?.(new Error(message), code);
    const sentOnLastCall = (): Array<[string, string]> =>
        (sendMessage.mock.calls.at(-1)![0] as UaiChatMessage[]).map((m) => [m.role, m.content]);

    return { controller, callbacks: () => callbacks!, streamText, finish, fail, sentOnLastCall, truncate };
}

const blockNotice = "The response was blocked by a guardrail policy: Prices > No prices";

afterEach(() => vi.restoreAllMocks());

/**
 * A model-based guardrail can only check a response after it has streamed, so the block arrives after the
 * text is already on screen. Before #712 the blocked text stayed in the chat, and it and the "Error: ..."
 * notice were sent back as assistant messages on the next turn, where the Copilot Workspace stored them.
 */
describe("UaiRunController — a guardrail block after a streamed response (#712)", () => {
    it("removes the blocked text, leaving only the block notice", async () => {
        const { controller, streamText, fail } = await createHarness();

        controller.sendUserMessage("Write a promotion with the price");
        streamText("Get yours now for just €149.");
        fail(blockNotice, "GuardrailBlocked");

        const assistantContent = controller.messages.filter((m) => m.role === "assistant").map((m) => m.content);
        expect(assistantContent).toEqual([`Error: ${blockNotice}`]);
    });

    it("sends nothing of the blocked turn on the next turn", async () => {
        const { controller, streamText, finish, fail, sentOnLastCall } = await createHarness();

        controller.sendUserMessage("Hello");
        streamText("Hi there.");
        finish();

        controller.sendUserMessage("Write a promotion with the price");
        streamText("Get yours now for just €149.");
        fail(blockNotice, "GuardrailBlocked");

        controller.sendUserMessage("Now one about scarves");

        // Sent without its answer, the blocked request would be answered again, and blocked again.
        expect(sentOnLastCall()).toEqual([["user", "Now one about scarves"]]);
    });

    it("keeps the tool calls of a blocked run on screen but out of what's sent", async () => {
        const { controller, callbacks, streamText, fail, sentOnLastCall } = await createHarness();

        controller.sendUserMessage("Find the jacket page and write a promotion with the price");
        callbacks().onToolCallStart?.({ id: "call-1", name: "search_umbraco", arguments: "", status: "pending" });
        callbacks().onToolCallArgsEnd?.("call-1", "{}");
        callbacks().onToolCallResult?.("call-1", "Winter jacket");
        streamText("Get yours now for just €149.");
        fail(blockNotice, "GuardrailBlocked");

        expect(controller.messages.some((m) => m.toolCalls?.some((tc) => tc.id === "call-1"))).toBe(true);

        controller.sendUserMessage("Now one about scarves");

        expect(sentOnLastCall()).toEqual([["user", "Now one about scarves"]]);
    });

    it("asks the blocked message again on regenerate, without truncating the stored conversation", async () => {
        const { controller, streamText, fail, sentOnLastCall, truncate } = await createHarness();

        controller.sendUserMessage("Write a promotion with the price");
        streamText("Get yours now for just €149.");
        fail(blockNotice, "GuardrailBlocked");
        await controller.regenerateLastMessage();

        expect(truncate).not.toHaveBeenCalled();
        expect(sentOnLastCall()).toEqual([["user", "Write a promotion with the price"]]);
    });

    it("still truncates on the next regenerate after a truncate fails", async () => {
        const { controller, streamText, finish, truncate } = await createHarness();
        vi.spyOn(console, "error").mockImplementation(() => {});

        controller.sendUserMessage("Hello");
        streamText("Hi there.");
        finish();

        truncate.mockResolvedValueOnce({ error: new Error("offline") } as never);
        await controller.regenerateLastMessage();
        await controller.regenerateLastMessage();

        expect(truncate).toHaveBeenCalledTimes(2);
    });

    it("leaves any other failed run as it was", async () => {
        const { controller, streamText, fail, sentOnLastCall } = await createHarness();

        controller.sendUserMessage("Hello");
        streamText("Partial answer");
        fail("Rate limit reached.", "RateLimited");
        controller.sendUserMessage("Try again");

        expect(controller.messages.some((m) => m.content === "Partial answer")).toBe(true);
        expect(sentOnLastCall().map(([, content]) => content)).toContain("Hello");
    });
});
