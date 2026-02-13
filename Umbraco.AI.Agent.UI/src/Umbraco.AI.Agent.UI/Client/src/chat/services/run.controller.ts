import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { BehaviorSubject, Subscription, map } from "rxjs";
import { UaiFrontendToolExecutor, type UaiFrontendToolResult, type UaiFrontendToolStatusUpdate } from "./frontend-tool.executor.js";
import { UaiInterruptHandlerRegistry } from "./interrupt-handler.registry.js";
import { UaiToolExecutionHandler } from "./handlers/tool-execution.handler.js";
import type { UaiToolRendererManager } from "./tool-renderer.manager.js";
import type { UaiFrontendToolManager } from "./frontend-tool.manager.js";
import type { UaiInterruptHandler, UaiInterruptContext } from "./interrupt.types.js";
import type UaiHitlContext from "./hitl.context.js";
import type {
    UaiAgentState,
    UaiChatMessage,
    UaiInterruptInfo,
    UaiToolCallInfo,
    UaiToolCallStatus,
    UaiAgentItem,
} from "../types/index.js";
import { safeParseJson } from "../utils/json.js";
import { UaiAgentClient } from "@umbraco-ai/agent";

/**
 * Configuration for the run controller.
 * Surfaces inject their tool infrastructure and interrupt handlers.
 */
export interface UaiRunControllerConfig {
    /** Tool renderer manager for manifest/element lookup */
    toolRendererManager: UaiToolRendererManager;
    /** Optional frontend tool manager -- surfaces that support frontend tools inject this */
    frontendToolManager?: UaiFrontendToolManager;
    /** Interrupt handlers to register */
    interruptHandlers: UaiInterruptHandler[];
}

/**
 * Shared run controller for managing AG-UI client lifecycle, chat state, and streaming.
 *
 * Refactored from UaiCopilotRunController to accept tool configuration as optional injection.
 * - Copilot creates with frontendToolManager set + UaiToolExecutionHandler
 * - Chat initially creates without frontendToolManager (server-side tools only)
 */
export class UaiRunController extends UmbControllerBase {
    #toolRendererManager: UaiToolRendererManager;
    #frontendToolManager?: UaiFrontendToolManager;
    #toolExecutor?: UaiFrontendToolExecutor;
    #client?: UaiAgentClient;
    #agent?: UaiAgentItem;
    #currentToolCalls: UaiToolCallInfo[] = [];
    #subscriptions: Subscription[] = [];
    #handlerRegistry = new UaiInterruptHandlerRegistry();

    /** ID of the assistant message currently being streamed */
    #currentAssistantMessageId: string | null = null;

    #messages = new BehaviorSubject<UaiChatMessage[]>([]);
    readonly messages$ = this.#messages.asObservable();

    #streamingContent = new BehaviorSubject<string>("");
    readonly streamingContent$ = this.#streamingContent.asObservable();

    #agentState = new BehaviorSubject<UaiAgentState | undefined>(undefined);
    readonly agentState$ = this.#agentState.asObservable();
    readonly isRunning$ = this.agentState$.pipe(map((state) => state !== undefined));

    #resolvedAgent = new BehaviorSubject<{ agentId: string; agentName: string; agentAlias: string } | undefined>(undefined);
    readonly resolvedAgent$ = this.#resolvedAgent.asObservable();

    /** Expose tool renderer manager for context provision */
    get toolRendererManager(): UaiToolRendererManager {
        return this.#toolRendererManager;
    }

