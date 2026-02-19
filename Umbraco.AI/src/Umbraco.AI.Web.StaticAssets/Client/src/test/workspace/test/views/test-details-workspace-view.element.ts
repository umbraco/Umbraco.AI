import { css, html, customElement, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { UMB_NOTIFICATION_CONTEXT } from "@umbraco-cms/backoffice/notification";
import { UmbFormControlMixin } from "@umbraco-cms/backoffice/validation";
import { UAI_TEST_WORKSPACE_CONTEXT } from "../test-workspace.context-token.js";
import type { TestGraderModel, TestFeatureResponseModel } from "../../../../api/types.gen.js";
import type { UaiTestDetailModel, UaiTestGraderConfig } from "../../../types.js";
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

	#onTestCaseChange(e: CustomEvent<UaiModelEditorChangeEventDetail>) {
		this.#workspaceContext?.handleCommand(
			new UaiPartialUpdateCommand<UaiTestDetailModel>({ testCase: e.detail.model }, "testCase"),
		);
	}

	#onRunCountChange(event: Event) {
		const target = event.target as HTMLInputElement;
		const runCount = parseInt(target.value) || 1;
		this.#workspaceContext?.handleCommand(
			new UaiPartialUpdateCommand<UaiTestDetailModel>({ runCount }, "runCount"),
		);
	}

	#onTagsChange(event: CustomEvent) {
		const target = event.target as any;
		const tags = target.items as string[];
		this.#workspaceContext?.handleCommand(
			new UaiPartialUpdateCommand<UaiTestDetailModel>({ tags }, "tags"),
		);
	}

	#onGradersChange(event: Event) {
		const target = event.target as any;
		const graderConfigs = target.graders as UaiTestGraderConfig[];

		// Convert UaiTestGraderConfig[] to TestGraderModel[]
		const graderModels: TestGraderModel[] = graderConfigs.map(config => ({
			id: config.id,
			graderTypeId: config.graderTypeId,
			name: config.name,
			description: config.description || null,
			configJson: config.config ? JSON.stringify(config.config) : "{}",
			negate: config.negate,
			severity: config.severity,
			weight: config.weight,
		}));

		this.#workspaceContext?.handleCommand(
			new UaiPartialUpdateCommand<UaiTestDetailModel>({ graders: graderModels }, "graders"),
		);
	}

	#getGraderConfigs(): UaiTestGraderConfig[] {
		if (!this._model) return [];

		// Convert TestGraderModel[] to UaiTestGraderConfig[]
		return this._model.graders.map(grader => {
			let config: Record<string, unknown> | undefined = undefined;

			// Parse configJson if it exists and is valid
			if (grader.configJson) {
				try {
					const parsed = JSON.parse(grader.configJson);
					config = Object.keys(parsed).length > 0 ? parsed : undefined;
				} catch {
					// Invalid JSON, leave as undefined
				}
			}

			return {
				id: grader.id,
				graderTypeId: grader.graderTypeId,
				name: grader.name,
				description: grader.description || undefined,
				config,
				negate: grader.negate,
				severity: grader.severity as "Info" | "Warning" | "Error",
				weight: grader.weight,
			};
		});
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
                .schema=${this._testFeature?.testCaseSchema}
                .model=${this._model.testCase}
                empty-message="This test feature has no configurable test case parameters."
                @change=${this.#onTestCaseChange}
                default-group="#uaiFieldGroups_configLabel"
            ></uai-model-editor>

			<uui-box headline="Tags">
				<umb-property-layout label="Tags" description="Tags to categorize this test">
					<uai-tags-input
						slot="editor"
						.items=${this._model.tags}
						@change=${this.#onTagsChange}
						placeholder="Add tag"
					></uai-tags-input>
				</umb-property-layout>
			</uui-box>

			<uui-box headline="Graders">
				<umb-property-layout
					label="Graders"
					description="Configure graders to validate test outputs">
					<uai-grader-config-builder
						slot="editor"
						.graders=${this.#getGraderConfigs()}
						@change=${this.#onGradersChange}
					></uai-grader-config-builder>
				</umb-property-layout>
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
