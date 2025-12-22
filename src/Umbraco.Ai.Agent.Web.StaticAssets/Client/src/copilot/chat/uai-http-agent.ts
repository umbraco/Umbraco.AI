import { AbstractAgent, type RunAgentInput, type BaseEvent, type Message, type Tool } from "@ag-ui/client";
import { Observable } from "rxjs";
import { AgentsService } from "../../api/sdk.gen.js";
import type {
  AguiRunRequestModel,
  AguiMessageModel,
  AguiToolModel,
  AguiMessageRoleModel,
  AguiContextItemModel,
} from "../../api/types.gen.js";

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
 */
export class UaiHttpAgent extends AbstractAgent {
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
      this.#abortController = new AbortController();

      this.#runAsync(input, subscriber).catch((error) => {
        subscriber.error(error);
      });

      // Cleanup function - called when unsubscribed
      return () => {
        this.#abortController?.abort();
      };
    });
  }

  async #runAsync(
    input: RunAgentInput,
    subscriber: {
      next: (event: BaseEvent) => void;
      complete: () => void;
      error: (err: unknown) => void;
    }
  ): Promise<void> {
    // Convert AG-UI RunAgentInput to hey-api AguiRunRequestModel
    const body: AguiRunRequestModel = {
      threadId: input.threadId,
      runId: input.runId,
      messages: input.messages.map((msg) => this.#toAguiMessage(msg)),
      tools: input.tools?.map((tool) => this.#toAguiTool(tool)),
      state: input.state,
      context: input.context?.map((ctx) => this.#toAguiContext(ctx)),
      forwardedProps: input.forwardedProps,
    };

    const result = await AgentsService.runAgent({
      path: { agentIdOrAlias: this.#agentId },
      body,
      signal: this.#abortController?.signal,
    });

    // Iterate over the SSE stream and emit events
    for await (const event of result.stream) {
      // SSE events are parsed JSON objects that conform to BaseEvent
      subscriber.next(event as unknown as BaseEvent);
    }

    subscriber.complete();
  }

  #toAguiMessage(msg: Message): AguiMessageModel {
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

    return {
      id: msg.id,
      role: this.#mapRole(msg.role),
      content,
      toolCallId: "toolCallId" in msg ? (msg.toolCallId as string) : undefined,
    };
  }

  #mapRole(role: string): AguiMessageRoleModel {
    const roleMap: Record<string, AguiMessageRoleModel> = {
      user: "User",
      assistant: "Assistant",
      system: "System",
      tool: "Tool",
      developer: "Developer",
    };
    return roleMap[role.toLowerCase()] ?? "User";
  }

  #toAguiTool(tool: Tool): AguiToolModel {
    return {
      name: tool.name,
      description: tool.description,
      parameters: tool.parameters,
    };
  }

  #toAguiContext(ctx: { description: string; value: unknown }): AguiContextItemModel {
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
