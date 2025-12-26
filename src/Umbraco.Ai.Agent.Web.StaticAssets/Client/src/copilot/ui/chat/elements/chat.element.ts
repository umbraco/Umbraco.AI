import { customElement, state, css, html, repeat, ref, createRef } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import type { ChatMessage, AgentState, InterruptInfo } from "../../../core/types.js";
import { UMB_COPILOT_CONTEXT, UMB_COPILOT_RUN_CONTEXT } from "../../../core/copilot.context.js";
import type { CopilotRunController } from "../../../core/controllers/copilot-run.controller.js";

import "./message.element.js";
import "./input.element.js";
import "./agent-status.element.js";
import "./interrupt.element.js";

/**
 * Main chat component.
 * Renders observables from the Copilot run controller and forwards user input.
 */
@customElement("uai-copilot-chat")
export class UaiCopilotChatElement extends UmbLitElement {
  @state()
  private _agentName = "";

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

  #runController?: CopilotRunController;
  #messagesRef = createRef<HTMLElement>();

  constructor() {
    super();
    this.consumeContext(UMB_COPILOT_CONTEXT, (context) => {
      if (context) {
        this.observe(context.agentName, (name) => (this._agentName = name));
      }
    });

    this.consumeContext(UMB_COPILOT_RUN_CONTEXT, (runController) => {
      this.#runController = runController;
      if (runController) {
        this.#observeRunController(runController);
      }
    });
  }

  #observeRunController(runController: CopilotRunController) {
    this.observe(runController.messages$, (messages) => {
      this._messages = messages;
      this.#scrollToBottom();
    });
    this.observe(runController.streamingContent$, (content) => {
      this._streamingContent = content;
      this.#scrollToBottom();
    });
    this.observe(runController.agentState$, (state) => {
      this._agentState = state;
    });
    this.observe(runController.interrupt$, (interrupt) => {
      this._activeInterrupt = interrupt;
      if (interrupt) {
        this._isLoading = false;
      }
    });
    this.observe(runController.runStatus$, (status) => {
      this._isLoading = status.isRunning;
    });
  }

  #handleSendMessage(e: CustomEvent<string>) {
    const content = e.detail;
    this.#runController?.sendUserMessage(content);
  }

  #handleInterruptResponse(e: CustomEvent<string>) {
    const response = e.detail;
    this.#runController?.respondToInterrupt(response);
  }

  #handleCancel() {
    this.#runController?.abortRun();
  }

  #scrollToBottom() {
    requestAnimationFrame(() => {
      const container = this.#messagesRef.value;
      if (container) {
        container.scrollTop = container.scrollHeight;
      }
    });
  }

  #renderMessages() {
    return html`
      ${repeat(
        this._messages,
        (msg) => msg.id,
        (msg) => html`<uai-copilot-message .message=${msg}></uai-copilot-message>`
      )}
      ${this._streamingContent
        ? html`
            <uai-copilot-message
              .message=${{
                id: "streaming",
                role: "assistant",
                content: this._streamingContent,
                // Don't render tool calls during streaming - wait for message finalization
                // This prevents tool-renderer being recreated when message is finalized
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
          ${ref(this.#messagesRef)}
        >
          ${this._messages.length === 0 && !this._streamingContent
            ? html`
                <div class="empty-state">
                  <uui-icon name="icon-chat"></uui-icon>
                  <p>Start a conversation with ${this._agentName || "the agent"}</p>
                </div>
              `
            : this.#renderMessages()}
        </div>

        ${this._agentState?.status && this._agentState.status !== "idle"
          ? html`<uai-copilot-agent-status
              .state=${this._agentState}
              @cancel=${this.#handleCancel}
            ></uai-copilot-agent-status>`
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
      box-sizing: border-box;
    }

    .empty-state uui-icon {
      font-size: 48px;
      margin-bottom: var(--uui-size-space-4);
      opacity: 0.5;
    }

    .empty-state p {
      margin: 0;
      font-size: var(--uui-type-default-size);
      color: var(--uui-color-text-alt);
    }
  `;
}

export default UaiCopilotChatElement;

declare global {
  interface HTMLElementTagNameMap {
    "uai-copilot-chat": UaiCopilotChatElement;
  }
}
