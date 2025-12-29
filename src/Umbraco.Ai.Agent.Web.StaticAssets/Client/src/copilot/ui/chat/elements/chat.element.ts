import { customElement, state, css, html, repeat, ref, createRef } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import type { ChatMessage, AgentState, InterruptInfo } from "../../../core/types.js";
import { UAI_COPILOT_CONTEXT, UAI_COPILOT_RUN_CONTEXT, type UaiCopilotContext } from "../../../core/copilot.context.js";
import type { UaiCopilotRunController } from "../../../core/controllers/copilot-run.controller.js";

import "./message.element.js";
import "./input.element.js";
import "./agent-status.element.js";
import "./interrupt.element.js";
import "./tool-renderer.element.js";

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
  private _hitlInterrupt?: InterruptInfo;

  @state()
  private _isRunning = false;

  #copilotContext?: UaiCopilotContext;
  #runController?: UaiCopilotRunController;
  #messagesRef = createRef<HTMLElement>();

  constructor() {
    super();
    this.consumeContext(UAI_COPILOT_CONTEXT, (context) => {
      if (!context) return;
      this.#copilotContext = context;
      this.observe(context.agentName, (name) => (this._agentName = name));
      this.observe(context.hitlInterrupt$, (interrupt) => {
        this._hitlInterrupt = interrupt;
        if (interrupt) {
          this._isRunning = false;
        }
      });
    });

    this.consumeContext(UAI_COPILOT_RUN_CONTEXT, (runController) => {
      this.#runController = runController;
      if (runController) {
        this.#observeRunController(runController);
      }
    });
  }

  #observeRunController(runController: UaiCopilotRunController) {
    this.observe(runController.messages$, (messages) => {
      this._messages = messages;
      this.#scrollToBottom();
    });
    this.observe(runController.agentState$, (state) => {
      this._agentState = state;
    });
    this.observe(runController.isRunning$, (isRunning) => {
      this._isRunning = isRunning;
    });
  }

  #handleSendMessage(e: CustomEvent<string>) {
    const content = e.detail;
    this.#runController?.sendUserMessage(content);
  }

  #handleInterruptResponse(e: CustomEvent<string>) {
    const response = e.detail;
    this.#copilotContext?.respondToHitl(response);
  }

  #handleCancel() {
    this.#runController?.abortRun();
  }

  #handleRegenerate() {
    this.#runController?.regenerateLastMessage();
  }

  #getLastAssistantMessageId(): string | undefined {
    for (let i = this._messages.length - 1; i >= 0; i--) {
      if (this._messages[i].role === "assistant") {
        return this._messages[i].id;
      }
    }
    return undefined;
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
    const lastAssistantId = this.#getLastAssistantMessageId();

    return html`
      ${repeat(
        this._messages,
        (msg) => msg.id,
        (msg) => html`
          <uai-copilot-message
            .message=${msg}
            ?is-last-assistant-message=${msg.id === lastAssistantId}
            ?is-running=${this._isRunning}
            @regenerate=${this.#handleRegenerate}
          ></uai-copilot-message>
        `
      )}
    `;
  }

  override render() {
    return html`
      <div class="chat-container">
        <div
          class="messages-area"
          ${ref(this.#messagesRef)}
        >
          ${this._messages.length === 0
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

        ${this._hitlInterrupt
          ? html`
              <uai-copilot-interrupt
                .interrupt=${this._hitlInterrupt}
                @respond=${this.#handleInterruptResponse}
              ></uai-copilot-interrupt>
            `
          : ""}

        <uai-copilot-input
          ?disabled=${this._isRunning || !!this._hitlInterrupt}
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
      padding: var(--uui-size-space-2);
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
