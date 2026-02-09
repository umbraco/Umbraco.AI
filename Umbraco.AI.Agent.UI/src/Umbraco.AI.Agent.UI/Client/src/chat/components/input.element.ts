import { customElement, property, state, css, html, ref, createRef, nothing } from "@umbraco-cms/backoffice/external/lit";
import type { PropertyValues } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UAI_CHAT_CONTEXT, type UaiChatContextApi } from "../context.js";
import type { UaiAgentItem } from "../types/index.js";

/**
 * Chat input component.
 * Provides a text input with send button, agent selector, and keyboard support.
 * Consumes UAI_CHAT_CONTEXT for agent data.
 *
 * @fires send - Dispatched when user sends a message
 */
@customElement("uai-chat-input")
export class UaiChatInputElement extends UmbLitElement {
    @property({ type: Boolean })
    disabled = false;

    @property({ type: String })
    placeholder = "Type a message...";

    @state()
    private _value = "";

    @state()
    private _agents: UaiAgentItem[] = [];

    @state()
    private _selectedAgentId = "";

    #chatContext?: UaiChatContextApi;
    #textareaRef = createRef<HTMLElement>();

    get #isDisabled(): boolean {
        return this.disabled || this._agents.length === 0;
    }

    constructor() {
        super();
        this.consumeContext(UAI_CHAT_CONTEXT, (context) => {
            if (context) {
                this.#chatContext = context;
                this.observe(context.agents, (agents) => (this._agents = agents));
                this.observe(context.selectedAgent, (agent) => (this._selectedAgentId = agent?.id ?? ""));
            }
        });
    }

    override updated(changedProperties: PropertyValues) {
        super.updated(changedProperties);

        if (changedProperties.has("disabled") && !this.disabled) {
            requestAnimationFrame(() => {
                this.#textareaRef.value?.focus();
            });
        }
    }

    #handleAgentChange(e: Event) {
        const select = e.target as HTMLSelectElement;
        this.#chatContext?.selectAgent(select.value);
    }

    #getAgentOptions(): Array<{ name: string; value: string; selected?: boolean }> {
        return this._agents.map((agent) => ({
            name: agent.name,
            value: agent.id,
            selected: agent.id === this._selectedAgentId,
        }));
    }

    #handleKeydown(e: KeyboardEvent) {
        if (e.key === "Enter" && !e.shiftKey) {
            e.preventDefault();
            this.#send();
        }
    }

    #handleInput(e: Event) {
        this._value = (e.target as HTMLTextAreaElement).value;
    }

    #send() {
        if (!this._value.trim() || this.disabled) return;

        this.dispatchEvent(
            new CustomEvent("send", {
                detail: this._value,
                bubbles: true,
                composed: true,
            }),
        );

        this._value = "";
    }

    override render() {
        const hasNoAgents = this._agents.length === 0;

        return html`
            <div class="input-wrapper">
                <div class="input-box">
                    <uui-textarea
                        ${ref(this.#textareaRef)}
                        .value=${this._value}
                        placeholder=${hasNoAgents ? "No agents available" : this.placeholder}
                        ?disabled=${this.#isDisabled}
                        auto-height
                        @input=${this.#handleInput}
                        @keydown=${this.#handleKeydown}
                    ></uui-textarea>
                    <hr class="divider" />
                    <div class="actions-row">
                        <div class="left-actions">
                            ${hasNoAgents
                                ? nothing
                                : html`
                                      <uui-select
                                          class="agent-select"
                                          .value=${this._selectedAgentId}
                                          .options=${this.#getAgentOptions()}
                                          @change=${this.#handleAgentChange}
                                      ></uui-select>
                                  `}
                        </div>
                        <uui-button
                            look="primary"
                            compact
                            ?disabled=${this.#isDisabled || !this._value.trim()}
                            @click=${this.#send}
                        >
                            <uui-icon name="icon-navigation-right"></uui-icon>
                        </uui-button>
                    </div>
                </div>
            </div>
        `;
    }

    static override styles = css`
        :host {
            display: block;
        }

        .input-wrapper {
            padding: var(--uui-size-space-4);
        }

        .input-box {
            display: flex;
            flex-direction: column;
            gap: var(--uui-size-space-2);
            padding: var(--uui-size-space-3);
            background: var(--uui-color-surface);
            border: 1px solid var(--uui-color-border);
            border-radius: var(--uui-border-radius);
        }

        uui-textarea {
            --uui-textarea-min-height: 24px;
            --uui-textarea-max-height: 200px;
            --uui-textarea-background-color: transparent;
            --uui-textarea-border-color: transparent;
        }

        uui-textarea:focus-within {
            --uui-textarea-border-color: transparent;
        }

        .divider {
            border: none;
            border-top: 1px solid var(--uui-color-border);
            margin: 0;
        }

        .actions-row {
            display: flex;
            justify-content: space-between;
            align-items: center;
        }

        .left-actions {
            display: flex;
            gap: var(--uui-size-space-2);
        }

        .agent-select {
            min-width: 120px;
            max-width: 180px;
        }
    `;
}

export default UaiChatInputElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-chat-input": UaiChatInputElement;
    }
}
