import { css, html, customElement, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { UmbFormControlMixin } from "@umbraco-cms/backoffice/validation";
import { UAI_TEST_WORKSPACE_CONTEXT } from "../test-workspace.context-token.js";
import type { UaiTestDetailModel } from "../../../types.js";
import { UaiPartialUpdateCommand } from "../../../../core/command/implement/partial-update.command.js";
import type { UaiModelEditorChangeEventDetail } from "../../../../core";
import type { TestFeatureResponseModel } from "../../../../api";
import { UaiTestFeatureItemRepository } from "../../../repository/test-feature/test-feature-item.repository.ts";
import { UMB_NOTIFICATION_CONTEXT } from "@umbraco-cms/backoffice/notification";

@customElement("umbraco-ai-test-details-workspace-view")
export class UmbracoAITestDetailsWorkspaceViewElement extends UmbFormControlMixin(UmbLitElement) {
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

	#onDescriptionChange(event: Event) {
		const target = event.target as HTMLTextAreaElement;
		this.#workspaceContext?.handleCommand(
			new UaiPartialUpdateCommand<UaiTestDetailModel>({ description: target.value || null }, "description"),
		);
	}

	#onTargetChange(event: Event) {
		event.stopPropagation();
		const target = event.target as HTMLElement & { value?: string };
		const testTargetId = target.value || "";

		this.#workspaceContext?.handleCommand(
			new UaiPartialUpdateCommand<UaiTestDetailModel>({ testTargetId }, "testTargetId"),
		);
	}

    #onProfileChange(event: Event) {
        event.stopPropagation();
        const picker = event.target as HTMLElement & { value?: string };
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiTestDetailModel>({ profileId: picker.value || null }, "profileId"),
        );
    }

    #onContextIdsChange(event: Event) {
        event.stopPropagation();
        const picker = event.target as HTMLElement & { value?: string | string[] };
        const value = picker.value;
        const contextIds = Array.isArray(value) ? value : value ? [value] : [];
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiTestDetailModel>({ contextIds }, "contextIds"),
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

	render() {
		if (!this._model) return html`<uui-loader></uui-loader>`;

		return html`
			<uui-box headline="General">
				<umb-property-layout label="Description" description="Optional description of this test">
					<uui-textarea
						slot="editor"
						.value=${this._model.description || ""}
						@input=${this.#onDescriptionChange}
						placeholder="Enter test description (optional)"
					></uui-textarea>
				</umb-property-layout>

				<umb-property-layout label="Target" description="The entity to test" mandatory>
					<uai-test-feature-entity-picker
						slot="editor"
						.testFeatureId=${this._model.testFeatureId}
						.value=${this._model.testTargetId}
						@change=${this.#onTargetChange}
					></uai-test-feature-entity-picker>
				</umb-property-layout>
			</uui-box>

            <uui-box headline="Configuration">
                <umb-property-layout label="Profile" description="AI profile to use for test execution (optional - falls back to feature default)">
                    <div slot="editor">
                        <uai-profile-picker
                            capability="Chat"
                            .value=${this._model.profileId || undefined}
                            @change=${this.#onProfileChange}
                        ></uai-profile-picker>
                    </div>
                </umb-property-layout>

                <umb-property-layout label="Contexts" description="Override the target's contexts for this test run (optional - leave empty to use the target's own contexts)">
                    <div slot="editor">
                        <uai-context-picker
                            multiple
                            .value=${this._model.contextIds.length > 0 ? this._model.contextIds : undefined}
                            @change=${this.#onContextIdsChange}
                        ></uai-context-picker>
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

export default UmbracoAITestDetailsWorkspaceViewElement;

declare global {
	interface HTMLElementTagNameMap {
		"umbraco-ai-test-details-workspace-view": UmbracoAITestDetailsWorkspaceViewElement;
	}
}
