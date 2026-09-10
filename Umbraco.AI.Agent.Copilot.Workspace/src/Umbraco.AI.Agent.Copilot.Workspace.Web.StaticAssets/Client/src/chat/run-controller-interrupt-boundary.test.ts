import { describe, expect, it, vi } from "vitest";
import {
    UaiHitlContext,
    UaiRunController,
    UaiToolRendererManager,
    type UaiAgentItem,
    type UaiConversationStrategy,
} from "@umbraco-ai/agent-ui";
import type { AgentClientCallbacks, RunFinishedEvent, UaiAgentClient } from "@umbraco-ai/agent";
import type { UmbController, UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";

// `@umbraco-ai/core` resolves to its real TS source under vitest — see the `test.alias` entry and its
// doc comment in `vitest.config.ts`. Real production code (and the actual app) never resolves it this
// way; the real build externalizes that package entirely.

/**
 * Minimal in-memory {@link UmbControllerHost} — just enough bookkeeping to host the
 * `UmbControllerBase`-derived classes constructed below in a plain vitest environment, with no real
 * Umbraco app/element tree behind it.
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

function createHarness() {
    const host = createFakeHost();
    const hitlContext = new UaiHitlContext(host);
    const toolRendererManager = new UaiToolRendererManager(host);

    let capturedCallbacks: AgentClientCallbacks | undefined;
    const onTurnComplete = vi.fn();
    const strategy: UaiConversationStrategy = {
        createClient: (_agent, callbacks) => {
            capturedCallbacks = callbacks;
            // Nothing in this test drives an actual stream, so the transport itself is never used.
            return {} as UaiAgentClient;
        },
        loadInitial: async () => [],
        outbound: (messages) => messages,
        onTurnComplete,
    };

    const controller = new UaiRunController(host, hitlContext, {
        toolRendererManager,
        conversationStrategy: strategy,
        // A catch-all handler that "handles" every interrupt (mirrors a real consumer registering
        // UaiHitlInterruptHandler/UaiDefaultInterruptHandler), so the run controller's interrupt branch
        // takes its early-return path.
        interruptHandlers: [{ reason: "*", handle: () => {} }],
    });

    const agent: UaiAgentItem = { id: "agent-1", name: "Agent", alias: "agent" };
    controller.setAgent(agent);

    return { onTurnComplete, getCallbacks: () => capturedCallbacks };
}

/**
 * Regression coverage for the Copilot Workspace conversation-message-duplication bug: a turn that ends
 * in a HITL/tool-approval interrupt must still advance the server-persisted strategy's boundary, or
 * every following turn re-sends (and the server re-persists) the same prefix, compounding the
 * duplication. See `UaiRunController#handleRunFinished` in `@umbraco-ai/agent-ui`.
 */
describe("UaiRunController — interrupt path advances the persisted boundary", () => {
    it("calls the strategy's onTurnComplete even when the run ends in an interrupt", () => {
        const { onTurnComplete, getCallbacks } = createHarness();
        expect(getCallbacks()).toBeDefined();

        // Simulate a turn that pauses on a human-approval interrupt rather than finishing cleanly.
        const event: RunFinishedEvent = {
            outcome: "interrupt",
            interrupt: {
                id: "approval:call-1",
                reason: "human_approval",
                type: "approval",
                title: "Approve action",
                message: "Approve this action?",
            },
        };
        getCallbacks()?.onRunFinished?.(event);

        expect(onTurnComplete).toHaveBeenCalledTimes(1);
    });

    it("does NOT advance the boundary on an unhandled run error", () => {
        // Contrast case: an error outcome is not a "the server holds this now" signal the way an
        // interrupt is, and the original (pre-fix) behaviour never called onTurnComplete for it either
        // — this fix must not change that.
        const { onTurnComplete, getCallbacks } = createHarness();

        getCallbacks()?.onRunFinished?.({ outcome: "error", error: "boom" });

        expect(onTurnComplete).not.toHaveBeenCalled();
    });
});
