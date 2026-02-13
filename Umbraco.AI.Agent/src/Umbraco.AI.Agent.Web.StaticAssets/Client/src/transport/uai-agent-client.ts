import { type Message, type BaseEvent, EventType as AGUIEventType, transformChunks, type Tool } from "@ag-ui/client";
import { UaiHttpAgent } from "./uai-http-agent.js";
import type {
    UaiChatMessage,
    UaiToolCallInfo,
    UaiInterruptInfo,
    AgentClientCallbacks,
    UaiFrontendTool,
    AgentTransport,
    TextMessageStartEvent,
    TextMessageContentEvent,
    ToolCallStartEvent,
    ToolCallArgsEvent,
    ToolCallEndEvent,
    ToolCallResultEvent,
    RunFinishedAGUIEvent,
    RunErrorEvent,
    StateSnapshotEvent,
    StateDeltaEvent,
    MessagesSnapshotEvent,
} from "./types.js";

/**
 * Configuration for the Uai Agent Client.
 */
export interface UaiAgentClientConfig {
    /** Agent ID to connect to */
    agentId: string;
}

/**
 * Client wrapper for AG-UI protocol.
 * Pure event bridge - receives AG-UI events and forwards via callbacks.
 * State management is handled by UaiCopilotRunController.
 */
export class UaiAgentClient {
    #transport: AgentTransport;
    #callbacks: AgentClientCallbacks;

    /** Accumulates tool call arguments during streaming */
    #pendingToolArgs = new Map<string, string>();

    /**
     * Create a new UaiAgentClient with an injected transport.
     * For production use, prefer the static create() factory method.
     * @param transport The transport layer for agent communication
     * @param callbacks Optional callbacks for handling events
     */
    constructor(transport: AgentTransport, callbacks: AgentClientCallbacks = {}) {
        this.#transport = transport;
        this.#callbacks = callbacks;
    }

    /**
     * Factory method for creating a UaiAgentClient in production.
     * Creates the appropriate transport layer internally.
     * @param config Configuration for the agent client
     * @param callbacks Optional callbacks for handling events
     * @returns A new UaiAgentClient instance
     */
    static create(config: UaiAgentClientConfig, callbacks?: AgentClientCallbacks): UaiAgentClient {
        const transport = new UaiHttpAgent({ agentId: config.agentId });
        return new UaiAgentClient(transport, callbacks);
    }

    /**
     * Update the callbacks dynamically.
     * @param callbacks The new set of callbacks to use
     */
    setCallbacks(callbacks: AgentClientCallbacks) {
        this.#callbacks = callbacks;
    }

