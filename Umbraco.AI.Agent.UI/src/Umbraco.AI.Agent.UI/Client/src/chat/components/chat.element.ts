import { customElement, property, state, css, html, nothing, repeat, ref, createRef } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import type { UaiChatMessage, UaiAgentState } from "../types/index.js";
import { UAI_CHAT_CONTEXT, type UaiChatContextApi } from "../context.js";
import type { PendingApproval } from "../services/hitl.context.js";

/**
 * Main chat component.
 * Renders observables from the shared chat context and forwards user input.
 * Consumes UAI_CHAT_CONTEXT -- works in any surface (copilot, chat, etc.).
 */
@customElement("uai-chat")
export class UaiChatElement extends UmbLitElement {
    /**
     * Renders the conversation as read-only: the composer, agent-status and per-message regenerate are
     * suppressed and an optional {@link readonlyNotice} is shown in the composer's place. Default false,
     * so surfaces that don't opt in (e.g. the contextual Copilot) are completely unaffected.
     */
    @property({ type: Boolean, reflect: true })
    readonly = false;

    /** Optional message shown in place of the composer when {@link readonly}. */
    @property({ type: String, attribute: "readonly-notice" })
    readonlyNotice = "";

    /**
     * Whether the surface has resolved which input mode to show. Defaults true so surfaces that never
     * set it (e.g. the contextual Copilot) are unaffected; a host that resolves its mode asynchronously
     * (e.g. the workspace, which must fetch a conversation to learn if it is archived) sets this false
     * until known, so neither the composer nor the read-only notice flashes prematurely.
     */
    @property({ type: Boolean })
    ready = true;

    /** Placeholder for the message input. Falls back to the input's own default when unset. */
    @property({ type: String })
    placeholder?: string;

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

    #chatContext?: UaiChatContextApi;
    #messagesRef = createRef<HTMLElement>();

    constructor() {
        super();
        this.consumeContext(UAI_CHAT_CONTEXT, (context) => {
            if (!context) return;
            this.#chatContext = context;

            this.observe(context.selectedAgent, (agent) => (this._agentName = agent?.name ?? ""));

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

            this.observe(context.pendingApproval$, (approval) => {
                this._pendingApproval = approval;
                if (approval) {
                    this._isRunning = false;
                }
            });
        });
    }

    #handleSendMessage(e: CustomEvent<{ text: string; contentParts?: import("../types/index.js").UaiInputContent[] }>) {
        const { text, contentParts } = e.detail;
        this.#chatContext?.sendUserMessage(text, contentParts);
    }

    #handleInterruptResponse(e: CustomEvent<string>) {
        const response = e.detail;
        this.#chatContext?.respondToHitl(response);
    }

    #handleCancel() {
        this.#chatContext?.abortRun();
    }

    #handleRegenerate() {
        this.#chatContext?.regenerateLastMessage();
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
                    <uai-chat-message
                        .message=${msg}
                        ?is-last-assistant-message=${msg.id === lastAssistantId}
                        ?is-running=${this._isRunning}
                        ?readonly=${this.readonly}
                        @regenerate=${this.#handleRegenerate}
                    ></uai-chat-message>
                    ${this.#renderInlineHitl(msg.id)}
                `,
            )}
        `;
    }

    #renderInlineHitl(messageId: string) {
        const shouldRender =
            this._pendingApproval &&
            (this._pendingApproval.targetMessageId === messageId ||
                (!this._pendingApproval.targetMessageId && messageId === this.#getLastAssistantMessageId()));

        if (!shouldRender) return html``;

        return html`
            <uai-hitl-approval
                .interrupt=${this._pendingApproval!.interrupt}
                @respond=${this.#handleInterruptResponse}
            ></uai-hitl-approval>
        `;
    }

    #renderComposer() {
        return html`
            ${this._agentState?.status && this._agentState.status !== "idle"
                ? html`<uai-agent-status .state=${this._agentState} @cancel=${this.#handleCancel}></uai-agent-status>`
                : ""}
            <uai-chat-input
                ?disabled=${this._isRunning || !!this._pendingApproval}
                placeholder=${this.placeholder ?? "Type a message..."}
                @send=${this.#handleSendMessage}
            ></uai-chat-input>
        `;
    }

    #renderReadonlyNotice() {
        if (!this.readonlyNotice) return nothing;
        return html`<div class="readonly-notice"><uui-icon name="icon-lock"></uui-icon>${this.readonlyNotice}</div>`;
    }

    override render() {
        return html`
            <div class="chat-container">
                <div class="messages-area" ${ref(this.#messagesRef)}>
                    <div class="content-column">
                        ${this._messages.length === 0
                            ? html`
                                  <div class="empty-state">
                                      <slot name="empty-state-message">
                                          <uui-icon name="icon-chat"></uui-icon>
                                          <p>Start a conversation with ${this._agentName || "an agent"}</p>
                                      </slot>
                                  </div>
                              `
                            : this.#renderMessages()}
                    </div>
                </div>

                <div class="content-column composer">
                    ${!this.ready ? nothing : this.readonly ? this.#renderReadonlyNotice() : this.#renderComposer()}
                </div>
            </div>
        `;
    }

    /** Focuses the message composer. Delegates to the chat input; safe to call before it renders. */
    focusComposer(): void {
        const input = this.shadowRoot?.querySelector("uai-chat-input") as
            | (HTMLElement & { focusComposer?: () => void })
            | null;
        input?.focusComposer?.();
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

        /*
         * Centres the message list and composer to an optional comfortable reading width while the
         * scroll container (.messages-area) stays full width — so the scrollbar sits at the far edge.
         * Defaults to no cap (full width), so surfaces that don't opt in (e.g. the Copilot sidebar)
         * are unchanged; set --uai-chat-content-max-width to enable it.
         */
        .content-column {
            width: 100%;
            max-width: var(--uai-chat-content-max-width, none);
            margin-inline: auto;
            box-sizing: border-box;
        }

        /* Let the column fill the scroll area's height so the empty state can centre vertically. */
        .messages-area > .content-column {
            display: flex;
            flex-direction: column;
            min-height: 100%;
        }

        .empty-state {
            display: flex;
            flex: 1;
            flex-direction: column;
            align-items: center;
            justify-content: center;
            color: var(--uui-color-text-alt);
            text-align: center;
            padding: var(--uui-size-space-5);
            box-sizing: border-box;
        }

        /* Let projected empty-state content (and the default fallback) participate directly in the
           centered flex column above, rather than nesting inside an inline slot box. */
        .empty-state slot {
            display: contents;
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

        .readonly-notice {
            display: flex;
            align-items: center;
            justify-content: center;
            gap: var(--uui-size-space-2);
            padding: var(--uui-size-space-4);
            margin: var(--uui-size-space-4);
            border: 1px dashed var(--uui-color-border);
            border-radius: var(--uui-border-radius);
            color: var(--uui-color-text-alt);
            font-size: var(--uui-type-small-size);
            text-align: center;
        }
    `;
}

export default UaiChatElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-chat": UaiChatElement;
    }
}
