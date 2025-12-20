import { customElement, property, state, css, html } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UaiAgentClient } from "./ag-ui-client.js";
import type {
  ChatMessage,
  AgentState,
  InterruptInfo,
  ToolCallInfo,
} from "./types.js";

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

    this.#client = new UaiAgentClient(
      { agentId: this.agentId },
      {
        onTextDelta: (delta) => {
          this._streamingContent += delta;
        },
        onTextEnd: (content) => {
          this.#finalizeAssistantMessage(content);
        },
        onToolCallStart: (info) => {
          this._currentToolCalls = [...this._currentToolCalls, info];
          this._agentState = { status: "executing", currentStep: `Calling ${info.name}...` };
        },
        onToolCallEnd: (id) => {
          this.#handleToolCallEnd(id);
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

  #handleToolCallEnd(toolCallId: string) {
    // Update tool call status
    this._currentToolCalls = this._currentToolCalls.map((tc) =>
      tc.id === toolCallId ? { ...tc, status: "completed" as const } : tc
    );
  }

  #handleRunFinished(event: { outcome: string; interrupt?: InterruptInfo; error?: string }) {
    this._isLoading = false;
    this._agentState = undefined;

    if (event.outcome === "interrupt" && event.interrupt) {
      this._activeInterrupt = event.interrupt;
    } else if (event.outcome === "error") {
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
