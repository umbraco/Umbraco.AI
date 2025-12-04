import { html, css, customElement, state, nothing } from '@umbraco-cms/backoffice/external/lit';
import { UmbModalBaseElement } from '@umbraco-cms/backoffice/modal';
import { UMB_NOTIFICATION_CONTEXT } from '@umbraco-cms/backoffice/notification';
import { UaiChatController, type UaiChatMessage } from '@umbraco-ai/core';
import type { UaiPromptPreviewModalData, UaiPromptPreviewModalValue } from './types.js';

/**
 * Modal element for previewing prompt content with insert/copy options.
 * Automatically generates an AI response when opened.
 */
@customElement('uai-prompt-preview-modal')
export class UaiPromptPreviewModalElement extends UmbModalBaseElement<
    UaiPromptPreviewModalData,
    UaiPromptPreviewModalValue
> {
    #chatController = new UaiChatController(this);
    #abortController?: AbortController;

    @state()
    private _copied = false;

    @state()
    private _loading = false;

    @state()
    private _response = '';

    @state()
    private _error?: string;

    override connectedCallback() {
        super.connectedCallback();
        this.#generateResponse();
    }

    override disconnectedCallback() {
        super.disconnectedCallback();
        this.#abortController?.abort();
    }

    async #generateResponse() {
        if (!this.data?.promptContent) return;

        this._loading = true;
        this._error = undefined;
        this._response = '';

        this.#abortController = new AbortController();

        const messages: UaiChatMessage[] = [
            { role: 'user', content: this.data.promptContent }
        ];

        try {
            const { data, error } = await this.#chatController.complete(messages, {
                profileIdOrAlias: this.data.promptProfileId ?? undefined,
                signal: this.#abortController.signal,
            });

            if (error) {
                this._error = error instanceof Error ? error.message : 'Failed to generate response';
            } else if (data) {
                this._response = data.message.content;
            }
        } catch (err) {
            if ((err as Error)?.name !== 'AbortError') {
                this._error = err instanceof Error ? err.message : 'Failed to generate response';
            }
        } finally {
            this._loading = false;
        }
    }

    #onRetry() {
        this.#generateResponse();
    }

    async #onInsert() {
        this.updateValue({ action: 'insert', content: this._response });
        this._submitModal();
    }

    async #onCopy() {
        if (!this._response) return;

        try {
            await navigator.clipboard.writeText(this._response);
            this._copied = true;

            // Show notification
            const notificationContext = await this.getContext(UMB_NOTIFICATION_CONTEXT);
            notificationContext?.peek('positive', {
                data: { message: 'Response copied to clipboard' },
            });

            // Reset copied state after 2 seconds
            setTimeout(() => {
                this._copied = false;
            }, 2000);
        } catch {
            const notificationContext = await this.getContext(UMB_NOTIFICATION_CONTEXT);
            notificationContext?.peek('danger', {
                data: { message: 'Failed to copy to clipboard' },
            });
        }
    }

    #onCancel() {
        this.updateValue({ action: 'cancel' });
        this._rejectModal();
    }

    #renderResponse() {
        if (this._error) {
            return html`
                <div class="error-container">
                    <uui-icon name="icon-alert"></uui-icon>
                    <span>${this._error}</span>
                    <uui-button
                        label="Retry"
                        look="secondary"
                        compact
                        @click=${this.#onRetry}>
                        Retry
                    </uui-button>
                </div>
            `;
        }

        if (this._loading) {
            return html`
                <div class="loading-container">
                    <uui-loader-bar></uui-loader-bar>
                    <span>Generating response...</span>
                </div>
            `;
        }

        if (this._response) {
            return html`
                <uui-scroll-container class="response-container">
                    <pre class="response-content">${this._response}</pre>
                </uui-scroll-container>
            `;
        }

        return nothing;
    }

    override render() {
        return html`
            <umb-body-layout headline=${this.data?.promptName ?? 'Prompt Preview'}>
                <div id="content">
                    ${this.data?.promptDescription
                        ? html`<p class="description">${this.data.promptDescription}</p>`
                        : nothing}

                    <details class="prompt-details">
                        <summary>Prompt</summary>
                        <pre class="prompt-content">${this.data?.promptContent}</pre>
                    </details>

                    <div class="response-section">
                        <h4>Response</h4>
                        ${this.#renderResponse()}
                    </div>
                </div>

                <div slot="actions">
                    <uui-button
                        label="Cancel"
                        @click=${this.#onCancel}>
                        Cancel
                    </uui-button>
                    <uui-button
                        label=${this._copied ? 'Copied!' : 'Copy Response'}
                        look="secondary"
                        ?disabled=${!this._response || this._loading}
                        @click=${this.#onCopy}>
                        <uui-icon name=${this._copied ? 'icon-check' : 'icon-clipboard'}></uui-icon>
                        ${this._copied ? 'Copied!' : 'Copy Response'}
                    </uui-button>
                    <uui-button
                        label="Insert Response"
                        look="primary"
                        ?disabled=${!this._response || this._loading}
                        @click=${this.#onInsert}>
                        <uui-icon name="icon-enter"></uui-icon>
                        Insert Response
                    </uui-button>
                </div>
            </umb-body-layout>
        `;
    }

    static override styles = [
        css`
            #content {
                display: flex;
                flex-direction: column;
                gap: var(--uui-size-space-4);
                height: 100%;
                width: 500px;
            }

            .description {
                margin: 0;
                color: var(--uui-color-text-alt);
                font-style: italic;
            }

            .prompt-details {
                border: 1px solid var(--uui-color-border);
                border-radius: var(--uui-border-radius);
                background: var(--uui-color-surface-alt);
            }

            .prompt-details summary {
                padding: var(--uui-size-space-3) var(--uui-size-space-4);
                cursor: pointer;
                font-weight: 600;
                user-select: none;
            }

            .prompt-details summary:hover {
                background: var(--uui-color-surface-emphasis);
            }

            .prompt-content {
                margin: 0;
                padding: var(--uui-size-space-4);
                padding-top: 0;
                white-space: pre-wrap;
                word-break: break-word;
                font-family: var(--uui-font-family-monospace);
                font-size: var(--uui-type-small-size);
                line-height: 1.5;
                max-height: 150px;
                overflow-y: auto;
            }

            .response-section {
                flex: 1;
                display: flex;
                flex-direction: column;
                min-height: 0;
            }

            .response-section h4 {
                margin: 0 0 var(--uui-size-space-2) 0;
                font-size: var(--uui-type-default-size);
            }

            .response-container {
                flex: 1;
                min-height: 150px;
                max-height: 300px;
                border: 1px solid var(--uui-color-border);
                border-radius: var(--uui-border-radius);
                background: var(--uui-color-surface);
            }

            .response-content {
                margin: 0;
                padding: var(--uui-size-space-4);
                white-space: pre-wrap;
                word-break: break-word;
                font-family: var(--uui-font-family-monospace);
                font-size: var(--uui-type-small-size);
                line-height: 1.5;
            }

            .loading-container {
                display: flex;
                flex-direction: column;
                align-items: center;
                justify-content: center;
                gap: var(--uui-size-space-3);
                padding: var(--uui-size-space-6);
                border: 1px solid var(--uui-color-border);
                border-radius: var(--uui-border-radius);
                background: var(--uui-color-surface);
                color: var(--uui-color-text-alt);
            }

            .loading-container uui-loader-bar {
                width: 100%;
                max-width: 200px;
            }

            .error-container {
                display: flex;
                align-items: center;
                gap: var(--uui-size-space-3);
                padding: var(--uui-size-space-4);
                border: 1px solid var(--uui-color-danger);
                border-radius: var(--uui-border-radius);
                background: var(--uui-color-danger-emphasis);
                color: var(--uui-color-danger-standalone);
            }

            .error-container uui-icon {
                flex-shrink: 0;
            }

            .error-container span {
                flex: 1;
            }

            [slot='actions'] {
                display: flex;
                gap: var(--uui-size-space-2);
            }

            uui-button uui-icon {
                margin-right: var(--uui-size-space-1);
            }
        `,
    ];
}

export default UaiPromptPreviewModalElement;

declare global {
    interface HTMLElementTagNameMap {
        'uai-prompt-preview-modal': UaiPromptPreviewModalElement;
    }
}
