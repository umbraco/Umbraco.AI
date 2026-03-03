import { css, html, customElement, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { UMB_NOTIFICATION_CONTEXT } from "@umbraco-cms/backoffice/notification";
import { UmbFormControlMixin } from "@umbraco-cms/backoffice/validation";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import { UAI_TEST_WORKSPACE_CONTEXT } from "../test-workspace.context-token.js";
import type { TestFeatureResponseModel } from "../../../../api/types.gen.js";
import type { UaiTestDetailModel, UaiTestVariation } from "../../../types.js";
import { UaiPartialUpdateCommand } from "../../../../core/command/implement/partial-update.command.js";
import { UaiTestFeatureItemRepository } from "../../../repository/test-feature/test-feature-item.repository.js";
import type { UaiModelEditorChangeEventDetail } from "../../../../core/components/exports.js";

@customElement("umbraco-ai-test-configuration-workspace-view")
export class UmbracoAITestConfigurationWorkspaceViewElement extends UmbFormControlMixin(UmbLitElement) {
    #workspaceContext?: typeof UAI_TEST_WORKSPACE_CONTEXT.TYPE;
    #notificationContext?: typeof UMB_NOTIFICATION_CONTEXT.TYPE;
    #repository!: UaiTestFeatureItemRepository;

    @state()
    private _model?: UaiTestDetailModel;

    @state()
    private _testFeature?: TestFeatureResponseModel | null;

    constructor() {
        super();

        this.#repository = new UaiTestFeatureItemRepository(this);

        this.consumeContext(UAI_TEST_WORKSPACE_CONTEXT, (context) => {
            if (!context) return;
            this.#workspaceContext = context;
            this.observe(context.model, (model) => {
                const testFeatureChanged = model?.testFeatureId && model.testFeatureId !== this._model?.testFeatureId;

                this._model = model;

                if (testFeatureChanged) {
                    this.#loadTestFeatureDetails(model!.testFeatureId);
                }
            });
        });

        this.consumeContext(UMB_NOTIFICATION_CONTEXT, (context) => {
            this.#notificationContext = context;
        });
    }

    async #loadTestFeatureDetails(testFeatureId: string) {
        const { data, error } = await this.#repository.requestById(testFeatureId);
        if (error) {
            console.error("Failed to load test feature details:", error);
            this._testFeature = null;
            this.#notificationContext?.peek("danger", {
                data: { message: "Failed to load test feature details" },
            });
        } else {
            this._testFeature = data ?? null;
        }
    }

    #onProfileChange(event: Event) {
        event.stopPropagation();
        const picker = event.target as HTMLElement & { value?: string };
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiTestDetailModel>({ profileId: picker.value || null }, "profileId"),
        );
    }

    #onRunCountChange(event: Event) {
        const target = event.target as HTMLInputElement;
        const runCount = parseInt(target.value) || 1;
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiTestDetailModel>({ runCount }, "runCount"),
        );
    }

    #onTestFeatureConfigChange(e: CustomEvent<UaiModelEditorChangeEventDetail>) {
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiTestDetailModel>({ testFeatureConfig: e.detail.model }, "testFeatureConfig"),
        );
    }

    #onVariationsChange(e: UmbChangeEvent) {
        e.stopPropagation();
        const builder = e.target as HTMLElement & { variations?: UaiTestVariation[] };
        const variations = builder.variations ?? [];
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiTestDetailModel>({ variations }, "variations"),
        );
    }

    render() {
        if (!this._model) return html`<uui-loader></uui-loader>`;

        return html`
            <uui-box headline="Default Configuration">
                <umb-property-layout label="Profile" description="AI profile to use for test execution (optional - falls back to feature default)">
                    <div slot="editor">
                        <uai-profile-picker
                            capability="Chat"
                            .value=${this._model.profileId || undefined}
                            @change=${this.#onProfileChange}
                        ></uai-profile-picker>
                    </div>
                </umb-property-layout>

                <umb-property-layout label="Run Count" description="Number of times to run this test (for pass@k calculation)">
                    <uui-input
                        slot="editor"
                        type="number"
                        .value=${this._model.runCount}
                        @input=${this.#onRunCountChange}
                        min="1"
                        max="100"
                    ></uui-input>
                </umb-property-layout>
            </uui-box>

            <uai-model-editor
                .schema=${this._testFeature?.testFeatureConfigSchema}
                .model=${this._model.testFeatureConfig}
                empty-message="This test feature has no configurable parameters."
                @change=${this.#onTestFeatureConfigChange}
                default-group="#uaiFieldGroups_configLabel"
            ></uai-model-editor>

            <uui-box headline="Variations" class="variations-box">
                <p class="variations-description">
                    Variations allow you to test different configurations side-by-side.
                    Each variation can override the profile, run count, or feature config above.
                </p>
                <uai-variation-config-builder
                    .variations=${this._model.variations}
                    .testFeatureId=${this._model.testFeatureId}
                    @change=${this.#onVariationsChange}
                ></uai-variation-config-builder>
            </uui-box>
        `;
    }

    static styles = [
        UmbTextStyles,
        css`
            :host {
                display: block;
                padding: var(--uui-size-layout-1);
            }

            uui-box {
                --uui-box-default-padding: 0 var(--uui-size-space-5);
            }

            uui-box:not(:first-child) {
                margin-top: var(--uui-size-layout-1);
            }

            uai-model-editor {
                margin-top: var(--uui-size-layout-1);
            }

            .variations-box {
                margin-top: var(--uui-size-layout-1);
            }

            .variations-description {
                font-size: 0.85em;
                color: var(--uui-color-text-alt);
                margin: var(--uui-size-space-3) 0;
                padding: 0 var(--uui-size-space-5);
                line-height: 1.5;
            }

            uui-loader {
                display: block;
                margin: auto;
                position: absolute;
                top: 50%;
                left: 50%;
                transform: translate(-50%, -50%);
            }

            uui-select,
            uui-input,
            uui-textarea {
                width: 100%;
            }
        `,
    ];
}

export default UmbracoAITestConfigurationWorkspaceViewElement;

declare global {
    interface HTMLElementTagNameMap {
        "umbraco-ai-test-configuration-workspace-view": UmbracoAITestConfigurationWorkspaceViewElement;
    }
}
