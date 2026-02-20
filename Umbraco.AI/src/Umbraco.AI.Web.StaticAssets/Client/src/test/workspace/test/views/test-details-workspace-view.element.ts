import { css, html, customElement, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { UMB_NOTIFICATION_CONTEXT } from "@umbraco-cms/backoffice/notification";
import { UmbFormControlMixin } from "@umbraco-cms/backoffice/validation";
import { UAI_TEST_WORKSPACE_CONTEXT } from "../test-workspace.context-token.js";
import type { TestFeatureResponseModel } from "../../../../api/types.gen.js";
import type { UaiTestDetailModel } from "../../../types.js";
import { UaiPartialUpdateCommand } from "../../../../core/command/implement/partial-update.command.js";
import { AITestRepository } from "../../../repository/test.repository.js";
import type { UaiModelEditorChangeEventDetail } from "../../../../core/components/exports.js";

@customElement("umbraco-ai-test-details-workspace-view")
export class UmbracoAITestDetailsWorkspaceViewElement extends UmbFormControlMixin(UmbLitElement) {
	#workspaceContext?: typeof UAI_TEST_WORKSPACE_CONTEXT.TYPE;
	#notificationContext?: typeof UMB_NOTIFICATION_CONTEXT.TYPE;
	#repository!: AITestRepository;

	@state()
	private _model?: UaiTestDetailModel;

	@state()
	private _testFeature?: TestFeatureResponseModel | null;

	constructor() {
		super();

		this.#repository = new AITestRepository(this);

		this.consumeContext(UAI_TEST_WORKSPACE_CONTEXT, (context) => {
			if (!context) return;
			this.#workspaceContext = context;
			this.observe(context.model, (model) => {
				// Only load test feature details when testFeatureId changes, not on every model update.
				// This prevents unnecessary re-renders that cause cursor jumping in form inputs.
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
		try {
			this._testFeature = await this.#repository.getTestFeatureById(testFeatureId);
		} catch (error) {
			console.error("Failed to load test feature details:", error);
			this._testFeature = null;
			this.#notificationContext?.peek("danger", {
				data: { message: "Failed to load test feature details" },
			});
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

	#onTestFeatureConfigChange(e: CustomEvent<UaiModelEditorChangeEventDetail>) {
		this.#workspaceContext?.handleCommand(
			new UaiPartialUpdateCommand<UaiTestDetailModel>({ testFeatureConfig: e.detail.model }, "testFeatureConfig"),
		);
	}

	#onRunCountChange(event: Event) {
		const target = event.target as HTMLInputElement;
		const runCount = parseInt(target.value) || 1;
		this.#workspaceContext?.handleCommand(
			new UaiPartialUpdateCommand<UaiTestDetailModel>({ runCount }, "runCount"),
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
                slot="editor"
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

			.target-input {
				display: flex;
				gap: var(--uui-size-space-3);
				align-items: center;
			}

			.target-input uui-input {
				flex: 1;
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
