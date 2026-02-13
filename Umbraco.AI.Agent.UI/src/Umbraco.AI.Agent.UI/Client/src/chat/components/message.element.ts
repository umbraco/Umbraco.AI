import { customElement, property, css, html } from "@umbraco-cms/backoffice/external/lit";
import { unsafeHTML, repeat } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { marked } from "@umbraco-cms/backoffice/external/marked";
import type { UaiChatMessage } from "../types/index.js";

/**
 * Chat message component.
 * Renders a single message with markdown support and embedded tool status.
 */
@customElement("uai-chat-message")
export class UaiChatMessageElement extends UmbLitElement {
    @property({ type: Object })
    message!: UaiChatMessage;

    @property({ type: Boolean, attribute: "is-last-assistant-message" })
    isLastAssistantMessage = false;

    @property({ type: Boolean, attribute: "is-running" })
    isRunning = false;

    #renderAgentAttribution() {
        if (this.message.role !== "assistant" || !this.message.agentName) {
            return html``;
        }

        return html`<div class="agent-attribution">
            <uui-icon name="icon-bot"></uui-icon>
            ${this.message.agentName}
        </div>`;
    }

    #renderContent() {
        if (!this.message.content) {
            return html``;
        }

        const isUser = this.message.role === "user";

        if (isUser) {
            return html`<p>${this.message.content}</p>`;
        }

        const htmlContent = marked.parse(this.message.content) as string;
        return html`<div class="markdown-content">${unsafeHTML(htmlContent)}</div>`;
    }

    #renderToolCalls() {
        if (!this.message.toolCalls?.length) {
            return html``;
        }

        return html`
            <div class="tool-calls">
                ${repeat(
                    this.message.toolCalls,
                    (tc) => tc.id,
                    (tc) => html`<uai-tool-renderer .toolCall=${tc}></uai-tool-renderer>`,
                )}
            </div>
        `;
    }

    #renderActions() {
        if (this.message.role !== "assistant" || !this.message.content?.trim()) {
            return html``;
        }

        const isHidden = this.isRunning && this.isLastAssistantMessage;
        const visibilityClass = isHidden ? "hidden" : this.isLastAssistantMessage ? "always-visible" : "";

        return html`
            <div class="message-actions ${visibilityClass}">
                ${this.isLastAssistantMessage
                    ? html`<uai-message-regenerate-button></uai-message-regenerate-button>`
                    : ""}
                <uai-message-copy-button .content=${this.message.content}></uai-message-copy-button>
            </div>
        `;
    }

    override render() {
        if (this.message.role === "tool") {
            return html``;
        }

        return html`
            <div class="message ${this.message.role}">
                <div class="message-content">
                    ${this.#renderAgentAttribution()} ${this.#renderContent()} ${this.#renderToolCalls()} ${this.#renderActions()}
                </div>
            </div>
        `;
    }

    static override styles = css`
        :host {
            display: block;
        }

        .message {
            display: flex;
            gap: var(--uui-size-space-3);
            padding: var(--uui-size-space-3);
            padding-bottom: 0;
        }

        .message.user {
            flex-direction: row-reverse;
        }

        .message-content {
            flex: 1;
            max-width: 90%;
            overflow-wrap: break-word;
        }

        .message.user .message-content {
            text-align: right;
        }

        .message.user .message-content p {
            background: var(--uui-color-selected);
            color: var(--uui-color-selected-contrast);
            padding: var(--uui-size-space-2) var(--uui-size-space-3);
            border-radius: var(--uui-border-radius);
            display: inline-block;
            margin: 0;
        }

        .markdown-content {
            background: var(--uui-color-surface-alt);
            padding: var(--uui-size-space-3);
            border-radius: var(--uui-border-radius);
        }

        .markdown-content :first-child {
            margin-top: 0;
        }

        .markdown-content :last-child {
            margin-bottom: 0;
        }

        .markdown-content pre {
            background: var(--uui-color-surface-emphasis);
            padding: var(--uui-size-space-3);
            border-radius: var(--uui-border-radius);
            overflow-x: auto;
        }

        .markdown-content code {
            font-family: var(--uui-font-monospace);
            font-size: 0.9em;
        }

        .markdown-content p code {
            background: var(--uui-color-surface-emphasis);
            padding: 2px 6px;
            border-radius: 3px;
        }

        .markdown-content ul,
        .markdown-content ol {
            padding-left: var(--uui-size-space-6);
            margin-top: var(--uui-size-space-2);
            margin-bottom: var(--uui-size-space-2);
        }

        .markdown-content h2,
        .markdown-content h3,
        .markdown-content h4 {
            margin-top: var(--uui-size-space-4);
            margin-bottom: var(--uui-size-space-2);
            font-size: 1.2em;
        }

        .tool-calls {
            margin-top: var(--uui-size-space-2);
            display: flex;
            flex-wrap: wrap;
            gap: var(--uui-size-space-2);
        }

        .message-actions {
            display: flex;
            gap: var(--uui-size-space-1);
            margin-top: var(--uui-size-space-1);
            opacity: 0;
            transition: opacity 0.15s ease;
        }

        .message:hover .message-actions,
        .message-actions.always-visible {
            opacity: 1;
        }

        .message-actions.hidden {
            visibility: hidden;
        }

        .agent-attribution {
            font-size: 0.75rem;
            color: var(--uui-color-text-alt);
            margin-bottom: var(--uui-size-space-1);
            display: block;
            opacity: 0.8;
        }
    `;
}

export default UaiChatMessageElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-chat-message": UaiChatMessageElement;
    }
}
