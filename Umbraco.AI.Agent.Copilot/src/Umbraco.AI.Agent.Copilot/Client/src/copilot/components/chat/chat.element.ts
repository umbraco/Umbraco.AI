import { customElement, state, css, html, repeat, ref, createRef } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import type { UaiChatMessage, UaiAgentState } from "../../types.js";
import { UAI_COPILOT_CONTEXT, type UaiCopilotContext } from "../../copilot.context.js";
import type { PendingApproval } from "../../hitl.context.js";

/**
 * Main chat component.
 * Renders observables from the Copilot run controller and forwards user input.
 */
@customElement("uai-copilot-chat")
export class UaiCopilotChatElement extends UmbLitElement {
  @state()
  private _agentName = "";

  @state()
  private _messages: UaiChatMessage[] = [];

  @state()
  private _agentState?: UaiAgentState;

  @state()
  private _pendingApproval?: PendingApproval;

  @state()
  private _isRunning = false;

  #copilotContext?: UaiCopilotContext;
  #messagesRef = createRef<HTMLElement>();

  constructor() {
    super();
    this.consumeContext(UAI_COPILOT_CONTEXT, (context) => {
      if (!context) return;
      this.#copilotContext = context;

      // Agent info
      this.observe(context.selectedAgent, (agent) => (this._agentName = agent?.name ?? ""));

      // Run state (via passthrough getters)
      this.observe(context.messages$, (messages) => {
        this._messages = messages;
        this.#scrollToBottom();
      });
      this.observe(context.agentState$, (state) => {
        this._agentState = state;
      });
      this.observe(context.isRunning$, (isRunning) => {
        this._isRunning = isRunning;
      });

      // HITL interrupts (with target message for inline rendering)
      this.observe(context.pendingApproval$, (approval) => {
        this._pendingApproval = approval;
        if (approval) {
          this._isRunning = false;
        }
      });
    });
  }

  #handleSendMessage(e: CustomEvent<string>) {
    const content = e.detail;
    this.#copilotContext?.sendUserMessage(content);
  }

  #handleInterruptResponse(e: CustomEvent<string>) {
    const response = e.detail;
    this.#copilotContext?.respondToHitl(response);
  }

  #handleCancel() {
    this.#copilotContext?.abortRun();
  }

  #handleRegenerate() {
    this.#copilotContext?.regenerateLastMessage();
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
          ${this.#renderInlineHitl(msg.id)}
        `
      )}
    `;
  }

  /**
   * Render HITL approval inline after the target message.
   * Falls back to last assistant message if no target specified.
   */
  #renderInlineHitl(messageId: string) {
    // Render HITL after target message, or after last assistant message if no target
    const shouldRender = this._pendingApproval && (
      this._pendingApproval.targetMessageId === messageId ||
      (!this._pendingApproval.targetMessageId && messageId === this.#getLastAssistantMessageId())
    );

    if (!shouldRender) return html``;

    return html`
      <uai-copilot-hitl-approval
        .interrupt=${this._pendingApproval!.interrupt}
        @respond=${this.#handleInterruptResponse}
      ></uai-copilot-hitl-approval>
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

        <uai-copilot-input
          ?disabled=${this._isRunning || !!this._pendingApproval}
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
