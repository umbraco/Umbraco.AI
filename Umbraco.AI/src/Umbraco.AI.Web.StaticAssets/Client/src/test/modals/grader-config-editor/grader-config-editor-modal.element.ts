import { html, customElement, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbModalBaseElement } from "@umbraco-cms/backoffice/modal";
import type {
    UaiGraderConfigEditorModalData,
    UaiGraderConfigEditorModalValue,
} from "./grader-config-editor-modal.token.js";
import type { UaiTestGraderConfig } from "../../types.js";
import { createEmptyGraderConfig } from "../../types.js";
import type { EditableModelSchemaModel } from "../../../api/types.gen.js";
import { TestsService } from "../../../api/sdk.gen.js";

@customElement("uai-grader-config-editor-modal")
export class UaiGraderConfigEditorModalElement extends UmbModalBaseElement<
    UaiGraderConfigEditorModalData,
    UaiGraderConfigEditorModalValue
> {
    @state()
    private _grader: UaiTestGraderConfig = createEmptyGraderConfig();

    @state()
    private _schema: EditableModelSchemaModel | null = null;

    @state()
    private _loading = true;

    @state()
    private _configModel: Record<string, unknown> = {};

    override async firstUpdated() {
        // Initialize from existing grader or create new
        if (this.data?.existingGrader) {
            this._grader = { ...this.data.existingGrader };
            this._configModel = this._grader.config ? { ...this._grader.config } : {};
        } else {
            this._grader = createEmptyGraderConfig();
            this._grader.graderTypeId = this.data?.graderTypeId || "";
            this._configModel = {};
        }

        // Fetch grader type schema
        await this.#fetchGraderSchema();
    }

    async #fetchGraderSchema() {
        if (!this.data?.graderTypeId) {
            this._loading = false;
            return;
        }

        try {
            const { data } = await TestsService.getTestGraderById({
                path: { id: this.data.graderTypeId },
            });

            this._schema = data?.configSchema || null;
        } catch (error) {
            console.error("Error fetching grader schema:", error);
        } finally {
            this._loading = false;
        }
    }

    #onNameChange(e: Event) {
        const input = e.target as HTMLInputElement;
        this._grader = { ...this._grader, name: input.value };
    }

    #onDescriptionChange(e: Event) {
        const input = e.target as HTMLInputElement;
        this._grader = { ...this._grader, description: input.value };
    }

    #onSeverityChange(e: Event) {
        const select = e.target as HTMLSelectElement;
        this._grader = {
            ...this._grader,
            severity: select.value as "Info" | "Warning" | "Error",
        };
    }

    #onWeightChange(e: Event) {
        const input = e.target as HTMLInputElement;
        const weight = parseFloat(input.value);
        if (!isNaN(weight)) {
            this._grader = { ...this._grader, weight };
        }
    }

    #onNegateChange(e: Event) {
        const input = e.target as HTMLInputElement;
        this._grader = { ...this._grader, negate: input.checked };
    }

    #onConfigChange(e: CustomEvent) {
        // Model editor dispatches change event with updated config
        this._configModel = { ...this._configModel, ...e.detail };
    }

    #onSubmit(e: Event) {
        e.preventDefault();

        // Validate required fields
        if (!this._grader.name.trim()) {
            return;
        }

        // Serialize config model to grader config
        const finalGrader: UaiTestGraderConfig = {
            ...this._grader,
            config: Object.keys(this._configModel).length > 0 ? this._configModel : undefined,
        };

        this.value = { grader: finalGrader };
        this.modalContext?.submit();
    }

    #onCancel() {
        this.modalContext?.reject();
    }

    override render() {
        if (this._loading) {
            return html`<uui-loader></uui-loader>`;
        }

        return html`
            <umb-body-layout headline=${this.data?.graderTypeName || "Configure Grader"}>
                <form id="grader-form" @submit=${this.#onSubmit}>
                    <uui-box>
                        <div slot="header">Basic Configuration</div>

                        <uui-form-layout-item>
                            <uui-label slot="label" for="name" required>Name</uui-label>
                            <uui-input
                                id="name"
                                type="text"
                                .value=${this._grader.name}
                                @input=${this.#onNameChange}
                                required
                            ></uui-input>
                        </uui-form-layout-item>

                        <uui-form-layout-item>
                            <uui-label slot="label" for="description">Description</uui-label>
                            <uui-input
                                id="description"
                                type="text"
                                .value=${this._grader.description || ""}
                                @input=${this.#onDescriptionChange}
                            ></uui-input>
                        </uui-form-layout-item>

                        <uui-form-layout-item>
                            <uui-label slot="label" for="severity">Severity</uui-label>
                            <uui-select
                                id="severity"
                                .value=${this._grader.severity}
                                @change=${this.#onSeverityChange}
                            >
                                <uui-select-option value="Info">Info</uui-select-option>
                                <uui-select-option value="Warning">Warning</uui-select-option>
                                <uui-select-option value="Error">Error</uui-select-option>
                            </uui-select>
                        </uui-form-layout-item>

                        <uui-form-layout-item>
                            <uui-label slot="label" for="weight">Weight</uui-label>
                            <uui-input
                                id="weight"
                                type="number"
                                step="0.1"
                                min="0"
                                .value=${this._grader.weight.toString()}
                                @input=${this.#onWeightChange}
                            ></uui-input>
                        </uui-form-layout-item>

                        <uui-form-layout-item>
                            <uui-label slot="label">
                                <uui-checkbox
                                    .checked=${this._grader.negate}
                                    @change=${this.#onNegateChange}
                                >
                                    Negate (invert pass/fail)
                                </uui-checkbox>
                            </uui-label>
                        </uui-form-layout-item>
                    </uui-box>

                    ${this._schema
                        ? html`
                              <uui-box>
                                  <div slot="header">Grader-Specific Configuration</div>
                                  <uai-model-editor
                                      .schema=${this._schema}
                                      .model=${this._configModel}
                                      @change=${this.#onConfigChange}
                                  ></uai-model-editor>
                              </uui-box>
                          `
                        : ""}
                </form>

                <div slot="actions">
                    <uui-button label="Cancel" @click=${this.#onCancel}></uui-button>
                    <uui-button
                        type="submit"
                        form="grader-form"
                        look="primary"
                        color="positive"
                        label="Save"
                    ></uui-button>
                </div>
            </umb-body-layout>
        `;
    }
}

export default UaiGraderConfigEditorModalElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-grader-config-editor-modal": UaiGraderConfigEditorModalElement;
    }
}
