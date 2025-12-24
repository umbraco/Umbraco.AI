import { customElement, property, css, html } from "@umbraco-cms/backoffice/external/lit";
import { unsafeHTML, repeat } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { marked } from "@umbraco-cms/backoffice/external/marked";
import type { ChatMessage } from "../../../core/models/chat.types.js";

// Import the tool renderer element (handles extension lookup and custom UI)
import "./tool-renderer.element.js";

/**
 * Chat message component.
 * Renders a single message with markdown support and embedded tool status.
 */
@customElement("uai-copilot-message")
export class UaiCopilotMessageElement extends UmbLitElement {
  @property({ type: Object })
  message!: ChatMessage;

  #renderContent() {
    if (!this.message.content) {
      return html``;
    }

    const isUser = this.message.role === "user";

    if (isUser) {
      return html`<p>${this.message.content}</p>`;
    }

    // Parse markdown for assistant messages
    const htmlContent = marked.parse(this.message.content) as string;
    return html`<div class="markdown-content">${unsafeHTML(htmlContent)}</div>`;
  }

  #renderToolCalls() {
    if (!this.message.toolCalls?.length) {
      return html``;
    }

    // Use repeat with key to ensure element identity across re-renders
    // This prevents losing approval state when the message re-renders
    return html`
      <div class="tool-calls">
        ${repeat(
          this.message.toolCalls,
          (tc) => tc.id,
          (tc) => html`<uai-tool-renderer .toolCall=${tc}></uai-tool-renderer>`
        )}
      </div>
    `;
  }

  override render() {
    // Hide tool result messages - they're internal to the conversation
    // Users only see the assistant's response that uses the tool result
    if (this.message.role === "tool") {
      return html``;
    }

    const isUser = this.message.role === "user";
    const iconName = isUser ? "icon-user" : "icon-wand";

    return html`
      <div class="message ${this.message.role}">
        <div class="message-avatar">
          <uui-icon name=${iconName}></uui-icon>
        </div>
        <div class="message-content">
          ${this.#renderContent()}
          ${this.#renderToolCalls()}
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
    }

    .message.user {
      flex-direction: row-reverse;
    }

    .message-avatar {
      flex-shrink: 0;
      width: 32px;
      height: 32px;
      display: flex;
      align-items: center;
      justify-content: center;
      border-radius: 50%;
      background: var(--uui-color-surface-alt);
    }

    .message.user .message-avatar {
      background: var(--uui-color-selected);
      color: var(--uui-color-selected-contrast);
    }

    .message.assistant .message-avatar {
      background: var(--uui-color-positive);
      color: var(--uui-color-positive-contrast);
    }

    .message-content {
      flex: 1;
      max-width: 80%;
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

    .tool-calls {
      margin-top: var(--uui-size-space-2);
      display: flex;
      flex-wrap: wrap;
      gap: var(--uui-size-space-2);
    }
  `;
}

export default UaiCopilotMessageElement;

declare global {
  interface HTMLElementTagNameMap {
    "uai-copilot-message": UaiCopilotMessageElement;
  }
}
