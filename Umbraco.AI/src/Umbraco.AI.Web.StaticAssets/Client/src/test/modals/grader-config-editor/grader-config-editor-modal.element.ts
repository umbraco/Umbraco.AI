import { css, html, customElement, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbModalBaseElement } from "@umbraco-cms/backoffice/modal";
import type {
    UaiGraderConfigEditorModalData,
    UaiGraderConfigEditorModalValue,
} from "./grader-config-editor-modal.token.js";
import type { UaiTestGraderConfig } from "../../types.js";
import { createEmptyGraderConfig } from "../../types.js";
import type { EditableModelSchemaModel } from "../../../api/types.gen.js";
import { TestsService } from "../../../api/sdk.gen.js";
import type { UaiModelEditorChangeEventDetail } from "../../../core/components/exports.js";

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

    #onConfigChange(e: CustomEvent<UaiModelEditorChangeEventDetail>) {
        // Model editor dispatches change event with updated config
        this._configModel = e.detail.model;
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
            <umb-body-layout headline="Configure ${this.data?.graderTypeName} Grader">
                <form id="grader-form" @submit=${this.#onSubmit}>
                    <uui-box headline="General">
                        <umb-property-layout label="Name" description="Display name for this grader">
                            <div slot="editor">
                                <uui-input
                                    id="name"
                                    type="text"
                                    .value=${this._grader.name}
                                    @input=${this.#onNameChange}
                                    placeholder="Enter grader name"
                                    required
                                ></uui-input>
                            </div>
                        </umb-property-layout>

                        <umb-property-layout
                            label="Description"
                            description="Optional description of what this grader checks"
                        >
                            <div slot="editor">
                                <uui-input
                                    id="description"
                                    type="text"
                                    .value=${this._grader.description || ""}
                                    @input=${this.#onDescriptionChange}
                                    placeholder="Enter description"
                                ></uui-input>
                            </div>
                        </umb-property-layout>

                        <umb-property-layout
                            label="Severity"
                            description="Severity level when this grader fails"
                        >
                            <div slot="editor">
                                <uui-select
                                    id="severity"
                                    .value=${this._grader.severity}
                                    .options=${[
                                        {
                                            value: "Info",
                                            name: "Info",
                                            selected: this._grader.severity === "Info",
                                        },
                                        {
                                            value: "Warning",
                                            name: "Warning",
                                            selected: this._grader.severity === "Warning",
                                        },
                                        {
                                            value: "Error",
                                            name: "Error",
                                            selected: this._grader.severity === "Error",
                                        },
                                    ]}
                                    @change=${this.#onSeverityChange}
                                >
                                </uui-select>
                            </div>
                        </umb-property-layout>

                        <umb-property-layout label="Weight" description="Scoring weight for this grader (default: 1.0)">
                            <div slot="editor">
                                <uui-input
                                    id="weight"
                                    type="number"
                                    step="0.1"
                                    min="0"
                                    .value=${this._grader.weight.toString()}
                                    @input=${this.#onWeightChange}
                                ></uui-input>
                            </div>
                        </umb-property-layout>

                        <umb-property-layout
                            label="Negate"
                            description="Invert the pass/fail result of this grader"
                        >
                            <div slot="editor">
                                <uui-toggle
                                    .checked=${this._grader.negate}
                                    @change=${this.#onNegateChange}
                                    label="Negate (invert pass/fail)"
                                >
                                </uui-toggle>
                            </div>
                        </umb-property-layout>
                    </uui-box>

                    ${this._schema
                        ? html`
                              <uai-model-editor
                                  .schema=${this._schema}
                                  .model=${this._configModel}
                                  @change=${this.#onConfigChange}
                                  default-group="#uaiFieldGroups_configLabel"
                                  style="margin-top: var(--uui-size-layout-1);"
                              >
                              </uai-model-editor>
                          `
                        : ""}
                </form>

                <div slot="actions">
                    <uui-button label="Cancel" @click=${this.#onCancel}> Cancel </uui-button>
                    <uui-button
                        type="submit"
                        form="grader-form"
                        look="primary"
                        color="positive"
                        label="Save"
                    >
                        Save
                    </uui-button>
                </div>
            </umb-body-layout>
        `;
    }

    static override styles = [
        css`
            uui-box {
                --uui-box-default-padding: 0 var(--uui-size-space-5);
            }

            uui-box:not(:first-child) {
                margin-top: var(--uui-size-layout-1);
            }

            uui-input,
            uui-select {
                width: 100%;
            }
        `,
    ];
}

export default UaiGraderConfigEditorModalElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-grader-config-editor-modal": UaiGraderConfigEditorModalElement;
    }
}