    constructor(host: UmbControllerHost, hitlContext: UaiHitlContext, config: UaiRunControllerConfig) {
        super(host);
        this.#toolRendererManager = config.toolRendererManager;
        this.#frontendToolManager = config.frontendToolManager;

        // Create frontend tool executor if frontend tools are available
        if (this.#frontendToolManager) {
            this.#toolExecutor = new UaiFrontendToolExecutor(
                this.#toolRendererManager,
                this.#frontendToolManager,
                hitlContext,
            );
            this.#subscriptions.push(
                this.#toolExecutor.results$.subscribe((result) => this.#handleToolResult(result)),
                this.#toolExecutor.statusUpdates$.subscribe((update) => this.#handleToolStatusUpdate(update)),
            );
        }

        // Register interrupt handlers
        // Auto-register tool execution handler when frontend tools are available
        // (it needs the executor created above, so it can't be passed in via config)
        this.#handlerRegistry.clear();
        const handlers: UaiInterruptHandler[] = [];
        if (this.#frontendToolManager && this.#toolExecutor) {
            handlers.push(new UaiToolExecutionHandler(this.#frontendToolManager, this.#toolExecutor));
        }
        handlers.push(...config.interruptHandlers);
        this.#handlerRegistry.registerAll(handlers);
    }

    override destroy(): void {
        super.destroy();
        this.#subscriptions.forEach((sub) => sub.unsubscribe());
    }

    setAgent(agent: UaiAgentItem): void {
        if (this.#agent?.id === agent.id) return;
        this.#agent = agent;
        this.#createClient();
        this.resetConversation();
    }

    /** Context items to include in the next request */
    #pendingContext: Array<{ description: string; value: string }> = [];

    sendUserMessage(content: string, context?: Array<{ description: string; value: string }>): void {
        if (!this.#client || !content.trim()) return;

        this.#pendingContext = context ?? [];

        const userMessage: UaiChatMessage = {
            id: crypto.randomUUID(),
            role: "user",
            content,
            timestamp: new Date(),
        };

        const nextMessages = [...this.#messages.value, userMessage];
        this.#messages.next(nextMessages);
        this.#agentState.next({ status: "thinking" });

        const frontendTools = this.#frontendToolManager?.frontendTools ?? [];
        this.#client.sendMessage(nextMessages, frontendTools, this.#pendingContext);
    }

    resetConversation(): void {
        this.#messages.next([]);
        this.#streamingContent.next("");
        this.#agentState.next(undefined);
        this.#currentToolCalls = [];
        this.#currentAssistantMessageId = null;
        this.#resolvedAgent.next(undefined);
    }

    abortRun(): void {
        if (!this.#client) return;

        this.#client.reset();
        this.#streamingContent.next("");
        this.#agentState.next(undefined);
        this.#currentToolCalls = [];
        this.#currentAssistantMessageId = null;
        this.#resolvedAgent.next(undefined);
    }

    regenerateLastMessage(): void {
        if (!this.#client) return;

        const messages = this.#messages.value;

        let lastAssistantIndex = -1;
        for (let i = messages.length - 1; i >= 0; i--) {
            if (messages[i].role === "assistant") {
                lastAssistantIndex = i;
                break;
            }
        }

        if (lastAssistantIndex === -1) return;

        const truncatedMessages = messages.slice(0, lastAssistantIndex);
        this.#messages.next(truncatedMessages);

        this.#streamingContent.next("");
        this.#currentToolCalls = [];
        this.#currentAssistantMessageId = null;

        this.#agentState.next({ status: "thinking" });
        const frontendTools = this.#frontendToolManager?.frontendTools ?? [];
        this.#client.sendMessage(truncatedMessages, frontendTools, this.#pendingContext);
    }

    #createClient(): void {
        if (!this.#agent?.id) return;

        this.#client = UaiAgentClient.create(
            { agentId: this.#agent.id },
            {
                onTextStart: (messageId) => {
                    const messages = this.#messages.value;
                    const lastMessage = messages[messages.length - 1];

                    const isAfterTool =
                        lastMessage?.role === "tool" ||
                        (lastMessage?.role === "assistant" && lastMessage.toolCalls?.length);

                    const isDifferentMessageId =
                        messageId && this.#currentAssistantMessageId && messageId !== this.#currentAssistantMessageId;

                    if (isAfterTool || isDifferentMessageId) {
                        const newMessage: UaiChatMessage = {
                            id: messageId || crypto.randomUUID(),
                            role: "assistant",
                            content: "",
                            timestamp: new Date(),
                        };
                        this.#messages.next([...messages, newMessage]);
                        this.#currentAssistantMessageId = newMessage.id;
                    } else if (!this.#currentAssistantMessageId) {
                        const newMessage: UaiChatMessage = {
                            id: messageId || crypto.randomUUID(),
                            role: "assistant",
                            content: "",
                            timestamp: new Date(),
                        };
                        this.#messages.next([...messages, newMessage]);
                        this.#currentAssistantMessageId = newMessage.id;
                    }
                },
                onTextDelta: (delta) => {
                    this.#streamingContent.next(this.#streamingContent.value + delta);

                    if (this.#currentAssistantMessageId) {
                        const messages = this.#messages.value.map((msg) =>
                            msg.id === this.#currentAssistantMessageId ? { ...msg, content: msg.content + delta } : msg,
                        );
                        this.#messages.next(messages);
                    }
                },
                onTextEnd: () => {
                    // Text complete
                },
                onToolCallStart: (info) => {
                    const toolCall: UaiToolCallInfo = { ...info, status: "pending" };
                    this.#currentToolCalls = [...this.#currentToolCalls, toolCall];

                    let messages = [...this.#messages.value];

                    if (!this.#currentAssistantMessageId) {
                        const newMessage: UaiChatMessage = {
                            id: crypto.randomUUID(),
                            role: "assistant",
                            content: "",
                            toolCalls: [toolCall],
                            timestamp: new Date(),
                        };
                        messages.push(newMessage);
                        this.#currentAssistantMessageId = newMessage.id;
                    } else {
                        messages = messages.map((msg) =>
                            msg.id === this.#currentAssistantMessageId
                                ? { ...msg, toolCalls: [...(msg.toolCalls || []), toolCall] }
                                : msg,
                        );
                    }

                    this.#messages.next(messages);
                    this.#agentState.next({ status: "executing", currentStep: `Calling ${info.name}...` });
                },
                onToolCallArgsEnd: (id, args) => this.#handleToolCallArgsEnd(id, args),
                onToolCallResult: (id, result) => this.#handleServerToolResult(id, result),
                onRunFinished: (event) => this.#handleRunFinished(event),
                onStateSnapshot: (state) => this.#agentState.next(state),
                onStateDelta: (delta) => {
                    const merged = { ...this.#agentState.value, ...delta } as UaiAgentState;
                    this.#agentState.next(merged);
                },
                onMessagesSnapshot: (messages) => this.#messages.next(messages),
                onCustomEvent: (name, value) => {
                    if (name === "agent_selected") {
                        const agentInfo = value as { agentId: string; agentName: string; agentAlias: string };
                        this.#resolvedAgent.next(agentInfo);
                    }
                },
                onError: (error) => {
                    console.error("Run error:", error);
                    this.#agentState.next(undefined);
                },
            },
        );
    }

    #handleToolCallArgsEnd(toolCallId: string, argsJson: string): void {
        const parsedArgs = safeParseJson(argsJson);

        this.#currentToolCalls = this.#currentToolCalls.map((tc) =>
            tc.id === toolCallId ? { ...tc, arguments: argsJson, parsedArgs } : tc,
        );

        const messages = this.#messages.value.map((msg) => {
            if (msg.id === this.#currentAssistantMessageId && msg.toolCalls) {
                return {
                    ...msg,
                    toolCalls: msg.toolCalls.map((tc) =>
                        tc.id === toolCallId ? { ...tc, arguments: argsJson, parsedArgs } : tc,
                    ),
                };
            }
            return msg;
        });
        this.#messages.next(messages);
    }

    #handleServerToolResult(toolCallId: string, result: string): void {
        this.#currentToolCalls = this.#currentToolCalls.map((tc) =>
            tc.id === toolCallId ? { ...tc, status: "completed", result } : tc,
        );

        const updated = this.#messages.value.map((msg) => {
            if (msg.id === this.#currentAssistantMessageId && msg.toolCalls) {
                return {
                    ...msg,
                    toolCalls: msg.toolCalls.map((tc) =>
                        tc.id === toolCallId ? { ...tc, status: "completed" as const, result } : tc,
                    ),
                };
            }
            return msg;
        });

        const toolMessage: UaiChatMessage = {
            id: crypto.randomUUID(),
            role: "tool",
            content: result,
            toolCallId: toolCallId,
            timestamp: new Date(),
        };

        this.#messages.next([...updated, toolMessage]);
    }

    #handleRunFinished(event: { outcome: string; interrupt?: UaiInterruptInfo; error?: string }): void {
        this.#streamingContent.next("");

        const assistantMessageId = this.#currentAssistantMessageId;

        this.#currentAssistantMessageId = null;
        this.#currentToolCalls = [];

        if (event.outcome === "error") {
            this.#handleError(event.error);
            return;
        }

        if (event.outcome === "interrupt" && event.interrupt) {
            const context = this.#createInterruptContext(assistantMessageId);
            if (this.#handlerRegistry.handle(event.interrupt, context)) {
                return;
            }
        }

        this.#agentState.next(undefined);
    }

    #createInterruptContext(assistantMessageId: string | null): UaiInterruptContext {
        return {
            resume: (response?: unknown) => this.#resumeRun(response),
            setAgentState: (state?: UaiAgentState) => this.#agentState.next(state),
            lastAssistantMessageId: assistantMessageId ?? this.#currentAssistantMessageId ?? undefined,
            messages: this.#messages.value,
        };
    }

    #handleError(error?: string): void {
        const errorMessage: UaiChatMessage = {
            id: crypto.randomUUID(),
            role: "assistant",
            content: `Error: ${error ?? "An error occurred"}`,
            timestamp: new Date(),
        };
        this.#messages.next([...this.#messages.value, errorMessage]);
        this.#agentState.next(undefined);
    }

    #resumeRun(response?: unknown): void {
        if (response !== undefined) {
            const userMessage: UaiChatMessage = {
                id: crypto.randomUUID(),
                role: "user",
                content: typeof response === "string" ? response : JSON.stringify(response),
                timestamp: new Date(),
            };
            this.#messages.next([...this.#messages.value, userMessage]);
        }
        this.#agentState.next({ status: "thinking" });
        const frontendTools = this.#frontendToolManager?.frontendTools ?? [];
        this.#client?.sendMessage(this.#messages.value, frontendTools, this.#pendingContext);
    }

    #handleToolResult(result: UaiFrontendToolResult): void {
        const resultContent = typeof result.result === "string" ? result.result : JSON.stringify(result.result);
        const newStatus: UaiToolCallStatus = result.error ? "error" : "completed";

        const updated = this.#messages.value.map((msg) => {
            if (msg.role === "assistant" && msg.toolCalls) {
                return {
                    ...msg,
                    toolCalls: msg.toolCalls.map((tc) =>
                        tc.id === result.toolCallId ? { ...tc, status: newStatus, result: resultContent } : tc,
                    ),
                };
            }
            return msg;
        });

        const toolMessage: UaiChatMessage = {
            id: crypto.randomUUID(),
            role: "tool",
            content: resultContent,
            toolCallId: result.toolCallId,
            timestamp: new Date(),
        };
        this.#messages.next([...updated, toolMessage]);
    }

    #handleToolStatusUpdate(update: UaiFrontendToolStatusUpdate): void {
        const updated = this.#messages.value.map((msg) => {
            if (msg.role === "assistant" && msg.toolCalls) {
                return {
                    ...msg,
                    toolCalls: msg.toolCalls.map((tc) =>
                        tc.id === update.toolCallId ? { ...tc, status: update.status } : tc,
                    ),
                };
            }
            return msg;
        });
        this.#messages.next(updated);
    }
}