    /**
     * Convert UaiChatMessage to AG-UI Message format.
     */
    static #toAGUIMessage(m: UaiChatMessage): Message {
        if (m.role === "user") {
            return {
                id: m.id,
                role: "user" as const,
                content: m.content,
            };
        } else if (m.role === "assistant") {
            // Include tool calls if present - critical for LLM to know what was already called
            const toolCalls = m.toolCalls?.map((tc) => ({
                id: tc.id,
                type: "function" as const,
                function: {
                    name: tc.name,
                    arguments: tc.arguments ?? "{}",
                },
            }));

            return {
                id: m.id,
                role: "assistant" as const,
                content: m.content,
                ...(toolCalls && toolCalls.length > 0 && { toolCalls }),
            };
        } else {
            // tool message requires toolCallId
            return {
                id: m.id,
                role: "tool" as const,
                content: m.content,
                toolCallId: m.toolCallId ?? m.id,
            };
        }
    }

    /**
     * Send messages and start a new run.
     * @param messages The messages to send
     * @param tools Optional frontend tools to include (with metadata)
     * @param context Optional context items to include for LLM awareness
     */
    sendMessage(
        messages: UaiChatMessage[],
        tools?: UaiFrontendTool[],
        context?: Array<{ description: string; value: string }>,
    ): void {
        const threadId = crypto.randomUUID();
        const runId = crypto.randomUUID();

        // Clear any pending tool args from previous run
        this.#pendingToolArgs.clear();

        // Convert and set messages on transport
        const convertedMessages = messages.map((m) => UaiAgentClient.#toAGUIMessage(m));
        this.#transport.setMessages(convertedMessages);

        // Split UaiFrontendTool into AGUITool[] and tool metadata
        const { aguiTools, toolMetadata } = this.#splitFrontendTools(tools ?? []);

        // Subscribe to the transport's event stream
        // Apply transformChunks to convert CHUNK events → START/CONTENT/END events
        this.#transport
            .run({
                threadId,
                runId,
                messages: convertedMessages,
                tools: aguiTools,
                context: context ?? [],
                forwardedProps: toolMetadata.length > 0 ? { toolMetadata } : undefined,
            })
            .pipe(transformChunks(false))
            .subscribe({
                next: (event) => this.#handleEvent(event),
                error: (error) => {
                    const err = error instanceof Error ? error : new Error(String(error));
                    this.#callbacks.onError?.(err);
                },
            });
    }

    /**
     * Split UaiFrontendTool[] into AGUITool[] and tool metadata for forwardedProps.
     * @param tools Array of UaiFrontendTool objects with metadata
     * @returns Object with aguiTools (for LLM) and toolMetadata (for backend permission filtering)
     */
    #splitFrontendTools(tools: UaiFrontendTool[]): {
        aguiTools: Tool[];
        toolMetadata: Array<{ toolName: string; scope?: string; isDestructive: boolean }>;
    } {
        const aguiTools: Tool[] = [];
        const toolMetadata: Array<{ toolName: string; scope?: string; isDestructive: boolean }> = [];

        for (const tool of tools) {
            // AG-UI tool (for LLM)
            aguiTools.push({
                name: tool.name,
                description: tool.description,
                parameters: tool.parameters,
            });

            // Tool metadata (for backend permission filtering)
            toolMetadata.push({
                toolName: tool.name,
                scope: tool.scope,
                isDestructive: tool.isDestructive ?? false,
            });
        }

        return { aguiTools, toolMetadata };
    }

    /**
     * Handle incoming AG-UI events.
     */
    #handleEvent(event: BaseEvent) {
        switch (event.type) {
            case AGUIEventType.TEXT_MESSAGE_START: {
                const messageId = (event as TextMessageStartEvent).messageId;
                if (messageId) {
                    this.#callbacks.onTextStart?.(messageId);
                }
                break;
            }

            case AGUIEventType.TEXT_MESSAGE_CONTENT:
                this.#callbacks.onTextDelta?.((event as TextMessageContentEvent).delta);
                break;

            case AGUIEventType.TEXT_MESSAGE_END:
                this.#callbacks.onTextEnd?.();
                break;

            case AGUIEventType.TOOL_CALL_START:
                this.#handleToolCallStart(event as ToolCallStartEvent);
                break;

            case AGUIEventType.TOOL_CALL_ARGS:
                this.#handleToolCallArgs(event as ToolCallArgsEvent);
                break;

            case AGUIEventType.TOOL_CALL_END:
                this.#handleToolCallEnd(event as ToolCallEndEvent);
                break;

            case AGUIEventType.TOOL_CALL_RESULT:
                this.#callbacks.onToolCallResult?.(
                    (event as ToolCallResultEvent).toolCallId,
                    (event as ToolCallResultEvent).content,
                );
                break;

            case AGUIEventType.RUN_FINISHED:
                this.#handleRunFinished(event as RunFinishedAGUIEvent);
                break;

            case AGUIEventType.RUN_ERROR:
                this.#callbacks.onError?.(new Error((event as RunErrorEvent).message));
                break;

            case AGUIEventType.STATE_SNAPSHOT:
                this.#callbacks.onStateSnapshot?.((event as StateSnapshotEvent).state);
                break;

            case AGUIEventType.STATE_DELTA:
                this.#callbacks.onStateDelta?.((event as StateDeltaEvent).delta);
                break;

            case AGUIEventType.MESSAGES_SNAPSHOT:
                this.#handleMessagesSnapshot(event as MessagesSnapshotEvent);
                break;

            default:
                // Handle CUSTOM event type
                // AG-UI client may not export CUSTOM in EventType enum, so check for string match
                if ((event as any).type === "CUSTOM") {
                    const customEvent = event as { name: string; value: unknown };
                    this.#callbacks.onCustomEvent?.(customEvent.name, customEvent.value);
                }
                break;
        }
    }

    #handleToolCallStart(event: ToolCallStartEvent) {
        const toolCall: UaiToolCallInfo = {
            id: event.toolCallId,
            name: event.toolCallName,
            arguments: "",
            status: "pending",
        };
        this.#pendingToolArgs.set(event.toolCallId, "");
        this.#callbacks.onToolCallStart?.(toolCall);
    }

    #handleToolCallArgs(event: ToolCallArgsEvent) {
        const current = this.#pendingToolArgs.get(event.toolCallId) ?? "";
        this.#pendingToolArgs.set(event.toolCallId, current + event.delta);
    }

    #handleToolCallEnd(event: ToolCallEndEvent) {
        const args = this.#pendingToolArgs.get(event.toolCallId);
        if (args !== undefined) {
            this.#callbacks.onToolCallArgsEnd?.(event.toolCallId, args);
            this.#callbacks.onToolCallEnd?.(event.toolCallId);
        }
    }

    #handleRunFinished(event: RunFinishedAGUIEvent) {
        // Normalize outcome to lowercase for case-insensitive comparison
        // Backend sends PascalCase (e.g., "Interrupt") but we use lowercase
        const outcome = (event.outcome ?? "").toLowerCase();

        if (outcome === "interrupt") {
            const interrupt = UaiAgentClient.#parseInterrupt(event.interrupt);
            this.#callbacks.onRunFinished?.({
                outcome: "interrupt",
                interrupt,
            });
        } else if (outcome === "error") {
            this.#callbacks.onRunFinished?.({
                outcome: "error",
                error: event.error as string,
            });
        } else {
            this.#callbacks.onRunFinished?.({
                outcome: "success",
            });
        }
    }

    #handleMessagesSnapshot(event: MessagesSnapshotEvent) {
        const rawMessages = event.messages as Array<{
            id: string;
            role: string;
            content: string;
        }>;

        const messages: UaiChatMessage[] = rawMessages.map((m) => ({
            id: m.id,
            role: m.role as "user" | "assistant" | "tool",
            content: m.content,
            timestamp: new Date(),
        }));

        this.#callbacks.onMessagesSnapshot?.(messages);
    }

    static #parseInterrupt(raw: unknown): UaiInterruptInfo {
        const data = raw as Record<string, unknown>;

        return {
            id: (data.id as string) ?? crypto.randomUUID(),
            reason: data.reason as string | undefined,
            type: (data.type as UaiInterruptInfo["type"]) ?? "custom",
            title: (data.title as string) ?? "Action Required",
            message: (data.message as string) ?? "",
            options: data.options as UaiInterruptInfo["options"],
            inputConfig: data.inputConfig as UaiInterruptInfo["inputConfig"],
            payload: data.payload as Record<string, unknown>,
            metadata: data.metadata as Record<string, unknown>,
        };
    }

    /**
     * Reset the client state.
     * Clears pending tool arguments.
     */
    reset(): void {
        this.#pendingToolArgs.clear();
    }
}
