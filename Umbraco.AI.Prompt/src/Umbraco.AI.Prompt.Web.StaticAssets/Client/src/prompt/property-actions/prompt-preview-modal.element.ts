import { html, css, customElement, state, nothing } from "@umbraco-cms/backoffice/external/lit";
import { UmbModalBaseElement } from "@umbraco-cms/backoffice/modal";
import { UMB_NOTIFICATION_CONTEXT } from "@umbraco-cms/backoffice/notification";
import { UaiPromptController } from "../controllers/prompt.controller.js";
import type {
    UaiPromptPreviewModalData,
    UaiPromptPreviewModalValue,
    UaiPromptResultOption,
} from "./types.js";

/**
 * Modal element for previewing prompt content with insert/copy options.
 * Automatically generates an AI response when opened.
 */
@customElement("uai-prompt-preview-modal")
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
    private _response = "";

    @state()
    private _error?: string;

    @state()
    private _resultOptions?: UaiPromptResultOption[];

    @state()
    private _selectedOptionIndex?: number;

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
        this._response = "";

        this.#abortController = new AbortController();

        // Call controller with prompt ID and entity context
        // Controller internally calls the server execute endpoint
        const { data, error } = await this.#promptController.execute(this.data.promptUnique, {
            signal: this.#abortController.signal,
            entityId: this.data.entityId,
            entityType: this.data.entityType,
            propertyAlias: this.data.propertyAlias,
            culture: this.data.culture,
            segment: this.data.segment,
            // Pass serialized entity context for AI processing
            context: this.data.context,
        });

        if (error) {
            if (error.name !== "AbortError") {
                this._error = error.message;
            }
        } else if (data) {
            this._response = data.content;
            this._resultOptions = data.resultOptions;

            // Auto-select for single option, reset for multiple options
            if (this._resultOptions && this._resultOptions.length > 1) {
                this._selectedOptionIndex = undefined; // Reset selection for multiple options
            } else if (this._resultOptions && this._resultOptions.length === 1) {
                this._selectedOptionIndex = 0; // Auto-select single option
            }
        }

        this._loading = false;
    }

    #onRetry() {
        this.#generateResponse();
    }

    async #onInsert() {
        // Get value changes from selected option (or single option)
        const valueChanges =
            this._resultOptions && this._selectedOptionIndex !== undefined
                ? [this._resultOptions[this._selectedOptionIndex].valueChange].filter(
                      (vc): vc is NonNullable<typeof vc> => vc !== null && vc !== undefined,
                  )
                : [];

        this.updateValue({
            action: "insert",
            content: this._response,
            valueChanges: valueChanges,
        });
        this._submitModal();
    }

    async #copyValueToClipboard(value: string) {
        try {
            await navigator.clipboard.writeText(value);
            const notificationContext = await this.getContext(UMB_NOTIFICATION_CONTEXT);
            notificationContext?.peek("positive", {
                data: { message: "Value copied to clipboard" },
            });
        } catch {
            const notificationContext = await this.getContext(UMB_NOTIFICATION_CONTEXT);
            notificationContext?.peek("danger", {
                data: { message: "Failed to copy to clipboard" },
            });
        }
    }

    #onCancel() {
        this.updateValue({ action: "cancel" });
        this._rejectModal();
    }

    #onOptionSelect(index: number) {
        this._selectedOptionIndex = index;
    }

    #renderCopyButton(value:string) {
        return html`
            <uui-action-bar class="copy-action">
                <uui-button
                    label="Copy"
                    look="secondary"
                    compact
                    @click=${(e: Event) => {
                        e.stopPropagation();
                        this.#copyValueToClipboard(value);
                    }}>
                    <uui-icon name="icon-clipboard-copy"></uui-icon>
                </uui-button>
            </uui-action-bar>
        `;
    }

    #renderMultipleOptions() {
        if (!this._resultOptions?.length) return nothing;

        return html`
            <div class="options-container">
                ${this._resultOptions.map(
                    (option, index) => html`
                        <div
                            class="option-card ${this._selectedOptionIndex === index ? "selected" : ""} copy-container"
                            @click=${() => this.#onOptionSelect(index)}
                        >
                            <umb-icon name="icon-wand color-blue" class="option-icon"></umb-icon>
                            <div class="option-content">
                                <div class="option-value">${option.displayValue}</div>
                                <div class="option-label">
                                    <span>${option.label}</span>
                                    ${option.description
                                        ? html` • ${option.description}`
                                        : nothing}
                                </div>
                                ${this.#renderCopyButton(option.displayValue)}
                            </div>
                        </div>
                    `,
                )}
            </div>
        `;
    }

    #renderSingleOption() {
        return html`
            <div class="response-content copy-container">
                <umb-icon name="icon-wand color-blue" class="option-icon"></umb-icon>
                <div style="white-space: pre-wrap;word-break: break-word;">${this._response}</div>
                ${this.#renderCopyButton(this._response)}
            </div>
        `;
    }



    #renderResponse() {
        if (this._error) {
            return html`
                <div class="error-container">
                    <uui-icon name="icon-alert"></uui-icon>
                    <span>${this._error}</span>
                    <uui-button label="Retry" look="secondary" compact @click=${this.#onRetry}> Retry </uui-button>
                </div>
            `;
        }

        if (this._loading) {
            return html`
                <div class="loading-container">
                    <uui-loader></uui-loader>
                </div>
            `;
        }

        if (this._response) {
            return html`
                <uui-scroll-container class="response-container ${this._resultOptions && this._resultOptions.length > 1 ? "multiple" : ""}">
                    ${this._resultOptions && this._resultOptions.length > 1
                        ? this.#renderMultipleOptions()
                        : this.#renderSingleOption()
                    }
                </uui-scroll-container>
            `;
        }

        return nothing;
    }

    override render() {
        return html`
            <uui-dialog-layout>

                <div class="response-header">
                    <div>
                        <h3 class="headline">${this.data?.promptName ?? "Prompt Preview"}</h3>
                        ${this.data?.promptDescription
                            ? html`<p class="description">${this.data.promptDescription}</p>`
                            : nothing}
                    </div>
                    ${this._response && !this._loading
                        ? html`
                        <uui-action-bar>
                            <uui-button class="regenerate-btn"  label="Regenerate" look="default" compact @click=${this.#onRetry}>
                                <uui-icon name="icon-sync"></uui-icon>
                            </uui-button>
                        </uui-action-bar>
                        ` : nothing}
                </div>


                <div id="content">
                    ${this.#renderResponse()}
                </div>

                <div slot="actions">
                    <uui-button label="Cancel" @click=${this.#onCancel}> Cancel </uui-button>
                    ${this._resultOptions &&
                        this._resultOptions.length > 0 &&
                        this._resultOptions.some((opt) => opt.valueChange !== null && opt.valueChange !== undefined)
                        ? html`
                              <uui-button
                                  label="Insert"
                                  look="primary"
                                  color="positive"
                                  ?disabled=${!this._response ||
                                  this._loading ||
                                  this._selectedOptionIndex === undefined ||
                                  !this._resultOptions[this._selectedOptionIndex]?.valueChange}
                                  @click=${this.#onInsert}
                              >
                                  Insert
                              </uui-button>
                          `
                        : nothing}
                </div>
            </uui-dialog-layout>
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

            .headline {
                margin: 0;
                color: var(--uui-color-text);
                font-weight: 600;
            }

            .description {
                margin: 0;
                opacity: 0.6;
                font-size: var(--uui-type-small-size);
                font-style: italic;
                line-height: 1;
            }

            .response-section {
                flex: 1;
                display: flex;
                flex-direction: column;
                min-height: 0;
            }

            .response-container {
                flex: 1;
                min-height: 100px;
                border: 1px solid var(--uui-color-border);
                border-radius: var(--uui-border-radius);
                overflow: auto;
            }

            .response-container.multiple {
               border: 0;
            }

            .response-header {
                display: flex;
                justify-content: space-between;
                align-items: center;
                margin-bottom: var(--uui-size-space-5);
            }

            .response-content {
                display: flex;
                gap: var(--uui-size-space-3);
                align-items: flex-start;
                margin: 0;
                font-family: var(--uui-font-family);
                font-size: var(--uui-type-default-size);
                line-height: 1.6;
                color: var(--uui-color-text);
                padding: var(--uui-size-space-4);
            }

            .response-container.multiple .response-content {
                padding: 0;
            }

            .loading-container {
                display: flex;
                flex-direction: column;
                align-items: center;
                justify-content: center;
                gap: var(--uui-size-space-4);
                padding: var(--uui-size-space-8);
                min-height: 80px;
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
                color: var(--uui-color-danger-standalone);
            }

            .error-container uui-icon {
                flex-shrink: 0;
            }

            .error-container span {
                flex: 1;
            }

            [slot="actions"] {
                display: flex;
                gap: var(--uui-size-space-2);
            }

            uui-button uui-icon {
                margin-right: var(--uui-size-space-1);
            }

            .options-container {
                display: flex;
                flex-direction: column;
                gap: var(--uui-size-space-3);
            }

            .options-instruction {
                margin: 0 0 var(--uui-size-space-2) 0;
                font-weight: 500;
                color: var(--uui-color-text);
            }

            .option-card {
                display: flex;
                padding: var(--uui-size-space-4);
                border: 1px solid var(--uui-color-border);
                border-radius: var(--uui-border-radius);
                cursor: pointer;
                transition: all 0.2s;
                gap: var(--uui-size-space-3);
                align-items: flex-start;
            }

            .option-card:hover {
                border-color: var(--uui-color-focus);
            }

            .option-card.selected {
                border-color: var(--uui-color-focus);
                border-width: 2px;
            }

            .option-icon {
                margin-top: var(--uui-size-space-1);
            }

            .option-content {
                flex: 1;
                display: flex;
                flex-direction: column;
            }

            .option-label {
                font-size: var(--uui-type-small-size);
                color: var(--uui-color-text-alt);
                line-height: 1;
                opacity: 0.6;
            }

            .option-label span {
                font-weight: 600;
            }

            .option-value {
                color: var(--uui-color-text);
                white-space: pre-wrap;
                line-height: 1.5;
                margin-bottom: var(--uui-size-space-1);
            }

            .copy-container {
                position: relative;
            }

            .copy-action {
                position: absolute;
                top: var(--uui-size-space-2);
                right: var(--uui-size-space-2);
                opacity: var(--umb-block-list-entry-actions-opacity, 0);
                transition: opacity 120ms;
            }

            .copy-container:hover .copy-action {
                opacity: 1;
            }
        `,
    ];
}

export default UaiPromptPreviewModalElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-prompt-preview-modal": UaiPromptPreviewModalElement;
    }
}
