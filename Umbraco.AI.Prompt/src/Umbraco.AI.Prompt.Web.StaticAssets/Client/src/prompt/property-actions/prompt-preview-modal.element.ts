import { html, css, customElement, state, nothing } from '@umbraco-cms/backoffice/external/lit';
import { UmbModalBaseElement } from '@umbraco-cms/backoffice/modal';
import { UMB_NOTIFICATION_CONTEXT } from '@umbraco-cms/backoffice/notification';
import { UaiPromptController } from '../controllers/prompt.controller.js';
import type { UaiPromptPreviewModalData, UaiPromptPreviewModalValue, UaiPromptPropertyChange } from './types.js';

/**
 * Modal element for previewing prompt content with insert/copy options.
 * Automatically generates an AI response when opened.
 */
@customElement('uai-prompt-preview-modal')
export class UaiPromptPreviewModalElement extends UmbModalBaseElement<
    UaiPromptPreviewModalData,
    UaiPromptPreviewModalValue
> {
    #promptController = new UaiPromptController(this);
    #abortController?: AbortController;

    @state()
    private _copied = false;

    @state()
    private _loading = false;

    @state()
    private _response = '';

    @state()
    private _error?: string;

    @state()
    private _propertyChanges?: UaiPromptPropertyChange[];

    @state()
    private _characterCount = 0;

    override connectedCallback() {
        super.connectedCallback();
        this.#generateResponse();
    }

    override disconnectedCallback() {
        super.disconnectedCallback();
        this.#abortController?.abort();
    }

    async #generateResponse() {
        if (!this.data?.promptUnique) return;

        this._loading = true;
        this._error = undefined;
        this._response = '';

        this.#abortController = new AbortController();

        // Call controller with prompt ID and entity context
        // Controller internally calls the server execute endpoint
        const { data, error } = await this.#promptController.execute(
            this.data.promptUnique,
            {
                signal: this.#abortController.signal,
                entityId: this.data.entityId,
                entityType: this.data.entityType,
                propertyAlias: this.data.propertyAlias,
                culture: this.data.culture,
                segment: this.data.segment,
                // Pass serialized entity context for AI processing
                context: this.data.context,
            }
        );

        if (error) {
            if (error.name !== 'AbortError') {
                this._error = error.message;
            }
        } else if (data) {
            this._response = data.content;
            this._characterCount = data.content.length;
            this._propertyChanges = data.propertyChanges;
        }

        this._loading = false;
    }

    #onRetry() {
        this.#generateResponse();
    }

    async #onInsert() {
        this.updateValue({
            action: 'insert',
            content: this._response,
            propertyChanges: this._propertyChanges,
        });
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

    #renderCharacterIndicator() {
        if (!this._response || this._characterCount === 0) return nothing;

        const maxChars = this.data?.maxChars;

        // If no max limit configured, just show the count
        if (!maxChars) {
            return html`
                <div class="character-indicator neutral">
                    <span class="char-count">${this._characterCount} characters</span>
                </div>
            `;
        }

        // Show count vs max with status
        const isOverLimit = this._characterCount > maxChars;
        const statusClass = isOverLimit ? 'over' : 'ok';
        const statusText = isOverLimit ? 'Exceeds limit' : 'Within limit';

        return html`
            <div class="character-indicator ${statusClass}">
                <span class="char-count">${this._characterCount} / ${maxChars} characters</span>
                <span class="char-status">${statusText}</span>
            </div>
        `;
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
                    <div class="response-content">${this._response}</div>
                </uui-scroll-container>
                ${this.#renderCharacterIndicator()}
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

                    <div class="response-section">
                        <div class="response-header">
                            <h4>
                                <uui-icon name="icon-wand"></uui-icon>
                                AI Response
                            </h4>
                            ${this._response && !this._loading
                                ? html`
                                    <uui-button
                                        label="Regenerate"
                                        look="secondary"
                                        compact
                                        @click=${this.#onRetry}>
                                        <uui-icon name="icon-sync"></uui-icon>
                                        Regenerate
                                    </uui-button>
                                `
                                : nothing}
                        </div>
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
                width: 650px;
                max-width: 100%;
                box-sizing: border-box;
            }

            .description {
                margin: 0;
                color: var(--uui-color-text-alt);
                font-style: italic;
            }

            .response-section {
                flex: 1;
                display: flex;
                flex-direction: column;
                min-height: 0;
            }

            .response-header {
                display: flex;
                align-items: center;
                justify-content: space-between;
                margin-bottom: var(--uui-size-space-3);
            }

            .response-header h4 {
                margin: 0;
                font-size: var(--uui-type-default-size);
                font-weight: 600;
                display: flex;
                align-items: center;
                gap: var(--uui-size-space-2);
                color: var(--uui-color-text);
            }

            .response-header h4 uui-icon {
                color: var(--uui-color-interactive);
            }

            .response-container {
                flex: 1;
                min-height: 200px;
                border: 1px solid var(--uui-color-border);
                border-radius: var(--uui-border-radius);
                background: var(--uui-color-surface-alt);
                overflow: auto;
            }

            .response-content {
                margin: 0;
                padding: var(--uui-size-space-5);
                white-space: pre-wrap;
                word-break: break-word;
                font-family: var(--uui-font-family);
                font-size: var(--uui-type-default-size);
                line-height: 1.6;
                color: var(--uui-color-text);
            }

            .character-indicator {
                display: flex;
                justify-content: space-between;
                padding: var(--uui-size-space-2) var(--uui-size-space-3);
                margin-top: var(--uui-size-space-2);
                border-radius: var(--uui-border-radius);
                font-size: var(--uui-type-small-size);
            }

            .character-indicator.neutral {
                background: color-mix(in srgb, var(--uui-color-text) 10%, transparent);
                color: var(--uui-color-text-alt);
            }

            .character-indicator.ok {
                background: color-mix(in srgb, var(--uui-color-positive) 15%, transparent);
                color: var(--uui-color-positive-standalone);
            }

            .character-indicator.over {
                background: color-mix(in srgb, var(--uui-color-danger) 15%, transparent);
                color: var(--uui-color-danger-standalone);
            }

            .char-count {
                font-weight: 500;
            }

            .char-status {
                font-style: italic;
            }

            .loading-container {
                display: flex;
                flex-direction: column;
                align-items: center;
                justify-content: center;
                gap: var(--uui-size-space-4);
                padding: var(--uui-size-space-8);
                min-height: 200px;
                border: 1px solid var(--uui-color-border);
                border-radius: var(--uui-border-radius);
                background: var(--uui-color-surface-alt);
                color: var(--uui-color-text-alt);
            }

            .loading-container uui-loader-bar {
                width: 100%;
                max-width: 250px;
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
