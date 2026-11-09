import { describe, expect, it, vi } from "vitest";
import {
    UaiHitlContext,
    UaiRunController,
    UaiToolRendererManager,
    type UaiConversationStrategy,
    type UaiInterruptContext,
} from "@umbraco-ai/agent-ui";
import type { AgentClientCallbacks, UaiAgentClient } from "@umbraco-ai/agent";
import type { UmbController, UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";

type ContextItems = Array<{ description: string; value: string }>;

/**
 * Minimal in-memory {@link UmbControllerHost} — just enough bookkeeping to host the
 * `UmbControllerBase`-derived classes constructed below in a plain vitest environment.
 */
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

function createHarness(collectContext?: () => Promise<ContextItems>) {
    const host = createFakeHost();
    const sendMessage = vi.fn();
    let callbacks: AgentClientCallbacks | undefined;
    let interruptContext: UaiInterruptContext | undefined;

    const strategy: UaiConversationStrategy = {
        createClient: (_agent, cb) => {
            callbacks = cb;
            return { sendMessage, reset: vi.fn() } as unknown as UaiAgentClient;
        },
        loadInitial: async () => [],
        outbound: (messages) => messages,
    };

    const controller = new UaiRunController(host, new UaiHitlContext(host), {
        toolRendererManager: new UaiToolRendererManager(host),
        conversationStrategy: strategy,
        // Stands in for the frontend tool and HITL handlers: takes the interrupt so the test can resume it.
        interruptHandlers: [{ reason: "*", handle: (_interrupt, context) => void (interruptContext = context) }],
        collectContext,
    });
    controller.setAgent({ id: "agent-1", name: "Agent", alias: "agent" });

    const pauseOn = (reason: "tool_call" | "human_approval") =>
        callbacks?.onRunFinished?.({
            outcome: "interrupt",
            interrupt: { id: "approval:call-1", reason, type: reason, title: "", message: "" },
        });

    const answer = () => {
        callbacks?.onTextStart?.("assistant-1");
        callbacks?.onTextDelta?.("Done.");
        callbacks?.onTextEnd?.();
        callbacks?.onRunFinished?.({ outcome: "success" });
    };

    return {
        controller,
        sendMessage,
        pauseOn,
        answer,
        resume: (response?: unknown) => interruptContext?.resume(response),
    };
}

const flush = () => new Promise((resolve) => setTimeout(resolve, 0));
const stale: ContextItems = [{ description: "Entity", value: "metaName: (empty)" }];
const fresh: ContextItems = [{ description: "Entity", value: "metaName: About us" }];

/**
 * Regression coverage for #688: a run that follows a frontend tool edit must see the entity as the tool
 * left it. Resending the context captured with the user's message made models re-apply their edits.
 */
describe("UaiRunController — request context on follow-up runs", () => {
    it("sends a freshly collected context when resuming after a frontend tool", async () => {
        const { controller, sendMessage, pauseOn, resume } = createHarness(async () => fresh);

        controller.sendUserMessage("update the meta data", stale);
        pauseOn("tool_call");
        resume();
        await flush();

        expect(sendMessage).toHaveBeenCalledTimes(2);
        expect(sendMessage.mock.calls[1][2]).toEqual(fresh);
    });

    it("sends a freshly collected context when resuming after an approval", async () => {
        const { controller, sendMessage, pauseOn, resume } = createHarness(async () => fresh);

        controller.sendUserMessage("save the page", stale);
        pauseOn("human_approval");
        resume({ approved: true });
        await flush();

        expect(sendMessage.mock.calls[1][2]).toEqual(fresh);
    });

    it("sends a freshly collected context when regenerating", async () => {
        const { controller, sendMessage, answer } = createHarness(async () => fresh);

        controller.sendUserMessage("update the meta data", stale);
        answer();
        await controller.regenerateLastMessage();

        expect(sendMessage.mock.calls[1][2]).toEqual(fresh);
    });

    it("sends nothing when the run is aborted while the context is being collected", async () => {
        let release!: (items: ContextItems) => void;
        const { controller, sendMessage, pauseOn, resume } = createHarness(
            () => new Promise<ContextItems>((resolve) => (release = resolve)),
        );

        controller.sendUserMessage("update the meta data", stale);
        pauseOn("tool_call");
        resume();
        controller.abortRun();
        release(fresh);
        await flush();

        expect(sendMessage).toHaveBeenCalledTimes(1);
        expect(controller.isRunning).toBe(false);
    });

    it("resends the user's context when the surface supplies no collector", () => {
        const { controller, sendMessage, pauseOn, resume } = createHarness();

        controller.sendUserMessage("update the meta data", stale);
        pauseOn("tool_call");
        resume();

        // Still sent in the same tick, as before the collector existed.
        expect(sendMessage.mock.calls[1][2]).toEqual(stale);
    });
});
