import { customElement, property, state, css, html } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UaiAgentClient } from "../client/ag-ui-client.js";
import { FrontendToolManager } from "../client/frontend-tool-manager.js";
import type {
  ChatMessage,
  AgentState,
  InterruptInfo,
  ToolCallInfo,
} from "../types.js";

// Import sub-components
import "./message.element.js";
import "./input.element.js";
import "./agent-status.element.js";
import "./interrupt.element.js";

/**
 * Main chat component.
 * Orchestrates all sub-components and manages AG-UI client connection.
 */
@customElement("uai-copilot-chat")
export class UaiCopilotChatElement extends UmbLitElement {
  @property({ type: String })
  agentId = "";

  @property({ type: String })
  agentName = "";

  @state()
  private _messages: ChatMessage[] = [];

  @state()
  private _agentState?: AgentState;

  @state()
  private _activeInterrupt?: InterruptInfo;

  @state()
  private _isLoading = false;

  @state()
  private _streamingContent = "";

  @state()
  private _currentToolCalls: ToolCallInfo[] = [];

  #client?: UaiAgentClient;
  #toolManager?: FrontendToolManager;
  #messagesContainer?: HTMLElement;

  override connectedCallback() {
    super.connectedCallback();
    this.#initClient();
  }

  override updated(changedProperties: Map<string, unknown>) {
    super.updated(changedProperties);

    if (changedProperties.has("agentId") && this.agentId) {
      this.#initClient();
    }

    // Auto-scroll to bottom when messages change
    if (changedProperties.has("_messages") || changedProperties.has("_streamingContent")) {
      this.#scrollToBottom();
    }
  }

  #initClient() {
    if (!this.agentId) return;

    // Initialize tool manager
    this.#toolManager = new FrontendToolManager(this);
    const frontendTools = this.#toolManager.loadFromRegistry();

