import { describe, expect, it, vi } from "vitest";
import { Observable, type Subscriber } from "rxjs";
import {
    UaiFrontendToolExecutor,
    UaiHitlContext,
    UaiRunController,
    UaiToolRendererManager,
    type UaiConversationStrategy,
    type UaiFrontendToolManager,
    type UaiInterruptContext,
} from "@umbraco-ai/agent-ui";
import { UaiAgentClient, type AgentClientCallbacks, type AgentTransport } from "@umbraco-ai/agent";
import type { UmbController, UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";

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

/** A transport whose run stream the test drives by hand, recording whether it was torn down. */
function createFakeTransport() {
    let subscriber: Subscriber<unknown> | undefined;
    const state = { tornDown: false };
    const transport = {
        setMessages: () => {},
        abortRun: vi.fn(),
        run: () =>
            new Observable<unknown>((s) => {
                subscriber = s;
                return () => void (state.tornDown = true);
            }),
    } as unknown as AgentTransport;
    return { transport, state, emit: (event: unknown) => subscriber?.next(event) };
}

const flush = () => new Promise((resolve) => setTimeout(resolve, 0));

/**
 * Regression coverage for #692: Cancel must stop the run. It used to leave the request streaming, and a
 * frontend tool call at its end was still executed and resumed, so the run carried on.
 */
describe("Cancel stops the run", () => {
    it("closes the run's stream when its signal is aborted, so no more events arrive", () => {
        const { transport, state, emit } = createFakeTransport();
        const onTextStart = vi.fn();
        const client = new UaiAgentClient(transport, { onTextStart });
        const turn = new AbortController();

        client.sendMessage(
            [{ id: "u1", role: "user", content: "hi", timestamp: new Date() }],
            undefined,
            undefined,
            undefined,
            undefined,
            turn.signal,
        );
        turn.abort();
        emit({ type: "TEXT_MESSAGE_START", messageId: "m1", role: "assistant" });

        expect(state.tornDown).toBe(true);
        expect(onTextStart).not.toHaveBeenCalled();
    });

    it("aborts the signal the run was sent with", () => {
        const { controller, sendMessage } = createControllerHarness();

        controller.sendUserMessage("update the meta data");
        controller.abortRun();

        expect((sendMessage.mock.calls[0][5] as AbortSignal).aborted).toBe(true);
    });

    it("does not resume when a frontend tool finishes after Cancel", () => {
        const { controller, sendMessage, pauseOn, resume } = createControllerHarness();

        controller.sendUserMessage("update the meta data");
        pauseOn("tool_call");
        controller.abortRun();
        resume();

        expect(sendMessage).toHaveBeenCalledTimes(1);
        expect(controller.isRunning).toBe(false);
    });

    it("still resumes a backend approval answered after an idle Cancel, such as closing the panel", () => {
        // A backend approval leaves the run idle while its card waits, like UaiHitlContext does.
        const { controller, sendMessage, pauseOn, resume } = createControllerHarness({ idleOnInterrupt: true });

        controller.sendUserMessage("save the page");
        pauseOn("human_approval");
        controller.abortRun();
        resume({ approved: true });

        expect(sendMessage).toHaveBeenCalledTimes(2);
    });

    it("publishes nothing when a running tool finishes after Cancel", async () => {
        const host = createFakeHost();
        let finish!: (value: unknown) => void;
        const execute = vi.fn(() => new Promise((resolve) => (finish = resolve)));
        const toolManager = {
            getManifest: () => ({ meta: {} }),
            getApi: async () => ({ execute }),
        } as unknown as UaiFrontendToolManager;
        const executor = new UaiFrontendToolExecutor(new UaiToolRendererManager(host), toolManager);
        const results = vi.fn();
        executor.results$.subscribe(results);
        const turn = new AbortController();

        const running = executor.execute(
            [{ id: "call-1", name: "set_value", arguments: "{}" }] as Parameters<UaiFrontendToolExecutor["execute"]>[0],
            turn.signal,
        );
        await flush();
        expect(execute).toHaveBeenCalled();

        turn.abort();
        finish({ success: true });
        await running;

        expect(results).not.toHaveBeenCalled();
    });

    it("gives a tool call paused at Cancel a result, so the next message isn't rejected", () => {
        const { controller, sendMessage, callbacks } = createControllerHarness();

        controller.sendUserMessage("save the page");
        callbacks()?.onToolCallStart?.({ id: "call-1", name: "save", arguments: "", status: "pending" });
        callbacks()?.onToolCallArgsEnd?.("call-1", "{}");
        callbacks()?.onToolCallEnd?.("call-1");
        callbacks()?.onRunFinished?.({
            outcome: "interrupt",
            interrupt: { id: "call-1", reason: "tool_call", type: "tool_call", title: "", message: "" },
        });
        controller.abortRun();
        controller.sendUserMessage("something else");

        const history = sendMessage.mock.calls[1][0] as Array<{ role: string; toolCallId?: string }>;
        expect(history.some((msg) => msg.role === "tool" && msg.toolCallId === "call-1")).toBe(true);
    });

    it("closes a frontend tool's approval card and does not run the tool", async () => {
        const host = createFakeHost();
        const hitl = new UaiHitlContext(host);
        const execute = vi.fn();
        const toolManager = {
            getManifest: () => ({ meta: { approval: true } }),
            getApi: async () => ({ execute }),
        } as unknown as UaiFrontendToolManager;
        const executor = new UaiFrontendToolExecutor(new UaiToolRendererManager(host), toolManager, hitl);
        const results = vi.fn();
        executor.results$.subscribe(results);
        let card: unknown;
        hitl.interrupt$.subscribe((interrupt) => (card = interrupt));
        const turn = new AbortController();

        const running = executor.execute(
            [{ id: "call-1", name: "save", arguments: "{}" }] as Parameters<UaiFrontendToolExecutor["execute"]>[0],
            turn.signal,
        );
        await flush();
        expect(card).toBeDefined();

        turn.abort();
        await running;

        expect(card).toBeUndefined();
        expect(execute).not.toHaveBeenCalled();
        expect(results).not.toHaveBeenCalled();
    });
});

function createControllerHarness(options: { idleOnInterrupt?: boolean } = {}) {
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
        interruptHandlers: [
            {
                reason: "*",
                handle: (_interrupt, context) => {
                    interruptContext = context;
                    if (options.idleOnInterrupt) context.setAgentState(undefined);
                },
            },
        ],
    });
    controller.setAgent({ id: "agent-1", name: "Agent", alias: "agent" });

    const pauseOn = (reason: "tool_call" | "human_approval") =>
        callbacks?.onRunFinished?.({
            outcome: "interrupt",
            interrupt: { id: "approval:call-1", reason, type: reason, title: "", message: "" },
        });

    return {
        controller,
        sendMessage,
        pauseOn,
        callbacks: () => callbacks,
        resume: (response?: unknown) => interruptContext?.resume(response),
    };
}
