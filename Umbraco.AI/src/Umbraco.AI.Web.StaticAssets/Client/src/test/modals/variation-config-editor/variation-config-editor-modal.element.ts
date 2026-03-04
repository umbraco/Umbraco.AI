import { css, html, customElement, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbModalBaseElement } from "@umbraco-cms/backoffice/modal";
import { UMB_NOTIFICATION_CONTEXT } from "@umbraco-cms/backoffice/notification";
import type {
    UaiVariationConfigEditorModalData,
    UaiVariationConfigEditorModalValue,
} from "./variation-config-editor-modal.token.js";
import type { UaiTestVariation } from "../../types.js";
import { createEmptyVariation } from "../../types.js";
import type { EditableModelSchemaModel, TestFeatureResponseModel } from "../../../api/types.gen.js";
import { UaiTestFeatureItemRepository } from "../../repository/test-feature/test-feature-item.repository.js";
import type { UaiModelEditorChangeEventDetail } from "../../../core/components/exports.js";

@customElement("uai-variation-config-editor-modal")
export class UaiVariationConfigEditorModalElement extends UmbModalBaseElement<
    UaiVariationConfigEditorModalData,
    UaiVariationConfigEditorModalValue
> {
    @state()
    private _variation: UaiTestVariation = createEmptyVariation();

    @state()
    private _featureConfigSchema: EditableModelSchemaModel | null = null;

    @state()
    private _loading = true;

    @state()
    private _configModel: Record<string, unknown> = {};

    override async firstUpdated() {
        if (this.data?.existingVariation) {
            this._variation = { ...this.data.existingVariation };
            this._configModel = this._variation.testFeatureConfig ? { ...this._variation.testFeatureConfig } : {};
        } else {
            this._variation = createEmptyVariation();
            this._configModel = {};
        }

        await this.#fetchFeatureSchema();
    }

    async #fetchFeatureSchema() {
        if (!this.data?.testFeatureId) {
            this._loading = false;
            return;
        }

        const repository = new UaiTestFeatureItemRepository(this);
        const { data, error } = await repository.requestById(this.data.testFeatureId);

        if (error) {
            console.error("Error fetching feature schema:", error);
            const notificationContext = await this.getContext(UMB_NOTIFICATION_CONTEXT).catch(() => undefined);
            notificationContext?.peek("danger", {
                data: { message: "Failed to load feature configuration schema" },
            });
        } else {
            this._featureConfigSchema = (data as TestFeatureResponseModel)?.testFeatureConfigSchema || null;
        }

        this._loading = false;
    }

    #onNameChange(e: Event) {
        const input = e.target as HTMLInputElement;
        this._variation = { ...this._variation, name: input.value };
    }

    #onDescriptionChange(e: Event) {
        const input = e.target as HTMLInputElement;
        this._variation = { ...this._variation, description: input.value || null };
    }

    #onProfileChange(e: Event) {
        e.stopPropagation();
        const picker = e.target as HTMLElement & { value?: string };
        this._variation = { ...this._variation, profileId: picker.value || null };
    }

    #onContextIdsChange(e: Event) {
        e.stopPropagation();
        const picker = e.target as HTMLElement & { value?: string | string[] };
        const value = picker.value;
        const contextIds = Array.isArray(value) ? value : value ? [value] : null;
        this._variation = { ...this._variation, contextIds: contextIds?.length ? contextIds : null };
    }

    #onRunCountChange(e: Event) {
        const input = e.target as HTMLInputElement;
        const value = input.value.trim();
        this._variation = { ...this._variation, runCount: value ? parseInt(value) || null : null };
    }

    #onConfigChange(e: CustomEvent<UaiModelEditorChangeEventDetail>) {
        this._configModel = e.detail.model;
    }

    #onSubmit(e: Event) {
        e.preventDefault();

        if (!this._variation.name.trim()) {
            return;
        }

        const finalVariation: UaiTestVariation = {
            ...this._variation,
            testFeatureConfig: Object.keys(this._configModel).length > 0 ? this._configModel : null,
        };

        this.value = { variation: finalVariation };
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
            <umb-body-layout headline=${this.data?.existingVariation ? "Edit Variation" : "Add Variation"}>
                <form id="variation-form" @submit=${this.#onSubmit}>
                    <uui-box headline="General">
                        <umb-property-layout label="Name" description="A descriptive name for this variation" mandatory>
                            <div slot="editor">
                                <uui-input
                                    id="name"
                                    type="text"
                                    .value=${this._variation.name}
                                    @input=${this.#onNameChange}
                                    placeholder="e.g., GPT-4 Turbo, Claude Sonnet"
                                    required
                                ></uui-input>
                            </div>
                        </umb-property-layout>

                        <umb-property-layout label="Description" description="Optional description of what this variation tests">
                            <div slot="editor">
                                <uui-input
                                    id="description"
                                    type="text"
                                    .value=${this._variation.description || ""}
                                    @input=${this.#onDescriptionChange}
                                    placeholder="Enter description"
                                ></uui-input>
                            </div>
                        </umb-property-layout>
                    </uui-box>

                    <uui-box headline="Overrides">
                        <p class="override-hint">Leave fields empty to inherit from the test's default configuration.</p>

                        <umb-property-layout label="Profile" description="Override the AI profile for this variation">
                            <div slot="editor">
                                <uai-profile-picker
                                    capability="Chat"
                                    .value=${this._variation.profileId || undefined}
                                    @change=${this.#onProfileChange}
                                ></uai-profile-picker>
                            </div>
                        </umb-property-layout>

                        <umb-property-layout label="Contexts" description="Override the knowledge contexts for this variation">
                            <div slot="editor">
                                <uai-context-picker
                                    multiple
                                    .value=${this._variation.contextIds?.length ? this._variation.contextIds : undefined}
                                    @change=${this.#onContextIdsChange}
                                ></uai-context-picker>
                            </div>
                        </umb-property-layout>

                        <umb-property-layout label="Run Count" description="Override the number of runs for this variation">
                            <div slot="editor">
                                <uui-input
                                    id="runCount"
                                    type="number"
                                    .value=${this._variation.runCount?.toString() ?? ""}
                                    @input=${this.#onRunCountChange}
                                    min="1"
                                    max="100"
                                    placeholder="Inherit from test"
                                ></uui-input>
                            </div>
                        </umb-property-layout>
                    </uui-box>

                    ${this._featureConfigSchema
                        ? html`
                              <uai-model-editor
                                  .schema=${this._featureConfigSchema}
                                  .model=${this._configModel}
                                  @change=${this.#onConfigChange}
                                  empty-message="This test feature has no configurable parameters."
                                  default-group="Feature Config Overrides"
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
                        form="variation-form"
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

            .override-hint {
                font-size: 0.85em;
                color: var(--uui-color-text-alt);
                margin: var(--uui-size-space-3) 0;
                padding: 0 var(--uui-size-space-5);
            }
        `,
    ];
}

export default UaiVariationConfigEditorModalElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-variation-config-editor-modal": UaiVariationConfigEditorModalElement;
    }
}