    this.#client = UaiAgentClient.create(
      { agentId: this.agentId },
      {
        onTextDelta: (delta) => {
          this._streamingContent += delta;
        },
        onTextEnd: () => {
          // Don't finalize here - wait for RUN_FINISHED to include any tool calls
          // The streaming content is preserved until then
        },
        onToolCallStart: (info) => {
          this._currentToolCalls = [...this._currentToolCalls, info];
          this._agentState = { status: "executing", currentStep: `Calling ${info.name}...` };
        },
        onToolCallArgsEnd: (id, args) => {
          this.#handleToolCallArgsEnd(id, args);
        },
        onToolCallEnd: (id) => {
          // Queue frontend tools for execution after RUN_FINISHED
          const toolCall = this._currentToolCalls.find((tc) => tc.id === id) as ToolCallInfo & { parsedArgs?: Record<string, unknown> } | undefined;
          if (toolCall && this.#toolManager) {
            this.#toolManager.queueForExecution(id, toolCall.name, toolCall.parsedArgs ?? {});
          }
        },
        onRunFinished: (event) => {
          this.#handleRunFinished(event);
        },
        onStateSnapshot: (state) => {
          this._agentState = state;
        },
        onStateDelta: (delta) => {
          this._agentState = { ...this._agentState, ...delta } as AgentState;
        },
        onMessagesSnapshot: (messages) => {
          this._messages = messages;
        },
        onError: (error) => {
          console.error("Chat error:", error);
          this._isLoading = false;
          this._agentState = undefined;
        },
      }
    );

    // Register frontend tools with the client
    if (frontendTools.length > 0) {
      this.#client.setFrontendTools(frontendTools);
    }
  }

  #finalizeAssistantMessage(content: string) {
    const assistantMessage: ChatMessage = {
      id: crypto.randomUUID(),
      role: "assistant",
      content,
      toolCalls: this._currentToolCalls.length > 0 ? [...this._currentToolCalls] : undefined,
      timestamp: new Date(),
    };

    this._messages = [...this._messages, assistantMessage];
    this._streamingContent = "";
    this._currentToolCalls = [];
  }

  #handleToolCallArgsEnd(toolCallId: string, argsJson: string) {
    // Parse and update tool call with parsed arguments
    const args = FrontendToolManager.parseArgs(argsJson);
    this._currentToolCalls = FrontendToolManager.updateToolCall(
      this._currentToolCalls,
      toolCallId,
      { arguments: argsJson, parsedArgs: args }
    );
  }

  /**
   * Execute a frontend tool by ID with pre-parsed arguments.
   */
  async #executeFrontendToolById(toolCallId: string, toolName: string, args: Record<string, unknown>) {
    if (!this.#client || !this.#toolManager) return;

    // Check if this is a frontend tool we can execute
    if (!this.#toolManager.isFrontendTool(toolName)) {
      return;
    }

    this._agentState = { status: "executing", currentStep: `Executing ${toolName}...` };

    // Execute the tool using the manager
    const { result: resultContent, hasError } = await this.#toolManager.execute(toolName, args);

    // Update the assistant message's tool call with result and status
    this._messages = this._messages.map((msg) => {
      if (msg.role === "assistant" && msg.toolCalls) {
        return {
          ...msg,
          toolCalls: msg.toolCalls.map((tc) =>
            tc.id === toolCallId
              ? { ...tc, status: hasError ? "error" : "completed", result: resultContent }
              : tc
          ),
        } as ChatMessage;
      }
      return msg;
    });

    // Create tool result message and add to local messages
    const toolMessage: ChatMessage = {
      id: crypto.randomUUID(),
      role: "tool",
      content: resultContent,
      toolCallId,
      timestamp: new Date(),
    };

    this._messages = [...this._messages, toolMessage];

    // Continue the conversation with updated messages
    await this.#client.sendMessage(this._messages);
  }

  async #handleRunFinished(event: { outcome: string; interrupt?: InterruptInfo; error?: string }) {
    // First, finalize the assistant message with any tool calls
    if (this._streamingContent || this._currentToolCalls.length > 0) {
      this.#finalizeAssistantMessage(this._streamingContent);
    }

    // Get pending tool executions from the manager
    const toolsToExecute = this.#toolManager?.consumePendingExecutions() ?? [];

    if (event.outcome === "interrupt" && event.interrupt) {
      this._isLoading = false;
      this._agentState = undefined;
      this._activeInterrupt = event.interrupt;
    } else if (event.outcome === "error") {
      this._isLoading = false;
      this._agentState = undefined;
      // Add error message
      this._messages = [
        ...this._messages,
        {
          id: crypto.randomUUID(),
          role: "assistant",
          content: `Error: ${event.error ?? "An error occurred"}`,
          timestamp: new Date(),
        },
      ];
    } else {
      // Success - check if we have frontend tools to execute
      if (toolsToExecute.length > 0) {
        // Execute frontend tools
        for (const toolExec of toolsToExecute) {
          await this.#executeFrontendToolById(toolExec.id, toolExec.name, toolExec.args);
        }
        // Loading state is managed by tool execution
      } else {
        this._isLoading = false;
        this._agentState = undefined;
      }
    }
  }

  #handleSendMessage(e: CustomEvent<string>) {
    if (!this.#client) return;

    const content = e.detail;
    const userMessage: ChatMessage = {
      id: crypto.randomUUID(),
      role: "user",
      content,
      timestamp: new Date(),
    };

    this._messages = [...this._messages, userMessage];
    this._isLoading = true;
    this._agentState = { status: "thinking" };

    this.#client.sendMessage(this._messages);
  }

  #handleInterruptResponse(e: CustomEvent<string>) {
    if (!this.#client) return;

    const response = e.detail;
    this._activeInterrupt = undefined;
    this._isLoading = true;
    this._agentState = { status: "thinking" };

    this.#client.resumeRun(response);
  }

  #scrollToBottom() {
    requestAnimationFrame(() => {
      if (this.#messagesContainer) {
        this.#messagesContainer.scrollTop = this.#messagesContainer.scrollHeight;
      }
    });
  }

  #renderMessages() {
    return html`
      ${this._messages.map(
        (msg) => html`<uai-copilot-message .message=${msg}></uai-copilot-message>`
      )}
      ${this._streamingContent
        ? html`
            <uai-copilot-message
              .message=${{
                id: "streaming",
                role: "assistant",
                content: this._streamingContent,
                toolCalls: this._currentToolCalls.length > 0 ? this._currentToolCalls : undefined,
                timestamp: new Date(),
              }}
            ></uai-copilot-message>
          `
        : ""}
    `;
  }

  override render() {
    return html`
      <div class="chat-container">
        <div
          class="messages-area"
          @scroll=${() => {}}
          ${(el: HTMLElement) => (this.#messagesContainer = el)}
        >
          ${this._messages.length === 0 && !this._streamingContent
            ? html`
                <div class="empty-state">
                  <uui-icon name="icon-chat"></uui-icon>
                  <p>Start a conversation with ${this.agentName || "the agent"}</p>
                </div>
              `
            : this.#renderMessages()}
        </div>

        ${this._agentState?.status && this._agentState.status !== "idle"
          ? html`<uai-copilot-agent-status .state=${this._agentState}></uai-copilot-agent-status>`
          : ""}

        ${this._activeInterrupt
          ? html`
              <uai-copilot-interrupt
                .interrupt=${this._activeInterrupt}
                @respond=${this.#handleInterruptResponse}
              ></uai-copilot-interrupt>
            `
          : ""}

        <uai-copilot-input
          ?disabled=${this._isLoading || !!this._activeInterrupt}
          @send=${this.#handleSendMessage}
        ></uai-copilot-input>
      </div>
    `;
  }

  static override styles = css`
    :host {
      display: flex;
      flex-direction: column;
      height: 100%;
    }

    .chat-container {
      display: flex;
      flex-direction: column;
      height: 100%;
      overflow: hidden;
    }

    .messages-area {
      flex: 1;
      overflow-y: auto;
      padding: var(--uui-size-space-2) 0;
    }

    .empty-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      height: 100%;
      color: var(--uui-color-text-alt);
      text-align: center;
      padding: var(--uui-size-space-5);
    }

    .empty-state uui-icon {
      font-size: 48px;
      margin-bottom: var(--uui-size-space-4);
      opacity: 0.5;
    }

    .empty-state p {
      margin: 0;
      font-size: var(--uui-type-default-size);
    }
  `;
}

export default UaiCopilotChatElement;

declare global {
  interface HTMLElementTagNameMap {
    "uai-copilot-chat": UaiCopilotChatElement;
  }
}
