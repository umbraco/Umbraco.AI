import { AbstractAgent, type RunAgentInput, type BaseEvent, type Message, type Tool } from "@ag-ui/client";
import { Observable } from "rxjs";
import {
    AgentsService,
    type AGUIRunRequestModel,
    type AGUIMessageModel,
    type AGUIToolModel,
    type AGUIToolCallModel,
    type AGUIMessageRoleModel,
    type AGUIContextItemModel,
} from "../api/index.js";
import type { AgentTransport } from "./types.js";

/**
 * Configuration for the UaiHttpAgent.
 */
export interface UaiHttpAgentConfig {
    /** The agent ID to connect to */
    agentId: string;
}

/**
 * HTTP Agent implementation that uses the hey-api generated client.
 * This provides automatic authentication via the hey-api client configuration.
 * Implements AgentTransport for dependency injection support.
 */
export class UaiHttpAgent extends AbstractAgent implements AgentTransport {
    #agentId: string;
    #abortController?: AbortController;

    constructor(config: UaiHttpAgentConfig) {
        super({ agentId: config.agentId });
        this.#agentId = config.agentId;
    }

    /**
     * Run the agent with the given input.
     * Returns an Observable that emits BaseEvent objects as they arrive from the server.
     */
    run(input: RunAgentInput): Observable<BaseEvent> {
        return new Observable((subscriber) => {
            // Create a local AbortController for this specific run.
            // IMPORTANT: Capture in local variable to avoid race condition where
            // cleanup from a previous Observable aborts a newer run's controller.
            const abortController = new AbortController();
            this.#abortController = abortController;

            this.#runAsync(input, subscriber, abortController.signal).catch((error) => {
                subscriber.error(error);
            });

            // Cleanup function - called when unsubscribed
            // Uses local variable, not instance field, to abort only this run's controller
            return () => {
                abortController.abort();
            };
        });
    }

    async #runAsync(
        input: RunAgentInput,
        subscriber: {
            next: (event: BaseEvent) => void;
            complete: () => void;
            error: (err: unknown) => void;
        },
        signal: AbortSignal,
    ): Promise<void> {
        // Convert AG-UI RunAgentInput to hey-api AGUIRunRequestModel
        const body: AGUIRunRequestModel = {
            threadId: input.threadId,
            runId: input.runId,
            messages: input.messages.map((msg) => this.#toAGUIMessage(msg)),
            tools: input.tools?.map((tool) => this.#toAGUITool(tool)),
            state: input.state,
            context: input.context?.map((ctx) => this.#toAGUIContext(ctx)),
            forwardedProps: input.forwardedProps,
        };

        const result = await AgentsService.runAgent({
            path: { agentIdOrAlias: this.#agentId },
            body,
            signal,
        });

        // Iterate over the SSE stream and emit events
        for await (const event of result.stream) {
            // SSE events are parsed JSON objects that conform to BaseEvent
            subscriber.next(event as unknown as BaseEvent);
        }

        subscriber.complete();
    }

    #toAGUIMessage(msg: Message): AGUIMessageModel {
        // Handle content which can be string or ContentPart[]
        let content: string | undefined;
        if (typeof msg.content === "string") {
            content = msg.content;
        } else if (Array.isArray(msg.content)) {
            // Extract text from content parts
            content = msg.content
                .filter((part): part is { type: "text"; text: string } => part.type === "text")
                .map((part) => part.text)
                .join("");
        }

        // Convert toolCalls for assistant messages
        let toolCalls: AGUIToolCallModel[] | undefined;
        if ("toolCalls" in msg && Array.isArray(msg.toolCalls) && msg.toolCalls.length > 0) {
            toolCalls = msg.toolCalls.map(
                (tc: { id: string; type: string; function: { name: string; arguments: string } }) => ({
                    id: tc.id,
                    type: tc.type,
                    function: {
                        name: tc.function.name,
                        arguments: tc.function.arguments,
                    },
                }),
            );
        }

        return {
            id: msg.id,
            role: this.#mapRole(msg.role),
            content,
            toolCallId: "toolCallId" in msg ? (msg.toolCallId as string) : undefined,
            toolCalls,
        };
    }

    #mapRole(role: string): AGUIMessageRoleModel {
        return role.toLowerCase() as AGUIMessageRoleModel;
    }

    #toAGUITool(tool: Tool): AGUIToolModel {
        return {
            name: tool.name,
            description: tool.description,
            parameters: tool.parameters,
        };
    }

    #toAGUIContext(ctx: { description: string; value: string }): AGUIContextItemModel {
        return {
            description: ctx.description,
            value: ctx.value,
        };
    }

    /**
     * Abort the current run.
     */
    abortRun(): void {
        this.#abortController?.abort();
        this.#abortController = undefined;
    }

    /**
     * Create a clone of this agent.
     */
    clone(): UaiHttpAgent {
        return new UaiHttpAgent({ agentId: this.#agentId });
    }
}
