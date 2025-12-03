import { html, css, customElement, state } from '@umbraco-cms/backoffice/external/lit';
import { UmbModalBaseElement } from '@umbraco-cms/backoffice/modal';
import { UMB_NOTIFICATION_CONTEXT } from '@umbraco-cms/backoffice/notification';
import type { UaiPromptPreviewModalData, UaiPromptPreviewModalValue } from './types.js';

/**
 * Modal element for previewing prompt content with insert/copy options.
 */
@customElement('uai-prompt-preview-modal')
export class UaiPromptPreviewModalElement extends UmbModalBaseElement<
    UaiPromptPreviewModalData,
    UaiPromptPreviewModalValue
> {
    @state()
    private _copied = false;

    async #onInsert() {
        this.updateValue({ action: 'insert', content: this.data?.promptContent });
        this._submitModal();
    }

    async #onCopy() {
        if (!this.data?.promptContent) return;

        try {
            await navigator.clipboard.writeText(this.data.promptContent);
            this._copied = true;

            // Show notification
            const notificationContext = await this.getContext(UMB_NOTIFICATION_CONTEXT);
            notificationContext?.peek('positive', {
                data: { message: 'Prompt copied to clipboard' },
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

    override render() {
        return html`
            <umb-body-layout headline=${this.data?.promptName ?? 'Prompt Preview'}>
                <div id="content">
                    ${this.data?.promptDescription
                        ? html`<p class="description">${this.data.promptDescription}</p>`
                        : ''}
                    <uui-scroll-container>
                        <pre class="prompt-content">${this.data?.promptContent}</pre>
                    </uui-scroll-container>
                </div>

                <div slot="actions">
                    <uui-button
                        label="Cancel"
                        @click=${this.#onCancel}>
                    </uui-button>
                    <uui-button
                        label=${this._copied ? 'Copied!' : 'Copy'}
                        look="secondary"
                        @click=${this.#onCopy}>
                        <uui-icon name=${this._copied ? 'icon-check' : 'icon-clipboard'}></uui-icon>
                        ${this._copied ? 'Copied!' : 'Copy'}
                    </uui-button>
                    <uui-button
                        label="Insert"
                        look="primary"
                        @click=${this.#onInsert}>
                        <uui-icon name="icon-enter"></uui-icon>
                        Insert
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
            }

            .description {
                margin: 0;
                color: var(--uui-color-text-alt);
                font-style: italic;
            }

            uui-scroll-container {
                flex: 1;
                min-height: 200px;
                max-height: 400px;
                border: 1px solid var(--uui-color-border);
                border-radius: var(--uui-border-radius);
                background: var(--uui-color-surface-alt);
            }

            .prompt-content {
                margin: 0;
                padding: var(--uui-size-space-4);
                white-space: pre-wrap;
                word-break: break-word;
                font-family: var(--uui-font-family-monospace);
                font-size: var(--uui-type-small-size);
                line-height: 1.5;
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
