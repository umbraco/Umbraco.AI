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

    async #onCopy() {
        if (!this._response) return;

        try {
            await navigator.clipboard.writeText(this._response);
            this._copied = true;

            // Show notification
            const notificationContext = await this.getContext(UMB_NOTIFICATION_CONTEXT);
            notificationContext?.peek("positive", {
                data: { message: "Response copied to clipboard" },
            });

            // Reset copied state after 2 seconds
            setTimeout(() => {
                this._copied = false;
            }, 2000);
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

    #renderMultipleOptions() {
        if (!this._resultOptions?.length) return nothing;

        return html`
            <div class="options-container">
                ${this._resultOptions.map(
                    (option, index) => html`
                        <div
                            class="option-card ${this._selectedOptionIndex === index ? "selected" : ""}"
                            @click=${() => this.#onOptionSelect(index)}
                        >
                            <uui-radio-button
                                name="result-option"
                                .checked=${this._selectedOptionIndex === index}
                            ></uui-radio-button>
                            <div class="option-content">
                                <div class="option-label">${option.label}</div>
                                ${option.description
                                    ? html`<div class="option-description">${option.description}</div>`
                                    : nothing}
                                <div class="option-value">${option.displayValue}</div>
                            </div>
                        </div>
                    `,
                )}
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
                    <uui-loader-bar></uui-loader-bar>
                </div>
            `;
        }

        if (this._response) {
            return html`
                <uui-scroll-container class="response-container ${this._resultOptions && this._resultOptions.length > 1 ? "multiple" : ""}">
                    ${this._resultOptions && this._resultOptions.length > 1
                        ? this.#renderMultipleOptions()
                        : html`<div class="response-content">${this._response}</div>`}
                </uui-scroll-container>
            `;
        }

        return nothing;
    }

    override render() {
        return html`
            <umb-body-layout>
                <div slot="header" class="response-header">
                    <h3 class="headline">${this.data?.promptName ?? "Prompt Preview"}</h3>
                    ${this.data?.promptDescription
                        ? html`<p class="description">${this.data.promptDescription}</p>`
                        : nothing}
                </div>
                ${this._response && !this._loading
                    ? html`
                      <uui-button class="regenerate-btn" slot="navigation" label="Regenerate" look="default" compact @click=${this.#onRetry}>
                          <uui-icon name="icon-sync"></uui-icon>
                          Regenerate
                      </uui-button>`
                    : nothing}
                <div id="content">
                    ${this.#renderResponse()}
                </div>

                <div slot="actions">
                    <uui-button label="Cancel" @click=${this.#onCancel}> Cancel </uui-button>
                    <uui-button
                        label=${this._copied ? "Copied!" : "Copy Response"}
                        look="secondary"
                        ?disabled=${!this._response || this._loading}
                        @click=${this.#onCopy}
                    >
                        <uui-icon name=${this._copied ? "icon-check" : "icon-clipboard"}></uui-icon>
                        ${this._copied ? "Copied!" : "Copy Response"}
                    </uui-button>
                    ${this._resultOptions &&
                    this._resultOptions.length > 0 &&
                    this._resultOptions.some((opt) => opt.valueChange !== null && opt.valueChange !== undefined)
                        ? html`
                              <uui-button
                                  label="Insert Response"
                                  look="primary"
                                  ?disabled=${!this._response ||
                                  this._loading ||
                                  this._selectedOptionIndex === undefined ||
                                  !this._resultOptions[this._selectedOptionIndex]?.valueChange}
                                  @click=${this.#onInsert}
                              >
                                  <uui-icon name="icon-enter"></uui-icon>
                                  Insert Response
                              </uui-button>
                          `
                        : nothing}
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

            .regenerate-btn {
                margin-right: var(--uui-size-space-6);
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
                min-height: 200px;
                border: 1px solid var(--uui-color-border);
                border-radius: var(--uui-border-radius);
                background: var(--uui-color-surface-alt);
                overflow: auto;
            }

            .response-container.multiple {
               border: 0;
            }

            .response-content {
                margin: 0;
                white-space: pre-wrap;
                word-break: break-word;
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
                background: var(--uui-color-danger-emphasis);
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
                gap: var(--uui-size-space-3);
                padding: var(--uui-size-space-4);
                border: 1px solid var(--uui-color-border);
                border-radius: var(--uui-border-radius);
                cursor: pointer;
                transition: all 0.2s;
            }

            .option-card:hover {
                background: var(--uui-color-surface-alt);
                border-color: var(--uui-color-focus);
            }

            .option-card.selected {
                background: #fff;
                border-color: var(--uui-color-focus);
                border-width: 2px;
            }

            .option-content {
                flex: 1;
                display: flex;
                flex-direction: column;
            }

            .option-label {
                font-weight: 600;
                color: var(--uui-color-text);
            }

            .option-value {
                color: var(--uui-color-text);
                white-space: pre-wrap;
                line-height: 1.5;
                margin-top: var(--uui-size-space-4);
            }

            .option-description {
                font-size: var(--uui-type-small-size);
                color: var(--uui-color-text-alt);
                font-style: italic;
                line-height: 1;
                opacity: 0.6;
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
