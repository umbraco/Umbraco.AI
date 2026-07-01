import { type Message, EventType as AGUIEventType, transformChunks, type Tool } from "@ag-ui/client";
import { UaiHttpAgent } from "./uai-http-agent.js";
import {
    UaiChatMessage,
    UaiToolCallInfo,
    UaiInterruptInfo,
    AgentClientCallbacks,
    UaiFrontendTool,
    AgentTransport,
    type AGUIEvent,
    type Interrupt,
    type RunErrorEvent,
    type ToolCallStartEvent,
    type ToolCallArgsEvent,
    type ToolCallEndEvent,
    type AGUIRunFinishedEvent,
    type MessagesSnapshotEvent,
    type UaiAgentState,
    type UaiInputContent,
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

    /** Stable thread ID for the conversation — persists across turns so file references resolve */
    #threadId = crypto.randomUUID();

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
     * When contentParts is present, sends it as the content field (multimodal).
     */
    static #toAGUIMessage(m: UaiChatMessage): Message {
        if (m.role === "user") {
            // When contentParts are present, send as content array (AG-UI multimodal draft)
            const content = m.contentParts && m.contentParts.length > 0
                ? m.contentParts as unknown as string  // AG-UI client types content as string, but protocol accepts array
                : m.content;
            return {
                id: m.id,
                role: "user" as const,
                content,
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
     * @param resume Optional resume entries for human_approval or tool_call interrupts
     */
    sendMessage(
        messages: UaiChatMessage[],
        tools?: UaiFrontendTool[],
        context?: Array<{ description: string; value: string }>,
        resume?: Array<{ interruptId: string; status: "resolved" | "cancelled"; payload?: unknown }>,
    ): void {
        const runId = crypto.randomUUID();

        // Clear any pending tool args from previous run
        this.#pendingToolArgs.clear();

        // Convert and set messages on transport
        const convertedMessages = messages.map((m) => UaiAgentClient.#toAGUIMessage(m));
        this.#transport.setMessages(convertedMessages);

        // Map UaiFrontendTool[] -> AG-UI Tool[] with metadata inline (per AG-UI spec
        // Tool.metadata field). Replaces the previous forwardedProps.toolMetadata
        // side-channel where metadata was sent in a parallel array and rejoined by name.
        const aguiTools = (tools ?? []).map((tool) => UaiAgentClient.#toAGUITool(tool));

        // Subscribe to the transport's event stream
        // Apply transformChunks to convert CHUNK events → START/CONTENT/END events
        this.#transport
            .run({
                threadId: this.#threadId,
                runId,
                messages: convertedMessages,
                tools: aguiTools,
                context: context ?? [],
                // Thread resume entries through forwardedProps so UaiHttpAgent can lift
                // them into the typed body.resume field on the server request.
                forwardedProps: resume?.length ? { resume } : undefined,
            })
            .pipe(transformChunks(false))
            .subscribe({
                next: (event) => this.#handleEvent(event as AGUIEvent),
                error: (error) => {
                    const err = error instanceof Error ? error : new Error(String(error));
                    this.#callbacks.onError?.(err);
                },
            });
    }

    /**
     * Convert a UaiFrontendTool to an AG-UI Tool. Vendor-specific fields (scope,
     * isDestructive) travel inline via the spec's Tool.metadata field.
     */
    static #toAGUITool(tool: UaiFrontendTool): Tool {
        const metadata: Record<string, unknown> = {};
        if (tool.scope !== undefined) {
            metadata.scope = tool.scope;
        }
        if (tool.isDestructive !== undefined) {
            metadata.isDestructive = tool.isDestructive;
        }

        const result: Tool = {
            name: tool.name,
            description: tool.description,
            parameters: tool.parameters,
        };
        if (Object.keys(metadata).length > 0) {
            (result as Tool & { metadata?: Record<string, unknown> }).metadata = metadata;
        }
        return result;
    }

    /**
     * Handle incoming AG-UI events.
     */
    #handleEvent(event: AGUIEvent) {
        switch (event.type) {
            case AGUIEventType.RUN_STARTED:
                // No-op: the run started when sendMessage was called; no UI reaction needed.
                break;

            case AGUIEventType.TEXT_MESSAGE_START:
                if (event.messageId) {
                    this.#callbacks.onTextStart?.(event.messageId);
                }
                break;

            case AGUIEventType.TEXT_MESSAGE_CONTENT:
                this.#callbacks.onTextDelta?.(event.delta);
                break;

            case AGUIEventType.TEXT_MESSAGE_END:
                this.#callbacks.onTextEnd?.();
                break;

            case AGUIEventType.TOOL_CALL_START:
                this.#handleToolCallStart(event);
                break;

            case AGUIEventType.TOOL_CALL_ARGS:
                this.#handleToolCallArgs(event);
                break;

            case AGUIEventType.TOOL_CALL_END:
                this.#handleToolCallEnd(event);
                break;

            case AGUIEventType.TOOL_CALL_RESULT:
                this.#callbacks.onToolCallResult?.(event.toolCallId, event.content);
                break;

            case AGUIEventType.RUN_FINISHED:
                this.#handleRunFinished(event as AGUIRunFinishedEvent);
                break;

            case AGUIEventType.RUN_ERROR: {
                const runError = event as RunErrorEvent;
                this.#callbacks.onError?.(new Error(runError.message), runError.code);
                break;
            }

            case AGUIEventType.STATE_SNAPSHOT:
                this.#callbacks.onStateSnapshot?.(event.snapshot as UaiAgentState);
                break;

            case AGUIEventType.STATE_DELTA:
                // TODO: apply RFC 6902 JSON Patch ops instead of passing the raw array through.
                this.#callbacks.onStateDelta?.(event.delta as unknown as Partial<UaiAgentState>);
                break;

            case AGUIEventType.MESSAGES_SNAPSHOT:
                this.#handleMessagesSnapshot(event);
                break;

            case AGUIEventType.CUSTOM:
                this.#callbacks.onCustomEvent?.(event.name, event.value);
                break;

            default:
                console.warn("Received unhandled event type:", event.type);
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

    #handleRunFinished(event: AGUIRunFinishedEvent) {
        // `outcome` is optional in the SDK schema; absent/success both complete the run.
        if (event.outcome?.type === "interrupt") {
            // The callback shape currently surfaces a single interrupt; batched
            // interrupts beyond the first are dropped here. TODO: extend the
            // callback to iterate when batched HITL flows land.
            const first = event.outcome.interrupts[0];
            const interrupt = UaiAgentClient.#parseInterrupt(first);
            this.#callbacks.onRunFinished?.({
                outcome: "interrupt",
                interrupt,
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
            content: string | UaiInputContent[];
            toolCalls?: Array<{ id: string; type: string; function: { name: string; arguments: string } }>;
            toolCallId?: string;
        }>;

        const messages: UaiChatMessage[] = rawMessages.map((m) => {
            // Content can be a string or an array of content parts (multimodal)
            const isMultimodal = Array.isArray(m.content);
            const contentParts = isMultimodal ? (m.content as UaiInputContent[]) : undefined;
            const textContent = isMultimodal
                ? (m.content as UaiInputContent[])
                    .filter((p): p is { type: "text"; text: string } => p.type === "text")
                    .map((p) => p.text)
                    .join("")
                : (m.content as string);

            return {
                id: m.id,
                role: m.role as "user" | "assistant" | "tool",
                content: textContent,
                contentParts,
                toolCalls: m.toolCalls?.map((tc) => ({
                    id: tc.id,
                    name: tc.function.name,
                    arguments: tc.function.arguments ?? "{}",
                    status: "completed" as const,
                })),
                toolCallId: m.toolCallId,
                timestamp: new Date(),
            };
        });

        this.#callbacks.onMessagesSnapshot?.(messages);
    }

    /**
     * Map an AG-UI Interrupt object onto our UI-shaped UaiInterruptInfo.
     * Spec fields (id / reason / message / toolCallId / metadata) come from
     * the Interrupt directly; UI-render hints (type / title / options /
     * inputConfig) are read from `metadata` if the server attached them there.
     */
    static #parseInterrupt(raw: Interrupt): UaiInterruptInfo {
        const metadata = raw.metadata ?? {};
        return {
            id: raw.id ?? crypto.randomUUID(),
            reason: raw.reason,
            type: (metadata.type as UaiInterruptInfo["type"]) ?? "custom",
            title: (metadata.title as string) ?? "Action Required",
            message: raw.message ?? "",
            options: metadata.options as UaiInterruptInfo["options"],
            inputConfig: metadata.inputConfig as UaiInterruptInfo["inputConfig"],
            payload: raw.toolCallId ? { toolCallId: raw.toolCallId } : undefined,
            metadata: raw.metadata,
        };
    }

    /**
     * Reset the client state.
     * Clears pending tool arguments and generates a new thread ID
     * so file references from the previous conversation are not reused.
     */
    reset(): void {
        this.#pendingToolArgs.clear();
        this.#threadId = crypto.randomUUID();
    }
}
